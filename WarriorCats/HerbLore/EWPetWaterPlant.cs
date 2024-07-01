using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.Gameplay.Objects.Gardening.Plant;

namespace Echoweaver.Sims3Game.WarriorCats.HerbLore
{

    public class EWPetWaterPlant : ChainableGardeningInteraction<Plant>
	{
		public class Definition : InteractionDefinition<Sim, Plant, EWPetWaterPlant>
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWHerbLoreSkill.SkillNameID) >= 3)
				{
					if (target.mDormant)
					{
						greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString("Gameplay/Objects/Gardening:DormantPlant"));
						return false;
					}
					return target.GetSoil().Dampness != SoilDampness.Wet;
				}
				return false;
			}
			public override string GetInteractionName(Sim a, Plant target, InteractionObjectPair interaction)
			{
				// TODO: Localize!
				return "Water Plant";
			}
		}

		public const string kWaterSoundName = "garden_water_lp";

		public ObjectSound mWateringSound;

		public static InteractionDefinition Singleton = new Definition();

		public IGameObject mossBall;

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
			if (mossBall != null)
			{
				mossBall.Destroy();
				mossBall = null;
			}
			StopWateringSound(null, null);
			base.Cleanup();
		}

		public override bool Run()
		{
			bool result = false;
			IPond nearestWater = GetNearestWater(Actor.Position, float.MaxValue);
			if (nearestWater == null)
            {
				DebugNote("Water Plant: No water source found.");
				return false;
            }
			ulong notUsed = 10u; // Not used by the method. I don't know what it was supposed to be.
		
			if (!DrinkFromPondHelper.RouteToDrinkLocation(nearestWater.RepresentativePondPosition(), Actor,
				GameObjectHitType.WaterPond, notUsed))
			{
				return false;
			}
			mossBall = GlobalFunctions.CreateObjectOutOfWorld("petToyBallFoil", ProductVersion.EP5);
			//bool isChaining = Actor.CurrentInteraction is ITendGarden;
			mossBall.SetColorTint(0.75f, 1f, 0.35f, 0);  // RGB value for Dark Moss Green
            mossBall.AddToWorld();
			mossBall.SetPosition(Actor.Position);
			CarryUtils.Acquire(Actor, mossBall);
			EnterCarry(Actor, mossBall);
			CarryUtils.Request(Actor, "PickUp");
			CarryUtils.Request(Actor, "Carry");

			EnterStateMachine("DrinkFromPond", "Enter", "x");
			AnimateSim("Loop");
			AnimateSim("Loop");
			AnimateSim("Exit");

			if (Target.RouteSimToMeAndCheckInUse(Actor) && WaterTestDisregardGardeningSkill(Target, Actor))
			{
				ConfigureInteraction();
				//TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				result = DoWater();
			}
			CarryUtils.Request(Actor, "PutDown");
			CarryUtils.ExitCarry(Actor);
			mossBall.Destroy();
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
			return result;
		}

		public static void EnterCarry(Sim a, IGameObject target)
		{
			a.CarryStateMachine.SetActor("x", a);
			a.CarryStateMachine.SetActor("object", target);
			a.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
			a.CarryStateMachine.SetParameter("model", "catToy", ProductVersion.EP5);
			a.CarryStateMachine.SetParameter("NamespaceMap0From", "catToy");
			a.CarryStateMachine.SetParameter("NamespaceMap0To", "object");
			a.CarryStateMachine.EnterState("x", "Enter");
		}

		public bool DoWater()
		{
			// Not going to start skill gain until water is at least retrieved.
			// No extra points for that much routing.
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
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
				StartWateringSound();
				AddOneShotScriptEventHandler(201u, new SacsEventHandler(StopWateringSound));
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
				skill.StopSkillGain();
				//UpdateTendGardenTimeSpent(this, SetWaterTimeSpent);
				return flag;
			}
			return false;
		}

		public static IPond GetNearestWater(Vector3 starting_point, float max_distance)
		{
			IPond result = null;
			float nearest_distance = max_distance;
			IPond[] ponds = Sims3.Gameplay.Queries.GetObjects<IPond>();
			DebugNote("GetNearestWater: " + ponds.Length + " water sources found");
			foreach (IPond DHMOLocation in ponds)
			{

				float pond_distance = (starting_point - ((GameObject)DHMOLocation).Position).LengthSqr();
				if (pond_distance < nearest_distance)
				{
					result = DHMOLocation;
					nearest_distance = pond_distance;
				}
			}
			if (result != null)
			{
				DebugNote("Nearest water source distance: " + nearest_distance);
			}
			return result;
		}

		public static void SetWaterTimeSpent(ITendGarden tendGardenInteraction, float timeSpent)
		{
			tendGardenInteraction.WaterTimeSpent = timeSpent;
		}

		public void StartWateringSound()
		{
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
