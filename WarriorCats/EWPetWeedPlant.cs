using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Objects.Gardening.Plant;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetWeedPlant : ChainableGardeningInteraction<Plant>
	{
		public class Definition : InteractionDefinition<Sim, Plant, EWPetWeedPlant>
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWHerbLoreSkill.SkillNameID) >= 2)
				{
					return target.GardenInteractionLotValidityTest(a) && target.HasWeeds && target.Alive;
				}
				return false;
			}
			public override string GetInteractionName(Sim a, Plant target, InteractionObjectPair interaction)
			{
				return "EWPetWeedPlant";
			}
		}

		public Soil mDummyIk;

		public static InteractionDefinition Singleton = new Definition();

		public override void ConfigureInteraction()
		{
			float weedDuration = Target.GetWeedDuration(Actor);
			TimedStage timedStage = new TimedStage(GetInteractionName(), weedDuration, showCompletionTime: false, selectable: true, visibleProgress: true);
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
			if (Target.RouteSimToMeAndCheckInUse(Actor) && Target.HasWeeds)
			{
				ConfigureInteraction();
				//TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				flag = DoWeed();
			}
			//if (IsChainingPermitted(flag))
			//{
			//	IgnorePlants.Add(Target);
			//	PushNextInteractionInChain(Singleton, WeedTest, Target.LotCurrent);
			//}
			return flag;
		}

		public bool DoWeed()
		{
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
			{
				StandardEntry();
				BeginCommodityUpdates();
				AcquireStateMachine("eatharvestablepet");
				mCurrentStateMachine.SetActor("x", Actor);
				mCurrentStateMachine.EnterState("x", "Enter");
				SetParameter("IsEatingOnGround", paramValue: true);
				AnimateSim("EatHarvestable");
				StartStagesForTendableInteraction(this);
				bool flag = DoLoop(ExitReason.Default);
				//PauseTendGardenInteractionStage(Actor.CurrentInteraction);
				if (flag)
				{
					Target.AddSimWhoHelpedGrow(Actor);
					Target.HasWeeds = false;
					EventTracker.SendEvent(EventTypeId.kWeededPlant, Actor, Target);
				}
				AnimateSim("Exit");
				EndCommodityUpdates(flag);
				StandardExit();
				EventTracker.SendEvent(EventTypeId.kGardened, Actor);
				//UpdateTendGardenTimeSpent(this, SetWeedTimeSpent);
				skill.StopSkillGain();
				return flag;
			}
			return false;
		}

		public static void SetWeedTimeSpent(ITendGarden tendGardenInteraction, float timeSpent)
		{
			tendGardenInteraction.WeedTimeSpent = timeSpent;
		}
	}
}
