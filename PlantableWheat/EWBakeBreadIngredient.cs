using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.TuningValues;
using Sims3.SimIFace;
using Sims3.Store.Objects;

namespace Echoweaver.Sims3Game.PlantableWheat
{
	public class EWBakeBreadIngredient : Interaction<Sim, WoodFireOven>
	{
		public class Definition : InteractionDefinition<Sim, WoodFireOven, EWBakeBreadIngredient>
		{
			public override bool Test(Sim a, WoodFireOven target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				Ingredient i = a.Inventory.Find<Ingredient>(FlourTest);
				Ingredient j = a.Inventory.Find<Ingredient>(EggTest);
				if (i != null)
				{
					return !target.InUse;
				}
				else
				{
					// TODO: Localize!
					greyedOutTooltipCallback = CreateTooltipCallback("You must have flour and egg in inventory.");
				}
				return false;
			}

			public static bool FlourTest(IGameObject obj, object customData)
			{
				Ingredient ingredient = obj as Ingredient;
				if (ingredient != null && ingredient.IngredientKey == Loader.kFlourName)
				{
					return true;
				}
				return false;
			}

			public static bool EggTest(IGameObject obj, object customData)
			{
				Ingredient ingredient = obj as Ingredient;
				if (ingredient != null && ingredient.IngredientKey == "Egg")
				{
					return true;
				}
				return false;
			}

			public override string GetInteractionName(ref InteractionInstanceParameters parameters)
			{
				// TODO: Localize
				return "Bake Sandwich Bread";
			}
		}

		public static InteractionDefinition Singleton = new Definition();

		public override bool Run()
		{
			if (CheckForCancelAndCleanup())
			{
				return false;
			}

			Ingredient flourItem = Actor.Inventory.Find<Ingredient>(Definition.FlourTest);
			Ingredient eggItem = Actor.Inventory.Find<Ingredient>(Definition.EggTest);
			if (flourItem == null || eggItem == null)
			{
				return false;
			}

			Actor.RouteToSlot(Target, Slot.RoutingSlot_0);
				Target.mFailedToCook = false;

			BeginCommodityUpdates();
			EnterStateMachine("woodfiredoven_store", "Enter", "x", "WoodFireOvenClassic");
			if (Actor.CarryStateMachine != null)
			{
				Target.RemoveFoodItems(Actor);
			}
			ObjectGuid trayPropGuid = GlobalFunctions.CreateProp("FoodTray", ProductVersion.BaseGame,
				Vector3.OutOfWorld, 0, Vector3.UnitZ);
			mCurrentStateMachine.SetPropActor("FoodTray", trayPropGuid);
			GameObject trayPropObj = GameObject.GetObject(trayPropGuid);
			if (trayPropObj != null)
			{
				Target.RemoveContinueCookingInteraction(trayPropObj);
			}
			mCurrentStateMachine.RequestState(true, "x", "BakeBread");
			if (trayPropObj != null)
			{
				trayPropObj.UnParent();
				trayPropObj.Destroy();
			}
			Recipe breadRecipe = null;
			Recipe.NameToRecipeHash.TryGetValue("WOBakeBreadCountry", out breadRecipe);
			if (breadRecipe == null)
			{
				EarlyExit();
				return false;
			}
			Target.mCurrentRecipe = breadRecipe;
			bool isBurnt = CheckBurnt(Actor, breadRecipe, mCurrentStateMachine);
			Quality resultQuality = GetQuality(Actor, breadRecipe, eggItem.GetQuality(), flourItem.GetQuality(),
				isBurnt);

			//Quality resultQuality = Target.GetFoodQuality(Actor);
			IFoodContainer finishedRecipe = Target.mCurrentRecipe.CreateFinishedFood(Recipe.MealQuantity.Group,
				resultQuality);
			if (finishedRecipe == null)
			{
				EarlyExit();
				return false;
			}
			ServingContainer containerProp = finishedRecipe as ServingContainer;
			FoodProp containedFood = containerProp.GetContainedFood();
			if (containedFood != null)
			{
				SetActor("CookedFood", containedFood);
				Target.mCookedFood = containedFood;
			}
			SetActor("PlateServing", containerProp);
			AnimateSim("CookLoop");
			DoLoop(ExitReason.Default, new InsideLoopFunction(BakeLoopDelegate), mCurrentStateMachine);
			if (Actor.ExitReason != ExitReason.StageComplete)
			{
				EarlyExit();
				return false;
			}
			AnimateSim("CookEndBread");
			AnimateSim("Exit");
            CarrySystem.EnterWhileHolding(Actor, containerProp);
			CarrySystem.AnimateIntoSimInventory(Actor);
			StandardExit();
			flourItem.Dispose();
			eggItem.Dispose();
			DestroyObject(containedFood);
			DestroyObject(containerProp);
			return AddIngredientsToSimInventory(Actor, "Bread", 1, resultQuality);
		}

