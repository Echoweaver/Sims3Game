using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
namespace Echoweaver.Sims3Game.PetIllness
{
	public class BuffEWPetstilence : Buff
	{
		private const ulong kEWPetstilenceGuid = 0xD79EDE5CB789F85D;
		public static ulong StaticGuid
		{
			get
			{
				return kEWPetstilenceGuid;

			}
		}
		public static BuffNames buffName = (BuffNames)kEWPetstilenceGuid;

		[Tunable]
		[TunableComment("Odds of getting sick from being in proximity to a sick sim")]
		public static float kProximitySicknessOddsSniffles = 0.1f;

		public class BuffInstancePetstilence : BuffInstance
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

			public BuffInstancePetstilence()
			{
			}

			public BuffInstancePetstilence(Buff buff, BuffNames buffGuid, int effectValue,
				float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				BuffInstancePetstilence buffInstance = new BuffInstancePetstilence(mBuff,
					mBuffGuid, mEffectValue, mTimeoutCount);
				buffInstance.mIsIndoors = mIsIndoors;
				return buffInstance;
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

		public BuffEWPetstilence(BuffData info)
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
			return new BuffInstancePetstilence(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			BuffInstancePetstilence buffInstanceSniffles = bi as BuffInstancePetstilence;
			//Actor.PlaySoloAnimation("ac_idle_sit_groomSelf_hack_x", yield: true, (ProductVersion)512);

			buffInstanceSniffles.SnifflesContagionBroadcaster = new ReactionBroadcaster(bi.TargetSim.CreatedSim,
				kSickBroadcastParams, PetstilenceContagionCallback);
			base.OnAddition(bm, bi, travelReaddition);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			BuffInstancePetstilence buffInstanceGermy = bi as BuffInstancePetstilence;
			if (buffInstanceGermy.SnifflesContagionBroadcaster != null)
			{
				buffInstanceGermy.SnifflesContagionBroadcaster.Dispose();
				buffInstanceGermy.SnifflesContagionBroadcaster = null;
			}
			base.OnRemoval(bm, bi);
		}

		public void PetstilenceContagionCallback(Sim s, ReactionBroadcaster rb)
		{
			EWDisease.Manager(s.SimDescription).PossibleProximityContagion(kProximitySicknessOddsSniffles);
		}
	}
}
