using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWGradateApprentice : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWGradateApprentice>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return HasApprentice(a, target);
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                //return LocalizeStr("?");
                // TODO: Localize!
                return "Graduate Apprentice";
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

            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, "Become Apprentice",
                allowCarryChild: false), pairedSocial: true, doCallOver: true))
            {
                return false;
            }

            GraduateApprentice(Target);
            Actor.LookAtManager.SetInteractionLookAt(Target, Sim.LookAtPriorityForSocializingSim,
                (LookAtJointFilter)3);
            Target.LookAtManager.SetInteractionLookAt(Actor, Sim.LookAtPriorityForSocializingSim,
                (LookAtJointFilter)3);
            EnterStateMachine("Socialize", "Enter", "x", "y");
            BeginCommodityUpdates();
            ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData("balloon_trait_brave");
            balloonData.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
            balloonData.Duration = ThoughtBalloonDuration.Short;
            Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
            Target.ThoughtBalloonManager.ShowBalloon(balloonData);
            mCurrentStateMachine.RequestState(false, "x", "Socialize");
            mCurrentStateMachine.RequestState(true, "y", "Socialize");
            mCurrentStateMachine.RequestState(false, "x", "Exit");
            mCurrentStateMachine.RequestState(true, "y", "Exit");
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target,
                "Enlist Apprentice", wasRecipient: false, wasAccepted: true, actorWonFight: false,
                CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor,
                "Enlist Apprentice", wasRecipient: true, wasAccepted: true, actorWonFight: false,
                CommodityTypes.Undefined));
            EndCommodityUpdates(true);
            FinishLinkedInteraction();
            WaitForSyncComplete();
            RenameSim(Target, "Congratulations! This sim has graduated from apprenticeship. Change their name if applicable:");

            SocialInteractionA.Definition definition2 = new SocialInteractionA.Definition("Sniff",
                new string[0], null, initialGreet: false);
            InteractionInstance acceptInteraction = definition2.CreateInstance(Target, Actor,
                new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                true);
            Actor.InteractionQueue.TryPushAsContinuation(this, acceptInteraction);

            Cleanup();
            return true;
        }
    }
}

