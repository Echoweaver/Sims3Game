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
using static Echoweaver.Sims3Game.PetDisease.Buffs.BuffEWPetGermy;
using static Echoweaver.Sims3Game.PetDisease.Loader;


//Template Created by Battery
// Pneumonia (Greencough)
//   -- Develops from Germy.
//   -- Symptoms: Fever, exhaustion, frequent coughing
//   -- Contagion by contact
//   -- Can be lethal.

// Pant, feverish moodlet, pass out, cough
// Dog panting animation, coughing from EWPetGermy

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetPneumonia : Buff
	{
		public const ulong mGuid = 0x904F100B14974699ul;
        public const BuffNames buffName = (BuffNames)mGuid;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenSymptoms = 45f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenSymptoms = 90f;


        public class BuffInstanceEWPetPneumonia : BuffInstance
        {
            public Sim mSickSim;
            public override SimDescription TargetSim => mSickSim.SimDescription;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public bool isDeadly = false;

            public BuffInstanceEWPetPneumonia()
            {
            }

            public BuffInstanceEWPetPneumonia(Buff buff, BuffNames buffGuid, int effectValue,
                float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetPneumonia buffInstance = new BuffInstanceEWPetPneumonia(mBuff,
                    mBuffGuid, mEffectValue, mTimeoutCount);
                buffInstance.mSickSim = mSickSim;
                return buffInstance;
            }

            public override void SetTargetSim(SimDescription targetSim)
            {
                mSickSim = targetSim.CreatedSim;
            }

            public override void Dispose(BuffManager bm)
            {
                if (mSymptomAlarm != AlarmHandle.kInvalidHandle)
                {
                    bm.Actor.RemoveAlarm(mSymptomAlarm);
                    mSymptomAlarm = AlarmHandle.kInvalidHandle;
                }
            }

            public void DoSymptom()
            {
                int symptomType;
                if (mSickSim.IsSleeping)
                {
                    // if sim is sleeping 50% nothing will happen
                    // Otherwise wake up sim unless tuning says otherwise.
                    symptomType = RandomUtil.GetInt(1, 4);
                }
                else
                {
                    symptomType = RandomUtil.GetInt(1, 2);
                }
                DebugNote("Pneumonia symptom " + symptomType + ": " + mSickSim.FullName);

                if (symptomType == 1)
                {
                    mSickSim.InteractionQueue.AddNext(BuffEWPetGermy.Cough.Singleton.CreateInstance(mSickSim,
                        mSickSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                        isAutonomous: true, cancellableByPlayer: false));
                    // Additional energy loss for pneumonia
                    mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                            .GetMotiveValue(CommodityKind.Energy) - 10);
                }
                else if (symptomType == 2)
                {
                    mSickSim.Motives.SetValue(CommodityKind.Energy, 20);
                }

                mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetPneumonia: Time until next symptom",
                    AlarmType.DeleteOnReset);
            }
        }

        public BuffEWPetPneumonia(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWPetPneumonia(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetPneumonia buffInstance = bi as BuffInstanceEWPetPneumonia;
            buffInstance.mSickSim = bm.Actor;
            buffInstance.isDeadly = RandomUtil.RandomChance(kChanceOfPneumonia);
            if (buffInstance.isDeadly)
            {
                DebugNote("This pneumonia is deadly: " + buffInstance.mSickSim.FullName);
            }
            buffInstance.DoSymptom();
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetPneumonia buffInstance = bi as BuffInstanceEWPetPneumonia;
            // Check to see if this turns into Pneumonia and add buff if applicable.
            // TODO: when the buff is removed from treatment, make sure to set the
            // pneumonia check to false.
            if (buffInstance.isDeadly)
            {
                EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(buffInstance.mSickSim,
                    buffInstance.mSickSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath),
                    false, false) as EWPetSuccumbToDisease;
                buffInstance.mSickSim.InteractionQueue.AddNext(die);
            }

            base.OnRemoval(bm, bi);
        }
    }		
}