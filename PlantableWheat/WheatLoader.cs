using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PlantableWheat
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            World.sOnWorldLoadStartedEventHandler += new EventHandler(OnWorldLoadStarted);
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinished);
        }

        public static void OnWorldLoadFinished(object sender, System.EventArgs e)
        {
            foreach (GameObject o in Sims3.Gameplay.Queries.GetObjects<FoodProcessor>())
            {
                o.AddInteraction(EWGrindFlour.Singleton);
            }
            // TODO: Is this the best way to check for a newly added object?
            EventTracker.AddListener(EventTypeId.kBoughtObject, new ProcessEventDelegate(OnNewObject));
        }

        public static ListenerAction OnNewObject(Event e)
        {
            // TODO: Add interaction to new food processors
            return ListenerAction.Keep;
        }

        public static void OnWorldLoadStarted(object sender, System.EventArgs e)
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            string error_list = "";
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("WheatPlant"),
                0x0333406C, 0x00000000), false);

            if (data != null)
            {
                PlantDefinition.ParsePlantDefinitionData(data, false);
            } else
            {
                error_list += "Plant data null.";
            }

            data = null;
            data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("WheatIngredients"),
                0x0333406C, 0x00000000), false);

            if (data != null)
            {
                IngredientData.LoadIngredientData(data, false);
                Grocery.mItemDictionary.Clear();
                Grocery.LoadData();
            }
            else
            {
                error_list += "  Ingredient data null.";
            }

            data = null;
            data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("WheatRecipes"),
                0x0333406C, 0x00000000), false);

            if (data != null)
            {
                if (Recipe.NameToRecipeHash.ContainsKey("Pancakes"))
                {
                    Recipe.NameToRecipeHash.Remove("Pancakes");
                }
                Recipe.LoadRecipeData(data, false);
            }
            else
            {
                error_list += "  Recipe data null.";
            }

            if (!error_list.Equals(""))
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR Plantable Wheat: " + error_list,
                    StyledNotification.NotificationStyle.kDebugAlert));
                return;
            }
        }
    }
}
