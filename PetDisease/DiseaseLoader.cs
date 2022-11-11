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
        [Persistable]
		static public Dictionary<ulong, DateAndTime> VaccineRecord = new Dictionary<ulong, DateAndTime>();

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
			EventTracker.AddListener(EventTypeId.kWeatherStarted, new ProcessEventDelegate(OnWeatherStarted));
            EventTracker.AddListener(EventTypeId.kChangedInsideOutsideStatus,
				new ProcessEventDelegate(OnChangedInsideOutsideStatus));

			// Stomach Flu Check
            EventTracker.AddListener(EventTypeId.kGoFishingCat, new ProcessEventDelegate(OnGoFishingCat));
            EventTracker.AddListener(EventTypeId.kPlayedInToilet, new ProcessEventDelegate(OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kPlayInTrashPile, new ProcessEventDelegate(OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kDigThroughGarbage, new ProcessEventDelegate(OnPlayedInToilet));
            EventTracker.AddListener(EventTypeId.kEatTrashPile, new ProcessEventDelegate(OnEatTrash));

            // Food Poisoning
            EventTracker.AddListener(EventTypeId.kAteMeal, new ProcessEventDelegate(OnAteHumanFood));
            EventTracker.AddListener(EventTypeId.kAteFish, new ProcessEventDelegate(OnAtePrey));

            // Petstilence Check
            EventTracker.AddListener(EventTypeId.kGotFleas, new ProcessEventDelegate(OnGotFleas));
            EventTracker.AddListener(EventTypeId.kGoHuntingCat, new ProcessEventDelegate(OnGotFleas));
            EventTracker.AddListener(EventTypeId.kPetWooHooed, new ProcessEventDelegate(OnPetWoohooed));

            // Social event: Fight Pet
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));

            // Any disease check
            // kMetSim

        }

        public static ListenerAction OnWeatherStarted(Event e)
        {
            if (e is WeatherEvent)
            {
                WeatherEvent we = e as WeatherEvent;
                if (we.Weather == Weather.Hail || we.Weather == Weather.Rain || we.Weather == Weather.Snow)
                {
                    StyledNotification.Show(new StyledNotification.Format("Check for weather change GermyPet " +
                        e.GetType().ToString(), StyledNotification.NotificationStyle.kGameMessagePositive));
                    foreach (Lot allLot in LotManager.AllLots)
                    {
                        List<Sim> list = allLot.GetSims() as List<Sim>;
                        foreach (Sim s in list)
                        {
                            if (s.InWorld && (s.IsCat || s.IsADogSpecies) && s.SimDescription.AdultOrAbove)
                            {
                                // Including the role checks because maybe a mod uses pet roles? I don't think
                                // there are any pet roles or service sims right now.
                                if (s.SimDescription.AssignedRole != null || ServiceSituation.IsSimOnJob(s)
                                        || s.IsGhostOrHasGhostBuff || s.IsDying()
                                        || SeasonsManager.IsShelteredFromPrecipitation(s))
                                {
                                    continue;
                                }
                            }
                            Buffs.BuffEWPetGermy.CheckWeatherContagion(s);
                        }
                    }
                }
            } else
            {
                StyledNotification.Show(new StyledNotification.Format("Test: OnWeatherStarted event is not WeatherEvent type" +
                    e.GetType().ToString(), StyledNotification.NotificationStyle.kDebugAlert));

            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnChangedInsideOutsideStatus(Event e)
        {
            if (e.Actor.InWorld && (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
                && e.Actor.IsOutside)
            {
                PrecipitationIntensity intensity;

                // Can't check for hail?
                if (SeasonsManager.IsRaining(out intensity) || SeasonsManager.IsSnowing(out intensity)
                    || SeasonsManager.IsExtremeCold())
                {
                    Buffs.BuffEWPetGermy.CheckWeatherContagion(e.Actor as Sim);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoFishingCat(Event e)
        {
            Buffs.BuffEWTummyTrouble.CheckAmbientContagion(e.Actor as Sim);
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPlayedInToilet(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                Buffs.BuffEWTummyTrouble.CheckInteractionContagion(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnEatTrash(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                Buffs.BuffEWTummyTrouble.CheckEatContagion(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGotFleas(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                Buffs.BuffEWPetstilence.CheckAmbientContagion(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPetWoohooed(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                Buffs.BuffEWPetstilence.CheckContactContagion(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnAteHumanFood(Event e)
        {
            PreparedFood food = e.TargetObject as PreparedFood;

            if (food != null)
            {
                if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
                {
                    if (food.IsSpoiled)
                    {
                        Buffs.BuffEWTummyTrouble.CheckFoodPoisoningSpoiled(e.Actor as Sim);
                    } else
                    {
                        Buffs.BuffEWTummyTrouble.CheckFoodPoisoning(e.Actor as Sim);
                    }
                }
            }
            return ListenerAction.Keep;
        }


        public static ListenerAction OnAtePrey(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                Buffs.BuffEWTummyTrouble.CheckAmbientPoisoning(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }


        public static ListenerAction OnSocialInteraction(Event e)
        {
            // Turns out a social interaction like "Chat" triggers 4 events of EventTypeId kSocialInteraction.
            // Two cast to SocialEvent, one for the recipient and one for the initiator. I have no idea what
            // the other two are, but we don't want them.
            if (e is SocialEvent)
            {
                SocialEvent cevent = (SocialEvent)e;
                // There are two social interactions for discussing kelp recipes -- on land and in water.
                // I don't know that Discussing Kelp Recipes can be rejected, but obviously you shouldn't learn
                // anything if it was.
                if (cevent != null && cevent.SocialName.Contains("Fight Pet"))
                {
                    Sim fighter = cevent.Actor as Sim;
                    Sim opponent = cevent.TargetObject as Sim;

                    if ((fighter.IsCat || fighter.IsADogSpecies) && (opponent.IsCat || opponent.IsADogSpecies))
                    {
                        if (opponent.BuffManager.HasElement(Buffs.BuffEWPetstilence.buffName))
                        {
                            Buffs.BuffEWPetstilence.CheckContactContagion(fighter);
                        } else
                        {
                            Relationship relationship = Relationship.Get(fighter, opponent, createIfNone: false);

                            // This is a bit clunky, but I don't want it to crash because I tried to check
                            // against an attribute of a null object
                            if (relationship == null)
                            {
                                Buffs.BuffEWPetstilence.CheckAmbientContagion(fighter);
                            } else if (relationship.CurrentLTR == LongTermRelationshipTypes.Stranger)
                            {
                                Buffs.BuffEWPetstilence.CheckAmbientContagion(fighter);
                            }

                        }
                    }
                }
            }
            return ListenerAction.Keep;
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

		public static bool checkForVaccination(Sim s)
        {
            if (VaccineRecord.ContainsKey(s.SimDescription.SimDescriptionId))
            {
                DateAndTime vaccineDate = VaccineRecord[s.SimDescription.SimDescriptionId];
                if (SimClock.ElapsedTime(TimeUnit.Days, vaccineDate) <= (float)SeasonsManager.GetYearLength())
                {
                    // Sim is vaccinated. It can't get sick.
                    return true;
                }
            }
            return false;
        }

	}
}