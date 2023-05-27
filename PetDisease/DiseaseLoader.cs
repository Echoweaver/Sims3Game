using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects;
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
        public static bool kPetDiseaseDebug = false;

        // Word on the street is that ghost shaders don't require the associated EP.
        public static SimDescription.DeathType kDiseaseDeathType = SimDescription.DeathType.HauntingCurse;

        public static List<ulong> BuffGuids = new List<ulong>() {
            Buffs.BuffEWPetGermy.mGuid,
            Buffs.BuffEWTummyTrouble.mGuid,
            Buffs.BuffEWPetstilence.mGuid,
            Buffs.BuffEWPetPneumonia.mGuid
        };

        // Wound buffs from Pet Fighting mod
        public static BuffNames buffNameGraveWound = (BuffNames)0x384B537AE0B8F97A;
        public static BuffNames buffNameSeriousWound = (BuffNames)0xAE4D28F1BCEC603D;
        public static BuffNames buffNameMinorWound = (BuffNames)0x3BE0F368D4653A9E;
        public static BuffNames[] woundBuffList = new BuffNames[] { buffNameGraveWound,
            buffNameMinorWound, buffNameSeriousWound };

        public static string LocalizeStr(string name, params object[] parameters)
        {
            return Localization.LocalizeString("Echoweaver/PetDisease:" + name, parameters);
        }

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
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
                        s.AddInteraction(Buffs.BuffEWPetstilence.Stagger.Singleton, true);
                        s.AddInteraction(Buffs.BuffEWPetPneumonia.Wheeze.Singleton, true);
                        s.AddInteraction(EWPetSuccumbToDisease.Singleton, true);
                        s.AddInteraction(Buffs.BuffEWPetstilence.ActWacky.Singleton, true);
                    }
                }
            }

            // Replace Seasons pet germy with this germy moodlet
            EventTracker.AddListener(EventTypeId.kGotBuff, new ProcessEventDelegate(PetDiseaseManager.OnGotBuff));

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
            EventTracker.AddListener(EventTypeId.kAteFromPetBowl, new ProcessEventDelegate(PetDiseaseManager
                .OnAteFromBowl));
            EventTracker.AddListener(EventTypeId.kAteHarvestable, new ProcessEventDelegate(PetDiseaseManager
                .OnAteHarvestable));

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
            //EventTracker.AddListener(EventTypeId.kMetSim, new ProcessEventDelegate(PetDiseaseManager
            //.OnMetSim));

            // Fix cat ghost (remove inappropriate effects) -- Currently does not work
            //EventTracker.AddListener(EventTypeId.kSimInstantiated, new ProcessEventDelegate(FixGhost));

            if (kPetDiseaseDebug)
            {
                AlarmManager.Global.AddAlarm(10f, TimeUnit.Seconds, NotifyDebugState, "Notify that debug is on",
                    AlarmType.NeverPersisted, null);
            }

        }

        public static ListenerAction FixGhost(Event e)
        {
            // Weirdly, for a SimInstantiated event, the sim is in TargetObject.
            Sim s = e.TargetObject as Sim;
            if (s != null)
            {
                if (s.SimDescription.IsGhost && s.IsPet)
                {
                    if (s.SimDescription.DeathStyle == kDiseaseDeathType)
                    {
                        DebugNote("Pet disease ghost has spawned.");
                        World.ObjectSetGhostState(s.ObjectId, 23u, (uint)s.SimDescription.AgeGenderSpecies);
                        Urnstone urn = GetUrnstoneForGhost(s);
                        //if (urn != null)
                        //{
                        //    DebugNote("Trying to remove death effect.");
                        //urn.mDeathEffect.Stop();
                        //    urn.GhostTurnDeathEffectOff(VisualEffect.TransitionType.HardTransition);
                        //} else
                        //{
                        //    DebugNote("Pet disease urnstone not found.");
                        //}
                    }
                }
            } else
            {
                DebugNote("Sim spawned. Target is null. ");
            }

            return ListenerAction.Keep;
        }

        public static Urnstone GetUrnstoneForGhost(Sim s)
        {
            Urnstone[] objects = Queries.GetObjects<Urnstone>();
            DebugNote(objects.Length + "Urnstones in world.");
            foreach (Urnstone urn in objects)
            {
                if (object.ReferenceEquals(urn.DeadSimsDescription, s.SimDescription))
                {
                    DebugNote("Urn reference matches simdescription");
                    return urn;
                }
            }
            DebugNote("No urn matches");
            return null;
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