		public void BakeLoopDelegate(StateMachineClient smc, LoopData loopData)
		{
			if (loopData.mLifeTime > WoodFireOven.kBakingTime)
			{
				Actor.AddExitReason(ExitReason.StageComplete);
				Target.SetBurned();
			}
			Target.CheckForMotiveFailure(Actor);
		}

		public void EarlyExit()
		{
			AnimateSim("CookEndBread");
			EndCommodityUpdates(true);
			StandardExit();
			Target.mFailedToCook = true;
			AnimateSim("Exit");
			Target.RemoveFoodItems(Actor);
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
					gameObject = Ingredient.Create(value, quality, false,
						Sims3.Gameplay.Objects.Gardening.PlayerDisclosure.Exposed);
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

		public Quality GetQuality(Sim cook, Recipe recipe, Quality quality1, Quality quality2, bool isBurnt)
		{
			// This is a simulation of what normal recipe quality calculation looks like.
			// Since we can't count the number of times the recipe was made, I'm just using raw
			// skill level.

			Cooking skill = cook.SkillManager.GetSkill<Cooking>(SkillNames.Cooking);
			int foodPoints = 0;
			int naturalCookPoints = cook.HasTrait(TraitNames.NaturalCook)
				? TraitTuning.NaturalCookTraitEnhanceFoodPoints : 0;
			int bornToCookPoints = cook.HasTrait(TraitNames.BornToCook)
				? TraitTuning.BornToCookTraitEnhanceFoodPoints : 0;

			if (!isBurnt)
            {
				int recipePoints = Cooking.RecipeLevelFoodPoints[recipe.CookingSkillLevelRequired];
				int skillPoints = (int)(Cooking.FoodPointBonusPerTimesCooked * skill.SkillLevel);
				int ingredientPoints = Ingredient.QualityToIngredientPointMap[quality1]
					+ Ingredient.QualityToIngredientPointMap[quality2];
				foodPoints = recipePoints + ingredientPoints + skillPoints + naturalCookPoints
					+ bornToCookPoints;
			} else
            {
				foodPoints = Food.kNumFoodPointsBurnt + naturalCookPoints + bornToCookPoints;

			}
			return Cooking.GetQualityFromFoodPoints(recipe, foodPoints);
		}

		public static bool CheckBurnt(Sim sim, Recipe recipe, StateMachineClient stateMachine)
		{
			Cooking skill = sim.SkillManager.GetSkill<Cooking>(SkillNames.Cooking);
			float num = (skill != null) ? skill.CalculateChanceOfFailure(recipe) : 25f;

			BuffManager buffManager = sim.BuffManager;
			if (buffManager.HasLuckyBuff())
			{
				num *= TraitTuning.FeelingLuckyTraitBurnFoodMultiplier;
			}
			if (buffManager.HasUnluckyBuff())
			{
				num *= TraitTuning.FeelingUnluckyTraitBurnFoodMultiplier;
			}
			bool isBurnt = RandomUtil.RandomChance(num);
			if (isBurnt)
			{
				EventTracker.SendEvent(EventTypeId.kBurntMeal, sim);
				TraitFunctions.TraitReactionOnFailure(sim, ReactionSpeed.AfterInteraction,
					Origin.FromBurningFood);
			}
			stateMachine.SetParameter("burn", isBurnt);
			return isBurnt;
		}

	}
}
