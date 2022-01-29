using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Hud;

namespace Echoweaver.Sims3Game.PetFighting
{
    [Persistable]
    public class EWPetFightingSkill : Skill
    {
        public const SkillNames skillNameID = (SkillNames)0x20F47569;
        public const CommodityKind commodityKindID = (CommodityKind)0x262891D3;
        public string sStatsLocalizeKey = "Echoweaver/PetFighting/SkillStats:";
        public const string sEWLocalizationKey = "Echoweaver/Skills/EWPetFightingSkill";

        [Persistable(false)]
        private List<ITrackedStat> mTrackedStats;

        [Persistable(false)]
        public List<ILifetimeOpportunity> mLifetimeOpportunities;

        public int mFightsWon = 0;
        public int mFightsLost = 0;
        public int mFightsWonHuman = 0;
        public int mFightsLostHuman = 0;
        public int mFightsWonBigDog = 0;
        public int mFightsLostBigDog = 0;
        public int mFightsWonSmallPet = 0;
        public int mFightsLostSmallPet = 0;
        public int mFightsWonHomeLot = 0;

        public static float kSkillGainRateNormal = 10f;
        public static float kSkillGainRateExperienced = 13f;
        public static float kOppHomeDefenderBonus = 1.3f;
        public static float kOppHumanFighterBonus = 1.3f;
        public static float kOppBigPetFighterBonus = 1.3f;
        public static float kOppSmallPetFighterBonus = 1.3f;


        public EWPetFightingSkill(SkillNames guid) : base(guid)
        {
            mFightsWon = 0;
            mFightsLost = 0;
        }

        private EWPetFightingSkill()
        {
        }

        public new string LocalizeString(string name, params object[] parameters)
        {
            return Localization.LocalizeString(SkillOwner.IsFemale, sEWLocalizationKey + ":" + name, parameters);
        }

