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
        [Persistable]
        static public Dictionary<ulong, DateAndTime> VaccineRecord = new Dictionary<ulong, DateAndTime>();

        [Tunable]
        [TunableComment("Float. Temp in F where fishing pets could catch cold.")]
        public static float kChillyEnoughToCatchCold = 40f;

        [Tunable]
        [TunableComment("Float. Time in minutes between contagion checks for a specific illness.")]
        public static float kCheckForContagionInterval = 360f;

        static public Dictionary<ulong, DateAndTime> LastGermyCheck = new Dictionary<ulong, DateAndTime>();
        static public Dictionary<ulong, DateAndTime> LastFluCheck = new Dictionary<ulong, DateAndTime>();
        static public Dictionary<ulong, DateAndTime> LastFoodPoisonCheck = new Dictionary<ulong, DateAndTime>();

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
                DebugNote("Ready to check Contagion = TRUE: " + s.FullName);
                checkRecord[s.SimDescriptionId] = SimClock.CurrentTime();
                return true;
            }
            DebugNote("Ready to check Contagion = FALSE: " + s.FullName);
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
                DebugNote("Check for fleas contagion " + e.Actor.Name);
                Buffs.BuffEWPetstilence.CheckAmbientContagion(e.Actor as Sim);
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

        public static ListenerAction OnMetSim(Event e)
        {
            if (e.Actor.SimDescription.IsCat || e.Actor.SimDescription.IsADogSpecies)
            {
                DebugNote("On Met Sim Actor: " + e.Actor.Name + "; Target: " + e.TargetObject.GetLocalizedName());
                if (ReadyToCheckContagion(LastGermyCheck, e.Actor.SimDescription))
                {
                    Buffs.BuffEWPetGermy.CheckAmbientContagion(e.Actor as Sim);
                }
                if (ReadyToCheckContagion(LastFluCheck, e.Actor.SimDescription))
                {
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
                    if (targetSim.BuffManager.HasElement(Buffs.BuffEWPetstilence.buffName))
                    {
                        DebugNote("Check for bloodborne contagion " + actorSim.Name);
                        Buffs.BuffEWPetstilence.CheckBloodborneContagion(actorSim);
                    }
                    else if (targetSim.IsRaccoon)
                    {
                        // Raccoon can always transmit Petstilence
                        DebugNote("Check for raccoon contagion " + actorSim.Name);
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
                            DebugNote("Check for stranger contagion " + actorSim.Name);
                            Buffs.BuffEWPetstilence.CheckAmbientContagion(actorSim);
                        }
                    }
                }

                else if (cevent.SocialName.Contains("Greet Sniff") && (actorSim.IsCat || actorSim.IsADogSpecies)
                    && actorSim.SimDescription.AdultOrAbove)
                {
                    DebugNote("Greet sniff check for " + actorSim.Name);

                    // Meeting a strange pet has a low chance of generating any proximity contagion
                    Relationship relationship = Relationship.Get(actorSim, targetSim, createIfNone: false);

                    if (relationship == null)
                    {
                        DebugNote("Check for stranger contagion " + e.Actor.Name);
                        if (ReadyToCheckContagion(LastGermyCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWPetGermy.CheckAmbientContagion(actorSim);
                        }
                        if (ReadyToCheckContagion(LastFluCheck, actorSim.SimDescription))
                        {
                            Buffs.BuffEWTummyTrouble.CheckAmbientContagion(actorSim);
                        }
                    }
                    else if (relationship.CurrentLTR == LongTermRelationshipTypes.Stranger)
                    {
                        DebugNote("Check for stranger contagion " + e.Actor.Name);
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



