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

			public VisualEffect mLArmFx;

			public VisualEffect mRArmFx;

			public VisualEffect mLThighFx;

			public VisualEffect mRThighFx;
			//public VisualEffect[] mGlowFx = new VisualEffect[9];

			//public Slot[] mGlowFxSlots = new Slot[9] {
			//	Sim.FXJoints.Head,
			//	Sim.FXJoints.Spine2,
			//	Sim.FXJoints.LeftThigh,
			//	Sim.FXJoints.RightThigh,
			//	Sim.FXJoints.Pelvis,
			//	Sim.FXJoints.LeftUpperArm,
			//	Sim.FXJoints.RightUpperArm,
			//	Sim.FXJoints.LeftCalf,
			//	Sim.FXJoints.RightCalf
   //         };

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
				//for (int i = 0; i < 9; i++)
				//{
				//	if (mGlowFx[i] != null)
				//	{
				//		mGlowFx[i].Stop(VisualEffect.TransitionType.HardTransition);
				//		mGlowFx[i].Dispose();
				//		mGlowFx[i] = null;
				//	}
				//}
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
			//string text = "ep11BuffHealthyGlowLrg_main";
			//for (int i = 0; i < 9; i++)
			//{
			//	if (i > 4)
			//	{
			//		text = "ep11BuffHealthyGlow_main";
			//	}
			//	buffInstance.mGlowFx[i] = VisualEffect.Create(text);
			//	buffInstance.mGlowFx[i].ParentTo(bm.Actor, buffInstance.mGlowFxSlots[i]);
			//	buffInstance.mGlowFx[i].SetEffectColorScale(0.4f, 0f, 0f);    // RGB for dark red
			//	buffInstance.mGlowFx[i].Start();
			//}

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
