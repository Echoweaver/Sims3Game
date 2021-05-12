using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.PetBreeding
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
            Sim[] pets = Queries.GetObjects<Sim>();
            foreach (Sim pet in pets)
            {
                if (pet.IsPet)
                {
                    pet.AddInteraction(ShowPetBreed.Singleton);
                    pet.AddInteraction(SetPetBreed.Singleton);
                }
            }
            EventTracker.AddListener(EventTypeId.kNewPet, new ProcessEventDelegate(OnNewPet));
        }

        public static ListenerAction OnNewPet(Event e)
        {
            Sim newPet = e.TargetObject as Sim;
            if (newPet.IsPet)
            {
                newPet.AddInteraction(ShowPetBreed.Singleton);
                newPet.AddInteraction(SetPetBreed.Singleton);
            }
            return ListenerAction.Keep;
        }
    }
}
