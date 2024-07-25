using System;
using System.Collections.Generic;
using System.Xml;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.BuildBuy;
using Sims3.UI;
using static Sims3.SimIFace.PropertyStreamWriter;
using static Sims3.SimIFace.UserToolUtils;
using static Sims3.SimIFace.World;
//using Queries = Sims3.Gameplay.Queries;


//Template Created by Battery

namespace Echoweaver.Sims3Game.StatStandardizer
{
    public class Loader
    {
        [Tunable] protected static bool init;
        [Tunable] protected static bool kStatStandardizerDebug = true;

        public static int callcount = 0;

        static Loader()
        {
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
        }

        static void OnWorldLoaded(object sender, EventArgs e)
        {
            AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
                "Hello World Alarm", AlarmType.NeverPersisted, null);
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState,
                new ProcessEventDelegate(OnEnterWorldState));
            EventTracker.AddListener(EventTypeId.kExitInWorldSubState,
                new ProcessEventDelegate(OnExitWorldState));
        }


        public static void Initialize()
        {
            DebugNote("Stat Standardizer Debug ON");
        }

        public static ListenerAction OnEnterWorldState(Event e)
        {
            InWorldSubStateEvent inWorldSubStateEvent = e as InWorldSubStateEvent;
            if (inWorldSubStateEvent == null)
            {
                return ListenerAction.Keep;
            }
            InWorldSubState state = inWorldSubStateEvent.State;
            if (state.StateId == 2)  // Buy  Mode
            {
                Simulator.AddObject(new Sims3.Gameplay.OneShotFunctionTask(OnEnterBuyMode));
            }

            return ListenerAction.Keep;
        }

        public static ListenerAction OnExitWorldState(Event e)
        {
            InWorldSubStateEvent inWorldSubStateEvent = e as InWorldSubStateEvent;
            if (inWorldSubStateEvent == null)
            {
                return ListenerAction.Keep;
            }
            InWorldSubState state = inWorldSubStateEvent.State;
            if (state.StateId == 2)  // Buy  Mode
            {
                BuyController.sController.mCatalogPreviewController.ShowHide
                    -= OnShowHideBuyPreview;
            }
            return ListenerAction.Keep;
        }

        public static void OnEnterBuyMode()
        {
            if (BuyController.Active)
            {
                BuyController.sController.mCatalogPreviewController.ShowHide += OnShowHideBuyPreview;
            }
            else
            {
                DebugNote("OnEnterBuyMode: BuyController NOT Active");
            }
        }

        public static void OnShowHideBuyPreview(bool show)
        {
            if (show)
            {
                Simulator.AddObject(new Sims3.Gameplay.OneShotFunctionTask(ReplaceProductText));
            } 
        }

        public static void ReplaceProductText()
        {
            int currentCall = callcount;
            ++callcount;

            ObjectGuid t = ObjectGuid.InvalidObjectGuid;
            for (int i = 0; i < 3; ++i)
            {
                if (BuyController.sController.mCatalogPreviewController.CurrentObject == null
                || BuyController.sController.mCatalogPreviewController.CurrentObject == ObjectGuid.InvalidObjectGuid
                || BuyController.sController.mCatalogPreviewController.ProductChangeInProgress)
                {
                    Simulator.Sleep(0u);
                }
                else
                {
                    t = BuyController.sController.mCatalogPreviewController.CurrentObject;
                    break;
                }
            }
            if (t.Value == 0)   
            {
                // Shouldn't hit this code, but good to check.
                DebugNote("Call " + currentCall + ": Can't find product guid");
                return;
            }

            BuildBuyProduct p = BuyController.sController.mCatalogPreviewController.mBuildBuyModel.GetObjectProduct(t);
            for (int i = 0; i < 10; ++i)
            {
                if (p == null)
                {
                    Simulator.Sleep(0u);
                    t = BuyController.sController.mCatalogPreviewController.CurrentObject;
                    p = BuyController.sController.mCatalogPreviewController.mBuildBuyModel.GetObjectProduct(t);
                }
                else break;
            }
            if (p == null)
            {
                DebugNote("Call " + currentCall + ": Unable to find selected product after 10 attempts");
                return;
            }
            string productClass = LotManager.GetObjectClassName(t);
            if (productClass == null)
            {
                DebugNote("Can't get product class");
                productClass = "";
            } 
            if ((ulong)BuildBuyProduct.eBuySubCategory.kBuySubCategoryBeds == p.BuySubCategoryFlags)
            {
                BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption =
                    StandardizeBedPreview(productClass, p.Price);
            } else if ((ulong)BuildBuyProduct.eBuySubCategory.kBuySubCategoryLargeAppliances == p.BuySubCategoryFlags)
            {
                if (productClass.Contains("Refridgerator") || productClass.Contains("Fridge"))
                {
                    BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption =
                    StandardizeFridgePreview(productClass, p.Price);
                } else if (productClass.Contains("Stove"))
                {
                    BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption =
                    StandardizeStovePreview(productClass, p.Price);
                }
            }
        }

        public static string StandardizeBedPreview(string className, float cost)
        {
            float adjCost = cost;
            int energy = 4; // Everything seems to be energy 4 by default
            bool cheap = false;
            if (className.Contains("BedDouble"))
            {
                adjCost += 50;
            } else if (className.Contains("BunkBed") && !className.Contains("Loft"))
            {
                adjCost += 100;
            }
            if (adjCost < 300)
            {
                // Bed energy 2 = 0.5 multiplier
                energy = 2;
                cheap = true;
            }
            else if (adjCost < 450)
            {
                // Bed energy 3 = 0.75 multiplier
                energy = 3;
                cheap = true;
            }
            else if (adjCost < 600)
            {
                // Bed energy 4 = x1
                energy = 4;
                cheap = true;
            }
            else if (adjCost < 1100)
            {
                // Energy 4 but no bad night
                energy = 4;
                cheap = false;
            }
            else if (adjCost < 1450)
            {
                // Energy 5 = x1.07
                energy = 5;
                cheap = false;
            }
            else if (adjCost < 1800)
            {
                // Energy 6 = x1.1
                energy = 5;
                cheap = false;
            }
            else if (adjCost < 2150)
            {
                // Energy 7 = x1.15
                energy = 7;
                cheap = false;
            }
            else if (adjCost < 2950)
            {
                // Energy 8 = x1.25
                energy = 8;
                cheap = false;
            }
            else if (adjCost < 4000)
            {
                // Energy 9 = x1.35
                energy = 9;
                cheap = false;
            }
            else
            {
                // Energy 10 = x1.4
                energy = 10;
                cheap = false;
            }
            return ("Standardize Bed Text Energy = " + energy + " cheap=" + cheap);
        }


        // Beds : Alternate analysis
        //105, Single - Energy 2, Stress Relief 3
        //150
        //300, Single - Energy 4, Stress Relief 3
        //425, Single - Energy 4, Stress Relief 3
        //450, Double - Energy 4, Stress Relief 3
        //500(Cheap marker)
        //560, Single - Energy 4, Stress Relief 3
        //650, Double - Energy 4, Stress Relief 3
        //700, Single - Energy 4, Stress Relief 3
        //700
        //950, Single - Energy 5, Stress Relief 3, Env 2
        //990, Double - Energy 5, Stress Relief 3, Env 2
        //1100, Double - Energy 5, Stress Relief 3
        //1100
        //1500, Double - Energy 6, Stress Relief 3, Env 2
        //1500
        //Energy 7
        //1900
        //1950, Double - Energy 8, Stress Relief 3, Env 6
        //2200, Double - Energy 8, Stress Relief 3, Env 3
        //2400, Double - Energy 8, Stress Relief 3, Env 4
        //2500
        //2800, Double - Energy 8, Stress Relief 3, Env 2
        //Level 9
        //3400
        //3500, Double - Energy 10, Stress Relief 3, Env 5
        //4200, Double - Energy 10, Stress Relief 3, Env 7
        //5300, Double - Energy 10, Stress Relief 3, Env 7



        //foreach (Bed b in Queries.GetObjects<Bed>())
        //{
        //    int adjCost = b.Cost;
        //    // Double and bunk beds are 50 more expensive
        //    if (b is BedDouble)
        //        adjCost += 50;



        //}

        //foreach (BunkBedContainer b in Queries.GetObjects<BunkBedContainer>())
        //{
        //    // Double and bunk beds are 50 more expensive
        //    int adjCost = b.Cost - 50;

        //    if (adjCost < 300)
        //    {
        //        // Bed energy 2 = 0.5 multiplier
        //        b.BunkBedTuning.GivesBadNightsSleep = true;
        //        b.BunkBedTuning.EnergyMultiplier = 0.5f;
        //    }
        //    else if (adjCost < 550)
        //    {
        //        // Bed energy 3 = 0.75 multiplier
        //        b.BunkBedTuning.GivesBadNightsSleep = true;
        //        b.BunkBedTuning.EnergyMultiplier = 0.75f;
        //    }
        //    else if (adjCost < 800)
        //    {
        //        // Bed energy 4 = x1
        //        b.BunkBedTuning.GivesBadNightsSleep = true;
        //        b.BunkBedTuning.EnergyMultiplier = 1f;
        //    }
        //    else if (adjCost < 1100)
        //    {
        //        // Energy 4 but no bad night
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1f;
        //    }
        //    else if (adjCost < 1450)
        //    {
        //        // Energy 5 = x1.07
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.07f;
        //    }
        //    else if (adjCost < 1800)
        //    {
        //        // Energy 6 = x1.1
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.1f;
        //    }
        //    else if (adjCost < 2150)
        //    {
        //        // Energy 7 = x1.15
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.15f;
        //    }
        //    else if (adjCost < 2950)
        //    {
        //        // Energy 8 = x1.25
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.25f;
        //    }
        //    else if (adjCost < 4000)
        //    {
        //        // Energy 9 = x1.35
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.35f;
        //    }
        //    else
        //    {
        //        // Energy 10 = x1.4
        //        b.BunkBedTuning.GivesBadNightsSleep = false;
        //        b.BunkBedTuning.EnergyMultiplier = 1.4f;
        //    }

        //}

        // Fridges
        //375 - Hunger 5
        //650 - Hunger 6
        //1200 - Hunger 7
        // 1600 - Hunger 7
        //1800 - Hunger 8
        //2000 - Hunger 8
        //1700 - Hunger 8
        // 1900 - Hunger 9
        // 1910 - Hunger 10
        // 2000 - Hunger 9
        // 4000 - Hunger 10, fresh forever

        // Fridge cheap, spoilage x7
        // Fridge moderate, spoilage x 12
        // Fridge expensive, spoilage x21


        public static string StandardizeFridgePreview(string className, float cost)
        {
            int hunger = 4;
            bool cheap = false;
            int spoilage = 7;

            if (cost <= 375)
            {
                hunger = 5;
                spoilage = 7;
                cheap = true;
            } else if (cost <= 650)
            {
                hunger = 6;
                spoilage = 7;
            } else if (cost <= 1200)
            {
                hunger = 7;
                spoilage = 12;
            } else if (cost <= 1700)
            {
                hunger = 8;
                spoilage = 12;
            } else if (cost <= 3000)
            {
                hunger = 9;
                spoilage = 21;
            } else
            {
                hunger = 10;
                spoilage = 21;
            }

            return ("Standardize Fridge Text hunger " + hunger + " cheap=" + cheap + " spoilage "
                + spoilage);
        }

        // Ovens
        //400 - Hunger 4, Cooking Speed 0.8, Cheap
        //650 - Hunger 5, Cooking Speed 1
        //800 Hunger 6
        //1200 - Hunger 7,
        //1350 - Hunger 7, Cooking Speed x1.1, Skill Gain +15%, Self-Cleaning
        //1425 - Hunger 9, Cooking Speed x1.2, Self-Cleaning, Fireproof
        //1800 - Hunger 8, Env 4
        //1850 - Hunger 10


        public static string StandardizeStovePreview(string className, float cost)
        {
            int hunger = 4;
            bool cheap = false;
            string speed = "";
            string skillGain = "";
            
            if (cost <= 400)
            {
                hunger = 4;
                cheap = true;
                speed = "x0.8";
            } else if (cost <= 500)
            {
                hunger = 5;
                cheap = true;
            }
            else if (cost <= 650)
            {
                hunger = 5;

            }
            else if (cost <= 1200)
            {
                hunger = 6;
            } else if (cost <= 1350)
            {
                hunger = 7;
                speed = "x1.1";
            } else if (cost <= 1425)
            {
                hunger = 8;
                speed = "x1.1";
            } else if (cost <= 1800)
            {
                hunger = 9;
                speed = "x1.2";
                skillGain = "+15%";
            } else
            {
                hunger = 10;
                speed = "x1.2";
                skillGain = "+15%";
            }
            return ("Standardize Stove Text Hunger " + hunger + " cheap=" + cheap + " " + speed
                + " " + skillGain);
        }

        public static void DebugNote(string str)
        {
            if (kStatStandardizerDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
                    .NotificationStyle.kDebugAlert));
            }
        }
    }
}