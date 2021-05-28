using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.MedicineCat
{
    public class EWPetHarvest : HarvestPlant.Harvest 
    {
		public new class Definition : HarvestPlant.Harvest.Definition
		{
			public override bool Test(Sim a, HarvestPlant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (!a.IsCat || target is ForbiddenFruitTree)
				{
					return false;
				}
				if (!a.SkillManager.HasElement(EWHerbLoreSkill.SkillNameID))
				{
					return false;
				}
				return HarvestPlant.HarvestTest(target, a);
			}
		}

		public new static InteractionDefinition Singleton = new Definition();

		public EWHerbLoreSkill herbSkill;

		public new bool DoHarvest()
		{
			Target.RemoveHarvestStateTimeoutAlarm();
			StandardEntry();
			BeginCommodityUpdates();
			Soil dummyIk;

			// Todo: Fix state machine
			StateMachineClient stateMachine = Target.GetStateMachine(Actor, out dummyIk);
			mDummyIk = dummyIk;
			bool hasHarvested = true;
			if (stateMachine != null)
			{
				stateMachine.RequestState("x", "Loop Harvest");
			}
			Plant.StartStagesForTendableInteraction(this);
			while (!Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Default))
			{
				if (base.ActiveStage != null && base.ActiveStage.IsComplete((InteractionInstance)this))
				{
					Actor.AddExitReason(ExitReason.StageComplete);
				}
			}
			Plant.PauseTendGardenInteractionStage(Actor.CurrentInteraction);
			if (Actor.HasExitReason(ExitReason.StageComplete))
			{
				Target.DoHarvest(Actor, hasHarvested, mBurglarSituation);
			}
			if (stateMachine != null)
			{
				stateMachine.RequestState("x", "Exit Standing");
			}
			EndCommodityUpdates(succeeded: true);
			StandardExit();
			Plant.UpdateTendGardenTimeSpent(this, SetHarvestTimeSpent);
			return Actor.HasExitReason(ExitReason.StageComplete);
		}
	}
}
