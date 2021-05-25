using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;
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

        public static void AddBreedInteractions(Sim a)
        {
            if (a.IsPet)
            {
                a.AddInteraction(ShowPetBreed.Singleton, true);
                a.AddInteraction(SetPetBreed.Singleton, true);                
            }
            Sim.CustomizeCollarAndCoats.Singleton = EWCustomizeCollarAndCoats.Singleton;
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            Sim[] pets = Queries.GetObjects<Sim>();
            foreach (Sim pet in pets)
            {
                AddBreedInteractions(pet);
            }
            EventTracker.AddListener(EventTypeId.kNewOffspringPet, new ProcessEventDelegate(OnNewOffspringPet));
        }

        public static ListenerAction OnNewOffspringPet(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("New Offspring Pet Event - setting breed",
                StyledNotification.NotificationStyle.kDebugAlert));
            Sim pet = e.TargetObject as Sim;
            if (pet.SimDescription.PetManager.BreedName == "" || pet.SimDescription.PetManager.BreedName == null)
            {
                BreedManager.setOffspringBreed(pet);
            }
            AddBreedInteractions(pet);
            return ListenerAction.Keep;
        }
    }
}
