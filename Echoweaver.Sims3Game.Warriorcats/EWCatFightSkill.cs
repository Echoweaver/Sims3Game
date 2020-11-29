using System.Collections.Generic;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Hud;

namespace Echoweaver.Sims3Game
{
    public class EWCatFightSkill : Skill
    {
        [Persistable(false)]
        private List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

        public int mFightsWon = 0;
        public int mFightsLost = 0;

        public const SkillNames skillNameID = (SkillNames)0x06BFF0BE;

        public EWCatFightSkill(SkillNames guid) : base(guid)
        {
            mFightsWon = 0;
            mFightsLost = 0;
        }

        private EWCatFightSkill()
        {
        }

        public class FightsWon : ITrackedStat
        {
            private EWCatFightSkill mSkill;

            public string Description => Localization.LocalizeString("Echoweaver/Warriorcats/SkillStats:CountFightsWon",
                mSkill.mFightsWon);

            public FightsWon(EWCatFightSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsLost : ITrackedStat
        {
            private EWCatFightSkill mSkill;

            public string Description => Localization.LocalizeString("Echoweaver/Warriorcats/SkillStats:CountFightsLost") +
                " " + mSkill.mFightsLost;

            public FightsLost(EWCatFightSkill skill)

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
            EWCatFightSkill fightSkill = skill as EWCatFightSkill;
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
