using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery


namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetGermy : Buff
	{
		public const ulong mGuid = 0x9086F0050AC3673Dul;
        public const BuffNames buffName = (BuffNames)mGuid;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenSymptoms = 60f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenSymptoms = 120f;

        public class BuffInstanceEWPetGermy : BuffInstance
        {
            public ReactionBroadcaster PetGermyContagionBroadcaster;
            public SimDescription mSickSim;
            public override SimDescription TargetSim => mSickSim;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;

            public BuffInstanceEWPetGermy()
            {
            }

            public BuffInstanceEWPetGermy(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetGermy buffInstance = new BuffInstanceEWPetGermy(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
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
                    mSymptomAlarm = AlarmHandle.kInvalidHandle;
                }
            }

            public void DoSymptom()
            {
                // TODO: Now we need symptoms. For cough: Convert cat hairball animation to include dogs?
                // TODO: Add correct buff origin (from Tummy Trouble)
                //mSickSim.CreatedSim.BuffManager.AddElement(BuffNames.NauseousPet, Origin.FromUnknown);

                mSymptomAlarm = mSickSim.CreatedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetGermy: Time until next symptom",
                    AlarmType.DeleteOnReset);
            }
        }
        public BuffEWPetGermy(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            if ((bm.Actor.IsADogSpecies || bm.Actor.IsCat) && (bm.Actor.SimDescription.AdultOrAbove))
            {
                return true;
            }
            return false;
		}

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWPetGermy(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            buffInstance.PetGermyContagionBroadcaster = new ReactionBroadcaster(bi.TargetSim.CreatedSim,
                BuffGermy.kSickBroadcastParams, PetGermyContagionCallback);
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            if (buffInstance.PetGermyContagionBroadcaster != null)
            {
                buffInstance.PetGermyContagionBroadcaster.Dispose();
                buffInstance.PetGermyContagionBroadcaster = null;
            }
            base.OnRemoval(bm, bi);
        }

        public void PetGermyContagionCallback(Sim s, ReactionBroadcaster rb)
        {
            if (s.SimDescription.IsHuman)
            {
                // Humans can catch from cats but not the reverse unless I can figure
                // out some broadcaster magic.
                s.SimDescription.HealthManager?.PossibleProximityContagion();
            }
        }

    }

}