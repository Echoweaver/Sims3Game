using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
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
        // Word on the street is that ghost shaders don't require the associated EP.
        public static SimDescription.DeathType fightDeathType = SimDescription.DeathType.MummyCurse;

        public static BuffNames buffNamePetGermy = (BuffNames)0x9086F0050AC3673Dul;
        public static BuffNames buffNamePetPnumonia = (BuffNames)0x904F100B14974699ul;
        public static BuffNames buffNamePetstilence = (BuffNames)0x7768716F913C2054ul;
        public static BuffNames buffNameTummyTrouble = (BuffNames)0xDFF72BA95943E99Dul;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            AddEnumValue<SkillNames>("EWPetFightingSkill", EWPetFightingSkill.skillNameID);
            AddEnumValue<CommodityKind>("SkillEWPetFight", EWPetFightingSkill.commodityKindID);

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
            ChaseMean.Singleton = new EWChaseMean.EWChaseMeanDefinition();
            ChasePlay.Singleton = new EWChasePlay.EWChasePlayDefinition();
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            LoadSocialData("EWPetFighting_SocialData");

            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                AddInteraction(s);
            }
            EventTracker.AddListener(EventTypeId.kSimPassedOut, new ProcessEventDelegate(OnSimPassedOut));
            EventTracker.AddListener(EventTypeId.kSimInstantiated, new ProcessEventDelegate(OnSimInstantiated));
            EventTracker.AddListener(EventTypeId.kGotBuff, new ProcessEventDelegate(OnGotBuff));


            if (Tunables.kPetFightingDebug)
            {
                AlarmManager.Global.AddAlarm(10f, TimeUnit.Seconds, NotifyDebugState, "Notify that debug is on",
                    AlarmType.NeverPersisted, null);
            }
        }

        public static void AddInteraction(Sim s)
        {
            if (s != null)
            {
                s.AddInteraction(EWChaseOffLot.Singleton, true);
                if (s.IsCat || s.IsADogSpecies)
                {

                    s.AddInteraction(EWTakePetToVetWounds.Singleton, true);

                }
                else if (s.IsHuman)
                {
                    s.AddInteraction(EWPetAttackSim.Singleton, true);
                }
            }
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
                    ParserFunctions.TryParseEnum<CommodityTypes>(element.GetAttribute("com"),
                        out types, CommodityTypes.Undefined);
                    ActionData data = new ActionData(element.GetAttribute("key"),
                        types, ProductVersion.BaseGame, table, isEp5Installed);
                    ActionData.Add(data);
                }
            }
        }

        public static ListenerAction OnSimInstantiated(Event e)
        {
            // Check to see if pet sims have same passed out event
            Sim s = e.Actor as Sim;
            AddInteraction(s);

            return ListenerAction.Keep;
        }

        public static ListenerAction OnSimPassedOut(Event e)
        {
            // Check to see if pet sims have same passed out event
            Sim targetPet = e.Actor as Sim;

            if (targetPet.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
            {
                // Passing out with a Grave Wound means dying of the wound
                EWPetSuccumbToWounds die = EWPetSuccumbToWounds.Singleton.CreateInstance(targetPet, targetPet,
                    new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false) as EWPetSuccumbToWounds;
                targetPet.InteractionQueue.AddNext(die);
                if (Tunables.kAllowPetDeath)
                {
                    return ListenerAction.Remove;
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGotBuff(Event e)
        {
            Sim targetPet = e.Actor as Sim;

            // Starving pet with Grave Wound active dies/succumbs to wound.
            if (targetPet.BuffManager.HasElement(BuffNames.StarvingPet) &&
               targetPet.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
            {
                DebugNote("DEBUG: Buff is PetStarving and sim has Grave Wound. Die.");
                EWPetSuccumbToWounds die = EWPetSuccumbToWounds.Singleton.CreateInstance(targetPet, targetPet,
                    new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false) as EWPetSuccumbToWounds;
                targetPet.InteractionQueue.AddNext(die);
                return ListenerAction.Remove;
            }
            return ListenerAction.Keep;
        }

        public static void NotifyDebugState()
        {
            DebugNote("Pet Fighting Debug Mode ON");
        }

        public static void DebugNote(string str)
        {
            if (Tunables.kPetFightingDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
                    .NotificationStyle.kDebugAlert));
            }
        }

        public static void AddEnumValue<T>(string key, object value) where T : struct
        {
            Type typeFromHandle = typeof(T);
            if (!ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(typeFromHandle, out EnumParser ciparser))
            {
                ciparser = new EnumParser(typeFromHandle, ignoreCase: true);
                ParserFunctions.sCaseInsensitiveEnumParsers.Add(typeFromHandle, ciparser);
            }
            if (!ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(typeFromHandle, out EnumParser csparser))
            {
                csparser = new EnumParser(typeFromHandle, ignoreCase: false);
                ParserFunctions.sCaseSensitiveEnumParsers.Add(typeFromHandle, csparser);
            }
            if (!ciparser.mLookup.ContainsKey(key.ToLowerInvariant()) && !csparser.mLookup.ContainsKey(key))
            {
                ciparser.mLookup.Add(key.ToLowerInvariant(), value);
                csparser.mLookup.Add(key, value);
            }
        }
    }
}