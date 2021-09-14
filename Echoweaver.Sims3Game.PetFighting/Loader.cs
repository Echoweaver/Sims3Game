using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.PetObjects;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System.Diagnostics;
using static Sims3.Gameplay.Actors.Sim;
using System.Xml;
using Sims3.Gameplay.Socializing;
using System.Collections.Generic;
using Sims3.Gameplay.CAS;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        [Tunable]
        public static bool kAllowPetDeath = true;
        // Word on the street is that ghost shaders don't require the associated EP.
        public static SimDescription.DeathType fightDeathType = SimDescription.DeathType.Thirst;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // Load custom buffs
            (new BuffBooter()).LoadBuffData();

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x494F3A8118D98C44, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);

            FightPet.Singleton = new EWFightPet.EWFightPetDefinition();
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            LoadSocialData("EWPetFighting_SocialData");

            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                if (s.IsCat || s.IsADogSpecies)
                {
                    s.AddInteraction(EWKillNow.Singleton, true);
                }
                if (s.IsHuman)
                {
                    s.AddInteraction(EWPetAttackSim.Singleton, true);
                    s.AddInteraction(EWPetFightSim.Singleton, true);
                }
            }
            // Add listeners for the events you care about
            // EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
            EventTracker.AddListener(EventTypeId.kSimPassedOut, new ProcessEventDelegate(OnSimPassedOut));
            EventTracker.AddListener(EventTypeId.kSimDied, new ProcessEventDelegate(OnSimDied));
        }

        public static void LoadSocialData(string spreadsheet)
        {
            XmlDocument root = Simulator.LoadXML(spreadsheet);
            bool isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
            if (spreadsheet != null)
            {
                XmlElementLookup lookup = new XmlElementLookup(root);
                List<XmlElement> list = lookup["Action"];
                foreach (XmlElement element in list)
                {
                    CommodityTypes types;
                    XmlElementLookup table = new XmlElementLookup(element);
                    ParserFunctions.TryParseEnum<CommodityTypes>(element.GetAttribute("com"), out types, CommodityTypes.Undefined);
                    ActionData data = new ActionData(element.GetAttribute("key"), types, ProductVersion.BaseGame, table, isEp5Installed);
                    ActionData.Add(data);
                }
            }
        }

        public static ListenerAction OnSocialInteraction(Event e)
        {
            // Friendly interactions: Goof Around -- should gain fight skill
            // Mean interaction: Chase, Fight
            // Mean interaction with human: Chase, Attack
            // Interaction with some objects: Guard Object

            // New options -- chase off lot
            // Guard territory
            // Can't fight raccoon? Why not?
            if (e is SocialEvent)
            {
                SocialEvent cevent = (SocialEvent)e;

                // Pounce Play, Chase Play, PlayPetToPet, Sniff
                // Human - Pet: Let Sniff Hand
                StackTrace test_trace = new StackTrace();
                string test_output = "";
                foreach (StackFrame f in test_trace.GetFrames())
                {
                    test_output += " ||| " + f.GetMethod().ToString();
                }
                StyledNotification.Show(new StyledNotification.Format("Social Actor: " + cevent.Actor.Name +
                    ", SocialName: " + cevent.SocialName + "Stack: " + test_output,
                    StyledNotification.NotificationStyle.kGameMessagePositive));

            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnSimPassedOut(Event e)
        {
            // Check to see if pet sims have same passed out event
            StyledNotification.Show(new StyledNotification.Format("Passed Out Actor: " + e.Actor.Name,
                StyledNotification.NotificationStyle.kGameMessagePositive));

            return ListenerAction.Keep;
        }

        public static ListenerAction OnSimDied(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Sim Died Target: " + e.TargetObject.GetLocalizedName(),
                StyledNotification.NotificationStyle.kDebugAlert));
            // Human Statue is just a placeholder. The actual ghost on a cat is invisible!
            if (e.Actor.SimDescription.IsPet && e.Actor.SimDescription.DeathStyle == SimDescription.DeathType.HumanStatue)
            {
                StyledNotification.Show(new StyledNotification.Format("Sim Died Human Statue: " + e.Actor.Name,
                    StyledNotification.NotificationStyle.kDebugAlert));
                // Below must be done after gravestone is generated to use the Robot ghost
                World.ObjectSetGhostState(e.Actor.ObjectId, (uint)SimDescription.DeathType.Robot,
                    (uint)e.Actor.SimDescription.AgeGenderSpecies);
            }
            return ListenerAction.Keep;
        }
    }
}