using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.MedicineCat
{
	public class EWPetViewPlant : Plant.ViewPlant
	{
		public new class Definition : Plant.ViewPlant.Definition
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (!a.IsCat)
                {
					return false;
                }
				if (target.GrowthState != PlantGrowthState.Mature)
				{
					return target.GrowthState == PlantGrowthState.Harvest;
				}
				return true;
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
			AcquireStateMachine("catdoginvestigate");
			EnterStateMachine("catdoginvestigate", "Enter", "x");
			AnimateSim("Investigate");
			AnimateSim("Exit");
			if (Target.QualityLevel < 0.3f)
			{
				Actor.PlayReaction(ReactionTypes.HissPet, ReactionSpeed.ImmediateWithoutOverlay);
			}
			else
			{
				Actor.PlayReaction(ReactionTypes.PositivePetQuiet, ReactionSpeed.ImmediateWithoutOverlay);
			}
			EndCommodityUpdates(succeeded: true);
			return true;
		}
	}
}
