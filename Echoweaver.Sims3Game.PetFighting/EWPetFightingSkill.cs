using System.Collections.Generic;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Hud;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWPetFightingSkill : Skill
    {
        [Persistable(false)]
        private List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

        public int mFightsWon = 0;
        public int mFightsLost = 0;
        public string sStatsLocalizeKey = "Echoweaver/PetFighting/SkillStats:";

        public const SkillNames skillNameID = (SkillNames)0x20F47569;

        public EWPetFightingSkill(SkillNames guid) : base(guid)
        {
            mFightsWon = 0;
            mFightsLost = 0;
        }

        private EWPetFightingSkill()
        {
        }

        public class FightsWon : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => Localization.LocalizeString(mSkill.sStatsLocalizeKey + "CountFightsWon",
                mSkill.mFightsWon);

            public FightsWon(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsLost : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => Localization.LocalizeString(mSkill.sStatsLocalizeKey + "CountFightsLost") +
                " " + mSkill.mFightsLost;

            public FightsLost(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public void lostFight()
        {
            ++mFightsLost;
        }

        public void wonFight()
        {
            ++mFightsWon;
        }

        public override List<ITrackedStat> TrackedStats
        {
            get
            {
                return mTrackedStats;
            }
        }

        public override List<ILifetimeOpportunity> LifetimeOpportunities
        {
            get
            {
                return mLifetimeOpportunities;
            }
        }

        public override void MergeTravelData(Skill skill)
        {
            base.MergeTravelData(skill);
            EWPetFightingSkill fightSkill = skill as EWPetFightingSkill;
            if (fightSkill != null)
            {
                mFightsLost = fightSkill.mFightsLost;
                mFightsWon = fightSkill.mFightsWon;
                mLifetimeOpportunities = fightSkill.mLifetimeOpportunities;
            }
        }

        public override void CreateSkillJournalInfo()
        {
            mTrackedStats = new List<ITrackedStat>();
            mTrackedStats.Add(new FightsLost(this));
            mTrackedStats.Add(new FightsWon(this));
            mLifetimeOpportunities = new List<ILifetimeOpportunity>();
            //            mLifetimeOpportunities.Add(new PatternCollector(this));
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
    }
}
