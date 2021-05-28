using System;
using NRaas.ConsignerSpace.Helpers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetBreederCareer
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            //            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kSoldConsignedObject, new ProcessEventDelegate(OnSoldConsignedObject));
            EventTracker.AddListener(EventTypeId.kBoughtConsignedObject, new ProcessEventDelegate(OnBoughtConsignedObject));
        }

        public static ListenerAction OnSoldConsignedObject(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Sold Consigned Object",
                StyledNotification.NotificationStyle.kDebugAlert));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnBoughtConsignedObject(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Bought Consigned Object",
                StyledNotification.NotificationStyle.kDebugAlert));
            return ListenerAction.Keep;
        }
    }
}
