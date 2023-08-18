using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.Gameplay.Objects.FoodObjects;
using System.Collections.Generic;
using Sims3.UI;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using System;

namespace Sims3.Gameplay.Objects.CookingObjects.CustomCake
{
    public class CustomBirthdayCake : Cake
    {
        public class CustomCakeCookingProcess : CookingProcess
        {
            public CustomCakeCookingProcess()
            {
            }

            public CustomCakeCookingProcess(Cake cake, string recipeHash)
                : base(Recipe.NameToRecipeHash[recipeHash], null, null, null, Recipe.MealDestination.SurfaceOrEat,
                      Recipe.MealQuantity.Group, Recipe.MealRepetition.MakeOne, null, null, cake, null)
            {
            }
        }

        public override void OnCreation()
        {
            base.OnCreation();
            CookingProcess = new CustomCakeCookingProcess(this, GetRecipeKey());
            SetGeoStates();
        }

        public override void OnStartup()
        {
            base.OnStartup();
            AddInteraction(Serve.Singleton);
        }

        public string GetRecipeKey()
        {
            if (!string.IsNullOrEmpty(base.ObjectInstanceName) && Recipe.NameToRecipeHash
                .ContainsKey(base.ObjectInstanceName))
            {
                return base.ObjectInstanceName;
            }
            return "Cake Slice";
        }

        [DoesntRequireTuning]
        public class Serve : ImmediateInteraction<Sim, Cake>
        {
            public class Definition : ImmediateInteractionDefinition<Sim, Cake, Serve>
            {
                public override bool Test(Sim a, Cake target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (!target.mbIsServed && a.IsHuman)
                    {
                        return true;
                    }
                    return false;
                }

                public override string GetInteractionName(Sim actor, Cake target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString("Gameplay/Objects/CookingObjects/PizzaBox/PizzaBox_Serve:InteractionName");
                }
            }

            public static readonly InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SetToServed();
                return true;
            }
        }

    }

    public class CustomNonBirthdayCake : Cake
    {
        public class CustomCakeCookingProcess : CookingProcess
        {
            public CustomCakeCookingProcess()
            {
            }

            public CustomCakeCookingProcess(Cake cake, string recipeHash)
                : base(Recipe.NameToRecipeHash[recipeHash], null, null, null, Recipe.MealDestination.SurfaceOrEat,
                      Recipe.MealQuantity.Group, Recipe.MealRepetition.MakeOne, null, null, cake, null)
            {
            }
        }

        public override void OnCreation()
        {
            base.OnCreation();
            CookingProcess = new CustomCakeCookingProcess(this, GetRecipeKey());
            SetGeoStates();
        }

        public override void OnStartup()
        {
            base.OnStartup();
            RemoveInteractionByType(HaveBirthdayFor.Singleton);
            AddInteraction(CustomBirthdayCake.Serve.Singleton);
        }

        public string GetRecipeKey()
        {
            if (!string.IsNullOrEmpty(base.ObjectInstanceName) && Recipe.NameToRecipeHash.ContainsKey(base.ObjectInstanceName))
            {
                return base.ObjectInstanceName;
            }
            return "Cake Slice";
        }
    }


    public class CustomWeddingCake : WeddingCake
    {
        public class CustomWeddingCakeCookingProcess : CookingProcess
        {
            public CustomWeddingCakeCookingProcess()
            {
            }

            public CustomWeddingCakeCookingProcess(WeddingCake cake, string recipeHash)
                : base(Recipe.NameToRecipeHash[recipeHash], null, null, null, Recipe.MealDestination.SurfaceOrEat,
                      Recipe.MealQuantity.Group, Recipe.MealRepetition.MakeOne, null, null, cake, null)
            {
            }
        }

        public override void OnCreation()
        {
            base.OnCreation();
            CookingProcess = new CustomWeddingCakeCookingProcess(this, GetRecipeKey());
            SetGeometryState("full");
        }

        public override void OnStartup()
        {
            base.OnStartup();
            AddInteraction(Serve.Singleton);
        }

        public string GetRecipeKey()
        {
            if (!string.IsNullOrEmpty(base.ObjectInstanceName) && Recipe.NameToRecipeHash.ContainsKey(base.ObjectInstanceName))
            {
                return base.ObjectInstanceName;
            }
            return "Wedding Cake Slice";
        }

        private sealed class Serve : ImmediateInteraction<Sim, WeddingCake>
        {
            public sealed class Definition : ImmediateInteractionDefinition<Sim, WeddingCake, Serve>
            {
                public override bool Test(Sim a, WeddingCake target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (!target.mbIsServed && a.IsHuman)
                    {
                        return true;
                    }
                    return false;
                }

                public override string GetInteractionName(Sim actor, WeddingCake target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString("Gameplay/Objects/CookingObjects/PizzaBox/PizzaBox_Serve:InteractionName");
                }
            }

            public static readonly InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SetToServed();
                return true;
            }
        }
    }


}