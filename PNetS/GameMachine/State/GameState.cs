using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using PNet;

namespace PNetS
{
    /// <summary>
    /// current state of the gamemachine
    /// </summary>
    public static class GameState
    {
        static double _frameTime = 0.020d;
        private const int LOOP_TIGHTNESS = 5;
        static readonly IntDictionary<GameObject> GameObjects = new IntDictionary<GameObject>(256);
        static List<Action> _starteds = new List<Action>(8);
        private static Thread _createdThread;
        private static readonly Queue<Action> InvokeQueue = new Queue<Action>();
        private static readonly object InvokeLocker = new object();

        internal static event Action RoomUpdates;
        internal static event Action DestroyDelays;
        internal static void AddStart(Action startMethod)
        {
            _starteds.Add(startMethod);
        }
        static readonly Stopwatch Watch = new Stopwatch();

        internal static void Quit() { _quit = true; }
        static bool _quit = true;
               

        static GameState()
        {
        }

        /// <summary>
        /// whether or not the current thread is the same thread as what the game state is running on
        /// </summary>
        public static bool InvokeRequired
        {
            get { return Thread.CurrentThread != _createdThread; }
        }

        /// <summary>
        /// Run an action on the gamestate's thread, next update
        /// </summary>
        /// <param name="action"></param>
        public static void Invoke(Action action)
        {
            lock (InvokeLocker)
            {
                InvokeQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Invoke the specified action if it needs to be invoked. Otherwise, run it now.
        /// </summary>
        /// <param name="action"></param>
        public static void InvokeIfRequired(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else 
                action();
        }

        internal static void RemoveObject(GameObject gobj)
        {
            //
            if (gobj.Id == -1)
            {
                Debug.LogError("Attempted to remove a gameobject with an Id of -1. This happens only if the gameobject has not fully registered with the gamestate before being destroyed. This should never happen. Throwing.");
                throw new Exception("Attempted to remove a gameobject with an Id of -1. This would be due to a gameobject not fully registering itself with the gamestate before being destroyed. This should never happen");
            }
            GameObjects.Remove(gobj.Id);
        }

        /// <summary>
        /// start the game states with the specified time between frames
        /// </summary>
        /// <param name="frameTime"></param>
        public static void Start(double frameTime = 0.02d)
        {
            if (!_quit) return;

            _frameTime = frameTime;
            Watch.Start();
            _createdThread = Thread.CurrentThread;

            _quit = false;

            while (!_quit)
            {
                if (Watch.Elapsed.TotalSeconds - TimeSinceStartup > _frameTime)
                    Update();
                Thread.Sleep(LOOP_TIGHTNESS);
            }
        }

        internal static GameObject CreateGameObject(SlimMath.Vector3 position, SlimMath.Quaternion rotation)
        {
            var gameObject = new GameObject {Position = position, Rotation = rotation};

            return gameObject;
        }

        internal static int AddGameObject(GameObject newObject)
        {
            var newId = GameObjects.Add(newObject);
            return newId;
        }

        static void Update()
        {
            PreviousFrameTime = TimeSinceStartup;
            TimeSinceStartup = Watch.Elapsed.TotalSeconds;
            NetFrameTime = Lidgren.Network.NetTime.Now;

            Action[] invokes;
            lock (InvokeLocker)
            {
                invokes = InvokeQueue.ToArray();
                InvokeQueue.Clear();
            }

// ReSharper disable ForCanBeConvertedToForeach - speed
            for (int i = 0; i < invokes.Length; i++)
// ReSharper restore ForCanBeConvertedToForeach
            {
                var invoke = invokes[i];
                try
                {
                    invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("[Invoke] {0}", e.ToString());
                }
            }

            if (_starteds.Count > 0)
            {
                List<Action> startsToRun = _starteds;
                _starteds = new List<Action>();
                startsToRun.ForEach(a => 
                    {
                        try { a(); }
                        catch (Exception e)
                        {
                            Debug.LogError("[Start] {0}", e.ToString());
                        }
                    });
            }

            for (int i = 0; i < GameObjects.Capacity; i++)
            {
                GameObject get;
                if (!GameObjects.TryGetValue(i, out get)) continue;
                try
                {
                    get.Update();
                }
                catch (Exception e)
                {
                    Debug.LogError("[Update Loop] {0}", e.ToString());
                }
            }

            if (RoomUpdates != null)
                RoomUpdates();

            try { update(); }
            catch (Exception e)
            {
                Debug.LogError("[Server Update] {0}", e.ToString());
            }

            LoopRoutines();

            for (int i = 0; i < GameObjects.Capacity; i++ )
            {
                GameObject get;
                if (!GameObjects.TryGetValue(i, out get)) continue;
                try
                {
                    get.LateUpdate();
                }
                catch(Exception e)
                {
                    Debug.LogError("[Late Update] {0}", e.ToString());
                }
            }

            if (DestroyDelays == null) return;
            try
            {
                var frameDestructions = DestroyDelays;
                DestroyDelays = null;
                frameDestructions();
            }
            catch(Exception e)
            {
                Debug.LogError("[Destruction] {0}", e.ToString());
            }
        }

        /// <summary>
        /// static update
        /// </summary>
        public static Action update = delegate { };
        /// <summary>
        /// static late update
        /// </summary>
        public static Action lateUpdate = delegate { };

        private static readonly List<IEnumerator<YieldInstruction>> Routines = new List<IEnumerator<YieldInstruction>>();
        private static readonly List<IEnumerator<YieldInstruction>> FrameRoutineAdds = new List<IEnumerator<YieldInstruction>>();

        internal static void AddRoutine(IEnumerator<YieldInstruction> toAdd)
        {
            FrameRoutineAdds.Add(toAdd);
        }

        private static int _removeOffsetting = 0;

        internal static void RemoveRoutine(IEnumerator<YieldInstruction> toRemove)
        {
            var ind = Routines.FindIndex(c => object.ReferenceEquals(c, toRemove));

            if (ind != -1)
            {
                Routines.RemoveAt(ind);
                _removeOffsetting += 1;
            }
        }

        internal static void LoopRoutines()
        {
            var toRemove = new List<int>(8);

            for (var i = Routines.Count - 1; i >= 0; i--)
            {
                //used for keeping track of routine removes during the routine
                _removeOffsetting = 0;
                var yield = Routines[i].Current;
                var remaining = false;

                try
                {

                    if (yield != null)
                    {
                        remaining = !yield.IsDone || Routines[i].MoveNext();
                    }
                    else
                    {
                        //haven't started...
                        remaining = Routines[i].MoveNext();
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError("[Yield] {0}", e.ToString());
                }


                i -= _removeOffsetting;
                if (!remaining)
                {
                    //remove it
                    Routines.RemoveAt(i);
                }
            }
            
            //add new routines
            if (FrameRoutineAdds.Count <= 0) return;
            Routines.AddRange(FrameRoutineAdds);
            FrameRoutineAdds.Clear();
        }

        internal static double PreviousFrameTime { get; private set; }
        internal static double TimeSinceStartup { get; private set; }
        internal static double NetFrameTime { get; private set; }
    }
}
