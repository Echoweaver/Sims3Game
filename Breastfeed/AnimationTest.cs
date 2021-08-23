using System;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.Breastfeed
{
    public class AnimationTest : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, AnimationTest>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "Test Animation";
            }
        }
        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            StyledNotification.Show(new StyledNotification.Format("Run",
                StyledNotification.NotificationStyle.kDebugAlert));

            if (!(Actor.Posture is CarryingChildPosture) )
            {
                ChildUtils.CarryChild(Actor, Target, false);
            }
            StyledNotification.Show(new StyledNotification.Format("Carried",
                StyledNotification.NotificationStyle.kDebugAlert));

            SocialInteractionB interactionB = new CarriedChildInteractionB.Definition("BeGivenBottle")
                .CreateInstance(Actor, Target, GetPriority(), EffectivelyAutonomous, CancellableByPlayer) as SocialInteractionB;
            StyledNotification.Show(new StyledNotification.Format("Have Interaction B",
                StyledNotification.NotificationStyle.kDebugAlert));

            if (!ChildUtils.StartInteractionWithCarriedChild(this, interactionB))
            {
                return false;
            }
            StyledNotification.Show(new StyledNotification.Format("Started Interaction",
                StyledNotification.NotificationStyle.kDebugAlert));

            StateMachineClient feedMachine = AcquireBreastfeedStateMachine(Actor, Target);
            StyledNotification.Show(new StyledNotification.Format("Retrieved Breastfeed State Machine",
                StyledNotification.NotificationStyle.kDebugAlert));

            ChildUtils.StartCarry(Actor, Target, feedMachine, true);
            StyledNotification.Show(new StyledNotification.Format("Start Carry",
                StyledNotification.NotificationStyle.kDebugAlert));

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Nurse");
            StyledNotification.Show(new StyledNotification.Format("Animated Interaction",
                StyledNotification.NotificationStyle.kDebugAlert));

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Exit");
            StyledNotification.Show(new StyledNotification.Format("Exited Interaction",
                StyledNotification.NotificationStyle.kDebugAlert));

            FinishLinkedInteraction(); 
            WaitForSyncComplete(3);  // Not sure why the ChildUtils finish method did not work here.

            //ChildUtils.FinishInteractionWithCarriedChild(this);
            StyledNotification.Show(new StyledNotification.Format("Finished Interaction",
                StyledNotification.NotificationStyle.kDebugAlert));

            StateMachineClient carryMachine = ChildUtils.AcquireCarryStateMachine(Actor, Target);
            ChildUtils.StartCarry(Actor, Target, carryMachine, false);
            StyledNotification.Show(new StyledNotification.Format("Restored CarryStateMachine",
                StyledNotification.NotificationStyle.kDebugAlert));

            return true;
        }

        public static StateMachineClient AcquireBreastfeedStateMachine(Sim parent, Sim child)
        {
            StateMachineClient val = StateMachineClient.Acquire(parent, "breastfeed_carry");
            val.SetActor("x", parent);
            val.SetActor("y", child);
            val.EnterState("x", "Enter");
            val.EnterState("y", "Enter");
            return val;
        }
    }

}