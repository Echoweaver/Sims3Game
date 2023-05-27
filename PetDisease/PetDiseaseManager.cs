﻿using System;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using System.Collections.Generic;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace.Enums;
using Sims3.UI.Controller;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetDisease
{

    // Diseases:
    // Germy (Whitecough) -- common cold.
    //   -- Generated from weather changes
    //   -- Symptoms: Coughing, energy reduction
    // Pneumonia (Greencough)
    //   -- Develops from Germy.
    //   -- Symptoms: Fever, exhaustion, frequent coughing, delerium
    //   -- Contagion by contact
    //   -- Can be lethal.
    // Petstilence (Carrionplace Disease)
    //   -- Generated from fights or hunting.
    //   -- Chance of getting it with fleas
    //   -- Bloodborne, transmitted by fighting, woohoo
    //   -- Symptoms: passing out and vomiting,
    //   -- Frequently lethal.
    // Stomach Flu
    //   -- Generated from Contact with non-resident animals, drink/play in toilet
    //   -- Transmitted by proximity to vomit piles or vomiting
    //   -- Is it possible to tag vomit piles as diseased?
    //   -- Symptoms: random nausea moodlets
    //   -- Called Tummy Trouble, looks identical to Food Poisoning
    // Food Poisoning
    //   -- Generated from eating, likely with spoiled food
    //   -- Also generated by from some kinds of berries?
    //      -- (holly, juniper, nightshade, dogwood, poke, mistletoe, elderberry,  gooseberries,
    //      -- marionberries, salmonberries, cherries, and serviceberries, baneberries, *tomatoes,
    //      -- *onions, *garlic, rubarb, tree nuts, aloe (cats), parsley (cats))
    //      -- Proposed: Cherry, tomato, onion, garlic
    //   -- Not contagious
    //   -- Symptoms: random nausea moodlets
    //   -- Called Tummy Trouble, looks identical to Stomach Flu
    // Childbirth Crisis (not implemented in this mod):
    //   -- Triggered when pet enters childbirth?
    //   -- If not cured, generates a wound and/or one offspring doesn't survive.

    public class PetDiseaseManager
    {
        [Tunable]
        public static bool kAllowPetDiseaseDeath = true;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Length of time a pet will recuperate instead of dying for games with death turned off.")]
        public static float kRecuperateTime = 720f;

        [Tunable]
        [TunableComment("Float. Temp in F where fishing pets could catch cold.")]
        public static float kChillyEnoughToCatchCold = 40f;

        [Tunable]
        [TunableComment("Float. Time in minutes between contagion checks for a specific illness.")]
        public static float kCheckForContagionInterval = 360f;


        // Tunable parameters for PetGermy moodlet
        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenGermySymptoms = 60f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenGermySymptoms = 120f;

        [TunableComment("Min cold duration (Minutes")]
        [Tunable]
        public static float kMinGermyDuration = 2880f; // 2 days

        [TunableComment("Max cold duration (Minutes)")]
        [Tunable]
        public static float kMaxGermyDuration = 10080f;  // 7 days

        [TunableComment("1 in x chance cold will become pneumonia if left untreated")]
        [Tunable]
        public static float kChanceOfPneumonia = 4f;

        // Tunable parameters for TummyTrouble moodlet
        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenNauseaSymptoms = 60f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenNauseaSymptoms = 180f;

        [TunableComment("Odds of getting sick from being in proximity to a sick sim")]
        [Tunable]
        public static float kStomachFluProximityOdds = 0.5f;

        [TunableComment("Odds of getting food poisoning from eating unobjectionable food")]
        [Tunable]
        public static float kAmbientPoisonOdds = 0.01f;

        [TunableComment("Min stomach flu incubation time (Hours)")]
        [Tunable]
        public static float kMinStomachFluIncubationTime = 6f;

        [TunableComment("Max stomach flu incubation time (Hours)")]
        [Tunable]
        public static float kMaxStomachFluIncubationTime = 24f;

        [TunableComment("Min tummy trouble duration (Minutes)")]
        [Tunable]
        public static float kMinNauseaDuration = 720f; // 12 hours

        [TunableComment("Max tummy trouble duration (Minutes)")]
        [Tunable]
        public static float kMaxNauseaDuration = 2880f; // 2 days

        // Tunable parameters for Pneumonia moodlet

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenPneumoniaSymptoms = 45f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenPneumoniaSymptoms = 90f;

        [TunableComment("Min cold duration (Minutes)")]
        [Tunable]
        public static float kMinPneumoniaDuration = 2880f;  // 2 days

        [TunableComment("Max cold duration (Minutes)")]
        [Tunable]
        public static float kMaxPneumoniaDuration = 7200f;  // 5 days

        [TunableComment("1 in x chance pneumonia will become lethal if left untreated")]
        [Tunable]
        public static float kChanceOfLethalPneumonia = 4f;

        // Tunable parameters for Petstilence moodlet
        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenPetstilenceSymptoms = 120f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenPetstilenceSymptoms = 360f;

        [TunableComment("Min petstilence duration (Minutes)")]
        [Tunable]
        public static float kMinPetstilenceDuration = 5760f;  // 4 days

        [TunableComment("Max petstilence duration (Minutes)")]
        [Tunable]
        public static float kMaxPetstilenceDuration = 10080f;  // 1 week

        [TunableComment("Chance of catching from hunting/fleas/etc.")]
        [Tunable]
        public static float kAmbientPetstilenceOdds = 0.05f;  // 5%

        [TunableComment("Chance of catching petstilence from bodily fluid contact")]
        [Tunable]
        public static float kBloodbornePetstilenceOdds = 0.6f;  // 60%


        [Persistable]
        static public Dictionary<ulong, DateAndTime> VaccineRecord = new Dictionary<ulong, DateAndTime>();

        static public Dictionary<ulong, DateAndTime> LastGermyCheck = new Dictionary<ulong, DateAndTime>();
        static public Dictionary<ulong, DateAndTime> LastFluCheck = new Dictionary<ulong, DateAndTime>();
        static public Dictionary<ulong, DateAndTime> LastFoodPoisonCheck = new Dictionary<ulong, DateAndTime>();
        static public Dictionary<ulong, DateAndTime> LastPetstilenceCheck = new Dictionary<ulong, DateAndTime>();

        public static BuffNames[] CurableDiseases =
        {
            Buffs.BuffEWTummyTrouble.buffName,
            Buffs.BuffEWPetstilence.buffName,
            Buffs.BuffEWPetPneumonia.buffName
        };

        public PetDiseaseManager()
        {
        }

        public static bool CheckForVaccination(Sim s)
        {
            if (VaccineRecord.ContainsKey(s.SimDescription.SimDescriptionId))
            {
                DateAndTime vaccineDate = VaccineRecord[s.SimDescription.SimDescriptionId];
                if (SimClock.ElapsedTime(TimeUnit.Days, vaccineDate) <= SeasonsManager.GetYearLength())
                {
                    // Sim is vaccinated. It can't get germy, pneumonia, or petstilence
                    return true;
                }
            }
            return false;
        }

        public static bool ReadyToCheckContagion(Dictionary<ulong, DateAndTime> checkRecord, SimDescription s)
        {
            DateAndTime lastCheck = DateAndTime.Invalid;

            // Including the role checks because maybe a mod uses pet roles? I don't think
            // there are any pet roles or service sims right now.
            if (s.CreatedSim.InWorld && s.AssignedRole != null || ServiceSituation.IsSimOnJob(s.CreatedSim)
                || s.CreatedSim.IsGhostOrHasGhostBuff || s.CreatedSim.IsDying())
            {
                DebugNote("Sim not valid for contagion: " + s.FullName);
                return false;
            }
            
            checkRecord.TryGetValue(s.SimDescriptionId, out lastCheck);

            if (lastCheck == null || lastCheck == DateAndTime.Invalid
                || SimClock.ElapsedTime(TimeUnit.Minutes, lastCheck) >= kCheckForContagionInterval)
            {
                DebugNote("Ready to check Contagion" + s.FullName);
                checkRecord[s.SimDescriptionId] = SimClock.CurrentTime();
                return true;
            }
            DebugNote("Contagion check to recent for this disease: " + s.FullName);
            return false;
        }

        public static void Vaccinate(Sim s)
        {
            VaccineRecord[s.SimDescription.SimDescriptionId] = SimClock.CurrentTime();

            // TODO: See if this sting makes any sense for pets. We can make a new one.
            s.ShowTNSAndPlayStingIfSelectable("sting_get_immunized", TNSNames.FluShotTNS, s,
                null, null, null, new bool[1] { s.IsFemale }, false, s);
        }

        public static ListenerAction OnGotBuff(Event e)
        {
            Sim sim = e.Actor as Sim;
            if (sim != null)
            {
                // I'm pretty sure cats don't get EA's Germy, but might as well check
                if (sim.IsADogSpecies || sim.IsCat)
                {
                    // An MTS thread indicated that you needed a delay after the event triggered to
                    // be able to check the buff
                    Simulator.AddObject(new OneShotFunctionWithParams(new FunctionWithParam(ProcessBuff),
                        sim));
                }
            }
            return ListenerAction.Keep;
        }

        private static void ProcessBuff(object obj)
        {
            Sim sim = obj as Sim;
            if (sim.BuffManager.HasElement(BuffNames.Germy))
            {
                DebugNote("Recieved Germy moodlet. Replaced with Pet Germy: " + sim.FullName);
                sim.BuffManager.RemoveElement(BuffNames.Germy);
                sim.BuffManager.AddElement(Buffs.BuffEWPetGermy.buffName, Origin.None);
            }

        }

        public static ListenerAction OnWeatherStarted(Event e)
        {
            if (e is WeatherEvent)
            {
                WeatherEvent we = e as WeatherEvent;
                if (we.Weather == Weather.Hail || we.Weather == Weather.Rain || we.Weather == Weather.Snow)
                {
                    DebugNote("Check for weather change GermyPet " + e.Actor.Name);
                    foreach (Lot allLot in LotManager.AllLots)
                    {
                        List<Sim> list = allLot.GetSims() as List<Sim>;
                        foreach (Sim s in list)
                        {
                            if ((s.IsCat || s.IsADogSpecies) && s.SimDescription.AdultOrAbove
                                && !SeasonsManager.IsShelteredFromPrecipitation(s))
                            {
                                if (ReadyToCheckContagion(LastGermyCheck, s.SimDescription))
                                {
                                    Buffs.BuffEWPetGermy.CheckWeatherContagion(s);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                DebugNote("Test: OnWeatherStarted event is not WeatherEvent type" + e.GetType().ToString());
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
                    DebugNote("Check for outside during precip " + e.Actor.Name);
                    if (ReadyToCheckContagion(LastGermyCheck, e.Actor.SimDescription))
                    {
                        Buffs.BuffEWPetGermy.CheckWeatherContagion(e.Actor as Sim);
                    }
                } 
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoFishingCat(Event e)
        {
            DebugNote("Check for fishing illness " + e.Actor.Name);

            if (ReadyToCheckContagion(LastFluCheck, e.Actor.SimDescription))
            {
                Buffs.BuffEWTummyTrouble.CheckAmbientContagion(e.Actor as Sim);
            }

            if (SeasonsManager.Temperature <= kChillyEnoughToCatchCold)
            {
                if (ReadyToCheckContagion(LastGermyCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWPetGermy.CheckWeatherContagion(e.Actor as Sim);
                    LastGermyCheck[e.Actor.SimDescription.SimDescriptionId] = SimClock.CurrentTime();
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPlayedInToilet(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("Check for toilet contagion " + e.Actor.Name);
                if (ReadyToCheckContagion(LastFluCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWTummyTrouble.CheckInteractionContagion(e.Actor as Sim);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnEatTrash(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("Check for eat trash contagion " + e.Actor.Name);
                if (ReadyToCheckContagion(LastFluCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWTummyTrouble.CheckEatContagion(e.Actor as Sim);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGotFleas(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("Check for fleas/hunting contagion " + e.Actor.Name);
                if (ReadyToCheckContagion(LastPetstilenceCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWPetstilence.CheckAmbientContagion(e.Actor as Sim);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPetWoohooed(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("Check for woohoo contagion " + e.Actor.Name);
                Buffs.BuffEWPetstilence.CheckBloodborneContagion(e.Actor as Sim);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnAteHumanFood(Event e)
        {
            DebugNote("Ate meal " + e.Actor.Name);
            PreparedFood food = e.TargetObject as PreparedFood;

            if (food != null)
            {
                if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
                {
                    if (ReadyToCheckContagion(LastFoodPoisonCheck, e.Actor.SimDescription))
                    {
                        if (food.IsSpoiled)
                        {
                            DebugNote("Check for spoiled human food contagion " + e.Actor.Name);
                            Buffs.BuffEWTummyTrouble.CheckFoodPoisoningSpoiled(e.Actor as Sim);
                        }
                        else
                        {
                            DebugNote("Check for human food contagion " + e.Actor.Name);
                            Buffs.BuffEWTummyTrouble.CheckFoodPoisoning(e.Actor as Sim);
                        }
                    }
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnAtePrey(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("Check for ate prey contagion " + e.Actor.Name);
                if (ReadyToCheckContagion(LastFoodPoisonCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWTummyTrouble.CheckAmbientPoisoning(e.Actor as Sim);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnAteFromBowl(Event e)
        {
            DebugNote("Ate From Bowl Event " + e.Actor.Name);
            return ListenerAction.Keep;
        }

        public static ListenerAction OnAteHarvestable(Event e)
        {
            DebugNote("Ate Havestable Event " + e.Actor.Name);
            return ListenerAction.Keep;
        }

        public static ListenerAction OnMetSim(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("On Met Sim: " + e.Actor.Name + " met " + e.TargetObject.GetLocalizedName());
                if (ReadyToCheckContagion(LastGermyCheck, e.Actor.SimDescription))
                {
                    DebugNote("Check for stranger germy contagion " + e.Actor.Name);
                    Buffs.BuffEWPetGermy.CheckAmbientContagion(e.Actor as Sim);
                }
                if (ReadyToCheckContagion(LastFluCheck, e.Actor.SimDescription))
                {
                    DebugNote("Check for stranger germy contagion " + e.Actor.Name);
                    Buffs.BuffEWTummyTrouble.CheckAmbientContagion(e.Actor as Sim);
                }

            }
            return ListenerAction.Keep;

        }

        public static ListenerAction OnSocialInteraction(Event e)
        {
            // Turns out a social interaction like "Chat" triggers 4 events of EventTypeId kSocialInteraction.
            // Two cast to SocialEvent, one for the recipient and one for the initiator. I have no idea what
            // the other two are, but we don't care about them.
            if (e is SocialEvent)
            {
                SocialEvent cevent = (SocialEvent)e;
                Sim actorSim = cevent.Actor as Sim;
                Sim targetSim = cevent.TargetObject as Sim;

                // We're going to get a social event with each sim as Actor, so just check the Actor one
                if (cevent != null && cevent.SocialName.Contains("Fight Pet"))
                {
                    DebugNote("Fight Pet Contagion Check");
                    if (!actorSim.BuffManager.HasElement(Buffs.BuffEWPetstilence.buffName))
                    {
                        if (targetSim.BuffManager.HasElement(Buffs.BuffEWPetstilence.buffName))
                        {
                            DebugNote("Check for bloodborne contagion " + actorSim.Name);
                            Buffs.BuffEWPetstilence.CheckBloodborneContagion(actorSim);
                        }
                        else if (targetSim.IsRaccoon)
                        {
                            // Raccoon can always transmit Petstilence
                            DebugNote("Check for fight raccoon contagion " + actorSim.Name);
                            Buffs.BuffEWPetstilence.CheckContactContagion(actorSim);
                        }
                        else
                        {
                            // A fight with a stranger has a low chance of generating Petstilence
                            Relationship relationship = Relationship.Get(actorSim, targetSim, createIfNone: false);

                            // This is a bit clunky, but I don't want it to crash because I tried to check
                            // against an attribute of a null object
                            if (relationship == null || relationship.CurrentLTR == LongTermRelationshipTypes.Stranger)
                            {
                                DebugNote("Check for fight stranger contagion " + actorSim.Name);
                                Buffs.BuffEWPetstilence.CheckAmbientContagion(actorSim);
                            }
                        }
                    } else
                    {
                        DebugNote(actorSim.FullName + " already has Petstilence");
                    }
                }

                else if (cevent.SocialName.Contains("Greet Sniff") && (actorSim.IsCat || actorSim.IsADogSpecies)
                    && actorSim.SimDescription.AdultOrAbove)
                {
                    Relationship relationship = Relationship.Get(actorSim, targetSim, createIfNone: false);
                    if (relationship == null)
                    {
                        DebugNote("Check for stranger germy contagion " + e.Actor.Name);
                        if (ReadyToCheckContagion(LastGermyCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWPetGermy.CheckAmbientContagion(actorSim);
                        }
                        if (ReadyToCheckContagion(LastFluCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWTummyTrouble.CheckAmbientContagion(actorSim);
                        }
                    }
                    else if (targetSim.IsRaccoon || relationship.CurrentLTR == LongTermRelationshipTypes.Stranger)
                    {
                        DebugNote("Check for stranger germy contagion " + e.Actor.Name);
                        if (ReadyToCheckContagion(LastGermyCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWPetGermy.CheckAmbientContagion(actorSim);
                        }
                        if (ReadyToCheckContagion(LastFluCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWTummyTrouble.CheckAmbientContagion(actorSim);
                        }
                    }
                }

            }

            return ListenerAction.Keep;
        }

    }
}



