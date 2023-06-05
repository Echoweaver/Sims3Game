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
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.Store.Objects;

namespace Echoweaver.Sims3Game.PlantableWheat
{

	public class EWBakeIngredient : Interaction<Sim, WoodFireOven>
	{

		public class DefinitionBase : InteractionDefinition<Sim, WoodFireOven, EWBakeIngredient>
		{
			public string recipe_name = "WOBakeBreadCountry";
			public string interaction_name = Loader.Localize("BakeBread");
			public string ingredient_name = "Bread";

			public DefinitionBase(string p_recipe, string p_interaction, string p_ingredient)
			{
				recipe_name = p_recipe;
				interaction_name = p_interaction;
				ingredient_name = p_ingredient;
			}

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
					greyedOutTooltipCallback = CreateTooltipCallback(Loader.Localize("BreadIngredients"));
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


            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                    Localization.LocalizeString (Loader.Localize("BakeBread"))
                };
            }

            public override string GetInteractionName(ref InteractionInstanceParameters parameters)
			{
				return interaction_name;
			}

		}

		public static InteractionDefinition BreadSingleton = new DefinitionBase("WOBakeBreadCountry",
            Localization.LocalizeString(0xC73DD5A96B067B33), "Bread");
        public static InteractionDefinition RollsSingleton = new DefinitionBase("BSBakeDinnerRoll",
            Localization.LocalizeString(0x85C3CDCD3083615D), "Buns");
        public static InteractionDefinition LongRollsSingleton = new DefinitionBase("BSBakeBaguette",
            Localization.LocalizeString(0xB918BD96E2868C3B), "Long Buns");

        Cooking skill;

		public override bool Run()
		{
			if (CheckForCancelAndCleanup())
			{
				return false;
			}

			skill = Actor.SkillManager.GetSkill<Cooking>(SkillNames.Cooking);
			if (skill == null)
			{
				skill = (Actor.SkillManager.AddElement(SkillNames.Cooking) as Cooking);
				if (skill == null)
				{
					return false;
				}
			}

			Ingredient flourItem = Actor.Inventory.Find<Ingredient>(DefinitionBase.FlourTest);
			Ingredient eggItem = Actor.Inventory.Find<Ingredient>(DefinitionBase.EggTest);

			if (flourItem == null || eggItem == null)
			{
				return false;
			}


			if (!Actor.RouteToSlot(Target, Slot.RoutingSlot_0))
            {
				StandardExit();
				return false;
            }
			Target.mFailedToCook = false;

			BeginCommodityUpdates();
			skill.StartSkillGain(5f);
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
			string recipe_name = (this.InteractionDefinition as DefinitionBase).recipe_name;
			Recipe breadRecipe = null;
			Recipe.NameToRecipeHash.TryGetValue(recipe_name, out breadRecipe);
			if (breadRecipe == null)
			{
				EarlyExit();
				return false;
			}
			Target.mCurrentRecipe = breadRecipe;
			Target.mFoodWillBurn = CheckBurnt(Actor, breadRecipe, mCurrentStateMachine);
			Quality resultQuality = GetQuality(Actor, breadRecipe, eggItem.GetQuality(), flourItem.GetQuality());

			//Quality resultQuality = Target.GetFoodQuality(Actor);
			IFoodContainer finishedRecipe = Target.mCurrentRecipe.CreateFinishedFood(Recipe.MealQuantity.Group,
				Quality.Perfect);
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
			skill.StopSkillGain();
            CarrySystem.EnterWhileHolding(Actor, containerProp);
			CarrySystem.AnimateIntoSimInventory(Actor);
			StandardExit();
			flourItem.Dispose();
			eggItem.Dispose();
			DestroyObject(containedFood);
			DestroyObject(containerProp);
			string ingredient_name = (this.InteractionDefinition as DefinitionBase).ingredient_name;
			return EWGrindFlour.AddIngredientsToSimInventory(Actor, ingredient_name, 1, resultQuality);
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

		public Quality GetQuality(Sim cook, Recipe recipe, Quality quality1, Quality quality2)
		{
			// This is a simulation of what normal recipe quality calculation looks like.
			// Since we can't count the number of times the recipe was made, I'm just using raw
			// skill level.

			int foodPoints = 0;
			int naturalCookPoints = cook.HasTrait(TraitNames.NaturalCook)
				? TraitTuning.NaturalCookTraitEnhanceFoodPoints : 0;
			int bornToCookPoints = cook.HasTrait(TraitNames.BornToCook)
				? TraitTuning.BornToCookTraitEnhanceFoodPoints : 0;

			if (Target.mFoodWillBurn)
            {
                foodPoints = Food.kNumFoodPointsBurnt + naturalCookPoints + bornToCookPoints;
			} else
            {
                int recipePoints = Cooking.RecipeLevelFoodPoints[recipe.CookingSkillLevelRequired];
                int skillPoints = (int)(Cooking.FoodPointBonusPerTimesCooked * skill.SkillLevel);
                int ingredientPoints = Ingredient.QualityToIngredientPointMap[quality1]
                    + Ingredient.QualityToIngredientPointMap[quality2];
                foodPoints = recipePoints + ingredientPoints + skillPoints + naturalCookPoints
                    + bornToCookPoints;
            }
            return Cooking.GetQualityFromFoodPoints(recipe, foodPoints);
		}

		public bool CheckBurnt(Sim sim, Recipe recipe, StateMachineClient stateMachine)
		{
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
			Target.mFoodWillBurn = isBurnt;
			stateMachine.SetParameter("burn", isBurnt);
			return isBurnt;
		}

	}
}
