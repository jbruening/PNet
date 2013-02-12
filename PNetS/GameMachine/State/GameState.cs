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
        static int loopTightness = 5;
        static List<GameObject> objects = new List<GameObject>(256);
        static List<Action> _starteds = new List<Action>(8);
        internal static event Action RoomUpdates;
        internal static void AddStart(Action startMethod)
        {
            _starteds.Add(startMethod);
        }
        static Stopwatch watch = new Stopwatch();

        internal static void Quit() { quit = true; }
        static bool quit = true;
               

        static GameState()
        {
        }

        internal static void RemoveObject(GameObject gobj)
        {
            objects.RemoveAt(gobj.Id);
        }

        /// <summary>
        /// start the game states with the specified time between frames
        /// </summary>
        /// <param name="frameTime"></param>
        public static void Start(double frameTime = 0.02d)
        {
            if (!quit) return;

            _frameTime = frameTime;
            watch.Start();

            quit = false;

            while (!quit)
            {
                if (watch.Elapsed.TotalSeconds - TimeSinceStartup > _frameTime)
                    Update();
                Thread.Sleep(loopTightness);
            }
        }

        internal static GameObject CreateGameObject(SlimMath.Vector3 position, SlimMath.Quaternion rotation)
        {
            var gameObject = new GameObject();
            gameObject.Position = position;
            gameObject.Rotation = rotation;

            return gameObject;
        }

        internal static int AddGameObject(GameObject newObject)
        {
            var nInd = objects.Count;
            objects.Add(newObject);
            return nInd;
        }



        static void Update()
        {
            PreviousFrameTime = TimeSinceStartup;
            TimeSinceStartup = watch.Elapsed.TotalSeconds;

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

            for (var i = 0; i < objects.Count; ++i )
            {
                try { objects[i].Update(); }
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

            objects.ForEach(o =>
                {
                    try { o.LateUpdate(); }
                    catch (Exception e)
                    {
                        Debug.LogError("[Late Update] {0}", e.ToString());
                    }
                });
        }

        /// <summary>
        /// static update
        /// </summary>
        public static Action update = delegate { };
        /// <summary>
        /// static late update
        /// </summary>
        public static Action lateUpdate = delegate { };

        private static List<IEnumerator<YieldInstruction>> routines = new List<IEnumerator<YieldInstruction>>();
        private static List<IEnumerator<YieldInstruction>> frameRoutineAdds = new List<IEnumerator<YieldInstruction>>();

        internal static void AddRoutine(IEnumerator<YieldInstruction> toAdd)
        {
            frameRoutineAdds.Add(toAdd);
        }

        internal static void RemoveRoutine(IEnumerator<YieldInstruction> toRemove)
        {
            var ind = routines.FindIndex(c => object.ReferenceEquals(c, toRemove));

            if (ind != -1)
            {
                routines.RemoveAt(ind);
            }
        }

        internal static void LoopRoutines()
        {
            List<int> toRemove = new List<int>(8);
            
            for (int i = routines.Count - 1; i >= 0; i--)
            {
                var yield = routines[i].Current;
                bool remaining = false;

                try
                {

                    if (yield != null)
                    {
                        if (yield.IsDone)
                            remaining = routines[i].MoveNext();
                        else
                            remaining = true;
                    }
                    else
                    {
                        //haven't started...
                        remaining = routines[i].MoveNext();
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError("[Yield] {0}", e.ToString());
                }


                if (!remaining)
                {
                    //remove it
                    routines.RemoveAt(i);
                }
            }

            //remove all of the completed routines
            //foreach (var index in toRemove)
            //{
            //    routines.RemoveAt(index);
            //}

            //now add the new ones
            if (frameRoutineAdds.Count > 0)
            {
                routines.AddRange(frameRoutineAdds);
                frameRoutineAdds = new List<IEnumerator<YieldInstruction>>();
            }
        }

        internal static double PreviousFrameTime { get; private set; }
        internal static double TimeSinceStartup { get; private set; }
    }
}
