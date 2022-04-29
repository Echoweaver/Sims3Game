using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

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
			public VisualEffect mHeadFx;
			public VisualEffect mHeadFx2;

			public VisualEffect mLArmFx;
			public VisualEffect mLArmFx2;

			public VisualEffect mRArmFx;
			public VisualEffect mRArmFx2;

			public VisualEffect mLThighFx;
			public VisualEffect mLThighFx2;

			public VisualEffect mRThighFx;
			public VisualEffect mRThighFx2;

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

			public void StartFx(Sim owner)
			{
				Vector3 fxColor = new Vector3(1f, 0f, 0f); // RGB for red
				mHeadFx = VisualEffect.Create("ep1SoakedDripsHead");
				mHeadFx.ParentTo(owner, Sim.FXJoints.Head);
                mHeadFx.SetEffectColorScale(fxColor);
                mHeadFx.Start();
				mHeadFx2 = VisualEffect.Create("ep1SoakedDripsHead");
				mHeadFx2.ParentTo(owner, Sim.FXJoints.Head);
				mHeadFx2.SetEffectColorScale(fxColor);
				mHeadFx2.Start();
				mLArmFx = VisualEffect.Create("ep1SoakedDripsArm");
				mLArmFx.ParentTo(owner, Sim.FXJoints.LeftShoulder);
				mLArmFx.SetEffectColorScale(fxColor);    
				mLArmFx.Start();
				mLArmFx2 = VisualEffect.Create("ep1SoakedDripsArm");
				mLArmFx2.ParentTo(owner, Sim.FXJoints.LeftShoulder);
				mLArmFx2.SetEffectColorScale(fxColor);
				mLArmFx2.Start();
				mRArmFx = VisualEffect.Create("ep1SoakedDripsArm");
				mRArmFx.ParentTo(owner, Sim.FXJoints.RightShoulderblade);
				mRArmFx.SetEffectColorScale(fxColor);   
				mRArmFx.Start();
				mRArmFx2 = VisualEffect.Create("ep1SoakedDripsArm");
				mRArmFx2.ParentTo(owner, Sim.FXJoints.RightShoulderblade);
				mRArmFx2.SetEffectColorScale(fxColor);
				mRArmFx2.Start();
				mLThighFx = VisualEffect.Create("ep1SoakedDripsThigh");
				mLThighFx.ParentTo(owner, Sim.FXJoints.LeftSideThigh);
				mLThighFx.SetEffectColorScale(fxColor);    
				mLThighFx.Start();
				mLThighFx2 = VisualEffect.Create("ep1SoakedDripsThigh");
				mLThighFx2.ParentTo(owner, Sim.FXJoints.LeftSideThigh);
				mLThighFx2.SetEffectColorScale(fxColor);
				mLThighFx2.Start();
				mRThighFx = VisualEffect.Create("ep1SoakedDripsThigh");
				mRThighFx.ParentTo(owner, Sim.FXJoints.RightSideThigh);
				mRThighFx.SetEffectColorScale(fxColor);   
				mRThighFx.Start();
				mRThighFx2 = VisualEffect.Create("ep1SoakedDripsThigh");
				mRThighFx2.ParentTo(owner, Sim.FXJoints.RightSideThigh);
				mRThighFx2.SetEffectColorScale(fxColor);
				mRThighFx2.Start();
			}

			public void StopFx()
			{
				if (mHeadFx != null)
				{
					mHeadFx.Stop(VisualEffect.TransitionType.HardTransition);
					mHeadFx.Dispose();
					mHeadFx = null;
				}
				if (mHeadFx2 != null)
				{
					mHeadFx2.Stop(VisualEffect.TransitionType.HardTransition);
					mHeadFx2.Dispose();
					mHeadFx2 = null;
				}
				if (mLArmFx != null)
				{
					mLArmFx.Stop(VisualEffect.TransitionType.HardTransition);
					mLArmFx.Dispose();
					mLArmFx = null;
				}
				if (mLArmFx2 != null)
				{
					mLArmFx2.Stop(VisualEffect.TransitionType.HardTransition);
					mLArmFx2.Dispose();
					mLArmFx2 = null;
				}
				if (mRArmFx != null)
				{
					mRArmFx.Stop(VisualEffect.TransitionType.HardTransition);
					mRArmFx.Dispose();
					mRArmFx = null;
				}
				if (mRArmFx2 != null)
				{
					mRArmFx2.Stop(VisualEffect.TransitionType.HardTransition);
					mRArmFx2.Dispose();
					mRArmFx2 = null;
				}
				if (mLThighFx != null)
				{
					mLThighFx.Stop(VisualEffect.TransitionType.HardTransition);
					mLThighFx.Dispose();
					mLThighFx = null;
				}
				if (mLThighFx2 != null)
				{
					mLThighFx2.Stop(VisualEffect.TransitionType.HardTransition);
					mLThighFx2.Dispose();
					mLThighFx2 = null;
				}
				if (mRThighFx != null)
				{
					mRThighFx.Stop(VisualEffect.TransitionType.HardTransition);
					mRThighFx.Dispose();
					mRThighFx = null;
				}
				if (mRThighFx2 != null)
				{
					mRThighFx2.Stop(VisualEffect.TransitionType.HardTransition);
					mRThighFx2.Dispose();
					mRThighFx2 = null;
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

			buffInstance.StartFx(actor);
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
