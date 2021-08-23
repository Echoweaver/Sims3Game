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
    public class BreastfeedBaby : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, BreastfeedBaby>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "Breastfeed Baby";
            }
        }
        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            SocialInteractionB interactionB = new CarriedChildInteractionB.Definition("BeGivenBottle")
                .CreateInstance(Actor, Target, GetPriority(), EffectivelyAutonomous, CancellableByPlayer) as SocialInteractionB;

            if (!ChildUtils.StartInteractionWithCarriedChild(this, interactionB))
            {
                return false;
            }

            // Switch out the state machine used by CarryingChildPosture to my custom one
            StateMachineClient feedMachine = AcquireBreastfeedStateMachine(Actor, Target);
            ChildUtils.StartCarry(Actor, Target, feedMachine, true);

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Nurse");

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Exit");

            FinishLinkedInteraction(); 
            WaitForSyncComplete(3);  // Not sure why the ChildUtils finish method did not work here.

            // Restore the EA's child carry state machine (JAZZ script "CarryToddler")
            StateMachineClient carryMachine = ChildUtils.AcquireCarryStateMachine(Actor, Target);
            ChildUtils.StartCarry(Actor, Target, carryMachine, false);

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