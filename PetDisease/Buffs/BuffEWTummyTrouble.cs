using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery


namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
    //XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWTummyTrouble : Buff
	{
		public const ulong mGuid = 0xDFF72BA95943E99Dul;
		public const BuffNames buffName = (BuffNames)mGuid;

		[Tunable]
		[TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
		public static float kMinTimeBetweenNausea = 120f;

		[TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
		[Tunable]
		public static float kMaxTimeBetweenNausea = 480f;

        [TunableComment("Broadcaster that is on the sim who is sick")]
        [Tunable]
        public static ReactionBroadcasterParams kSickBroadcastParams = new ReactionBroadcasterParams();

        [TunableComment("Odds of getting sick from being in proximity to a sick sim")]
        [Tunable]
        public static float kBaseStomachFluOdds = 0.1f;

        [TunableComment("Min stomach flu incubation time (Hours)")]
        [Tunable]
        public static float kMinIncubationTime = 6f;

        [TunableComment("Max stomach flu incubation time (Hours)")]
        [Tunable]
        public static float kMaxIncubationTime = 24f;

        [TunableComment("Min stomach flu duration (Hours)")]
        [Tunable]
        public static float kMinDuration = 12f;

        [TunableComment("Max stomach flu duration (Hours)")]
        [Tunable]
        public static float kMaxDuration = 48f;

        public class BuffInstanceEWTummyTrouble : BuffInstance
		{
			public Sim mPlaguedSim;
			public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public bool isFlu = false;
            public ReactionBroadcaster NauseaContagionBroadcaster;

			public BuffInstanceEWTummyTrouble()
			{
			}

			public BuffInstanceEWTummyTrouble(Buff buff, BuffNames buffGuid, int effectValue,
                float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				BuffInstanceEWTummyTrouble buffInstance = new BuffInstanceEWTummyTrouble(mBuff, mBuffGuid,
                    mEffectValue, mTimeoutCount);
				buffInstance.mPlaguedSim = mPlaguedSim;
				return buffInstance;
			}

			public override void Dispose(BuffManager bm)
			{
				if (mSymptomAlarm != AlarmHandle.kInvalidHandle)
				{
					bm.Actor.RemoveAlarm(mSymptomAlarm);
					mSymptomAlarm = AlarmHandle.kInvalidHandle;
				}
			}

            public void SetIsFlu()
            {
                isFlu = true;
                NauseaContagionBroadcaster = new ReactionBroadcaster(mPlaguedSim,
                    kSickBroadcastParams, NauseaContagionCallback);
            }

            public void DoSymptom()
			{
				// TODO: Add correct buff origin (from Tummy Trouble)
				mPlaguedSim.BuffManager.AddElement(BuffNames.NauseousPet, Origin.FromUnknown);

				mSymptomAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenNausea,
					kMaxTimeBetweenNausea), TimeUnit.Minutes, DoSymptom, "BuffEWTummyTrouble: Time until next symptom",
					AlarmType.DeleteOnReset);
			}

            public void NauseaContagionCallback(Sim s, ReactionBroadcaster rb)
            {
                if (s.SimDescription.AdultOrAbove && s.IsPet && !s.TraitManager.HasElement(TraitNames
                    .VomitImmunityPet))
                {
                    TraitManager traitManager = s.TraitManager;
                    float getSickOdds = kBaseStomachFluOdds;
                    if (traitManager.HasElement(TraitNames.PiggyPet))
                    {
                        getSickOdds *= 1.2f;
                    }

                    if (RandomUtil.RandomChance01(getSickOdds))
                    {
                        // Get Sick
                        if (s != null && mSickIncubationAlarm == AlarmHandle.kInvalidHandle &&
                            !s.BuffManager.HasElement(buffName))
                        {
                            mSickIncubationAlarm = s.AddAlarm(RandomUtil.GetFloat(kMaxIncubationTime -
                                kMinIncubationTime) + kMinIncubationTime, TimeUnit.Hours, new GetSick(s).Execute,
                                "stomach flu incubation alarm", AlarmType.AlwaysPersisted);
                        }
                    }
                }
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
                if (!sickSim.BuffManager.HasElement(buffName))
                {
                    sickSim.BuffManager.AddElement(buffName, RandomUtil.GetFloat(kMinDuration,
                        kMaxDuration), Origin.None);
                    BuffInstanceEWTummyTrouble buffInstance = sickSim.BuffManager.GetElement(buffName)
                        as BuffInstanceEWTummyTrouble;
                    buffInstance.SetIsFlu();  // TODO: There should be a better way
                    
                    // TODO: Should we have a popup?
                    // createdSim.ShowTNSAndPlayStingIfSelectable("sting_get_sick", TNSNames.GotSickTNS, createdSim, null, null, null, new bool[1] { createdSim.IsFemale }, false, createdSim);
                }
            }
        }
        public BuffEWTummyTrouble(Buff.BuffData info) : base(info)
        {
        }
        public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
        {
            return bm.Actor.IsPet && bm.Actor.SimDescription.AdultOrAbove;
        }
        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWTummyTrouble(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWTummyTrouble buffInstance = bi as BuffInstanceEWTummyTrouble;
            buffInstance.mPlaguedSim = bm.Actor;
            buffInstance.DoSymptom();
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWTummyTrouble buffInstance = bi as BuffInstanceEWTummyTrouble;
            if (buffInstance.NauseaContagionBroadcaster != null)
            {
                buffInstance.NauseaContagionBroadcaster.Dispose();
                buffInstance.NauseaContagionBroadcaster = null;
            }
            base.OnRemoval(bm, bi);
        }
    }

}