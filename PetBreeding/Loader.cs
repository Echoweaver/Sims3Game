using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.PetBreedfix
{
    public class Loader
    {
        [Tunable]
        protected static bool kInstantiator = false;

        public static string sEWBreedLocalizeKey = "Echoweaver/BreedFix:";

        static Loader()
        {
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }


        public static void InitializePetBreed(Sim a)
        {
            if (a.IsPet)
            {
                a.AddInteraction(ShowPetBreed.Singleton, true);
                a.AddInteraction(SetPetBreed.Singleton, true);
                if (a.SimDescription.PetManager.BreedName != "")
                {
                    BreedManager.storePetBreed(a.SimDescription);
                }
            }
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            Sim[] pets = Queries.GetObjects<Sim>();
            foreach (Sim pet in pets)
            {
                InitializePetBreed(pet);
            }
            Sim.CustomizeCollarAndCoats.Singleton = EWCustomizeCollarAndCoats.Singleton;
            EventTracker.AddListener(EventTypeId.kNewOffspringPet, new ProcessEventDelegate(OnNewOffspringPet));
            EventTracker.AddListener(EventTypeId.kSimInstantiated, new ProcessEventDelegate(OnSimInstantiated));
            EventTracker.AddListener(EventTypeId.kPlannedOutfitPet, new ProcessEventDelegate(OnPlannedOutfitPet));
        }

        public static ListenerAction OnSimInstantiated(Event e)
        {
            Sim sim = e.TargetObject as Sim;
            if (sim.IsPet)
            {
                InitializePetBreed(sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnNewOffspringPet(Event e)
        {
            Sim pet = e.TargetObject as Sim;
            if (pet.SimDescription.PetManager.BreedName == "" || pet.SimDescription.PetManager.BreedName == null)
            {
                BreedManager.setOffspringBreed(pet);
            }
            InitializePetBreed(pet);
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPlannedOutfitPet(Event e)
        {
            if (e.Actor.SimDescription.PetManager.BreedName == "")
            {
                e.Actor.SimDescription.PetManager.BreedName =
                    BreedManager.retrievePetBreed(e.Actor.SimDescription.SimDescriptionId);
            }
            return ListenerAction.Keep;
        }
    }
}
