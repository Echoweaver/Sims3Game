using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery

// Petstilence (Carrionplace Disease)
//   -- Generated from fights or hunting (maybe just hunting rodents), getting fleas
//   -- Bloodborne, transmitted by fighting, woohoo
//   -- Symptoms: passing out and vomiting, moments of psychosis, drooling
//   -- Frequently lethal.

// drool effect codes:
// ep5catdrool
// ep5dogdrool
// ep5dogdrooljaw
// ep5doglittledrool
// ep5doglittledrooljaw

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetstilence : Buff
	{
		public const ulong mGuid = 0x7768716F913C2054ul;
        public const BuffNames buffName = (BuffNames)mGuid;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenSymptoms = 120f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenSymptoms = 360f;

        [TunableComment("Min petstilence duration (Hours)")]
        [Tunable]
        public static float kMinDuration = 48f; // 2 days

        [TunableComment("Max petstilence duration (Hours)")]
        [Tunable]
        public static float kMaxDuration = 96f; // 4 days

        [TunableComment("Max petstilence duration (Hours)")]
        [Tunable]
        public static float kAmbientSicknessOdds = 0.01f; // 4 days

        public class BuffInstanceEWPetstilence : BuffInstance
        {
            public SimDescription mSickSim;
            public override SimDescription TargetSim => mSickSim;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
            public float mStageLength = 24f;  // Length of disease stage in hours
            public int mStage = 0;

            public BuffInstanceEWPetstilence()
            {
            }

            public BuffInstanceEWPetstilence(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetstilence buffInstance = new BuffInstanceEWPetstilence(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
                buffInstance.mSickSim = mSickSim;
                return buffInstance;
            }

            public override void SetTargetSim(SimDescription targetSim)
            {
                mSickSim = targetSim;
            }

            public override void Dispose(BuffManager bm)
            {
                if (mSymptomAlarm != AlarmHandle.kInvalidHandle)
                {
                    bm.Actor.RemoveAlarm(mSymptomAlarm);
                    bm.Actor.RemoveAlarm(mAdvanceDiseaseAlarm);
                    mSymptomAlarm = AlarmHandle.kInvalidHandle;
                    mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
                }
            }

            public void DoSymptom()
            {
                // TODO: Symptoms. Passing out. Randomly mean-chasing someone on the lot. Drooling.
                // Vomiting.

                switch (mStage)
                {
                    case 0:
                        // First stage, mildest symptoms. Maybe just a fuzzy feeling
                        break;
                    case 1:
                        // Second stage, more serious symptoms. Vomiting.
                        break;
                    case 2:
                        // Final stage, fly swarms (?), hygiene drops, passing out 
                    default:
                        break;
                }

                mSymptomAlarm = mSickSim.CreatedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetstilence: Time until next symptom",
                    AlarmType.DeleteOnReset);
            }

            public void AdvanceDisease()
            {
                if (mStage < 3)
                {
                    mStage++;
                    mAdvanceDiseaseAlarm = mSickSim.CreatedSim.AddAlarm(mStageLength, TimeUnit.Minutes,
                        AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                        AlarmType.DeleteOnReset);
                    DoSymptom();
                } else if (mStage >= 3)
                {
                    // This disease is lethal if not cured
                    if (Loader.kAllowPetDeath)
                    {
                        mSickSim.CreatedSim.Kill(Loader.diseaseDeathType);
                    } else
                    {
                        // Passing out with a Grave Wound means dying of the wound
                        EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(mSickSim.CreatedSim,
                            mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false)
                            as EWPetSuccumbToDisease;
                        mSickSim.CreatedSim.InteractionQueue.AddNext(die);
                    }
                }
            }
        }

        public static void CheckAmbientContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(kAmbientSicknessOdds))
            // This ambient check is 1%. It's rare to catch.
            {
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckContactContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kRomanticSicknessOdds))
            // Woohoo/blood contact is 60%
            {
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public class GetSick
        {
            Sim sickSim;
            public GetSick(Sim sim)
            {
                sickSim = sim;
            }

            public void Execute()
            {
                if (!sickSim.BuffManager.HasElement(buffName) && !Loader.checkForVaccination(sickSim))
                {
                    // TODO: Should check for cooldown buff so pet can't get the same disease
                    // immediately after?
                    sickSim.BuffManager.AddElement(buffName, RandomUtil.GetFloat(kMinDuration,
                    kMaxDuration), Origin.None);
                }
            }
        }

        public BuffEWPetstilence(BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            buffInstance.mSickSim = bm.Actor.SimDescription;
            buffInstance.mStageLength = bi.TimeoutCount / 3;
            buffInstance.mAdvanceDiseaseAlarm = bm.Actor.AddAlarm(buffInstance.mStageLength,
                TimeUnit.Minutes, buffInstance.AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                AlarmType.DeleteOnReset);
            buffInstance.DoSymptom();
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            //BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            base.OnRemoval(bm, bi);
        }

        public void MaybeCatchPetstilence(Sim s)
        {
            // This is not transmitted by proximity, so no broadcaster.
            if ((s.IsADogSpecies || s.IsCat) && s.SimDescription.AdultOrAbove)
            {
                if (!s.BuffManager.HasElement(buffName) && RandomUtil
                    .RandomChance01(HealthManager.kRomanticSicknessOdds))
                {
                    // Get Sick
                    s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                        HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                        TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                        AlarmType.AlwaysPersisted);
                }
            }
        }
    }
		
}