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

namespace Echoweaver.Sims3.CustomCakeConnector
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

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
                IDish meal = e.TargetObject as IDish;
                // If the recipe burns, it's just a fail. Don't replace with the cake object
                if (mealEvent.Quality != Quality.Horrifying && mealEvent.RecipeUsed.HarvestableName == "CakeConnector")
                {
                    // The custom cake must have an NGMP resource connecting the recipe key
                    // to the instance ID of the swapped object.
                    IGameObject cake = GlobalFunctions.CreateObjectOutOfWorld(mealEvent
                        .RecipeUsed.Key);
                    if (cake is ServingContainer)
                    {
                        ((ServingContainerGroup)cake).CookingProcess.Quality = mealEvent.Quality;
                        ((ServingContainerGroup)cake).CookingProcess.SetFinalPreparer(s.SimDescription);
                    }
                    else if (cake is Ingredient)
                    {
                        ((Ingredient)cake).SetQuality(mealEvent.Quality);
                    }

                    if (cake is ServingContainer)
                    {
                        CarrySystem.PutDown(s, SurfaceType.Normal);
                        Vector3 pos = mealEvent.TargetObject.Position;
                        Vector3 forward = mealEvent.TargetObject.ForwardVector;
                        ISurface sur = mealEvent.TargetObject.Parent as ISurface;
                        // The cake object needs to be slotted into the same surface slot where the
                        // completed recipe was set down. I don't expect that the else statement
                        // will be accessed, but I thought it should do something reasonable
                        // if the recipe data is weird.
                        if (sur != null)
                        {
                            SurfaceSlot swapSlot = sur.Surface.GetSurfaceSlotFromContainedObject(mealEvent.TargetObject);
                            mealEvent.TargetObject.Destroy();
                            cake.SetPosition(pos);
                            cake.SetForward(forward);
                            cake.AddToWorld();
                            cake.ParentToSlot(sur, swapSlot.ContainmentSlot);
                        }
                        else
                        {
                            mealEvent.TargetObject.Destroy();
                            cake.SetPosition(pos);
                            cake.SetForward(forward);
                            cake.AddToWorld();
                            CarrySystem.PickUpWithoutRouting(s, (ServingContainer)cake);
                            CarrySystem.PutDown(s, SurfaceType.Normal);
                        }

                    }
                    else if (cake is InventoryItem)
                    {
                        // Just putting in the option of swapping out a recipe for something other
                        // than a cake.
                        CarrySystem.AnimateIntoSimInventory(s);
                        mealEvent.TargetObject.Destroy();
                        s.Inventory.TryToAdd(cake);
                    }
                    else
                    {
                        StyledNotification.Show(new StyledNotification.Format
                            ("CustomCakeConnector: Recipe swap object cannot be instatiated for type "
                            + cake.GetType().ToString(),
                            StyledNotification.NotificationStyle.kDebugAlert));
                    }
                }


            }
            return ListenerAction.Keep;
        }
    }
}

