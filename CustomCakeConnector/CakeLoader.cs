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
                if (mealEvent.Quality != Quality.Horrifying && mealEvent.RecipeUsed.IsHarvestable)
                {
                    if (mealEvent.RecipeUsed.HarvestableName == "")
                    {
                        // The custom cake must have an NGMP resource connecting the recipe key
                        // to the instance ID of the swapped object.
                        IGameObject cake = GlobalFunctions.CreateObjectOutOfWorld(mealEvent
                            .RecipeUsed.Key);
                        if (cake is ServingContainer)
                        {
                            ((ServingContainerGroup) cake).CookingProcess.Quality = mealEvent.Quality;
                            ((ServingContainerGroup) cake).CookingProcess.SetFinalPreparer(s.SimDescription);
                        } else if (cake is Ingredient)
                        {
                            ((Ingredient)cake).SetQuality(mealEvent.Quality);
                        }

                        if (cake is ServingContainer)
                        {
                            CarrySystem.PutDown(s, SurfaceType.Normal);
                            Vector3 pos = mealEvent.TargetObject.Position;
                            Vector3 forward = mealEvent.TargetObject.ForwardVector;
                            mealEvent.TargetObject.Destroy();
                            cake.SetPosition(pos);      
                            cake.SetForward(forward);
                            cake.AddToWorld();
                        } else if (cake is InventoryItem)
                        {
                            CarrySystem.AnimateIntoSimInventory(s);
                            mealEvent.TargetObject.Destroy();
                            s.Inventory.TryToAdd(cake);
                        } else
                        {
                            StyledNotification.Show(new StyledNotification.Format("Recipe swap object cannot be instatiated for type "
                                + cake.GetType().ToString(),
                                StyledNotification.NotificationStyle.kDebugAlert));
                        }
                    }

                }
            }
            return ListenerAction.Keep;
        }
    }
}

