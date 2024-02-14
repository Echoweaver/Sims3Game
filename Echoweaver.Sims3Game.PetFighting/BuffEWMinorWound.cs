using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWMinorWound : Buff
	{
		private const ulong kEWMinorWoundGuid = 0x3BE0F368D4653A9E; 
		public static ulong StaticGuid
		{
			get
			{
				return kEWMinorWoundGuid;

			}
		}
		public static BuffNames buffName = (BuffNames)kEWMinorWoundGuid;

		public class BuffInstanceEWMinorWound : BuffInstance
		{
			public ReactionBroadcaster Irb;

			public VisualEffect mEffect;

			public BuffInstanceEWMinorWound()
			{
			}

			public BuffInstanceEWMinorWound(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				return new BuffInstanceEWMinorWound(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
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

		public BuffEWMinorWound(BuffData data) : base(data)
		{
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWMinorWound(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			Sim actor = bm.Actor;
			BuffInstanceEWMinorWound buffInstance = bi as BuffInstanceEWMinorWound;
            buffInstance.TimeoutCount = Tunables.kMinorWoundDuration;
            base.OnAddition(bm, bi, travelReaddition);

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger, Tunables.kMinorWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy, Tunables.kMinorWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			base.OnRemoval(bm, bi);

			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger, Tunables.kMinorWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy, Tunables.kMinorWoundEnergyDecayMultiplier);
		}
	}
}
