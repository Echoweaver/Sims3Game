using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Hud;

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
 * 1 - Fleas: RODENT
 * 2 - Disease, nausea: GREENLEAF, BUZZBERRY, SWEETGRASS, WATERMELON (35)
 * 3 - Wounds, reduce by one level: [sulfur], GARLIC, <wolfsbane>, WATERMELON, APPLE (45)
 * 4 - Disease, whitecough: HONEY, ONION, [sweet william], LICORICE (55)
 * 5 - Disease, greencough: WONDERPETAL, [azalea],  PEPPERMINT, LAVENDER, [blue flax] (65)
 * 6 - Childbirth: RASPBERRY, BASIL, BELL PEPPER (75)
 * 7 - Wounds, remove: BLACKBERRY, <mandrake root>, [cosmos], BUMBLELEAF, LEMON, LIMEx (85)
 * 8 - 95
 * 9 - 100?
 */

namespace Echoweaver.Sims3Game.WarriorCats
{

    public class EWMedicineCatSkill : Skill
    {
        public const SkillNames SkillNameID = (SkillNames)0x277ECF3A;


        public const string sEWLocalizationKey = "Echoweaver/Skills/EWMedicineCatSkill:";

        public static float kEWMedicineCatGainRateNormal = 5f;

        int mFleasCured = 0;
		int mCureAttempts = 0;
		int mCureSuccess = 0;
		int mMinorWoundsHealed = 0;

        bool mTestOppIsNew = false;

        bool mTestOppIsCompleted = false;

        [Persistable(false)]
        public List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

		[Persistable(true)]
		public Dictionary<BuffInstance, List<string>> mFailedCures = new Dictionary<BuffInstance, List<string>>();

        public EWMedicineCatSkill(SkillNames guid) : base(guid)
        {
        }

        private EWMedicineCatSkill()
        {
        }

		public new string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + ":" + name, parameters);
		}

		public override List<ITrackedStat> TrackedStats => mTrackedStats;

		public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;

		public class FleasCured : ITrackedStat
		{
			public EWMedicineCatSkill mSkill;

			//public string Description => Localization.LocalizeString(sEWLocalizationKey + ":FleasCured", mSkill.mFleasCured);
			public string Description => "EWLocalize-FleasCured: " + mSkill.mFleasCured;

			public FleasCured(EWMedicineCatSkill skill)
			{
				mSkill = skill;
			}
		}

		public class MinorWoundsHealed : ITrackedStat
		{
			public EWMedicineCatSkill mSkill;

			//public string Description => Localization.LocalizeString(sEWLocalizationKey + ":FleasCured", mSkill.mFleasCured);
			public string Description => "EWLocalize-MinorWoundsHealed: " + mSkill.mMinorWoundsHealed;

			public MinorWoundsHealed(EWMedicineCatSkill skill)
			{
				mSkill = skill;
			}
		}

		public class SuccessRate : ITrackedStat
		{
			public EWMedicineCatSkill mSkill;

			int successStat = 0;

			//public string Description => Localization.LocalizeString(sEWLocalizationKey + ":SuccessRate",
			//successStat.ToString("P"));
			public string Description
            {
				get {
					if (mSkill.mCureAttempts != 0 && mSkill.mCureSuccess != 0)
                    {
						successStat = mSkill.mCureSuccess / mSkill.mCureAttempts;
                    }
					return "EWLocalize-SuccessRate: " + successStat.ToString("P");
				}
            }

			public SuccessRate(EWMedicineCatSkill skill)
			{
				mSkill = skill;
			}
		}

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

			public bool Completed => mSkill.mTestOppIsCompleted;

			public OppTest(EWMedicineCatSkill skill)
			{
				mSkill = skill;
			}
		}

		public override void CreateSkillJournalInfo()
		{
			mTrackedStats = new List<ITrackedStat>();
			mTrackedStats.Add(new FleasCured(this));
			mTrackedStats.Add(new SuccessRate(this));
			mLifetimeOpportunities = new List<ILifetimeOpportunity>();
			mLifetimeOpportunities.Add(new OppTest(this));
		}


		[Tunable]
		[TunableComment("")]
		public int kBaseTreatSuccessChance = 30;

		[Tunable]
		[TunableComment("")]
		public int kWoundChanceAdjPerSkillLevel = 7;

		public bool TreatSim(Sim target, BuffInstance buff, string cureName)
		{
			++mCureAttempts;

			if (mFailedCures.ContainsKey(buff))
            {
				// The same buff can't be treated by the same cure that failed before
				if (mFailedCures[buff].Contains(cureName))
                {
					return false;
				}
			}

			int success_chance = kBaseTreatSuccessChance;
			success_chance += kWoundChanceAdjPerSkillLevel * SkillLevel;

			bool success = RandomUtil.RandomChance(success_chance);

			if (!success)
			{
				if (!mFailedCures.ContainsKey(buff))
				{
					mFailedCures[buff] = new List<string>();
				}
				mFailedCures[buff].Add(cureName);
			}
			else
			{
				if (mFailedCures.ContainsKey(buff))
				{
					mFailedCures.Remove(buff);
				}
				++mFleasCured;  // TODO: Will need to record this differently;
				++mCureSuccess;
			}
			return success;
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
