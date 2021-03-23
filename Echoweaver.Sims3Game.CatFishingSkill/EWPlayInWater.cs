using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.CatFishing
{
	public class EWCatPlayInWater : TerrainInteraction, IPondInteraction
	{
		public class Definition : InteractionDefinition<Sim, Terrain, EWCatPlayInWater>
		{
			public override bool Test(Sim a, Terrain target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.IsCat || a.IsKitten)
				{
					return PetManager.PetSkillFatigueTest(a, ref greyedOutTooltipCallback);
				}
				return false;
			}

			public override InteractionTestResult Test(ref InteractionInstanceParameters parameters,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (((int)parameters.Hit.mType == 8 && PondManager.ArePondsLiquid()) || (int)parameters.Hit.mType == 9)
				{
					return base.Test(ref parameters, ref greyedOutTooltipCallback);
				}
				return InteractionTestResult.Gen_BadTerrainType;
			}

			public override string GetInteractionName(Sim a, Terrain target, InteractionObjectPair interaction)
			{
				return Localization.LocalizeString("Echoweaver/Interactions:EWCatPlayInWater");
			}

		}

		[Tunable]
		[TunableComment("Description:  Max amount of time (in minutes) to play in the water")]
		public static float kMaxPlayTime = 30f;

		public static InteractionDefinition Singleton = new Definition();

		public bool TerrainIsWaterPond => (int)Hit.mType == 8;

		public override bool Run()
		{
			LotLocation val = default(LotLocation);
			ulong lotLocation = World.GetLotLocation(Hit.mPoint, ref val);
			Vector3 val2 = Hit.mPoint;
			if (!DrinkFromPondHelper.RouteToDrinkLocation(Hit.mPoint, Actor, Hit.mType, Hit.mId))
			{
				return false;
			}
			EWCatFishingSkill skill = Actor.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
			if (skill == null)
			{
				skill = Actor.SkillManager.AddElement(EWCatFishingSkill.SkillNameID) as EWCatFishingSkill;
			}
			if (skill == null)
			{
				Show(new Format("Error: Attempt to add EWFishingSkill to " + Actor.Name + " FAILED.",
					NotificationStyle.kDebugAlert));
				return false;
			}
			skill.StartSkillGain(EWCatFishingSkill.kEWFishingSkillGainRateNormal);
			EnterStateMachine("Puddle", "Enter", "x");
			BeginCommodityUpdates();
			AnimateSim("Loop Play");
			bool flag = DoLoop(ExitReason.Default, LoopDelegate, mCurrentStateMachine);

			EndCommodityUpdates(flag);
			AnimateSim("Exit");
			skill.StopSkillGain();
			return flag;
		}

		public void LoopDelegate(StateMachineClient smc, LoopData ld)
		{
			if (ld.mLifeTime > kMaxPlayTime)
			{
				Actor.AddExitReason(ExitReason.Finished);
			}
		}
	}
}
