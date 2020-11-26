using System;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.PetObjects;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game
{
    public class Warriorcats
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Warriorcats()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            // Load custom buffs
            (new BuffBooter()).LoadBuffData();

            //try
            //{
                if (HasBeenLoaded) return; // you only want to run it once per gameplay session
                HasBeenLoaded = true;

                // fill this in with the resourcekey of your SKIL xml
                XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x67503AA43670DA12, 0xA8D58BE5, 0x00000000), false);

                if (data == null)
                {
                    return;
                }
                SkillManager.ParseSkillData(data, true);

            //}
            //catch (Exception ex)
            //{
            //    return;
            //}
        }



        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {

            // Add listeners for the events you care about
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
            EventTracker.AddListener(EventTypeId.kFighting, new ProcessEventDelegate(OnFight));
            EventTracker.AddListener(EventTypeId.kPreyTypeCaught, new ProcessEventDelegate(OnPreyTypeCaught));
            EventTracker.AddListener(EventTypeId.kPreyRarityCaught, new ProcessEventDelegate(OnPreyRarityCaught));
            EventTracker.AddListener(EventTypeId.kGoHuntingCat, new ProcessEventDelegate(OnGoHuntingCat));
            EventTracker.AddListener(EventTypeId.kGoFishingCat, new ProcessEventDelegate(OnGoFishingCat));
            // kInventoryObjectAdded for hunt failure?
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnInventoryObjectAdded));

            // kHunt, kSoloHunted (probably Vampire or Werewolf), kCaughtFish, kCaughtMinorPet, kCaughtBug, kGotBuff
        }

        public static ListenerAction OnSocialInteraction(Event e)
        {
            if (e is SocialEvent)
            {
                SocialEvent cevent = (SocialEvent)e;
                if (cevent.SocialName == "Fight Pet")  // How come kFight is its own ID, but pet fights are not? Bleh.
                {
                    StyledNotification.Show(new StyledNotification.Format("Actor: " + cevent.Actor.Name + " won fight " +
                        cevent.ActorWonFight, StyledNotification.NotificationStyle.kGameMessagePositive));
                    cevent.Actor.BuffManager.AddElement(BuffEWMinorWound.StaticGuid,
                        (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));

                } else
                {
                    // Pounce Play, Chase Play, PlayPetToPet, Sniff, GoHuntingCat (Does not involve catching)
                    // Human - Pet: Let Sniff Hand
                    StyledNotification.Show(new StyledNotification.Format("Social Actor: " + cevent.Actor.Name +
                        ", SocialName: " + cevent.SocialName, StyledNotification.NotificationStyle.kGameMessagePositive));
                }
            }
            return ListenerAction.Keep;
        }

        // Add wound or disease moodlets
        public static ListenerAction OnFight(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Fight Happened",
                StyledNotification.NotificationStyle.kGameMessagePositive));

            //StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
            //    + " fights!", StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPreyTypeCaught(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("PreyTypeCaught Happened: " + e.TargetObject.NameComponent.Name,
                StyledNotification.NotificationStyle.kGameMessagePositive));
            //StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
            //    + " CaughtPreyType!", StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPreyRarityCaught(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("PreyRarityCaught: " + e.TargetObject.NameComponent.Name,
                StyledNotification.NotificationStyle.kGameMessagePositive));
            //StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
            //    + " CaughtPreyRarity!", StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoHuntingCat(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
                + " GoHuntingCat: " + e.TargetObject.NameComponent.Name,
                StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoFishingCat(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("GoFishingCat Happened",
                StyledNotification.NotificationStyle.kGameMessagePositive));
            //StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
            //    + " CaughtPreyRarity!", StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnInventoryObjectAdded(Event e)
        {
            // I haven't figured out how to trap a hunting failure
            if (e.TargetObject is CatHuntFailureObject)
            {
                StyledNotification.Show(new StyledNotification.Format(e.Actor.Name + " Received failure object",
                    StyledNotification.NotificationStyle.kGameMessagePositive));
            }
            return ListenerAction.Keep;
        }

    }

    public class EWMedicineCatSkill : Skill
    {
        public EWMedicineCatSkill(SkillNames guid) : base(guid)
        {
        }
        private EWMedicineCatSkill()
        {
        }
    }

    public class EWCatFightSkill : Skill
    {
        public EWCatFightSkill(SkillNames guid) : base(guid)
        {
        }
        private EWCatFightSkill()
        {
        }
    }

    public class ExampleUsesClass : GameObject
    {
        private const SkillNames EWMedicineCatSkill = (SkillNames)0x277ECF3A;  // Hash32 of EWMedicineCatSkill

        public ExampleUsesClass()
        {
        }

        public void ExampleUses(Sim s)
        {
            if (!s.SkillManager.HasElement(EWMedicineCatSkill))
            {
                s.SkillManager.AddElement(EWMedicineCatSkill);
            }
            s.SkillManager.AddSkillPoints(EWMedicineCatSkill, 3.0f);
            Skill sk = s.SkillManager.GetElement(EWMedicineCatSkill);
            float sl = sk.SkillPoints;

        }

    }
}
