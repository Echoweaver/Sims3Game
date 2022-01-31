using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWRecuperateCat : Buff
	{
		private const ulong kEWRecuperateCatGuid = 0x8B1D57AADCBD08B4;
		public static ulong StaticGuid
		{
			get
			{
				return kEWRecuperateCatGuid;
			}
		}

		public class BuffInstanceEWRecuperateCat : BuffInstance
		{
			public ReactionBroadcaster Irb;

			public VisualEffect mEffect;

			public BuffInstanceEWRecuperateCat()
			{
			}

			public BuffInstanceEWRecuperateCat(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				return new BuffInstanceEWRecuperateCat(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
			}

			public override void Dispose(BuffManager bm)
			{
				if (Irb != null)
				{
					Irb.Dispose();
					Irb = null;
				}
				if (mEffect != null)
				{
					mEffect.Stop();
					mEffect.Dispose();
					mEffect = null;
				}
				base.Dispose(bm);
			}

		}
		public BuffEWRecuperateCat(BuffData data) : base(data)
		{
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWRecuperateCat(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi,
			bool travelReaddition)
		{
			Sim actor = bm.Actor;
			BuffInstanceEWRecuperateCat buffInstance = bi as BuffInstanceEWRecuperateCat;
			buffInstance.mEffect = VisualEffect.Create(actor.OccultManager.GetSleepFXName());
			buffInstance.mEffect.ParentTo(actor, Sim.ContainmentSlots.Mouth);
			buffInstance.mEffect.Start();

			base.OnAddition(bm, bi, travelReaddition);

			bm.RemoveElement(BuffEWGraveWound.StaticGuid);
			bm.RemoveElement(BuffEWSeriousWound.StaticGuid);
			bm.RemoveElement(BuffEWMinorWound.StaticGuid);

			bm.Actor.Motives.SetMax(CommodityKind.Energy);
			bm.Actor.Motives.SetMax(CommodityKind.Hunger);
		}
	}
}
