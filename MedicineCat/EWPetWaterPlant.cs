using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using static Sims3.Gameplay.Objects.Gardening.Plant;

namespace Echoweaver.Sims3Game.MedicineCat
{
    public class EWPetWaterPlant : WaterPlant
    {
		public new class Definition : WaterPlant.Definition
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (!a.IsCat || !WaterTestDisregardGardeningSkill(target, a))
				{
					return false;
				}
				if (!a.SkillManager.HasElement(EWHerbLoreSkill.SkillNameID))
                {
					return false;
                }
				// TODO: What level should the skill be? Tunable?
				return (a.SkillManager.GetSkill<EWHerbLoreSkill>(EWHerbLoreSkill.SkillNameID).SkillLevel >= 2);
			}
		}

		public new static InteractionDefinition Singleton = new Definition();

		public EWHerbLoreSkill herbSkill;

		public override bool Run()
		{
			bool flag = false;
			// This will be route to nearest water and back.
			if (Target.RouteSimToMeAndCheckInUse(Actor) && WaterTestDisregardGardeningSkill(Target, Actor))
			{
				TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				flag = DoWater();
			}

			if (IsChainingPermitted(flag))
			{
				IgnorePlants.Add(Target);
				PushNextInteractionInChain(Singleton, WaterTestDisregardGardeningSkill, Target.LotCurrent);
			}
			return flag;
		}

		public new bool DoWater()
		{
			if (herbSkill == null)
            {
				herbSkill = Actor.SkillManager.GetSkill<EWHerbLoreSkill>(EWHerbLoreSkill.SkillNameID);
            }

			if (herbSkill == null)
            {
				return false;
            }

			StandardEntry();
			BeginCommodityUpdates();
			// Probably just a quick animation, maybe dropping a toy prop?
			Actor.PlaySoloAnimation("a_idle_stand_sniffAround_x");
			//mCurrentStateMachine = Target.GetStateMachine(Actor, out Soil dummyIk);
			EndCommodityUpdates(succeeded: false);
			StandardExit();
			//AddOneShotScriptEventHandler(1001u, (SacsEventHandler)(object)new SacsEventHandler(StartWateringSound));
			//AddOneShotScriptEventHandler(1002u, (SacsEventHandler)(object)new SacsEventHandler(StopWateringSound));
			//mCurrentStateMachine.RequestState("x", "Loop Water");
			Target.WaterLevel = 100f;
			EventTracker.SendEvent(EventTypeId.kWateredPlant, Actor, Target);
			EventTracker.SendEvent(EventTypeId.kGardened, Actor);
			EndCommodityUpdates(succeeded: true);
			StandardExit();
			return true;
		}
	}

}
