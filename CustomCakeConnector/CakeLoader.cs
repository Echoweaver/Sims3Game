using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.Gameplay.Objects.CookingObjects.CustomCake;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Objects.FoodObjects;
using static Sims3.SimIFace.Simulator;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects;
using Sims3.SimIFace.Enums;
using Sims3.Store.Objects;

namespace Echoweaver.Sims3.CustomCakeConnector
{
    public class Loader
    {
        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kCookedMeal, new ProcessEventDelegate(OnMealCooked));
        }

        public static ListenerAction OnMealCooked(Event e)
        {
            CookedMealEvent mealEvent = (CookedMealEvent)e;
            if (mealEvent != null)
            {
                Sim s = e.Actor as Sim;
                ServingContainer meal = e.TargetObject as ServingContainer;
                // If the recipe burns, it's just a fail. Don't replace with the cake object
                if (mealEvent.Quality != Quality.Horrifying && mealEvent.RecipeUsed.HarvestableName == "CakeConnector")
                {
                    // The custom cake must have an NGMP resource connecting the recipe key
                    // to the instance ID of the swapped object.
                    IGameObject swapped_item = GlobalFunctions.CreateObjectOutOfWorld(mealEvent
                        .RecipeUsed.Key);
                    if (swapped_item is ServingContainer)
                    {
                        ((ServingContainerGroup)swapped_item).CookingProcess.Quality = mealEvent.Quality;
                        ((ServingContainerGroup)swapped_item).CookingProcess.SetFinalPreparer(s.SimDescription);
                    }
                    else if (swapped_item is Ingredient)
                    {
                        ((Ingredient)swapped_item).SetQuality(mealEvent.Quality);
                    }
                        
                    if (swapped_item is ServingContainer)
                    {
                        if (s.Posture is SimCarryingObjectPosture)
                        {
                            CarrySystem.PutDown(s, SurfaceType.Normal);
                        }
                        Vector3 pos = meal.Position;
                        Vector3 forward = meal.ForwardVector;
                        ISurface sur = meal.Parent as ISurface;
                        // The cake object needs to be slotted into the same surface slot where the
                        // completed recipe was set down. 
                        if (sur != null)
                        {
                            SurfaceSlot swapSlot = sur.Surface.GetSurfaceSlotFromContainedObject(meal);
                            meal.Destroy();
                            swapped_item.SetPosition(pos);
                            swapped_item.SetForward(forward);
                            swapped_item.AddToWorld();
                            swapped_item.ParentToSlot(sur, swapSlot.ContainmentSlot);
                        }
                        else
                        {
                            // I don't expect that this else statement
                            // will be accessed, but I thought it should do something reasonable
                            // if the recipe data is weird.
                            meal.Destroy();
                            swapped_item.SetPosition(pos);
                            swapped_item.SetForward(forward);
                            swapped_item.AddToWorld();
                            CarrySystem.PickUpWithoutRouting(s, (ServingContainer)swapped_item);
                            CarrySystem.PutDown(s, SurfaceType.Normal);
                        }
                    }
                    else if (s.Inventory.CanAdd(swapped_item))
                    {
                        if (!(s.Posture is SimCarryingObjectPosture))
                        {
                            CarrySystem.PickUp(s, meal);
                        }
                        // Just putting in the option of swapping out a recipe for something other
                        // than a cake.
                        CarrySystem.AnimateIntoSimInventory(s);
                        meal.Destroy();

                        // It appears this field can't be empty, so if it's unused, presumably it defaults
                        // to 1?
                        if (mealEvent.RecipeUsed.NumberPetFoodCreated > 1)
                        {
                            for (int i = 0; i < mealEvent.RecipeUsed.NumberPetFoodCreated - 1; ++i)
                            {
                                IGameObject cakeClone = swapped_item.Clone();
                                if (!s.Inventory.TryToAdd(cakeClone))
                                {
                                    StyledNotification.Show(new StyledNotification.Format
                                        ("CustomCakeConnector: Error creating total="
                                        + mealEvent.RecipeUsed.NumberPetFoodCreated + " inventory items of type "
                                        + swapped_item.GetType().ToString(),
                                        StyledNotification.NotificationStyle.kDebugAlert));
                                    break;
                                }
                            }
                        } 
                        s.Inventory.TryToAdd(swapped_item);
                    }
                    else
                    {
                        StyledNotification.Show(new StyledNotification.Format
                            ("CustomCakeConnector: Recipe swap object cannot be instatiated for type "
                            + swapped_item.GetType().ToString(),
                            StyledNotification.NotificationStyle.kDebugAlert));
                    }
                }


            }
            return ListenerAction.Keep;
        }
    }
}

