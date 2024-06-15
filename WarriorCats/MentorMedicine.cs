using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Echoweaver.Sims3Game.WarriorCats.MentorHerbLore;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class MentorMedicine : EWAbstractMentor
    {
        public class Definition : InteractionDefinition<Sim, Sim, MentorMedicine>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!HasApprentice(a, target))
                {
                    return false;
                }
                if (!a.SkillManager.HasElement(EWMedicineCatSkill.SkillNameID))
                {
                    return false;
                }
                if (target.SkillManager.HasElement(EWMedicineCatSkill.SkillNameID))
                {
                    if ((target.SkillManager.GetElement(EWMedicineCatSkill.SkillNameID).SkillLevel + 1) >=
                        a.SkillManager.GetElement(EWMedicineCatSkill.SkillNameID).SkillLevel)
                    {
                        // TODO: Localize!
                        greyedOutTooltipCallback = CreateTooltipCallback("This apprentice has learned everything you can teach right now");
                        return false;
                    }
                }
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                //return LocalizeStr("?");
                // TODO: Localize!
                return "Mentor Medicine";
            }

            public override string[] GetPath(bool isFemale)
            {
                // TODO: Localize!!
                return new string[1] {
                    "Apprentice..."
                };
            }
        }


        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            skillName = EWMedicineCatSkill.SkillNameID;
            remainingRepeats = 5;
            speechBallons = new string[] { "ep10_balloon_clean", "ep10_balloon_dirty",
                "ep11_balloon_meteor", "ep5_balloon_fleas", "ep5_balloon_grave", "ep5_balloon_kittens",
                "balloon_flies", "balloon_star", "balloon_question", "balloon_woohoo",
                "ep5_balloon_putmedown"
            };

            DemonstrateDefinition = new DemonstrateMedicine.Definition();
            return base.Run();
        }

        public class DemonstrateMedicine : DemonstrateSkill
        {
            public class Definition : InteractionDefinition<Sim, Sim, DemonstrateMedicine>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

            }

            public static InteractionDefinition Singleton = new Definition();

            ResourceKey[] thumbs = {
                new ResourceKey(0x764DD7AAAF84FF8F, 0x00B2D882, 0), //ep1_balloon_bandage
                //new ResourceKey(0x133F94232E1583FA, 0x2F7D0004, 0), // moodlet_kittens
                //new ResourceKey(0x989AE0A8448A2C6D, 0x2F7D0004, 0), // moodlet_meouch
                new ResourceKey(0x26F4420B337D0179, 0x00B2D882, 0),  // balloon_moodlet_min
                new ResourceKey(0x807BD979DDA3B605, 0x00B2D882, 0),  // balloon_moodlet_nausea
                new ResourceKey(0x053B3971FE3CFE76, 0x00B2D882, 0),   // balloon_moodlet_garlic
                new ResourceKey(0xD77D4F32E3596882, 0x00B2D882, 0), // balloon_moodlet_backache
                new ResourceKey(0xD75A3EBD8E7FB920, 0x00B2D882, 0)  // ep5_balloon_? (claw)
                //new ResourceKey(0xF26DC5E259050B26, 0x2F7D0004, 0)  // moodlet_EWCatGraveWound
            };
            public override bool DemonstrateAnim()
            {
                PetSittingOnGroundPosture.SitOnGround(Actor);
                Actor.Posture = Actor.PetSittingOnGround;
                ThumbnailKey key = new ThumbnailKey(RandomUtil.GetRandomObjectFromList(thumbs),
                    ThumbnailSize.Medium);
                ThoughtBalloonManager.BalloonData balloonData1 = new ThoughtBalloonManager.BalloonData(key);
                balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData1.Duration = ThoughtBalloonDuration.Short;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData1);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_start_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop2_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop2_x", ProductVersion.EP5);

                ThumbnailKey key2 = new ThumbnailKey(RandomUtil.GetRandomObjectFromList(thumbs),
                    ThumbnailSize.Medium);
                ThoughtBalloonManager.BalloonData balloonData2 = new ThoughtBalloonManager.BalloonData(key2);
                balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData1.Duration = ThoughtBalloonDuration.Short;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData2);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop2_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_loop2_x", ProductVersion.EP5);
                Actor.PlaySoloAnimation("ac_idle_sit_meow_stop_x", ProductVersion.EP5);
                ((PetSittingOnGroundPosture)Actor.Posture).Stand();
                return true;
            }
        }
    }
}

