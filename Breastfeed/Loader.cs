using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.Breastfeed
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            new BuffBooter().LoadBuffData();
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                if (s.SimDescription.ToddlerOrBelow)
                {
                    s.AddInteraction(BreastfeedBaby.Singleton, true);
                }
            }
            //RockingChair[] objects2 = Queries.GetObjects<RockingChair>();
            //foreach (RockingChair rockingChair in objects2)
            //{
            //    AddInteractionsRockingChair(rockingChair);
            //}

            EventTracker.AddListener((EventTypeId)311, (ProcessEventDelegate)(object)new ProcessEventDelegate(OnSimInstantiated));
            //EventTracker.AddListener((EventTypeId)113, (ProcessEventDelegate)(object)new ProcessEventDelegate(OnObjectChanged));

        }

        private static ListenerAction OnSimInstantiated(Event e)
        {
            try
            {
                IGameObject targetObject = e.TargetObject;
                Sim s = targetObject as Sim;
                if (s != null)
                {
                    if (s.SimDescription.ToddlerOrBelow)
                    {
                        s.AddInteraction(BreastfeedBaby.Singleton, true);
                    }
                }
            }
            catch (Exception)
            {
            }
            return ListenerAction.Keep;
        }

        //private static ListenerAction OnObjectChanged(Event e)
        //{
        //    try
        //    {
        //        IGameObject targetObject = e.get_TargetObject();
        //        RockingChair val = targetObject as RockingChair;
        //        if (val != null)
        //        {
        //            AddInteractionsRockingChair(val);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    return (ListenerAction)0;
        //}
    }
}