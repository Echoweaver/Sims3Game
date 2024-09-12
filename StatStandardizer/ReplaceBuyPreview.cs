using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Sims3.SimIFace.BuildBuy;
using Sims3.UI;
using static Echoweaver.Sims3Game.StatStandardizer.Loader;
using static Sims3.SimIFace.PropertyStreamWriter;

namespace Echoweaver.Sims3Game.StatStandardizer
{
	public static class ReplaceBuyPreview
	{
        public static int callcount = 0;

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

            uint[] moodletTextIDs = BuyController.sController.mCatalogPreviewController.mBuildBuyModel
                .GetMoodletEffectTextIds(t);
            uint[] moodletTextValues = BuyController.sController.mCatalogPreviewController.mBuildBuyModel
                    .GetMoodletEffectTextValues(t);

            if (moodletTextIDs == null || moodletTextIDs.Length == 0 || moodletTextIDs.Length
                != moodletTextValues.Length)
            {
                return;
            }           

            if ((ulong)BuildBuyProduct.eBuySubCategory.kBuySubCategoryBeds == p.BuySubCategoryFlags)
            {
                string newCaption = StandardizeBedPreview(productClass, p.Price, moodletTextIDs,
                    moodletTextValues);
                if (newCaption != "")
                {
                    BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption
                        = newCaption;
                    BuyController.sController.mCatalogPreviewController.mMoodletEffectText
                        .AutoSize(vertical: true);
                    BuyController.sController.mCatalogPreviewController.ResizePreviewForItem();
                }

            }
            else if ((ulong)BuildBuyProduct.eBuySubCategory.kBuySubCategoryLargeAppliances ==
                p.BuySubCategoryFlags)
            {
                if (productClass.ToLower().Contains("refrigerator") || productClass.ToLower().Contains("fridge"))
                {
                    string newCaption = StandardizeFridgePreview(productClass, p.Price, moodletTextIDs, moodletTextValues);
                    if (newCaption != "")
                    {
                        BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption =
                            newCaption;
                        BuyController.sController.mCatalogPreviewController.mMoodletEffectText
                            .AutoSize(vertical: true);
                        BuyController.sController.mCatalogPreviewController.ResizePreviewForItem();
                    }
                }
                else if (productClass.ToLower().Contains("stove"))
                {
                    string newCaption = StandardizeStovePreview(productClass, p.Price, moodletTextIDs, moodletTextValues);
                    if (newCaption != "")
                    {
                        BuyController.sController.mCatalogPreviewController.mMoodletEffectText.Caption =
                        newCaption;
                        BuyController.sController.mCatalogPreviewController.mMoodletEffectText
                            .AutoSize(vertical: true);
                        BuyController.sController.mCatalogPreviewController.ResizePreviewForItem();
                    }
                }
            }
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

        public static bool CalculateBedStats(string className, float cost, out int energy,
            out float multiplier, out bool cheap)
        {
            energy = 4;
            cheap = false;
            multiplier = 1;
            float adjCost = cost;
            if (className.Contains("BedDouble"))
            {
                adjCost -= 50;
            }
            else if (className.Contains("BunkBed") && !className.Contains("Loft"))
            {
                adjCost -= 100;
            }
            if (adjCost < 150)
            {
                multiplier = 0.5f;    
                energy = 2;
                cheap = true;
            }
            else if (adjCost < 300)
            {
                multiplier = 0.75f;
                energy = 3;
                cheap = true;
            }
            else if (adjCost < 500)
            {
                multiplier = 1;
                energy = 4;
                cheap = true;
            }
            else if (adjCost < 750)
            {
                multiplier = 1;
                energy = 4;
                cheap = false;
            }
            else if (adjCost < 1200)
            {
                multiplier = 1.07f;
                energy = 5;
                cheap = false;
            }
            else if (adjCost < 1600)
            {
                multiplier = 1.1f;
                energy = 6;
                cheap = false;
            }
            else if (adjCost < 2200)
            {
                multiplier = 1.15f;
                energy = 7;
                cheap = false;
            }
            else if (adjCost < 2900)
            {
                multiplier = 1.25f;
                energy = 8;
                cheap = false;
            }
            else if (adjCost < 4000)
            {
                multiplier = 1.35f;
                energy = 9;
                cheap = false;
            }
            else
            {
                multiplier = 1.4f;
                energy = 10;
                cheap = false;
            }
            return true;
        }

