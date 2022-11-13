using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.Controller;

//Template Created by Battery

namespace Echoweaver.Sims3Game.PetDisease
{
	public class Loader
	{
		[Tunable] static bool init = false;

        [Tunable]
        public static bool kAllowPetDeath = true;

        // Word on the street is that ghost shaders don't require the associated EP.
        [Tunable]
        public static SimDescription.DeathType diseaseDeathType = SimDescription.DeathType.Shark;

        public static List<ulong> BuffGuids = new List<ulong>() {
			Buffs.BuffEWPetGermy.mGuid,
			Buffs.BuffEWTummyTrouble.mGuid,
			Buffs.BuffEWPetstilence.mGuid,
			Buffs.BuffEWBabyDistress.mGuid,
			Buffs.BuffEWPetPneumonia.mGuid
		};

        // Index of SimDescriptionID and timestamp of vaccination
        // 		mVaccinationDate = SimClock.CurrentTime ();

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
			Initialize();

			// Germy check
			EventTracker.AddListener(EventTypeId.kWeatherStarted, new ProcessEventDelegate(PetDiseaseManager.OnWeatherStarted));
            EventTracker.AddListener(EventTypeId.kChangedInsideOutsideStatus,
				new ProcessEventDelegate(PetDiseaseManager.OnChangedInsideOutsideStatus));

			// Stomach Flu Check
            EventTracker.AddListener(EventTypeId.kGoFishingCat, new ProcessEventDelegate(PetDiseaseManager.OnGoFishingCat));
            EventTracker.AddListener(EventTypeId.kPlayedInToilet, new ProcessEventDelegate(PetDiseaseManager.OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kPlayInTrashPile, new ProcessEventDelegate(PetDiseaseManager.OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kDigThroughGarbage, new ProcessEventDelegate(PetDiseaseManager.OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kEatTrashPile, new ProcessEventDelegate(PetDiseaseManager.OnEatTrash));

            // Food Poisoning
            EventTracker.AddListener(EventTypeId.kAteMeal, new ProcessEventDelegate(PetDiseaseManager.OnAteHumanFood));
            EventTracker.AddListener(EventTypeId.kAteFish, new ProcessEventDelegate(PetDiseaseManager.OnAtePrey));

            // Petstilence Check
            EventTracker.AddListener(EventTypeId.kGotFleas, new ProcessEventDelegate(PetDiseaseManager.OnGotFleas));
            EventTracker.AddListener(EventTypeId.kGoHuntingCat, new ProcessEventDelegate(PetDiseaseManager.OnGotFleas));
            EventTracker.AddListener(EventTypeId.kPetWooHooed, new ProcessEventDelegate(PetDiseaseManager.OnPetWoohooed));

            // Social event: Fight Pet
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(PetDiseaseManager.OnSocialInteraction));

            // Any disease check
            // kMetSim

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
    }
}