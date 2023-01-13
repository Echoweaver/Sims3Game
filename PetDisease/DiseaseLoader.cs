using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Queries = Sims3.Gameplay.Queries;

//Template Created by Battery

namespace Echoweaver.Sims3Game.PetDisease
{
    public class Loader
    {
        [Tunable] protected static bool init = false;

        [Tunable]
        public static bool kAllowPetDiseaseDeath = true;
        [Tunable]
        public static bool kPetDiseaseDebug = true;

        // Word on the street is that ghost shaders don't require the associated EP.
        [Tunable]
        public static SimDescription.DeathType diseaseDeathType = SimDescription.DeathType.Shark;

        public static List<ulong> BuffGuids = new List<ulong>() {
            Buffs.BuffEWPetGermy.mGuid,
            Buffs.BuffEWTummyTrouble.mGuid,
            Buffs.BuffEWPetstilence.mGuid,
            Buffs.BuffEWPetPneumonia.mGuid
        };

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
        }

        public static void Initialize()
        {
            for (int i = 0; i < BuffGuids.Count; i++)
            {
                Sim.ActiveActor.BuffManager.AddElement(BuffGuids[i], Origin.None);
            }
        }

        static void OnPreload()
        {
            LoadBuffXMLandParse(null);
        }

        static void OnWorldLoaded(object sender, EventArgs e)
        {
            //Initialize();
            foreach (Hospital h in Queries.GetObjects<Hospital>())
            {
                h.AddInteraction(EWVaccinatePet.Singleton, true);
            }

            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                if ((s.IsCat || s.IsADogSpecies) && s.SimDescription.AdultOrAbove)
                {

                    s.AddInteraction(EWTakeToVetDisease.Singleton, true);
                    if (kPetDiseaseDebug)
                    {
                        s.AddInteraction(Buffs.BuffEWPetGermy.Cough.Singleton, true);
                        s.AddInteraction(Buffs.BuffEWPetstilence.Shiver.Singleton, true);
                        s.AddInteraction(Buffs.EWTestAnim.Singleton, true);
                    }
                }
            }


            // Germy check
            EventTracker.AddListener(EventTypeId.kWeatherStarted, new ProcessEventDelegate(PetDiseaseManager
            .OnWeatherStarted));
            EventTracker.AddListener(EventTypeId.kChangedInsideOutsideStatus,
                new ProcessEventDelegate(PetDiseaseManager.OnChangedInsideOutsideStatus));

            // Stomach Flu Check
            EventTracker.AddListener(EventTypeId.kGoFishingCat, new ProcessEventDelegate(PetDiseaseManager
                .OnGoFishingCat));
            EventTracker.AddListener(EventTypeId.kPlayedInToilet, new ProcessEventDelegate(PetDiseaseManager
                .OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kPlayInTrashPile, new ProcessEventDelegate(PetDiseaseManager
                .OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kDigThroughGarbage, new ProcessEventDelegate(PetDiseaseManager
                .OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kEatTrashPile, new ProcessEventDelegate(PetDiseaseManager
                .OnEatTrash));

            // Food Poisoning
            EventTracker.AddListener(EventTypeId.kAteMeal, new ProcessEventDelegate(PetDiseaseManager
                .OnAteHumanFood));
            EventTracker.AddListener(EventTypeId.kAteFish, new ProcessEventDelegate(PetDiseaseManager
                .OnAtePrey));

            // Petstilence Check
            EventTracker.AddListener(EventTypeId.kGotFleas, new ProcessEventDelegate(PetDiseaseManager
                .OnGotFleas));
            EventTracker.AddListener(EventTypeId.kGoHuntingCat, new ProcessEventDelegate(PetDiseaseManager
                .OnGotFleas));
            EventTracker.AddListener(EventTypeId.kPetWooHooed, new ProcessEventDelegate(PetDiseaseManager
                .OnPetWoohooed));

            // Social event: Fight Pet, Greet Sniff
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(PetDiseaseManager
                .OnSocialInteraction));

            // Any disease check
            EventTracker.AddListener(EventTypeId.kMetSim, new ProcessEventDelegate(PetDiseaseManager
                .OnMetSim));

            if (kPetDiseaseDebug)
            {
                AlarmManager.Global.AddAlarm(10f, TimeUnit.Seconds, NotifyDebugState, "Notify that debug is on",
                    AlarmType.NeverPersisted, null);
            }

        }

        public static void LoadBuffXMLandParse(ResourceKey[] resourceKeys)
        {
            ResourceKey key = new ResourceKey(5522594682370665020ul, 53690476u, 0u);
            XmlDbData xmlDbData = XmlDbData.ReadData(key, false);
            if (xmlDbData != null)
            {
                BuffManager.ParseBuffData(xmlDbData, true);
            }
            UIManager.NewHotInstallStoreBuffData += LoadBuffXMLandParse;
        }

        public static void NotifyDebugState()
        {
            DebugNote("Pet Disease Debug Mode ON");
        }

        public static void DebugNote(string str)
        {
            if (kPetDiseaseDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification.NotificationStyle.kDebugAlert));
            }
        }
    }
}