using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.PetObjects;
using Sims3.Gameplay.Pools;
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

            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x67503AA43670DA12, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            // Add custom fishing interaction that uses custom fishing skill
            // TODO: Remove old interaction
            if (Terrain.Singleton != null)
            {
                Terrain.Singleton.AddInteraction(EWCatFishHere.Singleton);
            }

            // Add listeners for the events you care about
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
            EventTracker.AddListener(EventTypeId.kPreyTypeCaught, new ProcessEventDelegate(OnPreyTypeCaught));
            EventTracker.AddListener(EventTypeId.kPreyRarityCaught, new ProcessEventDelegate(OnPreyRarityCaught));
            EventTracker.AddListener(EventTypeId.kGoHuntingCat, new ProcessEventDelegate(OnGoHuntingCat));
            EventTracker.AddListener(EventTypeId.kGoFishingCat, new ProcessEventDelegate(OnGoFishingCat));
            // kInventoryObjectAdded for hunt failure?
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnInventoryObjectAdded));

            // kGotBuff
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
                    if (cevent.ActorWonFight)
                    {
                        EWCatFightSkill simFighting = e.Actor.SkillManager.GetElement(EWCatFightSkill.skillNameID) as EWCatFightSkill;
                        simFighting.wonFight();
                        simFighting.AddPoints(3.0f);
                    }
                    else
                    {
                        EWCatFightSkill simFighting = e.Actor.SkillManager.GetElement(EWCatFightSkill.skillNameID) as EWCatFightSkill;
                        simFighting.lostFight();
                        simFighting.AddPoints(3.0f);
                    }

                }
                else
                {
                    // Pounce Play, Chase Play, PlayPetToPet, Sniff
                    // Human - Pet: Let Sniff Hand
                    //StyledNotification.Show(new StyledNotification.Format("Social Actor: " + cevent.Actor.Name +
                    //    ", SocialName: " + cevent.SocialName, StyledNotification.NotificationStyle.kGameMessagePositive));
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPreyTypeCaught(Event e)
        {
            {
                StyledNotification.Show(new StyledNotification.Format("PreyTypeCaught: " + e.TargetObject.NameComponent.Name,
                    StyledNotification.NotificationStyle.kGameMessagePositive));

                EWCatFightSkill simFighting = e.Actor.SkillManager.GetElement(EWCatFightSkill.skillNameID) as EWCatFightSkill;
                simFighting.wonFight();
                simFighting.AddPoints(3.0f);
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnPreyRarityCaught(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("PreyRarityCaught: " + e.TargetObject.NameComponent.Name,
                StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoHuntingCat(Event e)
        {
            //StyledNotification.Show(new StyledNotification.Format("Actor " + e.Actor.Name
            //    + " GoHuntingCat: " + e.TargetObject.NameComponent.Name,
            //    StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
        }

        public static ListenerAction OnGoFishingCat(Event e)
        {
            //StyledNotification.Show(new StyledNotification.Format("GoFishingCat Happened Type: " + e.ToDetailedString(),
            //    StyledNotification.NotificationStyle.kSystemMessage));
            
            return ListenerAction.Keep;
        }

        public static ListenerAction OnInventoryObjectAdded(Event e)
        {
            // I haven't figured out how to trap a hunting failure.
            // Also, catching a fish doesn't trigger the prey caught events, so we can catch when fish added to inventory
            // If cat eats fish immediately this won't be triggered.

            if (e.TargetObject is CatHuntFailureObject)
            {
                EWCatFightSkill simFighting = e.Actor.SkillManager.GetElement(EWCatFightSkill.skillNameID) as EWCatFightSkill;
                simFighting.lostFight();
                simFighting.AddPoints(3.0f);
                StyledNotification.Show(new StyledNotification.Format(e.Actor.Name + " Received failure object",
                    StyledNotification.NotificationStyle.kGameMessagePositive));
            }
            return ListenerAction.Keep;
        }

    }
}
