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
				for (int i = 0; i < 9; i++)
				{
					if (mGlowFx[i] != null)
					{
						mGlowFx[i].Stop(VisualEffect.TransitionType.HardTransition);
						mGlowFx[i].Dispose();
						mGlowFx[i] = null;
					}
				}
				base.Dispose(bm);
			}

		}

		public static float kSeriousWoundHungerDecayMultiplier = 2.0f;
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
			string text = "ep11BuffHealthyGlowLrg_main";
			for (int i = 0; i < 9; i++)
			{
				if (i > 4)
				{
					text = "ep11BuffHealthyGlow_main";
				}
				buffInstance.mGlowFx[i] = VisualEffect.Create(text);
				buffInstance.mGlowFx[i].ParentTo(bm.Actor, buffInstance.mGlowFxSlots[i]);
				buffInstance.mGlowFx[i].SetEffectColorScale(0.35f, 0.05f, 0f);    
				buffInstance.mGlowFx[i].Start();
			}

			base.OnAddition(bm, bi, travelReaddition);

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger, kSeriousWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy, kSeriousWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			base.OnRemoval(bm, bi);

			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger, kSeriousWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy, kSeriousWoundEnergyDecayMultiplier);
		}
	}
}
