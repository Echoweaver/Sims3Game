using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;
using static Sims3.Gameplay.Skills.Gardening;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats
{
    /*
     * Herb Lore Skill Levels:
     * 0: View Plant, Play with Plant, Pick up seeds
     * 1: Harvest
     * 2: Water
     * 3: Weed
     * 4: Dispose, Plant Harvestables
     * 5: Fertilize with Fish
     * 6: Tend 
     * 7: Tend gains speed
     * 8: Harvest gains quality
     * 9: Make traveling herbs
     * 10: Make more traveling herbs
     */


    public class EWHerbLoreSkill : Skill
    {
        public const SkillNames SkillNameID = (SkillNames)0x7808B943;

        public const string sEWLocalizationKey = "Echoweaver/Skills/EWHerbLoreSkill:";

        public static float kEWHerbLoreGainRateNormal = 5f;

        bool mTestOppIsNew = false;

        bool mTestOppIsCompleted = false;

        [Persistable(false)]
        public List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

        public Dictionary<string, PlantInfo> mHarvestCounts;

        public int mNumberPlanted;

        public int mNumberThingsHarvested;

        public List<string> mPlantsPlanted;

        public Quality mBestQualityHarvested = Quality.Any;


        public EWHerbLoreSkill(SkillNames guid) : base(guid)
        {
        }
        private EWHerbLoreSkill()
        {
        }

        public new string LocalizeString(string name, params object[] parameters)
        {
            return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + name, parameters);
        }

        [Persistable]
        public class PlantInfo
        {
            public int HarvestablesCount;

            public Quality BestQuality = Quality.Any;

            public int MostExpensive;
        }

        public class NumberPlanted : ITrackedStat
        {
            public EWHerbLoreSkill mSkill;

            public string Description => Gardening.LocalizeString("NumberPlanted", mSkill.mNumberPlanted);

            public NumberPlanted(EWHerbLoreSkill skill)
            {
                mSkill = skill;
            }
        }

        public class UniquePlantsPlanted : ITrackedStat
        {
            public EWHerbLoreSkill mSkill;

            public string Description
            {
                get
                {
                    double num = 0.0;
                    if (mSkill.mPlantsPlanted != null)
                    {
                        int num2 = 0;
                        foreach (string item in mSkill.mPlantsPlanted)
                        {
                            if (PlantDefinition.mDictionary.ContainsKey(item))
                            {
                                num2++;
                            }
                        }
                        num = (double)num2 / (double)sUniquePlants;
                        num = Math.Round(num, 2) * 100.0;
                    }
                    return Gardening.LocalizeString("UniquePlantsPlanted", num);
                }
            }

            public UniquePlantsPlanted(EWHerbLoreSkill skill)
            {
                mSkill = skill;
            }
        }

        public class ItemsHarvested : ITrackedStat
        {
            public EWHerbLoreSkill mSkill;

            public string Description => Gardening.LocalizeString("ItemsHarvested", mSkill.mNumberThingsHarvested);

            public ItemsHarvested(EWHerbLoreSkill skill)
            {
                mSkill = skill;
            }
        }

        public class BestHarvestable : ITrackedStat
        {
            public EWHerbLoreSkill mSkill;

            public string Description
            {
                get
                {
                    if (mSkill.mBestQualityHarvested == Quality.Any)
                    {
                        return Gardening.LocalizeString("BestHarvestable", new object[1] {
                    Gardening.LocalizeString ("NoneHarvested")
                });
                    }
                    return Gardening.LocalizeString("BestHarvestable", new object[1] {
                QualityHelper.GetQualityLocalizedString (mSkill.mBestQualityHarvested)
            });
                }
            }

            public BestHarvestable(EWHerbLoreSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool HasPlanted(string plantName)
        {
            if (mHarvestCounts != null)
            {
                return mHarvestCounts.ContainsKey(plantName);
            }
            return false;
        }

        public bool HasHarvested()
        {
            if (mHarvestCounts != null)
            {
                foreach (PlantInfo value in mHarvestCounts.Values)
                {
                    if (value.HarvestablesCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Harvested(Quality quality, PlantDefinition def)
        {
            if (mHarvestCounts == null)
            {
                mHarvestCounts = new Dictionary<string, PlantInfo>();
            }
            if (!mHarvestCounts.TryGetValue(def.PlantName, out PlantInfo value))
            {
                value = new PlantInfo();
                value.HarvestablesCount = 1;
                value.BestQuality = quality;
                mHarvestCounts.Add(def.PlantName, value);
            }
            else
            {
                value.HarvestablesCount++;
                if (quality > value.BestQuality)
                {
                    value.BestQuality = quality;
                }
            }
            if (quality > mBestQualityHarvested)
            {
                mBestQualityHarvested = quality;
            }
            mNumberThingsHarvested++;
            TestForNewLifetimeOpp();
        }

        public override List<ITrackedStat> TrackedStats => mTrackedStats;

        public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;

        // Skill Opportunities
        [Tunable]
        [TunableComment("")]
        public static int kNumForOpportunity = 20;
        public class OppTest : ILifetimeOpportunity
        {
            public EWHerbLoreSkill mSkill;

            public string Title => mSkill.LocalizeString("TestOpportunity");

            public string RewardDescription => mSkill.LocalizeString("TestDescription", kNumForOpportunity);

            public string AchievedDescription => mSkill.LocalizeString("TestAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mTestOppIsNew;
                }
                set
                {
                    mSkill.mTestOppIsNew = value;
                }
            }
            public bool Completed => mSkill.mTestOppIsCompleted;

            public OppTest(EWHerbLoreSkill skill)
            {
                mSkill = skill;
            }

        }

        public override void CreateSkillJournalInfo()
        {
            mTrackedStats = new List<ITrackedStat>();
            mTrackedStats.Add(new NumberPlanted(this));
            mTrackedStats.Add(new UniquePlantsPlanted(this));
            mTrackedStats.Add(new ItemsHarvested(this));
            mTrackedStats.Add(new BestHarvestable(this));
            mLifetimeOpportunities = new List<ILifetimeOpportunity>();
            mLifetimeOpportunities.Add(new OppTest(this));
        }

        public void UpdateSkillJournal(PlantDefinition harvestPlantDef, List<GameObject> objectsHarvested)
        {
            foreach (GameObject item in objectsHarvested)
            {
                Harvested(Plant.GetQuality(item.Plantable.QualityLevel), harvestPlantDef);
            }
        }

        public static StateMachineClient CreateStateMachine(Sim s, HarvestPlant p, out Soil dummyIk)
        {
            dummyIk = Soil.Create(isDummyIk: true);
            StateMachineClient val = StateMachineClient.Acquire(s, "petgardening");
            //dummyIk.SetHiddenFlags(HiddenFlags.Nothing);
            dummyIk.SetPosition(p.GetSoil().Position);
            Vector3 forward = p.GetSoil().Position - s.Position;
            dummyIk.SetForward(forward);
            dummyIk.AddToWorld();
            val.SetActor("x", s);
            val.SetActor("gardenPlantBush", p);
            val.SetActor("gardenSoil", p.GetSoil());
            val.SetActor("Dummy", dummyIk);
            if (!p.PlantDef.GetPlantHeight(out PlantHeight height))
            {
                height = PlantHeight.Medium;
            }
            val.SetParameter("Plant Height", height);
            return val;
        }

        public static bool DoHarvest(Sim actor, HarvestPlant target, bool hasHarvested)
        {
            Slot[] containmentSlots = target.GetContainmentSlots();
            List<GameObject> list = new List<GameObject>();
            Slot[] array = containmentSlots;
            foreach (Slot slotName in array)
            {
                GameObject gameObject = target.GetContainedObject(slotName) as GameObject;
                if (gameObject != null && target.HarvestHarvestable(gameObject, actor, null))
                {
                    list.Add(gameObject);
                }
            }

            if (list.Count > 0)
            {
                StyledNotification.Show(new StyledNotification.Format("List larger than 0",
                    StyledNotification.NotificationStyle.kDebugAlert));
                EWHerbLoreSkill skill = actor.SkillManager.GetSkill<EWHerbLoreSkill>(EWHerbLoreSkill.SkillNameID);
                if (skill != null)
                {
                    skill.UpdateSkillJournal(target.PlantDef, list);
                }

                if (!hasHarvested)
                {
                    actor.ShowTNSIfSelectable(Localization.LocalizeString(actor.IsFemale,
                        "Gameplay/Objects/Gardening/HarvestPlant/Harvest:FirstHarvest", actor, target.PlantDef.Name),
                        NotificationStyle.kGameMessagePositive, target.ObjectId, actor.ObjectId);
                }
                target.PostHarvest();
                return true;
            }
            return false;
        }

        public override bool ExportContent(IPropertyStreamWriter writer)
        {
            base.ExportContent(writer);
            return true;
        }

        public override bool ImportContent(IPropertyStreamReader reader)
        {
            base.ImportContent(reader);
            return true;
        }

        public override void MergeTravelData(Skill mergeSkill)
        {
            base.MergeTravelData(mergeSkill);
            EWHerbLoreSkill skill = mergeSkill as EWHerbLoreSkill;
        }
    }
}
