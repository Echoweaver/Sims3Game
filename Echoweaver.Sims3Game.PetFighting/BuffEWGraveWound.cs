using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWGraveWound : Buff
	{
		private const ulong kEWGraveWoundGuid = 0x384B537AE0B8F97A;
		public static ulong StaticGuid
		{
			get
			{
				return kEWGraveWoundGuid;

			}
		}
		public static BuffNames buffName = (BuffNames)kEWGraveWoundGuid;

		public class BuffInstanceEWGraveWound : BuffInstance
		{
			public ReactionBroadcaster Irb;

			public VisualEffect mEffect;

			public BuffInstanceEWGraveWound()
			{
			}

			public BuffInstanceEWGraveWound(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				return new BuffInstanceEWGraveWound(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
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


		public static float kGraveWoundHungerDecayMultiplier = 3.0f;
		public static float kGraveWoundEnergyDecayMultiplier = 3.0f;

		public BuffEWGraveWound(BuffData data) : base(data)
		{
		}

		public override BuffInstance CreateBuffInstance()
		{
			return new BuffInstanceEWGraveWound(this, BuffGuid, EffectValue, TimeoutSimMinutes);
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi,
			bool travelReaddition)
		{
			Sim actor = bm.Actor;
			BuffInstanceEWGraveWound buffInstance = bi as BuffInstanceEWGraveWound;
			buffInstance.mEffect = VisualEffect.Create(OccultUnicorn.GetUnicornSocialVfxName(actor,
				isFriendly: false, isToTarget: false));
			buffInstance.mEffect.SetEffectColorScale(0.4f, 0f, 0f);    // RGB for dark red
			buffInstance.mEffect.ParentTo(actor, Sim.FXJoints.Spine2);
			buffInstance.mEffect.Start();
			base.OnAddition(bm, bi, travelReaddition);

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger,
				kGraveWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy,
				kGraveWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger,
				kGraveWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy,
				kGraveWoundEnergyDecayMultiplier);

			base.OnRemoval(bm, bi);
		}

	}
}
