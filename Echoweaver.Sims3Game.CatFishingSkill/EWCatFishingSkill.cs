using Sims3.Gameplay.Skills;
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
using Sims3.Gameplay.Autonomy;

namespace Echoweaver.Sims3Game.CatFishing
{
	[Persistable]
    public class EWCatFishingSkill : Skill
	{
		public const SkillNames SkillNameID = (SkillNames)0xDE46D7FA;

		//		public const CommodityKind CommodityKindID = unchecked((CommodityKind)0xFD000E72);

		public static List<ulong> sGourmetSimIDs;

		public AlarmHandle mRemoveMapTagsHandle = AlarmHandle.kInvalidHandle;

		public const string sEWLocalizationKey = "Echoweaver/Skills/EWCatFishingSkill";

		public const string sRecordLocalizationKey = "Gameplay/Skills/Fishing/Fish";

		public static float kEWFishingSkillGainRateNormal = 5f;

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
			mNumberFishCaught = 0;
			mUniqueFishCaught = 0;
			mHeaviestTypeName = "";
			mHeaviestFishWeight = 0;
			mFishingInfo = new Dictionary<FishType, FishInfo>();
			mSaltFishCaught = 0;
			mFreshFishCaught = 0;
			sGourmetSimIDs = new List<ulong>();
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
			sGourmetSimIDs = new List<ulong>();
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

		[Tunable]
		[TunableComment("Num Fish cat must catch to get Fishercat skill achievement.")]
		public static float kEWFishingSkillGainRateFishercat = 7.5f;


		public class OppFishercat : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("OppFishercat");

			public string RewardDescription => mSkill.LocalizeString("OppFishercatDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("OppFishercatAchieved", mSkill.mSkillOwner);

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

		[Tunable]
		[TunableComment("Percent hunger multiplier for fish caught by a Seafood Gourmet.")]
		public static float kSeafoodGourmetHungerMultiplier = 1.3f;

		public bool mOppSeafoodGourmetIsNew = true;

		public class OppSeafoodGourmet : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("OppSeafoodGourmet");

			public string RewardDescription => mSkill.LocalizeString("OppSeafoodGourmetDescription", kPctFishTypesForSeafoodGourmet);

			public string AchievedDescription => mSkill.LocalizeString("OppSeafoodGourmetAchieved", mSkill.mSkillOwner);

			public bool IsNew
			{
				get
				{
					return mSkill.mOppSeafoodGourmetIsNew;
				}
				set
				{
					mSkill.mOppSeafoodGourmetIsNew = value;
					if (value == false)
                    {
						if (!sGourmetSimIDs.Contains(mSkill.mSkillOwner.mSimDescriptionId))
                        {
							sGourmetSimIDs.Add(mSkill.mSkillOwner.mSimDescriptionId);
                        }
                    }
				}
			}

			public bool Completed => mSkill.OppSeafoodGourmetCompleted;

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

		public bool SimIDIsSeafoodGourmet(ulong simID)
        {
			return false;
        }

		[Tunable]
		[TunableComment("Bonus to fishing success for either Saltaholic or Pond Provisioner.")]
		public static float kFishCatchingBonus = 10f;

		[Tunable]
		[TunableComment("Num Saltwater Fish cat must catch to get Saltaholic skill achievement.")]
		public static int kNumFishForSaltaholic = 20;

		public bool mOppSaltaholicIsNew = true;


		public class OppSaltaholic : ILifetimeOpportunity
		{
			public EWCatFishingSkill mSkill;

			public string Title => mSkill.LocalizeString("OppSaltaholic");

			public string RewardDescription => mSkill.LocalizeString("OppSaltaholicDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("OppSaltaholicAchieved", mSkill.mSkillOwner);

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

			public string Title => mSkill.LocalizeString("OppPondProvisioner");

			public string RewardDescription => mSkill.LocalizeString("OppPondProvisionerDescription", kNumFishForFelineFisher);

			public string AchievedDescription => mSkill.LocalizeString("OppPondProvisionerAchieved", mSkill.mSkillOwner);

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

		public bool KnowsAbout(FishType type)
		{
			if (mFishingInfo == null)
			{
				return false;
			}
			return mFishingInfo.ContainsKey(type);
		}

		public string RegisterCaughtFish(Fish prey, bool isFreshwater)
		{			
			CatHuntingComponent.PreyData mPreyData = prey.CatHuntingComponent.mPreyData;
			string message = "";
            if (mPreyData != null && mPreyData.PreyType == PreyType.Fish)
            {
                ++mNumberFishCaught;

                // Tried to find a cute way to do this but failed. There's really no easy test?
                if (isFreshwater)
                {
                    ++mFreshFishCaught;
                }
                else
                {
                    ++mSaltFishCaught;
                }
				if (!mFishingInfo.ContainsKey(prey.mFishType))
                {
                    ++mUniqueFishCaught;
					FishInfo fishInfo = new FishInfo();
					fishInfo.mHeaviestTypeWeight = prey.Weight;
					fishInfo.mNumberCaught = 1;
					mFishingInfo.Add(prey.mFishType, fishInfo);
					message += Localization.LocalizeString(sEWLocalizationKey + ":newBreed", mSkillOwner) + " ";
				}
				else
                {
                    mFishingInfo[prey.mFishType].mHeaviestTypeWeight =
                        Math.Max(mFishingInfo[prey.mFishType].mHeaviestTypeWeight, prey.mWeight);
                    ++mFishingInfo[prey.mFishType].mNumberCaught;
                }
                if (mHeaviestFishWeight < prey.mWeight)
                {
                    mHeaviestFishWeight = prey.mWeight;
                    mHeaviestTypeName = Fish.sFishData[prey.mFishType].StringKeyName;
                    message += Localization.LocalizeString(sEWLocalizationKey + ":newRecord", mSkillOwner) + " ";
                }
                EventTracker.SendEvent(new GuidEvent<PreyType>(EventTypeId.kPreyTypeCaught,
                    base.SkillOwner.CreatedSim, mPreyData.PreyType));
                EventTracker.SendEvent(new GuidEvent<MinorPetRarity>(EventTypeId.kPreyRarityCaught,
                    base.SkillOwner.CreatedSim, mPreyData.Rarity));
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
			return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + ":" + name, parameters);
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
	}
}


