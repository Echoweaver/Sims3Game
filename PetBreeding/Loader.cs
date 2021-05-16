using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
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
            EventTracker.AddListener(EventTypeId.kNewPet, new ProcessEventDelegate(OnNewPet));
            EventTracker.AddListener(EventTypeId.kPetHadOffspring, new ProcessEventDelegate(OnOffspring));
            EventTracker.AddListener(EventTypeId.kNewOffspringPet, new ProcessEventDelegate(OnNewOffspringPet));

            EventTracker.AddListener(EventTypeId.kChangeOutfit, new ProcessEventDelegate(OnChangeOutfit));
            EventTracker.AddListener(EventTypeId.kChangeFacialHair, new ProcessEventDelegate(OnChangeFacialHair));
            EventTracker.AddListener(EventTypeId.kChangeHairstyle, new ProcessEventDelegate(OnChangeHairstyle));
        }

        public static ListenerAction OnChangeOutfit(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("kChangeOutfit",
                StyledNotification.NotificationStyle.kDebugAlert));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnChangeFacialHair(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("kChangeFacialHair",
                StyledNotification.NotificationStyle.kDebugAlert));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnChangeHairstyle(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("kChangeHairstyle",
                StyledNotification.NotificationStyle.kDebugAlert));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnNewPet(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("New Pet Event",
                StyledNotification.NotificationStyle.kDebugAlert));
            AddBreedInteractions(e.TargetObject as Sim);
            return ListenerAction.Keep;
        }

        public static ListenerAction OnOffspring(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Pet Had Offspring Event",
                StyledNotification.NotificationStyle.kDebugAlert));
            AddBreedInteractions(e.TargetObject as Sim);
            return ListenerAction.Keep;
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
