using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using static Sims3.SimIFace.VisualEffect;

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
			//public Sim mSim;

			public VisualEffect[] mGlowFx = new VisualEffect[9];

			public Slot[] mGlowFxSlots = new Slot[9] {
				Sim.FXJoints.Head,
				Sim.FXJoints.Spine2,
				Sim.FXJoints.LeftThigh,
				Sim.FXJoints.RightThigh,
				Sim.FXJoints.Pelvis,
				Sim.FXJoints.LeftUpperArm,
				Sim.FXJoints.RightUpperArm,
				Sim.FXJoints.LeftCalf,
				Sim.FXJoints.RightCalf
            };

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
				for (int i = 0; i < 9; i++)
				{
					if (mGlowFx[i] != null)
					{
						mGlowFx[i].Stop(TransitionType.HardTransition);
						mGlowFx[i].Dispose();
						mGlowFx[i] = null;
					}
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
			// TODO: Determine if we need this code for users without ITF
			//buffInstance.mEffect = VisualEffect.Create(OccultUnicorn.GetUnicornSocialVfxName(actor,
			//	isFriendly: false, isToTarget: false));
			//buffInstance.mEffect.SetEffectColorScale(0.4f, 0f, 0f);    // RGB for dark red
			//buffInstance.mEffect.ParentTo(actor, Sim.FXJoints.Spine2);
			//buffInstance.mEffect.Start();
			string text = "ep11BuffHealthyGlowLrg_main";
			for (int i = 0; i < 9; i++)
			{
				if (i > 4)
				{
					text = "ep11BuffHealthyGlow_main";
				}
				buffInstance.mGlowFx[i] = VisualEffect.Create(text);
				buffInstance.mGlowFx[i].ParentTo(bm.Actor, buffInstance.mGlowFxSlots[i]);
				buffInstance.mGlowFx[i].SetEffectColorScale(0.4f, 0f, 0f);    // RGB for dark red
				buffInstance.mGlowFx[i].Start();
			}
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
