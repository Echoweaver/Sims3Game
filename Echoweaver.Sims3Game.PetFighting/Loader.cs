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

namespace Echoweaver.Sims3Game.PetFighting
{
    public class Loader
    {
//        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            // Load custom buffs
            (new BuffBooter()).LoadBuffData();

            //if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            //HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x494F3A8118D98C44, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            // Add listeners for the events you care about
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
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
                if (cevent.SocialName == "Fight Pet")  // How come kFight is its own ID, but pet fights are not? Bleh.
                {
                    StyledNotification.Show(new StyledNotification.Format("Actor: " + cevent.Actor.Name + " won fight " +
                        cevent.ActorWonFight, StyledNotification.NotificationStyle.kGameMessagePositive));
                    cevent.Actor.BuffManager.AddElement(BuffEWMinorWound.StaticGuid,
                        (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
                    if (cevent.ActorWonFight)
                    {
                        EWPetFightingSkill simFighting = e.Actor.SkillManager.GetElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                        simFighting.wonFight();
                        simFighting.AddPoints(3.0f);
                    }
                    else
                    {
                        EWPetFightingSkill simFighting = e.Actor.SkillManager.GetElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                        simFighting.lostFight();
                        simFighting.AddPoints(3.0f);
                    }

                }
                else
                {
                    // Pounce Play, Chase Play, PlayPetToPet, Sniff
                    // Human - Pet: Let Sniff Hand
                    StyledNotification.Show(new StyledNotification.Format("Social Actor: " + cevent.Actor.Name +
                        ", SocialName: " + cevent.SocialName, StyledNotification.NotificationStyle.kGameMessagePositive));
                }
            }
            return ListenerAction.Keep;
        }


    }
}
