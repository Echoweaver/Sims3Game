using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Controller;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.Actors.Sim;
using static Sims3.UI.CAS.CASFamilyScreen;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWEnlistApprentice : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWEnlistApprentice>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (a == target)
                    return false;
                if (HasApprentice(a, target))
                    return false;
                if (!CanTakeApprentice(a))
                {
                    if (kPetWarriorDebug)
                    {
                        // TODO: Localize!
                        greyedOutTooltipCallback = CreateTooltipCallback("Actor cannot take an apprentice");
                    }
                    return false;
                }
                if (!CanBeApprenticed(target))
                {
                    if (kPetWarriorDebug)
                    {
                        // TODO: Localize!
                        greyedOutTooltipCallback = CreateTooltipCallback("Target is not available as an apprentice");
                    }
                    return false;
                }
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                //return LocalizeStr("?");
                // TODO: Localize!
                return "Enlist Apprentice";
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

            AddApprentice(Actor, Target);
            Actor.LookAtManager.SetInteractionLookAt(Target, LookAtPriorityForSocializingSim, (LookAtJointFilter)3);
            Target.LookAtManager.SetInteractionLookAt(Actor, LookAtPriorityForSocializingSim, (LookAtJointFilter)3);
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

