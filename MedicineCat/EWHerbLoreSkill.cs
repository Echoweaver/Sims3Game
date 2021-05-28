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
using Sims3.UI;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Objects.Gardening;


/*
 * Herb Lore Skill Levels:
 * 0: View Plant, Play with Plant, Pick up seeds
 * 1: Harvest
 * 2: Water
 * 3: Weed
 * 4: Dispose, Plant Harvestables
 * 5: Tend
 * 6: 
 * 7: Fertilize with Fish
 * 8: 
 * 9:
 * 10: Harvest gains quality?
 */

namespace Echoweaver.Sims3Game.MedicineCat
{
	[Persistable]
	public class EWHerbLoreSkill : Skill
	{
		public const SkillNames SkillNameID = (SkillNames)0x7808B943;

		public const string sEWLocalizationKey = "Echoweaver/Skills/EWHerbLoreSkill:";

		public static float kEWHerbLoreGainRateNormal = 5f;

		int mSpeciesPlanted = 0;

		bool mTestOppIsNew = false;

		[Persistable(false)]
		public List<ITrackedStat> mTrackedStats;

		[Persistable(false)]
		public List<ILifetimeOpportunity> mLifetimeOpportunities;


		public EWHerbLoreSkill(SkillNames guid) : base(guid)
		{
		}

		private EWHerbLoreSkill()
		{
		}

		[Persistable]
		public class SpeciesPlanted : ITrackedStat
		{
			public EWHerbLoreSkill mSkill;

			public string Description => mSkill.LocalizeString("SpeciesPlanted", mSkill.mSpeciesPlanted);

			public SpeciesPlanted(EWHerbLoreSkill skill)
			{
				mSkill = skill;
			}
		}

		public override List<ITrackedStat> TrackedStats => mTrackedStats;

		public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;

		// Lifetime Opportunities
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

			public bool Completed => mSkill.OppFishercatCompleted;

			public OppTest(EWHerbLoreSkill skill)
			{
				mSkill = skill;
			}
		}

		public bool OppFishercatCompleted
		{
			get
			{
				return 0 >= kNumForOpportunity;
			}
		}


		public new string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + name, parameters);
		}


		public override void CreateSkillJournalInfo()
		{
			mTrackedStats = new List<ITrackedStat>();
			mTrackedStats.Add(new SpeciesPlanted(this));
			mLifetimeOpportunities = new List<ILifetimeOpportunity>();
			mLifetimeOpportunities.Add(new OppTest(this));
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


