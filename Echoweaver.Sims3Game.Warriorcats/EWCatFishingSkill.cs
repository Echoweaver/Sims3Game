﻿using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.SimIFace;
using Sims3.UI.Hud;
using System;
using System.Collections.Generic;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Abstracts;
using static Sims3.Gameplay.Skills.CatHuntingSkill;
using Sims3.UI;

namespace Echoweaver.Sims3Game
{

    public class EWCatFishingSkill : Skill
	{
		public const SkillNames SkillNameID = (SkillNames)0xDE46D7FA;

		public AlarmHandle mRemoveMapTagsHandle = AlarmHandle.kInvalidHandle;

		public const string sEWLocalizationKey = "Echoweaver/Skills/EWCatFishingSkill";
		public const string sRecordLocalizationKey = "Gameplay/Skills/Fishing/Fish";

		public static double sNumberOfFishTypes = Enum.GetValues(typeof(FishType)).Length - 3;

		public int mNumberFishCaught = 0;

		public int mUniqueFishCaught = 0;

		public float mHeaviestFishWeight = 0;

		public string mHeaviestTypeName = "";

		public int mSaltFishCaught = 0;

		public int mFreshFishCaught = 0;

		public Dictionary<FishType, FishInfo> mFishingInfo = new Dictionary<FishType, FishInfo>();

        [Persistable(false)]
        public List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

		public bool mOppFishercatIsNew = true;


		public EWCatFishingSkill(SkillNames guid) : base(guid)
        {
        }

		private EWCatFishingSkill()
		{
			mNumberFishCaught = 0;
			mUniqueFishCaught = 0;
			mHeaviestTypeName = "";
			mHeaviestFishWeight = 0;
			mFishingInfo = new Dictionary<FishType, FishInfo>();
			mSaltFishCaught = 0;
			mFreshFishCaught = 0;

		}

		[Persistable]
		public class FishInfo
		{
			public int mNumberCaught;

			public float mHeaviestTypeWeight;
		}

		public class FishCaught : ITrackedStat
		{
			public EWCatFishingSkill mSkill;

			public string Description => Localization.LocalizeString(sEWLocalizationKey + ":FishCaught", mSkill.mNumberFishCaught);

			public FishCaught(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public class FishTypesCaught : ITrackedStat
		{
			public EWCatFishingSkill mSkill;

			public string Description
			{
				get
				{
					double num = 0.0;
					if (mSkill.mFishingInfo != null)
					{
						int num2 = 0;
						Dictionary<FishType, FishInfo>.Enumerator enumerator = mSkill.mFishingInfo.GetEnumerator();
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.Value.mNumberCaught > 0 && Fish.sFishData.ContainsKey(enumerator.Current.Key))
							{
								num2++;
							}
						}
						num = (double)num2 / NumberOfFishTypes;
						num = Math.Round(num, 2) * 100.0;
					}
					return Localization.LocalizeString(sEWLocalizationKey + ":FishTypesCaught", num);
				}
			}

