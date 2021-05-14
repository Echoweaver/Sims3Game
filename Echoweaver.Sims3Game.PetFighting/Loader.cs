using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System.Diagnostics;
using static Sims3.Gameplay.Actors.Sim;
using System.Xml;
using Sims3.Gameplay.Socializing;
using System.Collections.Generic;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        [Tunable]
        public static bool kAllowPetDeath = true;
        public static SimDescription.DeathType fightDeathType = SimDescription.DeathType.Starve;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinished);
        }

        public static void OnPreLoad()
        {
            // Load custom buffs
            (new BuffBooter()).LoadBuffData();

            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x494F3A8118D98C44, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);

        }

        public static void OnWorldLoadFinished(object sender, System.EventArgs e)
        {
            LoadSocialData("EWPetFighting_SocialData");
            LoadSocializingActionAvailability("EWPetFighting_Availability");

            FightPet.Singleton = EWFightPet.Singleton;

            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                if (s.IsCat || s.IsADogSpecies)
                {
                    foreach (InteractionObjectPair pair in s.Interactions)
                    {
                        if (pair.InteractionDefinition.GetType() == EWFightPet.Singleton.GetType())
                        {
                            break;
                        }
                    }
                    s.AddInteraction(EWFightPet.Singleton);
                }
                if (s.IsHuman)
                {
                    foreach (InteractionObjectPair pair in s.Interactions)
                    {
                        if (pair.InteractionDefinition.GetType() == EWPetAttackSim.Singleton.GetType())
                        {
                            break;
                        }
                    }
                    s.AddInteraction(EWPetAttackSim.Singleton);
                }
            }
            // Add listeners for the events you care about
            // EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
            EventTracker.AddListener(EventTypeId.kSimPassedOut, new ProcessEventDelegate(OnSimPassedOut));
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
                    ParserFunctions.TryParseEnum(element.GetAttribute("com"), out types, CommodityTypes.Undefined);
                    ActionData data = new ActionData(element.GetAttribute("key"), types, ProductVersion.BaseGame, table, isEp5Installed);
                    ActionData.Add(data);
                }
            }
            if(GameUtils.IsInstalled(ProductVersion.EP9))
            {
                fightDeathType = SimDescription.DeathType.BluntForceTrauma;
            } else if (GameUtils.IsInstalled(ProductVersion.EP2))
            {
                fightDeathType = SimDescription.DeathType.Meteor;
            }
        }

        public static void LoadSocializingActionAvailability(string spreadsheet)
        {
            XmlDbData xdb = XmlDbData.ReadData(spreadsheet);
            if (xdb != null)
            {
                if (xdb.Tables.ContainsKey("SAA"))
                {
                    SocialManager.ParseStcActionAvailability(xdb);
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
                foreach(StackFrame f in test_trace.GetFrames())
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
            Sim actor = (Sim)e.Actor;
            if (actor.BuffManager.HasElement(BuffEWGraveWound.StaticGuid)) {
                // Pets who pass out with a Grave Wound have succumbed to their wound
                BuffEWGraveWound.Succumb(actor);
            }
            return ListenerAction.Keep;
        }
    }
}