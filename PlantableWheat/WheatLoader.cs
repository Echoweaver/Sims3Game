using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.Store.Objects;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PlantableWheat
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;
        [Tunable]
        public static string kFlourName = "Flour";
        public static bool kIngredientsOverhaul = false;
        public static string stblKey = "Gameplay/Excel/Ingredients/EWWheatData:";

        static Loader()
        {
            World.sOnWorldLoadStartedEventHandler += new EventHandler(OnWorldLoadStarted);
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinished);
        }

        public static void OnWorldLoadFinished(object sender, System.EventArgs e)
        {
            foreach (GameObject o in Sims3.Gameplay.Queries.GetObjects<FoodProcessor>())
            {
                o.AddInteraction(EWGrindFlour.Singleton, true);
            }

            if (kIngredientsOverhaul)
            {
                foreach (GameObject o in Sims3.Gameplay.Queries.GetObjects<WoodFireOven>())
                {
                    o.AddInteraction(EWBakeBreadIngredient.Singleton, true);
                }
            }

            EventTracker.AddListener(EventTypeId.kBoughtObject, new ProcessEventDelegate(OnNewObject));
        }

        public static ListenerAction OnNewObject(Event e)
        {
            FoodProcessor p = e.TargetObject as FoodProcessor;
            if (p != null)
            {
                p.AddInteraction(EWGrindFlour.Singleton, true);
            }

            if (kIngredientsOverhaul)
            {
                WoodFireOven w = e.TargetObject as WoodFireOven;
                if (w != null)
                {
                    w.AddInteraction(EWBakeBreadIngredient.Singleton, true);
                }
            }
            return ListenerAction.Keep;
        }

        public static void OnWorldLoadStarted(object sender, System.EventArgs e)
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            string error_list = "";
            XmlDbData data;
            if (IngredientData.NameToDataMap.ContainsKey("Bread"))
            {
                kIngredientsOverhaul = true;

                data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("WheatRecipes_Overhaul"),
                    0x0333406C, 0x00000000), false);
            } else
            {
                data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("CCL621144765_Recipes.xml"),
                    0x0333406C, 0x00000000), false);
            }

            if (data != null)
            {
                XmlDbTable xmlDbTable = data.Tables["Data"];
                foreach (XmlDbRow row in xmlDbTable.Rows)
                {
                    string recipe_key = row.GetString("Recipe_Key");
                    Recipe recipe;
                    if (Recipe.NameToRecipeHash.TryGetValue(recipe_key, out recipe))
                    {
                        Recipe.NameToRecipeHash.Remove(recipe_key);
                        Recipe.Recipes.Remove(recipe);
                        Recipe.AddNewRecipe(row, false);
                    } else
                    {
                        error_list += "  " + recipe_key + " NOT FOUND.";
                    }
                }
            }
            else
            {
                error_list += "  Base Game Recipe data error.";
            }

            // Replace Wood Fired Oven recipes
            data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("WheatRecipes_WoodOven"),
                0x0333406C, 0x00000000), false);

            if (data != null)
            {
                XmlDbTable xmlDbTable = data.Tables["Data"];
                foreach (XmlDbRow row in xmlDbTable.Rows)
                {
                    string recipe_key = row.GetString("Recipe_Key");
                    Recipe recipe;
                    if (Recipe.NameToRecipeHash.TryGetValue(recipe_key, out recipe))
                    {
                        Recipe.NameToRecipeHash.Remove(recipe_key);
                        Recipe.Recipes.Remove(recipe);
                        Recipe.AddNewRecipe(row, true);
                    }
                    else
                    {
                        error_list += "  " + recipe_key + " NOT FOUND.";
                    }
                }
            }
            else
            {
                error_list += "  Store Recipe data null.";
            }

            if (!error_list.Equals(""))
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR Plantable Wheat: " + error_list,
                    StyledNotification.NotificationStyle.kDebugAlert));
                return;
            }

            
        }

        public static string Localize(string key)
        {
           return Localization.LocalizeString(stblKey + key);
        }
    }
}
