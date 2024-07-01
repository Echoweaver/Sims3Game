using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats.Apprentice
{
    public abstract class EWAbstractMentor : Interaction<Sim, Sim>
    {
        public static SkillNames skillName;
        public static Skill skillMentor;
        public static Skill skillStudent;
        public static int remainingRepeats = 5;
        public static string[] speechBallons;

        public static InteractionDefinition DemonstrateDefinition;

        public override bool Run()
        {
            skillMentor = Actor.SkillManager.GetElement(skillName);
            skillStudent = Target.SkillManager.GetElement(skillName);

            if (skillMentor == null)
            {
                DebugNote("Mentor " + skillMentor.Name + ": Mentor's skill is unaccountably NULL.");
                return false;
            }
            if (skillStudent == null)
            {
                skillStudent = Target.SkillManager.AddElement(skillName);
                if (skillStudent == null)
                {
                    DebugNote("Mentor" + skillStudent.Name + ": Student's skill is unaccountably NULL.");
                    return false;
                }
            }
            DiscussSkill.Definition definition2 = new DiscussSkill.Definition();
            InteractionInstance discuss = definition2.CreateInstance(Target, Actor,
                new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                true);
            Actor.InteractionQueue.AddNext(discuss);
            return true;
        }

        public class DiscussSkill : SocialInteraction
        {
            public class Definition : InteractionDefinition<Sim, Sim, DiscussSkill>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

            }
            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, "Listen",
                    allowCarryChild: false), pairedSocial: true, doCallOver: true))
                {
                    DebugNote("Discuss Skill " + skillStudent.Name + ": BeginSocialInteraction failed");
                    return false;
                }
                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                EnterStateMachine("Socialize", "Enter", "x", "y");
                string randomBalloon1 = RandomUtil.GetRandomStringFromList(speechBallons);
                ThoughtBalloonManager.BalloonData balloonData1 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
                balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData1.Duration = ThoughtBalloonDuration.Short;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData1);
                randomBalloon1 = RandomUtil.GetRandomStringFromList(speechBallons);
                ThoughtBalloonManager.BalloonData balloonData2 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
                balloonData2.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData2.Duration = ThoughtBalloonDuration.Short;
                Target.ThoughtBalloonManager.ShowBalloon(balloonData2);

                mCurrentStateMachine.RequestState(false, "x", "Exit");
                mCurrentStateMachine.RequestState(true, "y", "Exit");
                FinishLinkedInteraction();

                if (skillMentor.SkillLevel <= (skillStudent.SkillLevel + 1))
                {
                    Actor.ShowTNSIfSelectable("This apprentice has learned everything the mentor can teach.",
                        NotificationStyle.kGameMessageNegative);
                }
                else
                {
                    DebugNote("Mentor skill " + skillStudent.Name + " remaining repeats = " + remainingRepeats);
                    if (remainingRepeats > 0 && skillMentor.SkillLevel > skillStudent.SkillLevel)
                    {
                        DemonstrateSkill demo = DemonstrateDefinition.CreateInstance(Target, Actor,
                            new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                            true) as DemonstrateSkill;
                        WatchSkill.Definition watchDef = new WatchSkill.Definition();
                        WatchSkill watch = watchDef.CreateInstance(Actor, Target,
                            new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                            true) as WatchSkill;
                        demo.waitInstance = watch;

                        Actor.InteractionQueue.TryPushAsContinuation(this, demo);
                        Target.InteractionQueue.TryPushAsContinuation(this, watch);
                    }
                }
                skillStudent.StopSkillGain();
                return true;
            }
        }

        public abstract class DemonstrateSkill : Interaction<Sim, Sim>
        {

            public abstract bool DemonstrateAnim();

            public WatchSkill waitInstance;


            // TODO: Localize!

            public override bool Run()
            {
                DebugNote("Begin Demonstrate ");

                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                // TODO: Not sure what, if anything, I want to do if the animation fails
                bool success = DemonstrateAnim();
                waitInstance.demoComplete = true;
                skillStudent.StopSkillGain();
                return true;
            }
        }

        public class WatchSkill : Interaction<Sim, Sim>
        {
            public class Definition : InteractionDefinition<Sim, Sim, WatchSkill>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    // Localize!
                    return "Watch " + skillStudent.Name;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            [Tunable]
            [TunableComment("Approximate distance to sim being watched")]
            public static float kRouteDistance = 2f;

            [Tunable]
            [TunableComment("Distance beyond kRouteDistance a watching sim can be before they try to re-route")]
            public static float kMaxViewingRing = 1f;

            [TunableComment("Minutes between watching sim reacting")]
            [Tunable]
            public static float kTimeBetweenReactions = 2f;

            public bool demoComplete = false;

            public override bool Run()
            {
                DebugNote("Begin Watch " + skillStudent.Name);
                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                if (!Actor.RoutingComponent.RouteToDynamicObjectRadius(Target, 0, kRouteDistance, null))
                {
                    DebugNote("Watch " + skillStudent.Name + ": RouteToDynamicObjectRadius failed");
                    return false;
                } else
                {
                    Actor.RouteTurnToFace(Target.Position);
                }
                StandardEntry();
                BeginCommodityUpdates();
                bool flag = DoLoop(ExitReason.Default, LoopFunc, null, kTimeBetweenReactions);
                EndCommodityUpdates(flag);
                StandardExit();
                skillStudent.StopSkillGain();
                return flag;
            }

            public void LoopFunc(StateMachineClient smc, LoopData ld)
            {

                ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(Target.GetThumbnailKey());
                balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
                balloonData.mPriority = ThoughtBalloonPriority.Low;
                balloonData.mFlags = ThoughtBalloonFlags.ShowIfSleeping;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
                if (demoComplete)
                {
                    DebugNote("Watch " + skillStudent.Name + " is complete");
                    Actor.AddExitReason(ExitReason.Finished);
                    Repeat();
                    return;
                }
                if (Actor.GetDistanceToObject(Target) > kRouteDistance + kMaxViewingRing
                    && !Actor.RoutingComponent.RouteToDynamicObjectRadius(Target, kRouteDistance,
                    null, null))
                {
                    DebugNote("Watch " + skillStudent.Name + " routing failed.");
                    Actor.AddExitReason(ExitReason.Finished);
                    Repeat();
                    return;
                }
            }

            public void Repeat()
            {
                --remainingRepeats;
                DiscussSkill.Definition definition2 = new DiscussSkill.Definition();
                InteractionInstance discuss = definition2.CreateInstance(Target, Actor,
                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                    true);
                Actor.InteractionQueue.TryPushAsContinuation(this, discuss);
            }
        }
    }    
}
        