        public float getEffectiveSkillLevel(bool isHomeLot, Sim target)
        {
            float mod_skill = Math.Max(0, SkillLevel);
            if (isHomeLot && OppHomeDefenderCompleted)
            {
                mod_skill *= kOppHomeDefenderBonus;
            }
            if (target.IsFullSizeDog && OppBigPetFighterCompleted)
            {
                mod_skill *= kOppBigPetFighterBonus;
            } else if (target.IsHuman && OppHumanFighterCompleted)
            {
                mod_skill *= kOppHumanFighterBonus;
            } else if ((target.IsLittleDog || target.IsCat || target.IsRaccoon) && OppSmallPetFighterCompleted)
            {
                mod_skill *= kOppSmallPetFighterBonus;
            }
            return mod_skill;
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

            public string Description => Localization.LocalizeString(mSkill.sStatsLocalizeKey + "CountFightsLost",
                mSkill.mFightsLost);

            public FightsLost(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsWonHuman : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Won Human " + mSkill.mFightsWonHuman;

            public FightsWonHuman(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsLostHuman : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Lost Human " + mSkill.mFightsLostHuman;

            public FightsLostHuman(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }


        public class FightsWonBigDog : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Won Big Dog " + mSkill.mFightsWonBigDog;

            public FightsWonBigDog(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsLostBigDog : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Lost Big Dog " + mSkill.mFightsLostBigDog;

            public FightsLostBigDog(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsWonSmallPet : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Won Small Pet " + mSkill.mFightsWonSmallPet;

            public FightsWonSmallPet(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsLostSmallPet : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights Lost Small Pet " + mSkill.mFightsLostSmallPet;

            public FightsLostSmallPet(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public class FightsWonHomeLot : ITrackedStat
        {
            private EWPetFightingSkill mSkill;

            public string Description => "Fights won on home lot " + mSkill.mFightsWonHomeLot;

            public FightsWonHomeLot(EWPetFightingSkill skill)

            {
                mSkill = skill;
            }
        }

        public void lostFight(Sim s)
        {
            ++mFightsLost;
            if (s.IsHuman)
            {
                ++mFightsLostHuman;
            } else if (s.IsFullSizeDog)
            {
                ++mFightsLostBigDog;
            } else if (s.IsRaccoon || s.IsLittleDog || s.IsCat)
            {
                ++mFightsLostSmallPet;
            }
        }

        public void wonFight(Sim s, bool isOnHomeLot)
        {
            AddPoints(200, true, true);
            ++mFightsWon;
            if (s.IsHuman)
            {
                ++mFightsWonHuman;
            }
            else if (s.IsFullSizeDog)
            {
                ++mFightsWonBigDog;
            }
            else if (s.IsRaccoon || s.IsLittleDog || s.IsCat)
            {
                ++mFightsWonSmallPet;
            }

            if (isOnHomeLot)
            {
                ++mFightsWonHomeLot;
            }
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

        public bool mOppExperiencedFighterIsNew = true;

        public static int kOppExperiencedFighterWinCount = 15;

        public class OppExperiencedFighter : ILifetimeOpportunity
        {
            public EWPetFightingSkill mSkill;

            public string Title => mSkill.LocalizeString("OppExperiencedFighter");

            public string RewardDescription => mSkill.LocalizeString("OppExperiencedFighterDescription", kOppExperiencedFighterWinCount);

            public string AchievedDescription => mSkill.LocalizeString("OppExperiencedFighterAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mOppExperiencedFighterIsNew;
                }
                set
                {
                    mSkill.mOppExperiencedFighterIsNew = value;
                }
            }

            public bool Completed => mSkill.OppExperiencedFighterCompleted;

            public OppExperiencedFighter(EWPetFightingSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool OppExperiencedFighterCompleted
        {
            get
            {
                return (mFightsWon + mFightsLost) >= kOppExperiencedFighterWinCount;
            }
        }

        public bool mOppHumanFighterIsNew = true;

        public static int kOppHumanFighterWinCount = 15;

        public class OppHumanFighter : ILifetimeOpportunity
        {
            public EWPetFightingSkill mSkill;

            public string Title => mSkill.LocalizeString("OppHumanFighter");

            public string RewardDescription => mSkill.LocalizeString("OppHumanFighterDescription", kOppHumanFighterWinCount);

            public string AchievedDescription => mSkill.LocalizeString("OppHumanFighterAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mOppHumanFighterIsNew;
                }
                set
                {
                    mSkill.mOppHumanFighterIsNew = value;
                }
            }

            public bool Completed => mSkill.OppHumanFighterCompleted;

            public OppHumanFighter(EWPetFightingSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool OppHumanFighterCompleted
        {
            get
            {
                return mFightsWonHuman >= kOppHumanFighterWinCount;
            }
        }

        public bool mOppBigPetFighterIsNew = true;

        public static int kOppBigPetFighterWinCount = 15;

        public class OppBigPetFighter : ILifetimeOpportunity
        {
            public EWPetFightingSkill mSkill;

            public string Title => mSkill.LocalizeString("OppBigPetFighter");

            public string RewardDescription => mSkill.LocalizeString("OppBigPetFighterDescription", kOppBigPetFighterWinCount);

            public string AchievedDescription => mSkill.LocalizeString("OppBigPetFighterAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mOppBigPetFighterIsNew;
                }
                set
                {
                    mSkill.mOppBigPetFighterIsNew = value;
                }
            }

            public bool Completed => mSkill.OppBigPetFighterCompleted;

            public OppBigPetFighter(EWPetFightingSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool OppBigPetFighterCompleted
        {
            get
            {
                return mFightsWonBigDog >= kOppBigPetFighterWinCount;
            }
        }

        public bool mOppSmallPetFighterIsNew = true;

        public static int kOppSmallPetFighterWinCount = 15;

        public class OppSmallPetFighter : ILifetimeOpportunity
        {
            public EWPetFightingSkill mSkill;

            public string Title => mSkill.LocalizeString("OppSmallPetFighter");

            public string RewardDescription => mSkill.LocalizeString("OppSmallPetFighterDescription", kOppSmallPetFighterWinCount);

            public string AchievedDescription => mSkill.LocalizeString("OppSmallPetFighterAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mOppSmallPetFighterIsNew;
                }
                set
                {
                    mSkill.mOppHumanFighterIsNew = value;
                }
            }

            public bool Completed => mSkill.OppSmallPetFighterCompleted;

            public OppSmallPetFighter(EWPetFightingSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool OppSmallPetFighterCompleted
        {
            get
            {
                return mFightsWonBigDog >= kOppBigPetFighterWinCount;
            }
        }

        public bool mOppHomeDefenderIsNew = true;

        public static int kOppHomeDefenderWinCount = 15;

        public class OppHomeDefender : ILifetimeOpportunity
        {
            public EWPetFightingSkill mSkill;

            public string Title => mSkill.LocalizeString("OppHomeDefender");

            public string RewardDescription => mSkill.LocalizeString("OppHomeDefenderDescription", kOppHomeDefenderWinCount);

            public string AchievedDescription => mSkill.LocalizeString("OppHomeDefenderAchieved", mSkill.mSkillOwner);

            public bool IsNew
            {
                get
                {
                    return mSkill.mOppHomeDefenderIsNew;
                }
                set
                {
                    mSkill.mOppHomeDefenderIsNew = value;
                }
            }

            public bool Completed => mSkill.OppHomeDefenderCompleted;

            public OppHomeDefender(EWPetFightingSkill skill)
            {
                mSkill = skill;
            }
        }

        public bool OppHomeDefenderCompleted
        {
            get
            {
                return mFightsWonHomeLot >= kOppHomeDefenderWinCount;
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
            mTrackedStats.Add(new FightsLostHuman(this));
            mTrackedStats.Add(new FightsWonHuman(this));
            mTrackedStats.Add(new FightsLostBigDog(this));
            mTrackedStats.Add(new FightsWonBigDog(this));
            mTrackedStats.Add(new FightsLostSmallPet(this));
            mTrackedStats.Add(new FightsWonSmallPet(this));
            mTrackedStats.Add(new FightsWonHomeLot(this));
            mLifetimeOpportunities = new List<ILifetimeOpportunity>();
            mLifetimeOpportunities.Add(new OppHumanFighter(this));
            mLifetimeOpportunities.Add(new OppBigPetFighter(this));
            mLifetimeOpportunities.Add(new OppSmallPetFighter(this));
            mLifetimeOpportunities.Add(new OppHomeDefender(this));
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
