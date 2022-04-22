using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.Germier
{
	internal class BuffEWGermy : Buff
	{
		private const ulong kEWGermyGuid = 0x7CB22EA3EBF0A2F3;
		public static BuffNames buffName = (BuffNames)kEWGermyGuid;

		public static ulong StaticGuid
		{
			get
			{
				return kEWGermyGuid;
			}
		}
		public BuffEWGermy(BuffData data) : base(data)
		{
		}
	}

	//public class BuffEWGermy : Buff
	//{
	//	private const ulong kEWGermyGuid = 0x7CB22EA3EBF0A2F3;
	//	public static ulong StaticGuid
	//	{
	//		get
	//		{
	//			return kEWGermyGuid;

	//		}
	//	}
	//	public static BuffNames buffName = (BuffNames)kEWGermyGuid;

	//	public class CoughingFit : Interaction<Sim, Sim>
	//	{
	//		[DoesntRequireTuning]
	//		public class Definition : SoloSimInteractionDefinition<CoughingFit>, ISoloInteractionDefinition
	//		{
	//			public const string sLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit";

	//			public static string LocalizeString(string name, params object[] parameters)
	//			{
	//				return Localization.LocalizeString("Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:" + name,
	//					parameters);
	//			}

	//			public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
	//			{
	//				return LocalizeString("CoughingFit");
	//			}

	//			public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
	//			{
	//				if (a == target)
	//				{
	//					return isAutonomous;
	//				}
	//				return false;
	//			}
	//		}

	//		public const string sLocalizationKey = "Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit";

	//		public static ISoloInteractionDefinition Singleton = new Definition();

	//		public static string LocalizeString(string name, params object[] parameters)
	//		{
	//			return Localization.LocalizeString("Gameplay/ActorSystems/BuffPestilencePlague/CoughingFit:" + name,
	//				parameters);
	//		}

	//		public override bool Run()
	//		{
	//			StandardEntry();
	//			EnterStateMachine("CoughingFit", "Enter", "x");
	//			AnimateSim("Exit");
	//			StandardExit();
	//			return true;
	//		}
	//	}
	//	public class BuffInstanceEWGermy : BuffInstance
	//	{
	//		public Sim mPlaguedSim;

	//		public AlarmHandle mCoughingFitAlarm = AlarmHandle.kInvalidHandle;

	//		public AlarmHandle mStageAdvanceAlarmHandle = AlarmHandle.kInvalidHandle;

	//		public VisualEffect mEffect;

	//		public bool mIsIndoors;

	//		public int currentHygiene;

	//		public int currentEnergy;

	//		public SimDescription mOwningSim;

	//		public override SimDescription TargetSim => mOwningSim;

	//		public BuffInstanceEWGermy()
	//		{
	//		}

	//		public BuffInstanceEWGermy(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
	//			: base(buff, buffGuid, effectValue, timeoutCount)
	//		{
	//		}

	//		public override BuffInstance Clone()
	//		{
	//			BuffInstanceEWGermy buffInstanceGermy = new BuffInstanceEWGermy(mBuff, mBuffGuid, mEffectValue,
	//				mTimeoutCount);
	//			buffInstanceGermy.mIsIndoors = mIsIndoors;
	//			return buffInstanceGermy;
	//		}

	//		public override void Dispose(BuffManager bm)
	//		{
	//			if (mCoughingFitAlarm != AlarmHandle.kInvalidHandle)
	//			{
	//				bm.Actor.RemoveAlarm(mCoughingFitAlarm);
	//				mCoughingFitAlarm = AlarmHandle.kInvalidHandle;
	//			}
	//			if (mEffect != null)
	//			{
	//				mEffect.Stop();
	//				mEffect.Dispose();
	//				mEffect = null;
	//			}
	//		}

	//		public override void SetTargetSim(SimDescription targetSim)
	//		{
	//			mOwningSim = targetSim;
	//		}

	//		public void AdvancePlagueStage()
	//		{
	//			mCoughingFitAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffPestilencePlague.MaxTimeBetweenCoughingFits
	//				- BuffPestilencePlague.MinTimeBetweenCoughingFits) + BuffPestilencePlague.MinTimeBetweenCoughingFits,
	//				TimeUnit.Minutes, DoCoughingFit, "Buff Pestilence: Time until next coughing fit", AlarmType.DeleteOnReset);
	//		}

	//		public void DoCoughingFit()
	//		{
	//			mPlaguedSim.InteractionQueue.AddNext(CoughingFit.Singleton
	//				.CreateInstance(mPlaguedSim, mPlaguedSim, new InteractionPriority(InteractionPriorityLevel.High),
	//				isAutonomous: true, cancellableByPlayer: false));
	//			mCoughingFitAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffPestilencePlague.MaxTimeBetweenCoughingFits
	//				- BuffPestilencePlague.MinTimeBetweenCoughingFits) + BuffPestilencePlague.MinTimeBetweenCoughingFits,
	//				TimeUnit.Minutes, DoCoughingFit, "Buff Pestilence: Time until next coughing fit", AlarmType.DeleteOnReset);
	//		}
	//	}

	//	public BuffEWGermy(BuffData info)
	//		: base(info)
	//	{
	//	}

	//	public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
	//	{
	//		if (bm.Actor.SimDescription.IsImmuneToAllergiesAndSickness())
	//		{
	//			return false;
	//		}
	//		return base.ShouldAdd(bm, axisEffected, moodValue);
	//	}

	//	public override BuffInstance CreateBuffInstance()
	//	{
	//		return new BuffInstanceEWGermy(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
	//	}

	//	public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
	//	{
	//		BuffInstanceEWGermy buffInstance = bi as BuffInstanceEWGermy;
	//		buffInstance.mPlaguedSim = bm.Actor;
	//		bm.Actor.InteractionQueue.AddNext(BuffPestilencePlague.CoughingFit.Singleton.CreateInstance(bm.Actor, bm.Actor,
	//			new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true, cancellableByPlayer: false));
	//		buffInstance.mStageAdvanceAlarmHandle = bm.Actor.AddAlarm(BuffPestilencePlague.TimeTilCoughingFits,
	//			TimeUnit.Minutes, buffInstance.AdvancePlagueStage, "Buff Pestilence: Advance to coughing fits",
	//			AlarmType.DeleteOnReset);
	//		base.OnAddition(bm, bi, travelReaddition);
	//	}

	//	public override void OnRemoval(BuffManager bm, BuffInstance bi)
	//	{
	//		base.OnRemoval(bm, bi);
	//	}

	//	public void GermyContagionCallback(Sim s, ReactionBroadcaster rb)
	//	{
	//		s.SimDescription.HealthManager?.PossibleProximityContagion();
	//	}

	//}

}