			public FishTypesCaught(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public class HeaviestFish : ITrackedStat
		{
			public EWCatFishingSkill mSkill;

			public string Description
			{
				get
				{
					if (mSkill.mHeaviestTypeName.Length > 0)
					{
						string text = Localization.LocalizeString(mSkill.mHeaviestTypeName, "");
						return Localization.LocalizeString(sEWLocalizationKey + ":HeaviestFish",
							Localization.LocalizeString(sEWLocalizationKey + ":HeaviestFishValue", text, mSkill.mHeaviestFishWeight));
					}
					return Localization.LocalizeString(sEWLocalizationKey + ":HeaviestFish",
						Localization.LocalizeString(sEWLocalizationKey + ":NoneCaught"));
				}
			}

			public HeaviestFish(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public class SaltwaterFishCaught : ITrackedStat
		{
			public EWCatFishingSkill mSkill;

			public string Description => Localization.LocalizeString(sEWLocalizationKey + ":SaltFishCaught", mSkill.mSaltFishCaught);

			public SaltwaterFishCaught(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public class FreshwaterFishCaught : ITrackedStat
		{
			public EWCatFishingSkill mSkill;

			public string Description => Localization.LocalizeString(sEWLocalizationKey + ":FreshFishCaught", mSkill.mFreshFishCaught);

			public FreshwaterFishCaught(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public override List<ITrackedStat> TrackedStats => mTrackedStats;

		public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;

		// Lifetime Opportunities
		[Tunable]
		[TunableComment("Num Fish cat must catch to get Fishercat skill achievement.")]
		public static int kNumFishForFishercat = 20;

		public class OppFishercat : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("Fishercat");

			public string RewardDescription => mSkill.LocalizeString("FishercatDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("FishercatAchieved", mSkill.mSkillOwner);

			public bool IsNew
			{
				get
				{
					return mSkill.mOppFishercatIsNew;
				}
				set
				{
					mSkill.mOppFishercatIsNew = value;
				}
			}

			public bool Completed => mSkill.OppFishercatCompleted;

			public OppFishercat(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public bool OppFishercatCompleted
		{
			get
			{
				return mNumberFishCaught >= kNumFishForFishercat;
			}
		}

		[Tunable]
		[TunableComment("Percent fish types cat must catch to get Seafood Gourmet skill achievement.")]
		public static int kPctFishTypesForSeafoodGourmet = 20;

		public bool mOppSeafoodGourmetIsNew = true;

		public class OppSeafoodGourmet : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("SeafoodGourmet");

			public string RewardDescription => mSkill.LocalizeString("SeafoodGourmetDescription", kPctFishTypesForSeafoodGourmet);

			public string AchievedDescription => mSkill.LocalizeString("SeafoodGourmetAchieved", mSkill.mSkillOwner);

			public bool IsNew
			{
				get
				{
					return mSkill.mOppSeafoodGourmetIsNew;
				}
				set
				{
					mSkill.mOppSeafoodGourmetIsNew = value;
				}
			}

			public bool Completed => mSkill.OppFishercatCompleted;

			public OppSeafoodGourmet(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public bool OppSeafoodGourmetCompleted
		{
			// Doesn't seem like I should need to repeat this calculation, but I haven't figured out how
			// to call the existing one in the TrackedStat
			get
			{
				double num = 0.0;
				if (mFishingInfo != null)
				{
					int num2 = 0;
					Dictionary<FishType, FishInfo>.Enumerator enumerator = mFishingInfo.GetEnumerator();
					while (enumerator.MoveNext())
					{
						// TODO: Rather than run this check for both trackedstat and opp, maybe better to purge invalid
						// fish types in one place? Somewhere? 
						if (enumerator.Current.Value.mNumberCaught > 0 && Fish.sFishData.ContainsKey(enumerator.Current.Key))
						{
							num2++;
						}
					}
					num = (double)num2 / NumberOfFishTypes;
					num = Math.Round(num, 2) * 100.0;
				}

				return num >= kPctFishTypesForSeafoodGourmet;
			}
		}

		[Tunable]
		[TunableComment("Num Saltwater Fish cat must catch to get Saltaholic skill achievement.")]
		public static int kNumFishForSaltaholic = 20;

		public bool mOppSaltaholicIsNew = true;


		public class OppSaltaholic : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("Saltaholic");

			public string RewardDescription => mSkill.LocalizeString("SaltaholicDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("SaltaholicAchieved", mSkill.mSkillOwner);

			public bool IsNew
			{
				get
				{
					return mSkill.mOppSaltaholicIsNew;
				}
				set
				{
					mSkill.mOppSaltaholicIsNew = value;
				}
			}

			public bool Completed => mSkill.OppSaltaholicCompleted;

			public OppSaltaholic(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public bool OppSaltaholicCompleted
		{
			get
			{
				return mSaltFishCaught >= kNumFishForSaltaholic;
			}
		}

		[Tunable]
		[TunableComment("Num Freshwater Fish cat must catch to get Pond Provisioner skill achievement.")]
		public static int kNumFishForPondProvisioner = 20;

		public bool mOppPondProvisionerIsNew = true;


		public class OppPondProvisioner : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("PondProvisioner");

			public string RewardDescription => mSkill.LocalizeString("PondProvisionerDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("PondProvisionerAchieved", mSkill.mSkillOwner);

			public bool IsNew
			{
				get
				{
					return mSkill.mOppPondProvisionerIsNew;
				}
				set
				{
					mSkill.mOppPondProvisionerIsNew = value;
				}
			}

			public bool Completed => mSkill.OppPondProvisionerCompleted;

			public OppPondProvisioner(EWCatFishingSkill skill)
			{
				mSkill = skill;
			}
		}

		public bool OppPondProvisionerCompleted
		{
			get
			{
				return mFreshFishCaught >= kNumFishForPondProvisioner;
			}
		}
		public static double NumberOfFishTypes
		{
			get
			{
				return sNumberOfFishTypes;
			}
			set
			{
				sNumberOfFishTypes = value;
			}
		}

		public int GetNumberOfTypesCaught()
		{
			return mUniqueFishCaught;
		}

		public int GetNumberCaught()
		{
			return mNumberFishCaught;
		}

		public int GetNumberCaught(FishType type)
		{
			if (mFishingInfo != null && mFishingInfo.TryGetValue(type, out FishInfo value))
			{
				return value.mNumberCaught;
			}
			return 0;
		}

		public float GetHeaviestCaught(FishType type)
		{
			if (mFishingInfo != null && mFishingInfo.TryGetValue(type, out FishInfo value))
			{
				return value.mHeaviestTypeWeight;
			}
			return 0f;
		}

		public bool CanCatchPreyFish()
		{

			foreach (KeyValuePair<FishType, FishData> sFishDatum in Fish.sFishData)
			{
				CatHuntingComponent.PreyData preyData = sFishDatum.Value.PreyData;
				if (preyData != null && SkillLevel >= preyData.MinSkillLevel && SkillLevel <= preyData.MaxSkillLevel)
				{
					return true;
				}
			}
			return false;
		}

		public string RegisterCaughtPrey(ICatPrey prey)
		{			
			CatHuntingComponent.PreyData mPreyData = prey.CatHuntingComponent.mPreyData;
			string message = "";
			if (mPreyData != null && mPreyData.PreyType == PreyType.Fish)
			{
				++mNumberFishCaught;
				Fish caughtFish = (Fish)prey;
				WaterTypes waterType = Fish.sFishData[caughtFish.mFishType].LocationFound;

				// Tried to find a cute way to do this but failed. There's really no easy test?
				if (waterType == WaterTypes.Ocean || waterType == WaterTypes.OceanPond ||
					waterType == WaterTypes.OceanPool || waterType == WaterTypes.All)
                {
					++mSaltFishCaught;
                }
				// Cats can't fish in pools, but hey
				if (waterType == WaterTypes.FreshWater || waterType == WaterTypes.Pond ||
					waterType == WaterTypes.Pool || waterType == WaterTypes.All)
				{
					++mFreshFishCaught;
                }
				if (!mFishingInfo.ContainsKey(caughtFish.mFishType))
				{
					++mUniqueFishCaught;
					FishInfo fishInfo = new FishInfo();
					fishInfo.mHeaviestTypeWeight = caughtFish.mWeight;
					fishInfo.mNumberCaught = 1;
					mFishingInfo.Add(caughtFish.mFishType, fishInfo);
					message += Localization.LocalizeString(sEWLocalizationKey + ":newBreed", mSkillOwner) + " ";
				}
				else
				{
					mFishingInfo[caughtFish.mFishType].mHeaviestTypeWeight =
						Math.Max(mFishingInfo[caughtFish.mFishType].mHeaviestTypeWeight, caughtFish.mWeight);
					++mFishingInfo[caughtFish.mFishType].mNumberCaught;
				}
				if (mHeaviestFishWeight < caughtFish.mWeight)
				{
					mHeaviestFishWeight = caughtFish.mWeight;
					mHeaviestTypeName = Fish.sFishData[caughtFish.mFishType].StringKeyName;
					message += Localization.LocalizeString(sEWLocalizationKey + ":newRecord", mSkillOwner) + " ";
				}
				EventTracker.SendEvent(new GuidEvent<PreyType>(EventTypeId.kPreyTypeCaught,
					base.SkillOwner.CreatedSim, mPreyData.PreyType));
				EventTracker.SendEvent(new GuidEvent<MinorPetRarity>(EventTypeId.kPreyRarityCaught,
					base.SkillOwner.CreatedSim, mPreyData.Rarity));
				// TestForNewLifetimeOpp();
			}
			return message;
		}

		public void StartMapTagAlarm()
		{
			if (mRemoveMapTagsHandle != AlarmHandle.kInvalidHandle)
			{
				mSkillOwner.CreatedSim.RemoveAlarm(mRemoveMapTagsHandle);
			}
			mRemoveMapTagsHandle = mSkillOwner.CreatedSim.AddAlarm(CatHuntingComponent.kMinsUntilMaptagsFade, TimeUnit.Minutes,
				RemoveMapTagsAlarmCallback, "Remove cathunting map tags", AlarmType.AlwaysPersisted);
		}

		public void RemoveMapTagsAlarmCallback()
		{
			mRemoveMapTagsHandle = AlarmHandle.kInvalidHandle;
			mSkillOwner.CreatedSim.MapTagManager.RemoveMapTagsOfType((MapTagType)46);
		}

		public new string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(SkillOwner.IsFemale, sLocalizationKey + ":" + name, parameters);
		}


		//		//public class FishingPropertyStreamWriter : PropertyStreamWriter
		//		//{
		//		//	public bool Export(Dictionary<FishType, FishInfo> data);

		//		//	public FishingPropertyStreamWriter()
		//		//		: this();
		//		//}

		//		//public class FishingPropertyStreamReader : PropertyStreamReader
		//		//{
		//		//	public bool Import(out Dictionary<FishType, FishInfo> data);

		//		//	public FishingPropertyStreamReader()
		//		//		: this();
		//		//}


		////		public override List<HeaderInfo> HeaderInfo(int page);

		public override void CreateSkillJournalInfo()
        {
            mTrackedStats = new List<ITrackedStat>();
			mTrackedStats.Add(new FishCaught(this));
            mTrackedStats.Add(new FishTypesCaught(this));
			mTrackedStats.Add(new HeaviestFish(this));
			mTrackedStats.Add(new SaltwaterFishCaught(this));
			mTrackedStats.Add(new FreshwaterFishCaught(this));
            mLifetimeOpportunities = new List<ILifetimeOpportunity>();
			mLifetimeOpportunities.Add(new OppFishercat(this));
			mLifetimeOpportunities.Add(new OppSeafoodGourmet(this));
			mLifetimeOpportunities.Add(new OppSaltaholic(this));
			mLifetimeOpportunities.Add(new OppPondProvisioner(this));
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

		public override void MergeTravelData(Skill skill)
		{
			base.MergeTravelData(skill);
			EWCatFishingSkill fishing = skill as EWCatFishingSkill;
			mNumberFishCaught = fishing.mNumberFishCaught;
			mHeaviestFishWeight = fishing.mHeaviestFishWeight;
			mFishingInfo = fishing.mFishingInfo;
			mHeaviestTypeName = fishing.mHeaviestTypeName;
			mUniqueFishCaught = fishing.mUniqueFishCaught;
		}

		//		//public bool ExportFishCaught(IPropertyStreamWriter writer);

		//		//public bool ImportFishCaught(IPropertyStreamReader reader);
	}
}


