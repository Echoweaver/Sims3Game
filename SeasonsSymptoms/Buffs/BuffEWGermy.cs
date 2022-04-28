using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery

namespace Echoweaver.Sims3Game.SeasonsSymptoms.Buffs
{
	//XMLBuffInstanceID = 1655640191445312029ul
	public class BuffEWGermy : Buff
	{
		public const ulong mGuid = 0xA50864C570FA9FC1ul;
		public const BuffNames buffName = (BuffNames)mGuid;

		public class BuffInstanceEWGermy : BuffInstance
		{
			public Sim mPlaguedSim;
			public AlarmHandle mCoughingAlarm = AlarmHandle.kInvalidHandle;
			public AlarmHandle mSneezeAlarm = AlarmHandle.kInvalidHandle;

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
				if (mCoughingAlarm != AlarmHandle.kInvalidHandle)
				{
					bm.Actor.RemoveAlarm(mCoughingAlarm);
					mCoughingAlarm = AlarmHandle.kInvalidHandle;
				}
				if (mSneezeAlarm != AlarmHandle.kInvalidHandle)
				{
					bm.Actor.RemoveAlarm(mCoughingAlarm);
					mSneezeAlarm = AlarmHandle.kInvalidHandle;
				}
			}

			public void DoCoughing()
			{
				mPlaguedSim.InteractionQueue.AddNext(Coughing.Singleton.CreateInstance(mPlaguedSim,
					mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true,
					cancellableByPlayer: false));
				mCoughingAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffPestilencePlague
					.MinTimeBetweenCoughingFits, BuffPestilencePlague.MaxTimeBetweenCoughingFits),
					TimeUnit.Minutes, DoCoughing, "BuffEWGermy: Time until next coughing fit",
					AlarmType.DeleteOnReset);
			}

			public void DoSneeze()
			{
				mPlaguedSim.InteractionQueue.AddNext(Sneeze.Singleton.CreateInstance(mPlaguedSim,
					mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true,
					cancellableByPlayer: false));
				mSneezeAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffPestilencePlague
					.MinTimeBetweenCoughingFits, BuffPestilencePlague.MaxTimeBetweenCoughingFits),
					TimeUnit.Minutes, DoSneeze, "BuffEWGermy: Time until next sneeze",
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

		public class Coughing : Interaction<Sim, Sim>
		{
			[DoesntRequireTuning]
			public class Definition : SoloSimInteractionDefinition<Coughing>, ISoloInteractionDefinition
			{
				public const string sLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit";

				public static string LocalizeString(string name, params object[] parameters)
				{
					return Localization.LocalizeString("Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:"
						+ name, parameters);
				}

				public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
				{
					return LocalizeString("CoughingFit");
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

			public const string sLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit";

			public static ISoloInteractionDefinition Singleton = new Definition();

			public static string LocalizeString(string name, params object[] parameters)
			{
				return Localization.LocalizeString("Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:"
					+ name, parameters);
			}

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
					// TODO: Localize!
					return "Sneeze";
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

			buffInstance.mSneezeAlarm = buffInstance.mPlaguedSim.AddAlarm(BuffPestilencePlague
				.MinTimeBetweenCoughingFits, TimeUnit.Minutes, buffInstance.DoSneeze,
				"BuffEWGermy: Time until next sneeze", AlarmType.DeleteOnReset);

            buffInstance.DoCoughing();
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWGermy(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

	}
}