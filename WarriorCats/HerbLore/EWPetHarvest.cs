using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.Gameplay.Objects.Gardening.Plant;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats.HerbLore
{
    public class EWPetHarvest : ChainableGardeningInteraction<HarvestPlant>
	{
		public class Definition : InteractionDefinition<Sim, HarvestPlant, EWPetHarvest>
		{
			public override bool Test(Sim a, HarvestPlant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWHerbLoreSkill.SkillNameID) >= 1
					&& !(target is ForbiddenFruitTree))  
				{
					return HarvestPlant.HarvestTest(target, a);
				}
				else return false;
			}
			public override string GetInteractionName(Sim actor, HarvestPlant target, InteractionObjectPair iop)
			{
				return "Localize - Harvest";
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
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
			{
				Target.RemoveHarvestStateTimeoutAlarm();
				StandardEntry();
				BeginCommodityUpdates();
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
				Slot[] containmentSlots = Target.GetContainmentSlots();
				List<GameObject> list = new List<GameObject>();
				Slot[] array = containmentSlots;
				foreach (Slot slotName in array)
				{
					GameObject gameObject = Target.GetContainedObject(slotName) as GameObject;
					if (gameObject != null && Target.HarvestHarvestable(gameObject, Actor, null))
					{
						list.Add(gameObject);
					}
				}

				if (list.Count > 0)
				{
					skill.UpdateSkillJournal(Target.PlantDef, list);

					if (!skill.HasHarvested())
					{
						Actor.ShowTNSIfSelectable(Localization.LocalizeString(Actor.IsFemale,
							"Gameplay/Objects/Gardening/HarvestPlant/Harvest:FirstHarvest", Actor, Target.PlantDef.Name),
							NotificationStyle.kGameMessagePositive, Target.ObjectId, Actor.ObjectId);
					}
					Target.PostHarvest();
					EndCommodityUpdates(succeeded: true);
					StandardExit();
					skill.StopSkillGain();
					//Plant.UpdateTendGardenTimeSpent(this, SetHarvestTimeSpent);
					return Actor.HasExitReason(ExitReason.StageComplete);
				}
			}
			return false;
		}

		public static void SetHarvestTimeSpent(ITendGarden tendGardenInteraction, float timeSpent)
		{
			tendGardenInteraction.HarvestTimeSpent = timeSpent;
		}
	}
}
