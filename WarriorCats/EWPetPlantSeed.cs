using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System.Collections.Generic;
using static Sims3.SimIFace.World;
using PlantableObjectData = Sims3.Gameplay.ObjectComponents.PlantObject.PlantableObjectData;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetPlantSeed : ImmediateInteraction<Sim, GameObject>
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetPlantSeed>
		{
			public override bool Test(Sim a, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWHerbLoreSkill.SkillNameID) >= 3)
					return CommonPlantingTest(a, target, LotManager.ActiveLot, ref greyedOutTooltipCallback);
				else return false;
			}
			public override string GetInteractionName(Sim a, GameObject target, InteractionObjectPair interaction)
			{
				return "Localize - Plant";
			}
		}

		public const string sLocalizationKey = "Gameplay/ObjectComponents/PlantObject";

		public IGameObject mCurrentTarget;

		public Soil mCurrentSoil;

		public PlantInteractionType mInteractionType;

		public List<PlantableObjectData> mObjectsToPlant;

		public bool mInteractionPushed;

		public bool mSoilPlacementFailure;

		public static InteractionDefinition Singleton = new Definition();

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString("Gameplay/ObjectComponents/PlantObject:" + name, parameters);
		}

		public override void Cleanup()
		{
			if (!mInteractionPushed || mSoilPlacementFailure)
			{
				if (mCurrentTarget != null)
				{
					UnreservePlantablePlantingFailed(mCurrentTarget, Actor, mInteractionType);
				}
				if (mCurrentSoil != null)
				{
					mCurrentSoil.Destroy();
					mCurrentSoil = null;
				}
			}
			if (!mInteractionPushed && mObjectsToPlant != null)
			{
				while (mObjectsToPlant.Count > 0)
				{
					EWPetDoPlant.CleanupPlantInstances(mObjectsToPlant[0], Actor, mInteractionType);
					mObjectsToPlant.RemoveAt(0);
				}
			}
			base.Cleanup();
		}

		public override bool RunFromInventory()
		{
			mInteractionType = PlantInteractionType.FromInventory;
			return DoSoilPlacementAndPlant();
		}

		public bool ReservePlantable()
		{
			if (mCurrentTarget.InUse)
			{
				return false;
			}
			mCurrentTarget.AddToUseList(Actor);
			if (mInteractionType == PlantInteractionType.FromInventory
				|| mInteractionType == PlantInteractionType.FromInventoryPlantMany)
			{
				return Actor.Inventory.SetInUse(mCurrentTarget);
			}
			mCurrentTarget.SetOpacity(0.5f, 0f);
			return true;
		}

		public static void UnreservePlantablePlantingFailed(IGameObject currentTarget, Sim sim,
			PlantInteractionType interactionType)
		{
			currentTarget.RemoveFromUseList(sim);
			if (interactionType == PlantInteractionType.FromInventory
				|| interactionType == PlantInteractionType.FromInventoryPlantMany)
			{
				if (!sim.Inventory.SetNotInUse(currentTarget))
				{
					currentTarget.Destroy();
				}
				return;
			}
			currentTarget.SetOpacity(1f, 0f);
			Ingredient ingredient = currentTarget as Ingredient;
			if (ingredient != null)
			{
				ingredient.EnableFootprint();
			}
			else
			{
				(currentTarget as PlantableNonIngredient)?.EnableFootprint();
			}
		}

		public virtual bool PlaceSoil()
		{
			if (!Actor.IsActiveSim)
			{
				return false;
			}
			mCurrentSoil.SetOwnerLot(Actor.LotHome);
			eEditTypeHandTool editEndedType;
			return GlobalFunctions.PlaceWithHandTool(mCurrentSoil, out editEndedType);
		}

		public bool DoSoilPlacementAndPlant()
		{
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
			{
				mCurrentTarget = Target;
				bool flag = false;
				do
				{
					flag = false;
					if (!ReservePlantable())
					{
						break;
					}
					mCurrentSoil = (GlobalFunctions.CreateObjectOutOfWorld("GardenSoil") as Soil);
					if (mCurrentSoil == null)
					{
						break;
					}
					mCurrentSoil.SetPlantDef(PlantHelper.GetPlantDefinition(mCurrentTarget));
					mCurrentSoil.AddToWorld();
					mCurrentSoil.Ghost();
					if (!PlaceSoil())
					{
						mSoilPlacementFailure = true;
						break;
					}
					mCurrentSoil.AddToUseList(Actor);
					mCurrentSoil.SetPlanted();
					if (mObjectsToPlant == null)
					{
						mObjectsToPlant = new List<PlantableObjectData>();
					}
					mObjectsToPlant.Add(new PlantableObjectData(mCurrentTarget, mCurrentSoil));
					if (mInteractionType != PlantInteractionType.FromInventoryPlantMany)
					{
						continue;
					}
					uint stackNumber = Actor.Inventory.GetStackNumber(mCurrentTarget);
					if (stackNumber != 0)
					{
						List<IGameObject> stackObjects = Actor.Inventory.GetStackObjects(stackNumber, checkInUse: true);
						if (stackObjects.Count > 0)
						{
							mCurrentTarget = stackObjects[0];
							flag = true;
						}
					}
				} while (flag);

				if (mObjectsToPlant != null && mObjectsToPlant.Count > 0)
				{
					EWPetDoPlant petDoPlant = EWPetDoPlant.Singleton.CreateInstance(Target, Actor, mPriority,
						base.Autonomous, cancellableByPlayer: true) as EWPetDoPlant;
					if (petDoPlant != null)
					{
						petDoPlant.SetObjectsToPlant(mObjectsToPlant);
						petDoPlant.PlantInteractionType = mInteractionType;
						if (Actor.InteractionQueue.Add(petDoPlant))
						{
							mInteractionPushed = true;
						}
					}
				}
				skill.StopSkillGain();
				return true;
			}
			return false;
		}

		public static bool CommonPlantingTest(Sim a, GameObject target, Lot lotTryingToPlantingOn, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
		{
			if (!PlantableComponent.PlantInteractionOpportunityTest(a, target))
			{
				return false;
			}
			if (CameraController.IsMapViewModeEnabled())
			{
				greyedOutTooltipCallback = new GreyedOutTooltipCallback(MapViewGreyedTooltip);
				return false;
			}
			if (!PlantingLotTest(lotTryingToPlantingOn, a))
			{
				greyedOutTooltipCallback = new GreyedOutTooltipCallback(CanOnlyPlantOnHomeLot);
				return false;
			}
			if (!a.IsCat)  // TODO: Skill test
			{
				return false;
			}
			return true;
		}

		public static string MapViewGreyedTooltip()
		{
			return LocalizeString("CantPlantInMapView");
		}

		public static string CanOnlyPlantOnHomeLot()
		{
			return LocalizeString("CanOnlyPlantOnHomeLot");
		}

		public static bool PlantingLotTest(Lot lotTryingToPlantingOn, Sim simTryingToPlant)
		{
			if (!lotTryingToPlantingOn.IsCommunityLot)
			{
				return lotTryingToPlantingOn.CanSimTreatAsHome(simTryingToPlant);
			}
			return false;
		}
	}

	public class EWPetDoPlant : Interaction<Sim, GameObject>, IRouteFromInventoryOrSelfWithoutCarrying
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetDoPlant>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				bool isFemale = actor.IsFemale;
				if (target != null)
				{
					return Localization.LocalizeString(isFemale, "Gameplay/ObjectComponents/DoPlant:DoPlant",
						target.GetLocalizedName());
				}
				return Localization.LocalizeString(isFemale, "Gameplay/Abstracts/GameObject/PlantObject:InteractionName");
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}
		}

		public List<PlantableObjectData> mObjectsToPlant;

		public IGameObject CurrentTarget;

		public Soil CurrentSoil;

		public PlantInteractionType PlantInteractionType;

		public Soil mDummyIk;

		public int mTotalObjectsToPlant;

		public static InteractionDefinition Singleton = new Definition();

		public void ScriptHandlerOnSimAboutToPlant(StateMachineClient sender, IEvent evt)
		{
            mObjectsToPlant.RemoveAt(0);
			CurrentSoil.UnGhost();
		}

		public override void Cleanup()
		{
			if (mObjectsToPlant != null)
			{
				while (mObjectsToPlant.Count > 0)
				{
					CleanupPlantInstances(mObjectsToPlant[0], Actor, PlantInteractionType);
					mObjectsToPlant.RemoveAt(0);
				}
			}
			if (mDummyIk != null)
			{
				mDummyIk.Destroy();
				mDummyIk = null;
			}
			base.Cleanup();
		}

		public float GetCompletionFraction(InteractionInstance instance)
		{
			if (mObjectsToPlant == null)
			{
				return 0f;
			}
			float num = mTotalObjectsToPlant;
			return (num - (float)mObjectsToPlant.Count) / num;
		}

		public override void ConfigureInteraction()
		{
			base.ConfigureInteraction();
			if (mTotalObjectsToPlant > 1)
			{
				SimpleStage simpleStage = new SimpleStage(GetInteractionName(), 10f, GetCompletionFraction,
					showCompletionTime: false, selectable: false, visibleProgress: true);
				base.Stages = new List<Stage>(new Stage[1] {
				simpleStage
			});
			}
		}

		public override bool Run()
		{
			return RunCommon();
		}

		public override bool RunFromInventory()
		{
			return RunCommon();
		}

		public bool RunCommon()
		{
			if (mObjectsToPlant == null || mObjectsToPlant.Count == 0)
			{
				return false;
			}
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
			{
				while (mObjectsToPlant.Count > 0)
				{
					PlantableObjectData data = mObjectsToPlant[0];
					CurrentTarget = data.PlantableObject;
					CurrentSoil = data.Soil;
					if (Plant.DoPlantRoute(Actor, CurrentSoil))
					{
						UnreservePlantablePlantingSucceeded();
						Plant plant = CreatePlantFromSeed(CurrentTarget, CurrentSoil, Actor);
						plant.UpdateHealth();
						plant.AddToUseList(Actor);
						AcquireStateMachine("eatharvestablepet");
						mCurrentStateMachine.SetActor("x", Actor);
						mCurrentStateMachine.EnterState("x", "Enter");
						SetParameter("IsEatingOnGround", paramValue: true);
						// Parrotting the dummyIK code from the plantMedium state machine
						mDummyIk = Soil.Create(isDummyIk: true);
						//mDummyIk.SetHiddenFlags(-1);
						mDummyIk.SetPosition(plant.GetSoil().Position);
						Vector3 forward = plant.GetSoil().Position - Actor.Position;
						mDummyIk.SetForward(forward);
						mDummyIk.AddToWorld();
						BeginCommodityUpdates();
						AddOneShotScriptEventHandler(201u, new SacsEventHandler(ScriptHandlerOnSimAboutToPlant));
						AnimateSim("EatHarvestable");
						skill.Planted(plant);
						AnimateSim("Exit");
						EndCommodityUpdates(succeeded: true);
						plant.RemoveFromUseList(Actor);
						CurrentSoil.RemoveFromUseList(Actor);
						if (mDummyIk != null)
						{
							mDummyIk.Destroy();
							mDummyIk = null;
						}
						EventTracker.SendEvent(EventTypeId.kGardened, Actor);
						if (PlantHelper.IsSeed(CurrentTarget))
						{
							EventTracker.SendEvent(EventTypeId.kEventSeedPlanted, Actor, CurrentTarget);
						}
						EventTracker.SendEvent(EventTypeId.kPlantedObject, Actor, plant);
					}
					else
					{
						CleanupPlantInstances(data, Actor, PlantInteractionType);
						mObjectsToPlant.RemoveAt(0);
					}
				}
				skill.StopSkillGain();
				skill.AddSkillPointsLevelClamped(200, 10); // Bonus, Planting takes animal thinking
				return true;
			}
			return false;
		}

		public void SetObjectsToPlant(List<PlantableObjectData> objectsToPlant)
		{
			mObjectsToPlant = new List<PlantableObjectData>(objectsToPlant);
			mTotalObjectsToPlant = mObjectsToPlant.Count;
			ConfigureInteraction();
		}

		public static void CleanupPlantInstances(PlantableObjectData data, Sim sim, PlantInteractionType plantInteractionType)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			if (data.Soil != null)
			{
				Plant plant = data.Soil.GetContainedObject(Soil.ContainmentSlots.Plant) as Plant;
				if (plant != null)
				{
					plant.RemoveFromUseList(sim);
					data.Soil.RemoveFromUseList(sim);
					if (data.Soil.IsGhosted())
					{
						data.Soil.UnGhost();
					}
					return;
				}
			}
			if (data.PlantableObject != null)
			{
				EWPetPlantSeed.UnreservePlantablePlantingFailed(data.PlantableObject, sim, plantInteractionType);
			}
			if (data.Soil != null)
			{
				data.Soil.Destroy();
			}
		}

		public void UnreservePlantablePlantingSucceeded()
		{
			CurrentTarget.RemoveFromUseList(Actor);
			if (PlantInteractionType == PlantInteractionType.FromInventory || PlantInteractionType == PlantInteractionType.FromInventoryPlantMany)
			{
				Actor.Inventory.SetNotInUse(CurrentTarget);
				Actor.Inventory.TryToRemove(CurrentTarget);
			}
		}

		public static Plant CreatePlantFromSeed(IGameObject seedObj, Soil soil, Sim planter)
		{
			PlantDefinition plantDefinition = PlantHelper.GetPlantDefinition(seedObj);
			if (plantDefinition == null)
			{
				return null;
			}
			float num = seedObj.Plantable.QualityLevel;
			if (planter != null)
			{
				//int num2 = planter.SkillManager.GetSkillLevel(SkillNames.Gardening);
				//if (num2 == -1)
				//{
				//	num2 = 0;
				//}
				//num *= Sims3.Gameplay.Skills.Gardening.QualityMultiplier[(int)plantDefinition.Rarity, num2];
				//if (planter.HasTrait(TraitNames.SuperGreenThumb))
				//{
				//	num *= TraitTuning.SuperGreenThumbPlantQualityMultiplier;
				//}
			}
			PlantInitParameters initParams = new PlantInitParameters(seedObj, num, plantDefinition, soil);
			Plant plant = Plant.DoCreatePlantFromSeed(plantDefinition, soil, initParams, Plant.IsMushroom(seedObj));
			plant?.AddSimWhoHelpedGrow(planter);
			return plant;
		}
	}
}
