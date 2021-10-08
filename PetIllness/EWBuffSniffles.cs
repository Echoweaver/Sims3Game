using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetIllness
{

	public class EWBuffSniffles : Buff
	{

		[Tunable]
		[TunableComment("Odds of getting sick from being in proximity to a sick sim")]
		public static float kProximitySicknessOddsSniffles = 0.1f;

		public class EWBuffInstanceSniffles : BuffInstance
		{
			public bool mIsIndoors;

			public ReactionBroadcaster SnifflesContagionBroadcaster;

			public float mCurrentTotalDurationIncrease;

			public SimDescription mOwningSim;

			public override SimDescription TargetSim => mOwningSim;

			public bool IsIndoors
			{
				set
				{
					if (mIsIndoors != value)
					{
						mIsIndoors = value;
						if (value)
						{
							mTimeoutCount = base.TimeoutCount * kIndoorDurationMutliplier;
						}
						else
						{
							mTimeoutCount = base.TimeoutCount / kIndoorDurationMutliplier;
						}
					}
				}
			}

			public EWBuffInstanceSniffles()
			{
			}

			public EWBuffInstanceSniffles(Buff buff, BuffNames buffGuid, int effectValue,
				float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				EWBuffInstanceSniffles buffInstanceSniffles = new EWBuffInstanceSniffles(mBuff,
					mBuffGuid, mEffectValue, mTimeoutCount);
				buffInstanceSniffles.mIsIndoors = mIsIndoors;
				return buffInstanceSniffles;
			}

			public override void SetTargetSim(SimDescription targetSim)
			{
				mOwningSim = targetSim;
			}

			public void ModifyDuration(float delta)
			{
				if (mIsIndoors)
				{
					delta *= kIndoorDurationMutliplier;
				}
				if (mCurrentTotalDurationIncrease + delta < kMaxDurationIncrease)
				{
					mTimeoutCount += delta;
					mCurrentTotalDurationIncrease += delta;
				}
				else
				{
					mTimeoutCount += kMaxDurationIncrease - mCurrentTotalDurationIncrease;
					mCurrentTotalDurationIncrease = kMaxDurationIncrease;
				}
			}
		}

		[Tunable]
		[TunableComment("duration modifer when sim is indoors")]
		public static float kIndoorDurationMutliplier = 0.7f;

		[TunableComment("Broadcaster that is on the sim who is sick")]
		[Tunable]
		public static ReactionBroadcasterParams kSickBroadcastParams = new ReactionBroadcasterParams();

		[TunableComment("max possible duration increase")]
		[Tunable]
		public static float kMaxDurationIncrease = 1440f;

		public EWBuffSniffles(BuffData info)
			: base(info)
		{
		}

		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
			if (bm.Actor.SimDescription.IsImmuneToAllergiesAndSickness())
			{
				return false;
			}
			return base.ShouldAdd(bm, axisEffected, moodValue);
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new EWBuffInstanceSniffles(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			EWBuffInstanceSniffles buffInstanceSniffles = bi as EWBuffInstanceSniffles;
			buffInstanceSniffles.SnifflesContagionBroadcaster = new ReactionBroadcaster(bi.TargetSim.CreatedSim,
				kSickBroadcastParams, SnifflesContagionCallback);
			base.OnAddition(bm, bi, travelReaddition);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			EWBuffInstanceSniffles buffInstanceGermy = bi as EWBuffInstanceSniffles;
			if (buffInstanceGermy.SnifflesContagionBroadcaster != null)
			{
				buffInstanceGermy.SnifflesContagionBroadcaster.Dispose();
				buffInstanceGermy.SnifflesContagionBroadcaster = null;
			}
			base.OnRemoval(bm, bi);
		}

		public void SnifflesContagionCallback(Sim s, ReactionBroadcaster rb)
		{
			EWDisease.Manager(s.SimDescription).PossibleProximityContagion(kProximitySicknessOddsSniffles);
		}
	}
}
