using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery

namespace Echoweaver.Sims3Game.SeasonsSymptoms.Buffs
{
	//XMLBuffInstanceID = 1655640191445312029ul
	public class BuffEWAllergies : Buff
	{
		public const ulong mGuid = 0x929FFB3EC2CFE481ul;
		public const BuffNames buffName = (BuffNames)mGuid;
		
		public class BuffInstanceEWAllergies : BuffInstance
		{
			public Sim mPlaguedSim;
			public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;

			[Tunable]
			[TunableComment("Range: Sim minutes.  Description:  Min time until next symptom.")]
			public static float kMinTimeBetweenSymptoms = 60f;

			[TunableComment("Range: Sim minutes.  Description:  Max time until next symptom.")]
			[Tunable]
			public static float kMaxTimeBetweenSymptoms = 180f;


			public BuffInstanceEWAllergies()
			{
			}

			public BuffInstanceEWAllergies(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				BuffInstanceEWAllergies buffInstance = new BuffInstanceEWAllergies(mBuff, mBuffGuid, mEffectValue,
					mTimeoutCount);
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

			public void DoSymptom()
			{
				int symptomType = RandomUtil.GetInt(1, 2);
				if (symptomType == 1)
				{
					mPlaguedSim.InteractionQueue.AddNext(ZoneOut.Singleton.CreateInstance(mPlaguedSim,
					mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true,
					cancellableByPlayer: false));
				} else
                {
					mPlaguedSim.BuffManager.AddElement(BuffNames.CottonMouth, (Origin)ResourceUtils
						.HashString64("fromEWAllergies"));
				}

				mSymptomAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
					kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWAllergies: Time until next symptom",
					AlarmType.DeleteOnReset);
			}


		}
		public BuffEWAllergies(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
			return true;
		}

		public class ZoneOut : Interaction<Sim, Sim>
		{
			[DoesntRequireTuning]
			public class Definition : SoloSimInteractionDefinition<ZoneOut>, ISoloInteractionDefinition
			{

				public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
				{
					return Localization.LocalizeString("Gameplay/ActorSystems/BuffEWAllergies:ZoneOut");
				}

				public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
				{
					if (a == target)
					{
						return isAutonomous;
					}
					return false;
				}
			}

			public static ISoloInteractionDefinition Singleton = new Definition();

			public override bool Run()
			{
				StandardEntry();
				EnterStateMachine("ewheadpain", "Enter", "x");
				AnimateSim("Exit");
				// TODO: Add Origin "From Allergies"
				Actor.BuffManager.AddElement(BuffNames.Dazed, (Origin)ResourceUtils.HashString64("fromEWAllergies"));
				StandardExit();

				return true;
			}
		}
		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			BuffInstanceEWAllergies buffInstance = bi as BuffInstanceEWAllergies;
			buffInstance.mPlaguedSim = bm.Actor;
            buffInstance.DoSymptom();
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWAllergies(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}
	}

}