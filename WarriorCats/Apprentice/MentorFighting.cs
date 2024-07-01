using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.WarriorCats.Apprentice
{
    public class MentorFighting : ChasePlay
    {
        public class MentorFightingDefinition : ChasePlayDefinition
        {
            public MentorFightingDefinition() : base()
            {
                ActionKey = "Mentor Fighting";
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                MentorFighting chaseBaseClass = new MentorFighting();
                chaseBaseClass.Init(ref parameters);
                chaseBaseClass.IsMeanChase = false;
                return chaseBaseClass;
            }

            public override string[] GetPath(bool isFemale)
            {
                // TODO: Localize!!
                return new string[1]
                {
                    "Apprentice..."
                };
            }

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!HasApprentice(actor, target))
                    return false;
                if (!actor.SkillManager.HasElement(FightingSkillName))
                    return false;
                if (target.SkillManager.HasElement(FightingSkillName))
                {
                    // Can only mentor up to one skill below the teacher
                    if ((target.SkillManager.GetElement(FightingSkillName).SkillLevel + 1) >=
                        actor.SkillManager.GetElement(FightingSkillName).SkillLevel)
                    {
                        // TODO: Localize!
                        greyedOutTooltipCallback = CreateTooltipCallback("This apprentice has learned everything you can teach right now");
                        return false;
                    }
                }
                return true;
            }
        }

        public static new InteractionDefinition Singleton = new MentorFightingDefinition();

        public override string SocialName => "Mentor Fighting";

        Skill skillMentor;
        Skill skillStudent;
        bool actorIsMentor = true;

        public override bool Run()
        {
            if (actorIsMentor)
            {
                skillMentor = Actor.SkillManager.GetElement(FightingSkillName);
                skillStudent = Target.SkillManager.GetElement(FightingSkillName);
            }
            else
            {
                skillStudent = Actor.SkillManager.GetElement(FightingSkillName);
                skillMentor = Target.SkillManager.GetElement(FightingSkillName);
            }

            if (skillMentor == null)
            {
                return false;
            }
            if (skillStudent == null)
            {
                skillStudent = Target.SkillManager.AddElement(FightingSkillName);
                if (skillStudent == null)
                {
                    return false;
                }
            }

            skillStudent.StartSkillGain(kApprenticeSkillGainRate);
            bool returnVal = base.Run();
            DiscussFighting();
            skillStudent.StopSkillGain();
            return returnVal;
        }

        public void DiscussFighting()
        {
            string[] kSpeechBallons = new string[] { "balloon_yeti", "balloon_trait_evil",
                    "balloon_moodlet_horror", "balloon_trait_brave", "balloon_moodlet_sore", "balloon_moodlet_stress",
                    "balloon_question"};
            EnterStateMachine("Socialize", "Enter", "x", "y");
            string randomBalloon1 = RandomUtil.GetRandomStringFromList(kSpeechBallons);
            ThoughtBalloonManager.BalloonData balloonData1 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
            balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
            //balloonData.LowAxis = (RandomUtil.CoinFlip() ? ThoughtBalloonAxis.kLike : ThoughtBalloonAxis.kDislike);
            balloonData1.Duration = ThoughtBalloonDuration.Short;
            if (actorIsMentor)
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData1);
            else Target.ThoughtBalloonManager.ShowBalloon(balloonData1);
            mCurrentStateMachine.RequestState(false, "x", "Socialize");
            mCurrentStateMachine.RequestState(true, "y", "Socialize");
            //string randomBalloon2 = RandomUtil.GetRandomStringFromList(kSpeechBallons);
            ThoughtBalloonManager.BalloonData balloonData2 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
            balloonData2.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
            balloonData2.Duration = ThoughtBalloonDuration.Short;
            if (actorIsMentor)
                Target.ThoughtBalloonManager.ShowBalloon(balloonData2);
            else Actor.ThoughtBalloonManager.ShowBalloon(balloonData2);
            mCurrentStateMachine.RequestState(false, "x", "Exit");
            mCurrentStateMachine.RequestState(true, "y", "Exit");
        }

        public override void RunPostChaseBehavior()
        {
            EnterStateMachine("ChasePlay", "Enter", "x", "y");
            BeginCommodityUpdates();
            AnimateJoinSims("Exit");
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "Chase Play", wasRecipient: false, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "Chase Play", wasRecipient: true, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
            EndCommodityUpdates(succeeded: true);
            if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default) && base.NumLoops > 0
                && skillMentor.SkillLevel > (skillStudent.SkillLevel + 1))
            {
                Sim sim = Actor;
                Sim target = Target;
                bool nextActorIsMentor = actorIsMentor;
                if (RandomUtil.CoinFlip())
                {
                    DebugNote("Swapping chaser");
                    sim = Target;
                    target = Actor;
                    nextActorIsMentor = !actorIsMentor;
                }
                MentorFightingFollowup mentorAction = MentorFightingFollowup.Singleton.CreateInstance(target, sim,
                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, true)
                    as MentorFightingFollowup;

                if (mentorAction != null)
                {
                    mentorAction.PreviouslyAccepted = true;
                    mentorAction.NumLoops = base.NumLoops - 1;
                    mentorAction.actorIsMentor = nextActorIsMentor;
                    sim.InteractionQueue.TryPushAsContinuation(sim.CurrentInteraction, mentorAction);
                }
            }
        }
    }

    public class MentorFightingFollowup : MentorFighting
    {
        public class MentorFightingFollowupDefinition : MentorFightingDefinition
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                MentorFightingFollowup chaseBaseClass = new MentorFightingFollowup();
                chaseBaseClass.Init(ref parameters);
                chaseBaseClass.IsMeanChase = false;
                return chaseBaseClass;
            }
        }
        public static new InteractionDefinition Singleton = new MentorFightingFollowupDefinition();
    }
}

