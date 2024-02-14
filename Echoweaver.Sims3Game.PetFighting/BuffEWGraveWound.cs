using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
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
				string effect_name;
				if (owner.IsCat || owner.IsLittleDog)
				{
					effect_name = "ep5doglittleeatvomit";

                } else
				{
                    effect_name = "ep5dogeatvomit";
                }
                Vector3 fxColor = new Vector3(1f, 0f, 0f); // RGB for red
				mHeadFx = VisualEffect.Create(effect_name);
				mHeadFx.ParentTo(owner, Sim.FXJoints.Head);
				mHeadFx.SetEffectColorScale(fxColor);
				mHeadFx.Start();
				mHeadFx2 = VisualEffect.Create(effect_name);
				mHeadFx2.ParentTo(owner, Sim.FXJoints.Head);
				mHeadFx2.SetEffectColorScale(fxColor);
				mHeadFx2.Start();
				mRArmFx = VisualEffect.Create(effect_name);
				mRArmFx.ParentTo(owner, Sim.FXJoints.RightUpperArm);
				mRArmFx.SetEffectColorScale(fxColor);   
				mRArmFx.Start();
				mRArmFx2 = VisualEffect.Create(effect_name);
				mRArmFx2.ParentTo(owner, Sim.FXJoints.RightUpperArm);
				mRArmFx2.SetEffectColorScale(fxColor);
				mRArmFx2.Start();
				mLArmFx = VisualEffect.Create(effect_name);
                mLArmFx.ParentTo(owner, Sim.FXJoints.LeftUpperArm);
                mLArmFx.SetEffectColorScale(fxColor);
                mLArmFx.Start();
                mLArmFx2 = VisualEffect.Create(effect_name);
                mLArmFx2.ParentTo(owner, Sim.FXJoints.LeftUpperArm);
                mLArmFx2.SetEffectColorScale(fxColor);
                mLArmFx2.Start();
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

			}

			public override void Dispose(BuffManager bm)
			{
				StopFx();
				base.Dispose(bm);
			}

		}

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
            buffInstance.TimeoutCount = Tunables.kGraveWoundDuration;

            buffInstance.StartFx(actor);
			base.OnAddition(bm, bi, travelReaddition);

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger,
				Tunables.kGraveWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy,
                Tunables.kGraveWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger,
                Tunables.kGraveWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy,
                Tunables.kGraveWoundEnergyDecayMultiplier);

			base.OnRemoval(bm, bi);
		}

	}
}
