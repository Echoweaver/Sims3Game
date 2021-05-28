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


/*
 * Types of medicine:
 *  - Fleas
 *    > Cure: rodent
 *  - Treatment of wounds (Pet Fighting Mod)
 *    - Lower levels: Reduce wound by one level
 *    - Higher levels: Remove wound entirely
 *      > goldenrod, marigold [sulfur], GARLIC, (WOLFSBANE), (WATERMELON), 
 *      > BLACKBERRY, burdock root (MANDRAKE ROOT), poppy [cosmos], chervil, comfrey root, dandelion, dock leaf 
 *      > elder leaf (BUMBLELEAF), fennel, goldenrod, horsetail, oak leaf (APPLE), stinging nettle (LEMON LIME), willow bark
 *  - Treatment of disease 
 *    - Whitecough Coughing and sneezing, fatigue
 *      > HONEY, (ONION), bright eyes [sweet william]
 *    - Greencough (Develops from untreated whitecough) Coughing, Fever, Rheumy Eyes, Difficulty Breathing
 *      > blazing star flower (WONDERPETAL)[azalea], catmint (PEPPERMINT), chickweed, hawkweed, LAVENDER, 
 *        borage leaves [blue flax], lungwort, sweet-sedge (LICORICE), tansy
 *  - Nausea (Vomits reduce hunger motive. Eating causes vomiting and halves the gain from eating)
 *      > Watermint, williow leaves (GREENLEAF), wintergreen, juniper berries (BUZZBERRY), mallow leaves, parsley, yarrow
 *      > (SWEETGRASS), (WATERMELON)
 *  - Problems with childbirth (requires childbirth trouble event)
 *    > RASPBERRY, chervil, fennel(BASIL), ragwort leaves, (BELL PEPPER)
 *  - ? Problems with nursing
 */

/* 
 * Types of medicine (converted to Sims)
 * - Fleas: RODENT
 * - Wounds, reduce by one level: [sulfur], GARLIC, <wolfsbane>, WATERMELON, APPLE
 * - Disease, whitecough: HONEY, ONION, [sweet william], LICORICE
 * - Disease, nausea: GREENLEAF, BUZZBERRY, SWEETGRASS, WATERMELON
 * - Disease, greencough: WONDERPETAL, [azalea],  PEPPERMINT, LAVENDER, [blue flax]
 * - Childbirth: RASPBERRY, BASIL, BELL PEPPER
 * - Wounds, remove: BLACKBERRY, <mandrake root>, [cosmos], BUMBLELEAF, LEMON, LIME
 */

namespace Echoweaver.Sims3Game.MedicineCat
{
	[Persistable]
	public class EWMedicineCatSkill : Skill
	{
		public const SkillNames SkillNameID = (SkillNames)0x277ECF3A;

		//		public const CommodityKind CommodityKindID = unchecked((CommodityKind)0xFD000E72);

		public const string sEWLocalizationKey = "Echoweaver/Skills/EWMedicineCatSkill";

		public static float kEWMedicineCatGainRateNormal = 5f;

		int mDiseasesCured = 0;

		bool mTestOppIsNew = false;

		[Persistable(false)]
		public List<ITrackedStat> mTrackedStats;

		[Persistable(false)]
		public List<ILifetimeOpportunity> mLifetimeOpportunities;


		public EWMedicineCatSkill(SkillNames guid) : base(guid)
		{
		}

		private EWMedicineCatSkill()
		{
		}

		[Persistable]
		public class FishInfo
		{
			public int mNumberCaught;

			public float mHeaviestTypeWeight;
		}

		public class DiseasesCured : ITrackedStat
		{
			public EWMedicineCatSkill mSkill;

			public string Description => Localization.LocalizeString(sEWLocalizationKey + ":DiseasesCured", mSkill.mDiseasesCured);

			public DiseasesCured(EWMedicineCatSkill skill)
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
			public EWMedicineCatSkill mSkill;

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

			public OppTest(EWMedicineCatSkill skill)
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
			return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + ":" + name, parameters);
		}


		public override void CreateSkillJournalInfo()
		{
			mTrackedStats = new List<ITrackedStat>();
			mTrackedStats.Add(new DiseasesCured(this));
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
			EWMedicineCatSkill skill = mergeSkill as EWMedicineCatSkill;
		}
	}
}


