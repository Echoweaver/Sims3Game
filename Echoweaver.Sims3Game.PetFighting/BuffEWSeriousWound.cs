using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
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
                string effect_name;
                if (owner.IsCat || owner.IsLittleDog)
                {
                    effect_name = "ep5doglittleeatvomit";

                }
                else
                {
                    effect_name = "ep5dogeatvomit";
                }
				Vector3 fxColor = new Vector3(1f, 0.2f, 0f); // RGB for red-brown		

				mHeadFx = VisualEffect.Create(effect_name);
				int location = RandomUtil.GetInt(1, 3);
				if (location == 1)
				{
                    mHeadFx.ParentTo(owner, Sim.FXJoints.Head);
                    mHeadFx.SetEffectColorScale(fxColor);
                    mHeadFx.Start();
                } else if (location == 2)
				{
                    mLArmFx = VisualEffect.Create(effect_name);
                    mLArmFx.ParentTo(owner, Sim.FXJoints.LeftUpperArm);
                    mLArmFx.SetEffectColorScale(fxColor);
                    mLArmFx.Start();
                } else
				{
					mRArmFx = VisualEffect.Create(effect_name);
					mRArmFx.ParentTo(owner, Sim.FXJoints.RightUpperArm);
					mRArmFx.SetEffectColorScale(fxColor);
					mRArmFx.Start();
				}
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
			}

			public override void Dispose(BuffManager bm)
			{
				StopFx();
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

            buffInstance.TimeoutCount = Tunables.kSeriousWoundDuration;
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
