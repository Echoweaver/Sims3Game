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
        public static string kFlourName = "Flour";

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
            // TODO: Is this the best way to check for a newly added object?
            EventTracker.AddListener(EventTypeId.kBoughtObject, new ProcessEventDelegate(OnNewObject));
        }

        public static ListenerAction OnNewObject(Event e)
        {
            FoodProcessor p = e.TargetObject as FoodProcessor;
            if (p != null)
            {
                p.AddInteraction(EWGrindFlour.Singleton, true);
            }
            return ListenerAction.Keep;
        }

        public static void OnWorldLoadStarted(object sender, System.EventArgs e)
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            string error_list = "";
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x907C1DF037C7A6D2,
                0x0333406C, 0x00000000), false);

            if (data != null)
            {
                XmlDbTable xmlDbTable = data.Tables["Data"];
                foreach (XmlDbRow row in xmlDbTable.Rows)
                {
                    string recipe_key = row.GetString("Recipe_Key");
                    if (Recipe.NameToRecipeHash.ContainsKey(recipe_key))
                    {
                        Recipe.NameToRecipeHash.Remove(recipe_key);
                        Recipe.AddNewRecipe(row, false);

                    } else
                    {
                        error_list += "  " + recipe_key + " NOT FOUND.";
                    }
                }
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
