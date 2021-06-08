using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Objects.Gardening.Plant;

namespace Echoweaver.Sims3Game.WarriorCats
{

	public class EWPetWaterPlant : ChainableGardeningInteraction<Plant>
	{
		public class Definition : InteractionDefinition<Sim, Plant, EWPetWaterPlant>
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				//if (!a.IsCat || !WaterTestDisregardGardeningSkill(target, a))
				//{
				//	return false;
				//}
				if (target.mDormant)
				{
					greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(Localization.LocalizeString("Gameplay/Objects/Gardening:DormantPlant"));
					return false;
				}
				return a.IsCat && (target.GetSoil().Dampness != SoilDampness.Wet);
			}
			public override string GetInteractionName(Sim a, Plant target, InteractionObjectPair interaction)
			{
				return "EWPetWaterPlant";
			}
		}

		public const string kWaterSoundName = "garden_water_lp";

		public Soil mDummyIk;

		public ObjectSound mWateringSound;

		public static InteractionDefinition Singleton = new Definition();

		public override void ConfigureInteraction()
		{
			float num = Target.GetWaterDuration(Actor);
			if (num == 0f)
			{
				num = 1f;
			}
			TimedStage timedStage = new TimedStage(GetInteractionName(), num, showCompletionTime: false, selectable: true, visibleProgress: true);
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
			StopWateringSound(null, null);
			base.Cleanup();
		}

		public override bool Run()
		{

			// TODO: Routing, check FindGoodLocation, Booleans describe ponds
			// Note: CreateObjectOutOfWorld
			//TrashPile trashPile = (TrashPile)GlobalFunctions.CreateObjectOutOfWorld("TrashPileOutdoor", null, (ObjectInitParameters)(object)new TrashPileInitParameters(isRecyclePile: true));
			//GlobalFunctions.FindGoodLocationNearby(Actor, )
			bool flag = false;
			bool flag2 = Actor.CurrentInteraction is ITendGarden;
			if (Target.RouteSimToMeAndCheckInUse(Actor) && (flag2 ? WaterTest(Target, Actor) : WaterTestDisregardGardeningSkill(Target, Actor)))
			{
				ConfigureInteraction();
				//TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				flag = DoWater();
			}
			//if (IsChainingPermitted(flag))
			//{
			//	IgnorePlants.Add(Target);
			//	if (flag2)
			//	{
			//		PushNextInteractionInChain(Singleton, WaterTest, Target.LotCurrent);
			//	}
			//	else
			//	{
			//		PushNextInteractionInChain(Singleton, WaterTestDisregardGardeningSkill, Target.LotCurrent);
			//	}
			//}
			return flag;
		}

		public bool DoWater()
		{

			StandardEntry();
			BeginCommodityUpdates();
			//mCurrentStateMachine = Target.GetStateMachine(Actor, out Soil dummyIk);
			//if (mCurrentStateMachine == null)
			//{
			//	EndCommodityUpdates(succeeded: false);
			//	StandardExit();
			//}
			//mDummyIk = dummyIk;
			AcquireStateMachine("eatharvestablepet");
			mCurrentStateMachine.SetActor("x", Actor);
			mCurrentStateMachine.EnterState("x", "Enter");
			SetParameter("IsEatingOnGround", paramValue: true);
			AnimateSim("EatHarvestable");
			AddOneShotScriptEventHandler(1001u, (SacsEventHandler)(object)new SacsEventHandler(StartWateringSound));
			AddOneShotScriptEventHandler(1002u, (SacsEventHandler)(object)new SacsEventHandler(StopWateringSound));
			//mCurrentStateMachine.RequestState("x", "Loop Water");
			StartStagesForTendableInteraction(this);
			float duration = Target.GetWaterDuration(Actor);
			if (duration == 0f)
			{
				duration = 1f;
			}
			float startingWaterLevel = Target.WaterLevel;
			float targetWaterLevel = 100f;
			Target.AddSimWhoHelpedGrow(Actor);
			EventTracker.SendEvent(EventTypeId.kWateredPlant, Actor, Target);
			bool flag = DoLoop(ExitReason.Default, delegate (StateMachineClient unused, LoopData ld)
			{
				float num2 = ld.mLifeTime / duration;
				this.Target.WaterLevel = num2 * targetWaterLevel + (1f - num2) * startingWaterLevel;
				if (this.Target.WaterLevel > targetWaterLevel)
				{
					this.Target.WaterLevel = targetWaterLevel;
				}
				if (ld.mLifeTime > duration)
				{
					this.Actor.AddExitReason(ExitReason.Finished);
				}
			}, null);
			//PauseTendGardenInteractionStage(Actor.CurrentInteraction);
			if (flag && Target.WaterLevel < targetWaterLevel)
			{
				Target.WaterLevel = targetWaterLevel;
			}

			AnimateSim("Exit");
			EventTracker.SendEvent(EventTypeId.kGardened, Actor);
			EndCommodityUpdates(succeeded: true);
			StandardExit();
			//UpdateTendGardenTimeSpent(this, SetWaterTimeSpent);
			return flag;
		}

		public static void SetWaterTimeSpent(ITendGarden tendGardenInteraction, float timeSpent)
		{
			tendGardenInteraction.WaterTimeSpent = timeSpent;
		}

		public void StartWateringSound(StateMachineClient sender, IEvent evt)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			if (mWateringSound == null)
			{
				mWateringSound = (ObjectSound)(object)new ObjectSound(Target.ObjectId, "garden_water_lp");
				((Sound)mWateringSound).StartLoop();
			}
		}

		public void StopWateringSound(StateMachineClient sender, IEvent evt)
		{
			if (mWateringSound != null)
			{
				((Sound)mWateringSound).Stop();
				((Sound)mWateringSound).Dispose();
				mWateringSound = null;
			}
		}
	}
}
