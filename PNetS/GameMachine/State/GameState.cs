using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace PNetS
{
    /// <summary>
    /// current state of the gamemachine
    /// </summary>
    public static class GameState
    {
        static double _frameTime = 0.020d;
        private const int LOOP_TIGHTNESS = 5;
        static readonly Dictionary<int, GameObject> Objects = new Dictionary<int, GameObject>(256);
        static readonly Stack<int> UseableIds = new Stack<int>(32);
        static List<Action> _starteds = new List<Action>(8);
        internal static event Action RoomUpdates;
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

        internal static void RemoveObject(GameObject gobj)
        {
            Objects.Remove(gobj.Id);
            UseableIds.Push(gobj.Id);
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
            var newId = UseableIds.Count > 0 ? UseableIds.Pop() : Objects.Count;
            Objects.Add(newId, newObject);
            return newId;
        }

        static void Update()
        {
            PreviousFrameTime = TimeSinceStartup;
            TimeSinceStartup = Watch.Elapsed.TotalSeconds;

            if (_starteds.Count > 0)
            {
                List<Action> startsToRun = _starteds;
                _starteds = new List<Action>();
                startsToRun.ForEach(a => 
                    {
                        try { a(); }
                        catch (Exception e)
                        {
                            Debug.LogError("[Start] ", e.ToString());
                        }
                    });
            }

            foreach (var o in Objects.Values)
            {
                try
                {
                    o.Update();
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

            foreach (var o in Objects.Values)
            {
                try { o.LateUpdate(); }
                catch (Exception e)
                {
                    Debug.LogError("[Late Update] {0}", e.ToString());
                }
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

        internal static void RemoveRoutine(IEnumerator<YieldInstruction> toRemove)
        {
            var ind = Routines.FindIndex(c => object.ReferenceEquals(c, toRemove));

            if (ind != -1)
            {
                Routines.RemoveAt(ind);
            }
        }

        internal static void LoopRoutines()
        {
            var toRemove = new List<int>(8);
            
            for (var i = Routines.Count - 1; i >= 0; i--)
            {
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
    }
}