        public static string StandardizeBedPreview(string className, float cost, uint[] effectIDs,
            uint[] effectValues)
        {
            bool success = CalculateBedStats(className, cost, out int energy, out float mulitplier,
                out bool cheap);
            if (!success) return "";

            // Build the effect text as it will be displayed in the preview box
            string text = "";
            for (uint i = 0u; i < effectIDs.Length; i++)
            {
                string localizedName = "";
                BuyController.sController.mCatalogPreviewController.mBuildBuyModel.MoodletEffectData(effectIDs[i],
                    out localizedName);
                if (effectIDs[i] == 4) // Energy
                {
                    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                        (text + localizedName + ": " + energy) : (text + "+ " + localizedName)));
                }
                else
                {
                    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                        (text + localizedName + ": " + effectValues[i]) : (text + "+ " + localizedName)));
                }
                if (i < effectIDs.Length - 1)
                {
                    text += "\n";
                }
            }
            if (cheap)
            {
                // TODO: Localize!
                text += "\nCheap\n";
            }
            return text;
        }


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

        public static bool CalculateFridgeStats(string className, float cost, out int hunger,
            out int spoilage, out bool cheap)
        {
            hunger = 4;
            cheap = className.ToLower().Contains("cheap");
            spoilage = 7;

            if (cost <= 375)
            {
                hunger = 5;
                spoilage = 7;
            }
            else if (cost <= 650)
            {
                hunger = 6;
                spoilage = 7;
            }
            else if (cost <= 1200)
            {
                hunger = 7;
                spoilage = 12;
            }
            else if (cost <= 1700)
            {
                hunger = 8;
                spoilage = 12;
            }
            else if (cost <= 3000)
            {
                hunger = 9;
                spoilage = 21;
            }
            else
            {
                hunger = 10;
                spoilage = 21;
            }
            return true;
        }

        public static string StandardizeFridgePreview(string className, float cost, uint[] effectIDs,
            uint[] effectValues)
        {
            bool success = CalculateFridgeStats(className, cost, out int hunger, out int spoilage, out bool cheap);
            if (!success) return "";

            // Build the effect text as it will be displayed in the preview box
            string text = "";
            for (uint i = 0u; i < effectIDs.Length; i++)
            {
                string localizedName = "";
                BuyController.sController.mCatalogPreviewController.mBuildBuyModel.MoodletEffectData(effectIDs[i],
                    out localizedName);
                //if (effectIDs[i] == 2) // Hunger
                //{
                //    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                //        (text + localizedName + ": " + hunger) : (text + "+ " + localizedName)));
                //}
                //else
                //{
                    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                        (text + localizedName + ": " + effectValues[i]) : (text + "+ " + localizedName)));
                //}
                if (i < effectIDs.Length - 1)
                {
                    text += "\n";
                }
            }
            if (spoilage == 7)
            {
                text += "\nBasic Freshness\n";
            } else if (spoilage == 12)
            {
                text += "\nAverage Freshness\n";
            } else
            {
                text += "\nImproved Freshness\n";
            }
            if (cheap)
            {
                // TODO: Localize!
                text += "Cheap\n";
            }
            return text;
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

        public static bool CalculateStoveStats(string className, float cost, out int hunger, out float speed,
            out float skillGain, out bool cheap)
        {
            hunger = 4;
            cheap = className.ToLower().Contains("cheap");
            speed = 1;
            skillGain = 1;

            if (cost <= 400)
            {
                hunger = 4;
                //cheap = true;
                speed = 0.8f;
            }
            else if (cost <= 500)
            {
                hunger = 5;
                //cheap = true;
                speed = 1;
            }
            else if (cost <= 650)
            {
                hunger = 5;
            }
            else if (cost <= 1200)
            {
                hunger = 6;
            }
            else if (cost <= 1350)
            {
                hunger = 7;
                speed = 1.1f;
            }
            else if (cost <= 1425)
            {
                hunger = 8;
                speed = 1.1f;
            }
            else if (cost <= 1800)
            {
                hunger = 9;
                speed = 1.2f;
                skillGain = 1.15f;
            }
            else
            {
                hunger = 10;
                speed = 1.2f;
                skillGain = 1.15f;
            }

            return true;
        }

        public static string StandardizeStovePreview(string className, float cost, uint[] effectIDs,
            uint[] effectValues)
        {
            bool success = CalculateStoveStats(className, cost, out int hunger, out float speed,
                out float skillGain, out bool cheap);
            if (!success) return "";

            // Build the effect text as it will be displayed in the preview box
            string text = "";
            for (uint i = 0u; i < effectIDs.Length; i++)
            {
                string localizedName = "";
                BuyController.sController.mCatalogPreviewController.mBuildBuyModel.MoodletEffectData(effectIDs[i],
                    out localizedName);
                if (effectIDs[i] == 2) // Hunger
                {
                    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                        (text + localizedName + ": " + hunger) : (text + "+ " + localizedName)));
                }
                else
                {
                    text = ((effectValues[i] == 0) ? (text + localizedName) : ((effectValues[i] != 11) ?
                        (text + localizedName + ": " + effectValues[i]) : (text + "+ " + localizedName)));
                }
                if (i < effectIDs.Length - 1)
                {
                    text += "\n";
                }
            }
            text += "\n";
            if (speed < 1)
            {
                text += "Cook Slower\n";
            }
            else if (speed == 1.1f)
            {
                text += "Cook Faster\n";
            }
            else if (speed > 1.1f)
            {
                text += "Cook Faster +\n";
            }
            if (skillGain > 1)
            {
                BuyController.sController.mCatalogPreviewController.mBuildBuyModel.MoodletEffectData(17,
                    out string skillText);

                text += skillText + "\n";
            }
            if (cheap)
            {
                // TODO: Localize!
                text += "Cheap\n";
            }
            return text;
        }

    }
}

