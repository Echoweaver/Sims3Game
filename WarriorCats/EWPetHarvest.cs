using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Objects.Gardening.Plant;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetHarvest : ChainableGardeningInteraction<HarvestPlant>
	{
		public class Definition : InteractionDefinition<Sim, HarvestPlant, EWPetHarvest>
		{
			public override bool Test(Sim a, HarvestPlant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.IsCat && !a.IsKitten && !(target is ForbiddenFruitTree))  // TODO: Check skill level
				{
					return HarvestPlant.HarvestTest(target, a);
				}
				else return false;
			}
		}

		public Soil mDummyIk;

		public static InteractionDefinition Singleton = new Definition();

		public override void ConfigureInteraction()
		{
			float harvestDuration = Target.GetHarvestDuration(Actor);
			TimedStage timedStage = new TimedStage(GetInteractionName(), harvestDuration, showCompletionTime: false,
				selectable: true, visibleProgress: true);
			base.Stages = new List<Stage>(new Stage[1] {
			timedStage
		});
		}

		public override void Cleanup()
		{
			if (mDummyIk != null)
			{
				mDummyIk.Destroy();
				mDummyIk = null;
			}
			base.Cleanup();
		}

		public override bool Run()
		{
			bool flag = false;
			if (Target.RouteSimToMeAndCheckInUse(Actor) && HarvestPlant.HarvestTest(Target, Actor))
			{
				ConfigureInteraction();
//				Plant.TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				flag = DoHarvest();
			}
			//if (IsChainingPermitted(flag))
			//{
			//	IgnorePlants.Add(Target);
			//	if (Target.LotCurrent != null && Target.LotCurrent.IsWorldLot)
			//	{
			//		PushNextInteractionInChain(Singleton, HarvestPlant.HarvestTestWorldLot, Target.LotCurrent);
			//	}
			//	else
			//	{
			//		PushNextInteractionInChain(Singleton, HarvestPlant.HarvestTest, Target.LotCurrent);
			//	}
			//}
			return flag;
		}

		public bool DoHarvest()
		{
			Target.RemoveHarvestStateTimeoutAlarm();
			StandardEntry();
			BeginCommodityUpdates();
			//Soil dummyIk;
			//StateMachineClient stateMachine = EWHerbLoreSkill.CreateStateMachine(Actor, Target, out dummyIk);

			//mDummyIk = dummyIk;
			bool hasHarvested = false;
			EWHerbLoreSkill skill = Actor.SkillManager.GetSkill<EWHerbLoreSkill>(EWHerbLoreSkill.SkillNameID);
			if (skill != null && skill.HasHarvested())
			{
				hasHarvested = true;
			}

			if (!Target.PlantDef.GetPlantHeight(out PlantHeight height))
			{
				height = PlantHeight.Medium;
			}
			// TODO: Different animations -- scratching post for medium and high plants?
			AcquireStateMachine("eatharvestablepet");
			mCurrentStateMachine.SetActor("x", Actor);
			mCurrentStateMachine.EnterState("x", "Enter");
			SetParameter("IsEatingOnGround", paramValue: true);
			AnimateSim("EatHarvestable");
            Plant.StartStagesForTendableInteraction(this);
            while (!Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Default))
            {
                if (base.ActiveStage != null && base.ActiveStage.IsComplete(this))
                {
                    Actor.AddExitReason(ExitReason.StageComplete);
                }
            }
            //Plant.PauseTendGardenInteractionStage(Actor.CurrentInteraction);
            AnimateSim("Exit");
			EWHerbLoreSkill.DoHarvest(Actor, Target, hasHarvested);
			//}
			//if (stateMachine != null)
			//{
			//	stateMachine.RequestState("x", "Exit Standing");
			//}
			EndCommodityUpdates(succeeded: true);
			StandardExit();
			//Plant.UpdateTendGardenTimeSpent(this, SetHarvestTimeSpent);
			return Actor.HasExitReason(ExitReason.StageComplete);
		}

		public static void SetHarvestTimeSpent(ITendGarden tendGardenInteraction, float timeSpent)
		{
			tendGardenInteraction.HarvestTimeSpent = timeSpent;
		}
	}
}
