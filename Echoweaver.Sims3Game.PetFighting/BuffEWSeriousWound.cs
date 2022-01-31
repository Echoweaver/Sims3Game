using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWSeriousWound : Buff
	{
		private const ulong kEWSeriousWoundGuid = 0xAE4D28F1BCEC603D;

		public static ulong StaticGuid
		{
			get
			{
				return kEWSeriousWoundGuid;
			}
		}
		public static BuffNames buffName = (BuffNames)kEWSeriousWoundGuid;

		public class BuffInstanceEWSeriousWound : BuffInstance
		{
			public ReactionBroadcaster Irb;

			public VisualEffect mEffect;

			public BuffInstanceEWSeriousWound()
			{
			}

			public BuffInstanceEWSeriousWound(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				return new BuffInstanceEWSeriousWound(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
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

		public static float kSeriusWoundHungerDecayMultiplier = 2.0f;
		public static float kSeriousWoundEnergyDecayMultiplier = 2.0f;

		public BuffEWSeriousWound(BuffData data) : base(data)
		{
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWSeriousWound(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			Sim actor = bm.Actor;
			BuffInstanceEWSeriousWound buffInstance = bi as BuffInstanceEWSeriousWound;
			buffInstance.mEffect = VisualEffect.Create(OccultUnicorn.GetUnicornSocialVfxName(actor,
				isFriendly: false, isToTarget: false));
			buffInstance.mEffect.SetEffectColorScale(0.35f, 0.12f, 0f);  // RGB for amber color
			buffInstance.mEffect.ParentTo(actor, Sim.FXJoints.Spine2);
			buffInstance.mEffect.Start();
			base.OnAddition(bm, bi, travelReaddition);

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger, kSeriusWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy, kSeriousWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			base.OnRemoval(bm, bi);

			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger, kSeriusWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy, kSeriousWoundEnergyDecayMultiplier);
		}
	}
}
