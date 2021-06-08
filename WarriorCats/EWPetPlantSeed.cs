using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System.Collections.Generic;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetPlantSeed : Interaction<Sim, GameObject>, IRouteFromInventoryOrSelfWithoutCarrying
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetPlantSeed>
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

		public List<PlantObject.PlantableObjectData> mObjectsToPlant;

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
			while (mObjectsToPlant.Count > 0)
			{
				PlantObject.PlantableObjectData data = mObjectsToPlant[0];
				CurrentTarget = data.PlantableObject;
				CurrentSoil = data.Soil;
				if (Plant.DoPlantRoute(Actor, CurrentSoil))
				{
					UnreservePlantablePlantingSucceeded();
					Plant plant = Plant.CreatePlantFromSeed(CurrentTarget, CurrentSoil, Actor);
					Collecting skill = Actor.SkillManager.GetSkill<Collecting>(SkillNames.Collecting);
					if (skill != null && skill.IsMushroomCollector())
					{
						plant.mIsMushroomCollector = true;
					}
					plant.UpdateHealth();
					plant.AddToUseList(Actor);
					AcquireStateMachine("eatharvestablepet");
					mCurrentStateMachine.SetActor("x", Actor);
					mCurrentStateMachine.EnterState("x", "Enter");
					SetParameter("IsEatingOnGround", paramValue: true);
					//mDummyIk = dummyIk;
					BeginCommodityUpdates();
					EWHerbLoreSkill herbSkill = Actor.SkillManager.AddElement(EWHerbLoreSkill.SkillNameID) as EWHerbLoreSkill;
					AddOneShotScriptEventHandler(501u, (SacsEventHandler)(object)new SacsEventHandler(ScriptHandlerOnSimAboutToPlant));
					AnimateSim("EatHarvestable");
					herbSkill.Planted(plant);
					AnimateSim("Exit");
					EndCommodityUpdates(succeeded: true);
					plant.RemoveFromUseList(Actor);
					CurrentSoil.RemoveFromUseList(Actor);
					int skillDifficulty = plant.PlantDef.GetSkillDifficulty();
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
			return true;
		}

		public void SetObjectsToPlant(List<PlantObject.PlantableObjectData> objectsToPlant)
		{
			StyledNotification.Show(new StyledNotification.Format("SetObjectsToPlant",
				StyledNotification.NotificationStyle.kDebugAlert));
			mObjectsToPlant = new List<PlantObject.PlantableObjectData>(objectsToPlant);
			mTotalObjectsToPlant = mObjectsToPlant.Count;
			ConfigureInteraction();
		}

		public static void CleanupPlantInstances(PlantObject.PlantableObjectData data, Sim sim, PlantInteractionType plantInteractionType)
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
				PlantObject.UnreservePlantablePlantingFailed(data.PlantableObject, sim, plantInteractionType);
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
	}
}
