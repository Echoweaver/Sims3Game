using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Autonomy;

namespace Echoweaver.Sims3Game.PetIllness
{
    public class LoadThis : GameObject
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        [Tunable]
        public static bool kAllowPetDeath = true;
        public static SimDescription.DeathType sickDeathType = SimDescription.DeathType.HauntingCurse;


        static LoadThis()
        {
            // gets the OnPreload method to run before the whole savegame is loaded so your sim doesn't find
            // the skill missing if they need to access its data
            LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        static void OnPreload()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // Load custom buffs
            (new BuffBooter()).LoadBuffData();
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kChangedInsideOutsideStatus,
                new ProcessEventDelegate(OnChangedInsideOutsideStatus));

        }

        public static ListenerAction OnChangedInsideOutsideStatus(Event e)
        {
            Sim sim = e.Actor as Sim;
            EWDisease.Manager(sim.SimDescription).UpdateInsideOutside();
            return ListenerAction.Keep;
        }
    }
}