using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.SimIFace;
using Sims3.UI.Hud;
using System;
using System.Collections.Generic;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Autonomy;
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

		public new const string sLocalizationKey = "Echoweaver/Warriorcats/SkillStats";

		public const string sRecordLocalizationKey = "Echoweaver/Warriorcats/SkillStats/Fish";

		//		public static float[] FishSkillPointCaps;

		public static double sNumberOfFishTypes = Enum.GetValues(typeof(FishType)).Length - 3;

		public int mNumberFishCaught;

		public int mUniqueFishCaught;

//		public Dictionary<FishType, FishInfo> mFishingInfo;


		//[Persistable(false)]
		//public List<ITrackedStat> mTrackedStats;

		//[Persistable(false)]
		//public List<ILifetimeOpportunity> mLifetimeOpportunities;

        public EWCatFishingSkill(SkillNames guid) : base(guid)
        {
			mNumberFishCaught = 0;
			mUniqueFishCaught = 0;
        }

        private EWCatFishingSkill()
        {
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

		public void RegisterCaughtPrey(ICatPrey prey)
		{			
			CatHuntingComponent.PreyData mPreyData = prey.CatHuntingComponent.mPreyData;
			if (mPreyData != null && mPreyData.PreyType == PreyType.Fish)
			{
				//if (!mPreyCaughtTypeStats.ContainsKey(mPreyData.PreyType))
				//{
				//	mPreyCaughtTypeStats.Add(mPreyData.PreyType, 1);
				//}
				//else
				//{
				//	mPreyCaughtTypeStats[mPreyData.PreyType]++;
				//}
				//if (!mPreyCaughtRarityStats.ContainsKey(mPreyData.Rarity))
				//{
				//	mPreyCaughtRarityStats.Add(mPreyData.Rarity, 1);
				//}
				//else
				//{
				//	mPreyCaughtRarityStats[mPreyData.Rarity]++;
				//}
				EventTracker.SendEvent(new GuidEvent<PreyType>(EventTypeId.kPreyTypeCaught, base.SkillOwner.CreatedSim, mPreyData.PreyType));
				EventTracker.SendEvent(new GuidEvent<MinorPetRarity>(EventTypeId.kPreyRarityCaught, base.SkillOwner.CreatedSim, mPreyData.Rarity));
			}
			// TestForNewLifetimeOpp();
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


		//		[Persistable]
		//		public class FishInfo
		//		{
		//			public int mNumberCaught;
		//		}

		//		public class FishCaught : ITrackedStat
		//		{
		//			public EWCatFishingSkill mSkill;

		//			public string Description => LocalizeString("Echoweaver/Warriorcats/SkillStats:FishCaught" +
		//				" " + mSkill.mNumberFishCaught);

		//			public FishCaught(EWCatFishingSkill skill)
		//            {
		//				mSkill.mNumberFishCaught = skill.mNumberFishCaught;
		//            }
		//		}

		//		public class FishTypesCaught : ITrackedStat
		//		{
		//			public EWCatFishingSkill mSkill;

		//			public string Description => LocalizeString("Echoweaver/Warriorcats/SkillStats:FishTypesCaught",
		//				mSkill.mFishingInfo.Keys.Count);

		//			public FishTypesCaught(EWCatFishingSkill skill)
		//            {
		//				mSkill = skill;
		//            }
		//		}


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


		//		public static double NumberOfFishTypes = sNumberOfFishTypes;  // How is this different from the s version?

		//		public override List<ITrackedStat> TrackedStats
		//		{
		//			get
		//			{
		//				return mTrackedStats;
		//			}
		//		}

		//		public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;

		//		public bool KnowsAbout(FishType type)
		//        {
		//			return mFishingInfo.ContainsKey(type);
		//        }

		//		//public List<CollectionRowInfo> GetFishCollectionInfo()
		//  //      {

		//  //      }

		//		public string CaughtFish(FishType type, float weight, float skillGained)
		//        {
		//			++mFishingInfo[type].mNumberCaught;
		//			AddPoints(skillGained);
		//			return "Caught " + type.ToString();
		//        }

		//		public string FirstTimeCaught(FishType type, float weight, FishInfo info)
		//        {
		//			mFishingInfo[type] = info;
		//			return "Caught first " + type.ToString();
		//        }

		//		public int GetNumberOfTypesCaught()
		//        {
		//			return mFishingInfo.Keys.Count;
		//        }

		//		public int GetNumberCaught()
		//        {
		//			return mNumberFishCaught;
		//        }

		//		public int GetNumberCaught(FishType type)
		//        {
		//			return mFishingInfo[type].mNumberCaught;
		//        }

		////		public override List<HeaderInfo> HeaderInfo(int page);

		public override void CreateSkillJournalInfo()
        {
//			mTrackedStats = new List<ITrackedStat>();
//			mTrackedStats.Add(new FishCaught(this));
//			mTrackedStats.Add(new FishTypesCaught(this));
//			mLifetimeOpportunities = new List<ILifetimeOpportunity>();

		}

//		//		public override uint GetSkillHash();

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

//		public override void MergeTravelData(Skill skill)
//		{
//			base.MergeTravelData(skill);
//			EWCatFishingSkill fishingSkill = skill as EWCatFishingSkill;
//			if (fishingSkill != null)
//			{
//				mUniqueFishCaught = fishingSkill.mUniqueFishCaught;
//				mNumberFishCaught = fishingSkill.mNumberFishCaught;
//				mFishingInfo = fishingSkill.mFishingInfo;
//			}
//		}

//		//public bool ExportFishCaught(IPropertyStreamWriter writer);

//		//public bool ImportFishCaught(IPropertyStreamReader reader);
	}
}


