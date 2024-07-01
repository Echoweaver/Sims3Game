using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.WarriorCats.HerbLore
{
    public class EWPetMarkPlant : Plant.ViewPlant
    {
        public new class Definition : Plant.ViewPlant.Definition
        {
            public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
//                return a.IsCat;
                return true;
            }

            public override string GetInteractionName(Sim actor, Plant target, InteractionObjectPair iop)
            {
                return "EWPetMarkPlant";

            }
        }

        public new static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            if (!Actor.RouteToPointRadialRange(Target.Position, Plant.kViewDistanceMin, Plant.kViewDistanceMax))
            {
                return false;
            }
            BeginCommodityUpdates();
            //AcquireStateMachine()
            Actor.PlaySoloAnimation("ac2a_soc_neutral_markSimAccept_friendly_neutral_x");
            mCurrentStateMachine.RequestState("X", ""); // Pseudocode
            //AcquireStateMachine("catdoginvestigate");
            //EnterStateMachine("catdoginvestigate", "Enter", "x");
            //AnimateSim("Investigate");
            //AnimateSim("Exit");   
            if (Target.QualityLevel < 0.3f)
            {
                Actor.PlayReaction(ReactionTypes.HissPet, ReactionSpeed.ImmediateWithoutOverlay);
            }
            else
            {
                Actor.PlayReaction(ReactionTypes.PositivePet, ReactionSpeed.ImmediateWithoutOverlay);
            }
            EndCommodityUpdates(succeeded: true);
            return true;
        }
    }
}
