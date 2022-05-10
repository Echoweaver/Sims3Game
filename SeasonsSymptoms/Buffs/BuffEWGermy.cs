using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.SeasonsSymptoms.Buffs
{
	//XMLBuffInstanceID = 1655640191445312029ul
	// Custom origin: (Origin)ResourceUtils.HashString64("fromEWGermy")

	public class BuffEWGermy : Buff
	{
		public const ulong mGuid = 0xA50864C570FA9FC1ul;
		public const BuffNames buffName = (BuffNames)mGuid;

		[Tunable]
		[TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
		public static float kMinTimeBetweenSymptoms = 60f;

		[TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
		[Tunable]
		public static float kMaxTimeBetweenSymptoms = 120f;


		public class BuffInstanceEWGermy : BuffInstance
		{
			public Sim mPlaguedSim;
			public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;

			public BuffInstanceEWGermy()
			{
			}

			public BuffInstanceEWGermy(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				BuffInstanceEWGermy buffInstance = new BuffInstanceEWGermy(mBuff, mBuffGuid, mEffectValue,
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
				int symptomType;
				// Not sure how the moodlet gets removed without clearing the alarm, but it
				// appears to be happening.
				if (!mPlaguedSim.BuffManager.HasElement(BuffNames.Germy))
                {
					return;
                }
				if (mPlaguedSim.IsSleeping)
                {
					// if sim is sleeping 50% nothing will happen
					// Since cough/sneeze wakes up the sim, too much is just too much
					symptomType = RandomUtil.GetInt(1, 4);
				} else
                {
					symptomType = RandomUtil.GetInt(1, 2);
				}
				if (symptomType == 1)
				{
					mPlaguedSim.InteractionQueue.AddNext(Cough.Singleton.CreateInstance(mPlaguedSim,
						mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true,
						cancellableByPlayer: false));
				} else if (symptomType == 2)
                {
					mPlaguedSim.InteractionQueue.AddNext(Sneeze.Singleton.CreateInstance(mPlaguedSim,
						mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true,
						cancellableByPlayer: false));
				}
				mSymptomAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
					kMaxTimeBetweenSymptoms),TimeUnit.Minutes, DoSymptom, "BuffEWGermy: Time until next symptom",
					AlarmType.DeleteOnReset);
			}

		}

		public BuffEWGermy(BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
			return true;
		}

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString("Gameplay/ActorSystems/BuffEWGermy:"
				+ name, parameters);
		}

		public class Cough : Interaction<Sim, Sim>
		{
			[DoesntRequireTuning]
			public class Definition : SoloSimInteractionDefinition<Cough>, ISoloInteractionDefinition
			{

				public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
				{
					return LocalizeString("Cough");
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
                int coughType = RandomUtil.GetInt(1, 4);
                StandardEntry();

				// 25% chance of more severe cough
				if (coughType == 1)
                {
                    EnterStateMachine("ewcoughingfit", "Enter", "x");
                    AnimateSim("Exit");
                    Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives
                        .GetMotiveValue(CommodityKind.Energy) - 20);
                }
                else
                {
                    EnterStateMachine("ewcoughing", "Enter", "x");
                    AnimateSim("Exit");
                    Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives
                        .GetMotiveValue(CommodityKind.Energy) - 10);
                }

                StandardExit();

                return true;
            }
		}

		public class Sneeze : Interaction<Sim, Sim>
		{
			[DoesntRequireTuning]
			public class Definition : SoloSimInteractionDefinition<Sneeze>, ISoloInteractionDefinition
			{

				public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
				{
					return LocalizeString("Sneeze");
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

				EnterStateMachine("ewsneeze", "Enter", "x");

				AnimateSim("Exit");

				EnterStateMachine("ewblownose", "Enter", "x");
				AnimateSim("Exit");
						
				StandardExit();

				Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives.GetMotiveValue
					(CommodityKind.Energy) - 10);

				return true;
			}
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			BuffInstanceEWGermy buffInstance = bi as BuffInstanceEWGermy;
			buffInstance.mPlaguedSim = bm.Actor;

            buffInstance.DoSymptom();
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWGermy(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

	}
}