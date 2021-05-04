using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.UI.StyledNotification;


namespace Echoweaver.Sims3Game.CatFishing
{
	public class EWCatFishHere : TerrainInteraction, IPondInteraction
	{
		public class Definition : InteractionDefinition<Sim, Terrain, EWCatFishHere>
		{
			public Definition()
			{
			}

			public override bool Test(Sim a, Terrain target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.IsCat && !a.IsKitten)
				{
					return PetManager.PetSkillFatigueTest(a, ref greyedOutTooltipCallback);
				}
				return false;
			}

			public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if ((parameters.Hit.mType == GameObjectHitType.WaterPond
					&& PondManager.ArePondsLiquid()) || parameters.Hit.mType == GameObjectHitType.WaterSea)
				{
					return base.Test(ref parameters, ref greyedOutTooltipCallback);
				}
				return InteractionTestResult.Gen_BadTerrainType;
			}

			public override string GetInteractionName(Sim a, Terrain target, InteractionObjectPair interaction)
			{
				return Localization.LocalizeString("Echoweaver/Interactions:EWCatFishHere");
			}

		}


		[TunableComment("Description:  Min To Max time the cat  does pre-pounce animations for")]
		[Tunable]
		public static float[] kMinMaxPrePounceTime = new float[2] {
		1f,
		5f
		};

		[Tunable]
		[TunableComment("Description:  If the cat's hunger is below this when they're done fishing they'll eat the fish instead of putting it in their inventory")]
		public static float kEatFishHungerThreshold = -50f;

		[TunableComment("  Description:  If the cat eats the fish they gain this much hunger")]
		[Tunable]
		public static float kHungerGainFromEating = 30f;

		[Tunable]
		[TunableComment("Description:  The min and max chances for success, will lerp between these values based on your hunting skill")]
		public static float[] kMinMaxSuccesChance = new float[2] {
		30f,
		75f
		};

		public static ulong kIconNameHash = ResourceUtils.HashString64("skill_EWCatFishing");

		public static InteractionDefinition Singleton = new Definition();

		public bool TerrainIsWaterPond => (int)Hit.mType == 8;


        public override ThumbnailKey GetIconKey()
		{
			return new ThumbnailKey(new ResourceKey(kIconNameHash, 796721156u, 0u), ThumbnailSize.Small);
		}

		public override bool Run()
		{
			EWCatFishingSkill skill = Actor.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
			if (skill == null)
			{
				skill = (Actor.SkillManager.AddElement(EWCatFishingSkill.SkillNameID) as EWCatFishingSkill);
				if (skill == null)
				{
					return false;
				}
			}
			if (!DrinkFromPondHelper.RouteToDrinkLocation(Hit.mPoint, Actor, Hit.mType, Hit.mId))
			{
				return false;
			}
			if (skill.OppFishercatCompleted)
			{
				skill.StartSkillGain(EWCatFishingSkill.kEWFishingSkillGainRateFishercat);
			}
			else
			{
				skill.StartSkillGain(EWCatFishingSkill.kEWFishingSkillGainRateNormal);
			}
			StandardEntry();
			EnterStateMachine("CatHuntInPond", "Enter", "x");
			AddOneShotScriptEventHandler(101u, (SacsEventHandler)(object)new SacsEventHandler(SnapOnExit));
			BeginCommodityUpdates();
			AnimateSim("PrePounceLoop");
			// TODO: If we don't have an opportunity for catching fish faster, we should
			bool flag = DoTimedLoop(RandomUtil.GetFloat(kMinMaxPrePounceTime[0], kMinMaxPrePounceTime[1]));
			if (flag)
			{
				EventTracker.SendEvent(EventTypeId.kGoFishingCat, Actor);
				AnimateSim("FishLoop");
				float successBonus = 0;
				if ((TerrainIsWaterPond && skill.OppPondProvisionerCompleted)
					|| (!TerrainIsWaterPond && skill.OppSaltaholicCompleted))
                {
					successBonus = EWCatFishingSkill.kFishCatchingBonus;
                }
				flag = RandomUtil.InterpolatedChance(0f, skill.MaxSkillLevel, kMinMaxSuccesChance[0] + successBonus,
					kMinMaxSuccesChance[1] + successBonus, skill.SkillLevel);
				if (flag)
				{
					FishType caughtFishType = GetCaughtFishType(Actor, Hit);
					Fish fish = Fish.CreateFishOfRandomWeight(caughtFishType, Actor.SimDescription);

					string message = skill.RegisterCaughtPrey(fish, TerrainIsWaterPond);  // Will return a message if the fish is new or interesting
					if (fish.CatHuntingComponent != null)
					{
						fish.CatHuntingComponent.SetCatcher(Actor);
					}
					fish.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
					SetActor("fish", (IHasScriptProxy)(object)fish);
					if (Actor.Motives.GetValue(CommodityKind.Hunger) <= kEatFishHungerThreshold)
					{
						message += Localization.LocalizeString("Gameplay/Abstracts/ScriptObject/CatFishHere:EatFishTns",
							Actor, fish.GetLocalizedName(), fish.Weight);
						Actor.ShowTNSIfSelectable(message, NotificationStyle.kGameMessagePositive);
						AnimateSim("ExitEat");
						fish.Destroy();
						Actor.Motives.ChangeValue(CommodityKind.Hunger, kHungerGainFromEating);
					}
					else
					{
						message += Localization.LocalizeString("Gameplay/Abstracts/ScriptObject/CatFishHere:PutFishInInventoryTns",
							Actor, fish.GetLocalizedName(), fish.Weight);
						Actor.ShowTNSIfSelectable(message, NotificationStyle.kGameMessagePositive);
						AnimateSim("ExitInventory");
						fish.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
						if (!Actor.Inventory.TryToAdd(fish))
						{
							fish.Destroy();
						}
					}
				}
				else
				{
					AnimateSim("ExitFailure");
				}
			}
			else
			{
				AnimateSim("ExitPrePounce");
			}
			EndCommodityUpdates(flag);
			StandardExit();
			skill.StopSkillGain();
			return true;
		}

		public void SnapOnExit(StateMachineClient sender, IEvent evt)
		{
			Vector3 forwardVector = Actor.ForwardVector;
//			Quaternion val = Quaternion.MakeFromEulerAngles(0f, MathF.PI, 0f);   TODO: It can't find MathF, not sure why. Don't want to deal.
			Quaternion val = Quaternion.MakeFromEulerAngles(0f, 3.14159274f, 0f);
			Matrix44 val2 = val.ToMatrix();
			forwardVector = val2.TransformVector(forwardVector);
			Actor.SetPosition(Actor.Position + 0.283f * forwardVector);
			Vector3 position = Actor.Position;
			position.y = World.GetTerrainHeight(position.x, position.z);
			Actor.SetPosition(position);
			Actor.SetForward(forwardVector);
		}

		public static FishType GetCaughtFishType(Sim actor, GameObjectHit hit)
		{
			GetSpawnerChances(actor, FishingSpot.GetFishingData(hit.mPoint, hit.mType), out List<FishType> fish, out float[] chances);			
			float num = 0f;
			for (int i = 0; i < chances.Length; i++)
			{
				num += chances[i];
			}
			if (num == 0f)
			{
				return FishType.None;
			}
			float num2 = RandomUtil.GetFloat(num);
			int j;
			for (j = 0; num2 > chances[j]; j++)
			{
				num2 -= chances[j];
			}
			FishType fishType = fish[j];
			return fishType;
		}

		public static void GetSpawnerChances(Sim Actor, FishingData fishingData, out List<FishType> fish, out float[] chances)
		{
			fish = fishingData.GetFish();
			int skillLevel = Actor.SkillManager.GetSkillLevel(EWCatFishingSkill.SkillNameID);
			List<int> chances2 = fishingData.GetChances();
			chances = new float[chances2.Count];
			int num = 0;
			foreach (FishType item in fish)
			{
				if (item != FishType.None)
				{
					Fish.sFishData.TryGetValue(item, out FishData value);
					if (value != null && skillLevel >= value.Level)
					{
						chances[num] = (float)chances2[num];
					}
					else
					{
						chances[num] = 0f;
					}
				}
				num++;
			}
		}
	}

}