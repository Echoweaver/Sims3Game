using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
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
		
		static bool once;
		
		public static void LoadBuffXMLandParse(ResourceKey[] resourceKeys)
		{
			ResourceKey key = new ResourceKey(1655640191445312029ul, 53690476u, 0u);
			XmlDbData xmlDbData = XmlDbData.ReadData(key, false);
			bool flag = xmlDbData != null;
			if (flag)
			{
				BuffManager.ParseBuffData(xmlDbData, true);
			}
			if(!once)
			{
				once = true;
				UIManager.NewHotInstallStoreBuffData += LoadBuffXMLandParse;
			}	
		}

		public class BuffInstanceEWAllergies : BuffInstance
		{
			public Sim mPlaguedSim;
			public AlarmHandle mZoneOutAlarm = AlarmHandle.kInvalidHandle;
			public AlarmHandle mScratchAlarm = AlarmHandle.kInvalidHandle;

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
				if (mZoneOutAlarm != AlarmHandle.kInvalidHandle)
				{
					bm.Actor.RemoveAlarm(mZoneOutAlarm);
					mZoneOutAlarm = AlarmHandle.kInvalidHandle;
				}
				if (mScratchAlarm != AlarmHandle.kInvalidHandle)
				{
					bm.Actor.RemoveAlarm(mScratchAlarm);
					mScratchAlarm = AlarmHandle.kInvalidHandle;
				}
			}

			public void DoZoneOut()
			{
				// TODO: Add Origin "From Allergies"
				mPlaguedSim.BuffManager.AddElement(BuffNames.Dazed, Origin.None);
				// TODO: Let's see if this existing interval works
				mZoneOutAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffPestilencePlague.MaxTimeBetweenCoughingFits
					- BuffPestilencePlague.MinTimeBetweenCoughingFits) + BuffPestilencePlague.MinTimeBetweenCoughingFits,
					TimeUnit.Minutes, DoZoneOut, "BuffEWAllergies: Time until next zone out", AlarmType.DeleteOnReset);
			}

			public void DoScratch()
			{
				mPlaguedSim.InteractionQueue.AddAfterCheckingForDuplicates(BuffCreepyCrawlies.Scratch
					.Singleton.CreateInstance(mPlaguedSim, mPlaguedSim,
					new InteractionPriority(InteractionPriorityLevel.Autonomous), isAutonomous: true,
					cancellableByPlayer: false));
				mPlaguedSim.RemoveAlarm(mScratchAlarm);
				mScratchAlarm = mPlaguedSim.AddAlarm(RandomUtil.GetFloat(BuffCreepyCrawlies.MaxTimeBetweenScratching -
					BuffCreepyCrawlies.MinTimeBetweenScratching) + BuffCreepyCrawlies.MinTimeBetweenScratching,
					TimeUnit.Minutes, DoScratch, "BuffEWAllergies: Time until next Scratch", AlarmType.DeleteOnReset);
			}
		}
		public BuffEWAllergies(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
			return true;
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			BuffInstanceEWAllergies buffInstance = bi as BuffInstanceEWAllergies;
			buffInstance.mPlaguedSim = bm.Actor;
			buffInstance.DoZoneOut();
			buffInstance.mScratchAlarm = buffInstance.mPlaguedSim.AddAlarm(BuffCreepyCrawlies.MinTimeBetweenScratching,
				TimeUnit.Minutes, buffInstance.DoScratch, "BuffEWAllergies: Time until next Scratch",
				AlarmType.DeleteOnReset);
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWAllergies(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}
	}

}