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
			public VisualEffect mHeadFx;

			public VisualEffect mLArmFx;

			public VisualEffect mRArmFx;

			public VisualEffect mLThighFx;

			public VisualEffect mRThighFx;
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

			public void StartFx(Sim owner)
			{
				Vector3 fxColor = new Vector3(1f, 0.2f, 0f); // RGB for red-brown
				mHeadFx = VisualEffect.Create("ep1SoakedDripsHead");
				mHeadFx.ParentTo(owner, Sim.FXJoints.Head);
				mHeadFx.SetEffectColorScale(fxColor);
				mHeadFx.Start();
				mLArmFx = VisualEffect.Create("ep1SoakedDripsArm");
				mLArmFx.ParentTo(owner, Sim.FXJoints.LeftShoulder);
				mLArmFx.SetEffectColorScale(fxColor);
				mLArmFx.Start();
				mRArmFx = VisualEffect.Create("ep1SoakedDripsArm");
				mRArmFx.ParentTo(owner, Sim.FXJoints.RightShoulderblade);
				mRArmFx.SetEffectColorScale(fxColor);
				mRArmFx.Start();
				mLThighFx = VisualEffect.Create("ep1SoakedDripsThigh");
				mLThighFx.ParentTo(owner, Sim.FXJoints.LeftSideThigh);
				mLThighFx.SetEffectColorScale(fxColor);
				mLThighFx.Start();
				mRThighFx = VisualEffect.Create("ep1SoakedDripsThigh");
				mRThighFx.ParentTo(owner, Sim.FXJoints.RightSideThigh);
				mRThighFx.SetEffectColorScale(fxColor);
				mRThighFx.Start();
			}

			public void StopFx()
			{
				if (mHeadFx != null)
				{
					mHeadFx.Stop(VisualEffect.TransitionType.HardTransition);
					mHeadFx.Dispose();
					mHeadFx = null;
				}
				if (mLArmFx != null)
				{
					mLArmFx.Stop(VisualEffect.TransitionType.HardTransition);
					mLArmFx.Dispose();
					mLArmFx = null;
				}
				if (mRArmFx != null)
				{
					mRArmFx.Stop(VisualEffect.TransitionType.HardTransition);
					mRArmFx.Dispose();
					mRArmFx = null;
				}
				if (mLThighFx != null)
				{
					mLThighFx.Stop(VisualEffect.TransitionType.HardTransition);
					mLThighFx.Dispose();
					mLThighFx = null;
				}
				if (mRThighFx != null)
				{
					mRThighFx.Stop(VisualEffect.TransitionType.HardTransition);
					mRThighFx.Dispose();
					mRThighFx = null;
				}
			}

			public override void Dispose(BuffManager bm)
			{
				StopFx();

				if (mEffect != null)
				{
					mEffect.Stop();
					mEffect.Dispose();
					mEffect = null;
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

			buffInstance.StartFx(actor);
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
