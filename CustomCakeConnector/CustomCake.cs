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
using static Sims3.Gameplay.Objects.CookingObjects.Cake;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Resort;
using Sims3.SimIFace.Enums;
using static Sims3.Gameplay.ActorSystems.SimQueue;
using static Sims3.SimIFace.Route;

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
            //SetGeoStates();
        }

        public override void OnStartup()
        {
            base.OnStartup();
            AddInteraction(Serve.Singleton, true);
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
            AddInteraction(Serve.Singleton, true);
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

    public class CustomIngredientGroup : WeddingCake
    {
        string ingredient_key = "Cheese";
        int aging_minutes = 0;
        bool isReady = false;
        public AlarmHandle mAgingAlarm = AlarmHandle.kInvalidHandle;

        public class CustomCheeseCookingProcess : CookingProcess
        {
            public CustomCheeseCookingProcess()
            {
            }
        }

        public override void OnCreation()
        {
            base.OnCreation();
            // Cheese geostates:Raw-0x3E72D935, Aged-0xAFEB2F22, AgedHalf-0x291AC721
        }

        public override void OnStartup()
        {
            base.OnStartup();
            SetIngredientInfo();
            if (aging_minutes > 0)
            {
                SetGeometryState("Raw");
                // create timer
                mAgingAlarm = this.AddAlarm(aging_minutes, TimeUnit.Minutes, FinishAging,
                    ingredient_key + " IngredientGroup Aging Alarm", AlarmType.AlwaysPersisted);
            }
            else
            {
                isReady = true;
                SetGeometryState("Aged");
            }
            kNumSlices = 6;
            mNumSlicesLeft = 6;
            RemoveInteractionByType(CutWeddingCake.Singleton);
            AddInteraction(GrabIngredient.Singleton, true);
            AddInteraction(GrabIngredient.SingletonAll, true);
        }

        public void SetIngredientInfo()
        {
            if (string.IsNullOrEmpty(base.ObjectInstanceName))
                return;
            string[] pieces = base.ObjectInstanceName.Split(':');
            string instance_string = pieces[0];
            if (pieces.Length > 1)
            {
                if (int.TryParse(pieces[1], out int instance_int))
                {
                    aging_minutes = instance_int;
                }
            }
            if (IngredientData.NameToDataMap.ContainsKey(instance_string))
            {
                ingredient_key = instance_string;
            }
        }

        public int GetRemainingTime()
        {
            return 0;
        }

        public void FinishAging()
        {
            aging_minutes = 0;
            isReady = true;
            SetGeometryState("Aged");
        }

        public override void DecrementServings()
        {
            DecrementServings(false);
        }

        public void DecrementServings(bool getAll)
        {
            if (getAll)
            {
                mNumSlicesLeft = 0;
            } else
            {
                mNumSlicesLeft--;
            }
            if (mNumSlicesLeft <= (kNumSlices/2))
            {
                AmountLeft = AmountLeftState.Half;
                SetGeometryState("AgedHalf");
            }
            else if (mNumSlicesLeft == 0)
            {
                CreateDirtyReactionBroadcaster();
                AmountLeft = AmountLeftState.Empty;
                SetGeometryState("used");
            }
        }

        public class GrabIngredient : Interaction<Sim, CustomIngredientGroup>, IPushAsContinuationCanFail
        {
            public class Definition : InteractionDefinition<Sim, CustomIngredientGroup, GrabIngredient>
            {
                public bool getAll = false;

                public Definition()
                {
                }

                public Definition(bool p_getAll)
                {
                    getAll = p_getAll;
                }

                public override string GetInteractionName(Sim a, CustomIngredientGroup target,
                    InteractionObjectPair interaction)
                {
                    if (getAll)
                    {
                        return "Grab ALL slices";
                    }
                    return "Grab One slice";
                        //Localization.LocalizeString("Gameplay/Objects/CookingObjects/PizzaBox:GrabASlice");
                }

                public override bool Test(Sim a, CustomIngredientGroup target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (target.Parent != null && !(target.Parent is ISurface) && (target.Parent != a))
                    {
                        return false;
                    }
                    if (!target.isReady)
                    {
                        if (getAll)
                        {
                            // Localize!!
                            greyedOutTooltipCallback = CreateTooltipCallback("This is not ready");
                        }
                        return false;
                    }
                    if (target.HasFoodLeft())
                    {
                        return true;
                    }
                    return false;
                }
            }

            public static InteractionDefinition Singleton = new Definition();
            public static InteractionDefinition SingletonAll = new Definition(true);


            public override bool Run()
            {
                if (RunBodyForGrabIngredient(Actor, Target, this, bInInventory: false))
                {
                    return true;
                }
                return false;
            }

            public override bool RunFromInventory()
            {
                if (RunBodyForGrabIngredient(Actor, Target, this, bInInventory: true))
                {
                    return true;
                }
                return false;
            }

            public bool RunBodyForGrabIngredient(Sim Actor, CustomIngredientGroup Target,
                GrabIngredient instance, bool bInInventory)
            {
                bool getAll = ((Definition)InteractionDefinition).getAll;
                ISurface locSurface = null;
                SurfaceSlot surfaceSlot = null;
                SurfaceHeight surfaceHeight = SurfaceHeight.Floor;
                if (!bInInventory)
                {
                    if (Target.SimLine != null)
                    {
                        Route route = Actor.CreateRoute();
                        route.SetOption(RouteOption.DoLineOfSightCheckUserOverride, true);
                        route.PlanToPointRadialRange(Target.Position, Target.CarryRouteToObjectRadius,
                            2f, Vector3.UnitZ, 360f, RouteDistancePreference.PreferNearestToRouteOrigin,
                            RouteOrientationPreference.TowardsObject);
                        Actor.DoRoute(route);
                        if (!Target.SimLine.WaitForTurn(instance, WaitBehavior.CutAheadOfLowerPrioritySims,
                            ExitReason.Default, kTimeToWaitInLine))
                        {
                            return false;
                        }
                    }
                    if (Target.Parent != null)
                    {
                        IGameObject parent = Target.Parent;
                        locSurface = (parent is ISurface) ? (ISurface)parent : null;
                        if (locSurface == null)
                        {
                            return false;
                        }
                        SurfacePair surface = locSurface.Surface;
                        surfaceSlot = surface.GetSurfaceSlotFromContainedObject(Target);
                        if (surfaceSlot == null)
                        {
                            if (locSurface.Surface.AddOn.SurfaceSlots.Count > 0)
                            {
                                surfaceSlot = locSurface.Surface.AddOn.SurfaceSlots[0];
                            }
                            else
                            {
                                surfaceHeight = SurfaceHeight.Table;
                            }
                        }
                        if (surfaceSlot != null)
                        {
                            if (!surfaceSlot.RouteToSurfaceSlot(Actor, locSurface))
                            {
                                return false;
                            }
                            surfaceHeight = surfaceSlot.Height;
                        }
                    }
                    else if (!Actor.RouteToObjectRadius(Target, Target.CarryRouteToObjectRadius))
                    {
                        return false;
                    }
                }
                if (Target.InUse || (Target.Parent != locSurface && !bInInventory))
                {
                    Actor.AddExitReason(ExitReason.ObjectInUse);
                    return false;
                }
                if (!Target.HasFoodLeft())
                {
                    return false;
                }
                StandardEntry();
                IngredientData data = null;
                IngredientData.NameToDataMap.TryGetValue(Target.ingredient_key, out data);
                if (data == null)
                    return false;
                Ingredient ing = Ingredient.Create(data, Target.GetQuality(), false, Gardening.PlayerDisclosure.Exposed);
                ing.AddToWorld();
                CarrySystem.PickUpWithoutRouting(Actor, ing);
                CarrySystem.AnimateIntoSimInventory(Actor);
                if (getAll)
                {
                    for (int i = 0; i < Target.mNumSlicesLeft - 1; ++i)
                    {
                        IGameObject ingClone = ing.Clone();
                        if (!Actor.Inventory.TryToAdd(ingClone))
                        {
                            StyledNotification.Show(new StyledNotification.Format
                                ("CustomCakeConnector: Error creating total="
                                + Target.mNumSlicesLeft + " ingredients of type "
                                + Target.ingredient_key,
                                StyledNotification.NotificationStyle.kDebugAlert));
                            break;
                        }
                    }
                }
                Actor.Inventory.TryToAdd(ing);
                Target.DecrementServings(getAll);
                if (!Target.HasFoodLeft())
                {
                    Target.mDirtyAlarm = Target.AddAlarm(kMinutesUntilDirty, TimeUnit.Minutes,
                        new AlarmTimerCallback(Target.AddDirtyCallback),
                        "Serving Container: Make Dirty", AlarmType.AlwaysPersisted);
                }
                StandardExit();
                return true;
            }

        }
    }

}