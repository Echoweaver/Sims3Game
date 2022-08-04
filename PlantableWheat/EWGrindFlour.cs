using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Objects.Counters;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Objects.Appliances.FoodProcessor;

namespace Echoweaver.Sims3Game.PlantableWheat
{
	public class EWGrindFlour : Interaction<Sim, FoodProcessor>
	{
		public class Definition : InteractionDefinition<Sim, FoodProcessor, EWGrindFlour>
		{
			public override bool Test(Sim a, FoodProcessor target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
                Ingredient i = a.Inventory.Find<Ingredient>(WheatTest);
				if (i != null)
				{
					return !target.InUse;
				}
				else
				{
					// TODO: Localize!
					greyedOutTooltipCallback = CreateTooltipCallback("You must have wheat in inventory.");
				}
				return false;
			}

			public static bool WheatTest(IGameObject obj, object customData)
			{
				Ingredient ingredient = obj as Ingredient;
				if (ingredient != null && ingredient.IngredientKey == "EWWheat")
				{
					return true;
				}
				return false;
			}

            public override string GetInteractionName(ref InteractionInstanceParameters parameters)
            {
                return "Grind Flour";
            }
		}

		public static InteractionDefinition Singleton = new Definition();

		public void SetFoodProcessorGeoState(StateMachineClient smc, IEvent evt)
		{
			Target.SetGeometryState("full");
			Target.SetMaterial("FoodSludge");
		}

		public override bool Run()
		{
			if (CheckForCancelAndCleanup())
			{
				return false;
			}
			Ingredient wheatItem = Actor.Inventory.Find<Ingredient>(Definition.WheatTest);
			if (wheatItem == null)
			{
				return false;
			}
			CarrySystem.PickUpFromSimInventory(Actor, wheatItem);
			Actor.RouteToObjectRadius(Target, 1f);
            if (!Target.RouteAndCheckInUse(Actor))
			{
				return false;
			}
			StandardEntry();
			EnterStateMachine("FoodProcessor", "Enter - Plate", "x", "FoodProcessor");
			SetParameter("wasInterrupted", false);
			SetActor("Start Platter", wheatItem);
			BowlLarge foodBowl = GlobalFunctions.CreateObject("BowlLarge", Vector3.OutOfWorld, 0,
				Vector3.UnitZ) as BowlLarge;
			SetActor("Stop Platter", foodBowl);

			FoodProp foodProp = FoodProp.Create("foodPrepAngelFoodCake");
            FoodTransferHelper foodTransferHelper = new FoodTransferHelper(wheatItem);
			CarrySystem.PickUpFromSimInventory(Actor, foodBowl);
			PutInContainerHelper putInContainerHelper = new PutInContainerHelper(Target,
				foodBowl, foodProp, ServingContainer.kContainmentSlot);
			PlayFoodVFXHelper playFoodVFXHelper = new PlayFoodVFXHelper(Target, Slot.FXJoint_0,
				"foodProcessorPrepLeftHand");
			AddOneShotScriptEventHandler(100u, new SacsEventHandler(playFoodVFXHelper.Callback));
			AddOneShotScriptEventHandler(108u, new SacsEventHandler(SetFoodProcessorGeoState));
			AddOneShotScriptEventHandler(102u, new SacsEventHandler(foodTransferHelper.HideFood));
			AddOneShotScriptEventHandler(109u, new SacsEventHandler(foodTransferHelper.Destroy));
			AddOneShotScriptEventHandler(103u, new SacsEventHandler(putInContainerHelper.Callback));
			AddOneShotScriptEventHandler(1001u, new SacsEventHandler(Target.StartSound));
			AddOneShotScriptEventHandler(1002u, new SacsEventHandler(Target.StopSound));
			BeginCommodityUpdates();
			AnimateSim("Process");
			AnimateSim("Exit - Bowl");
            CarrySystem.AnimateIntoSimInventory(Actor);
            DestroyObject(foodBowl);
            StandardExit();
			Counter counter = Target.Parent as Counter;
			if (counter != null && counter.IsCleanable)
			{
				counter.Cleanable.DirtyInc(Actor);
			}
			Quality flourQuality = wheatItem.GetQuality();
			wheatItem.Dispose();
			return AddIngredientsToSimInventory(Actor, Loader.kFlourName, 1, flourQuality); 
		}

		public static bool AddIngredientsToSimInventory(Sim sim, string key, int number, Quality quality)
		{
			if (sim == null)
			{
				return false;
			}
			List<IGameObject> ingredientStack = new List<IGameObject>();
			for (int i = 0; i < number; i++)
			{
				IGameObject gameObject = null;
				IngredientData value = null;
				if (IngredientData.NameToDataMap.TryGetValue(key, out value))
				{
					gameObject = Ingredient.Create(value, (Quality)10, false,
                        PlayerDisclosure.Exposed);
				}
				else
				{
					return false;
				}
				ingredientStack.Add(gameObject);
			}
			if (ingredientStack.Count > 0)
			{
				return sim.Inventory.TryToAddStack(ingredientStack);
			}
			return false;
		}
	}
}
