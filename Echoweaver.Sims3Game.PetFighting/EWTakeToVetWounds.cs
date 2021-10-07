using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Opportunities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System.Collections.Generic;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.UI.ObjectPicker;

namespace Echoweaver.Sims3Game.PetFighting
{
	public class VisitRabbitHoleWithPet : RabbitHoleInteraction<Sim, RabbitHole>,
		IOverrideGetSlaveInteractionName
	{
		public class Definition : InteractionDefinition<Sim, RabbitHole, VisitRabbitHoleWithPet>,
			IOpportunityInteractionDefinition, IOverridesVisualType
		{
			public CASAGSAvailabilityFlags mAGS;

			public Opportunity mOpportunity;

			public float TimeToWaitInside;

			public virtual Opportunity Opportunity => mOpportunity;

			public InteractionVisualTypes GetVisualType => InteractionVisualTypes.Opportunity;

			public Definition()
			{
				//IL_000d: Unknown result type (might be due to invalid IL or missing references)
				mAGS = CASAGSAvailabilityFlags.AllAnimalsMask;
			}

			public Definition(CASAGSAvailabilityFlags ags, Sim actor, Opportunity op, float duration)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0008: Unknown result type (might be due to invalid IL or missing references)
				mAGS = ags;
				mOpportunity = op;
				TimeToWaitInside = duration;
			}

			public override bool Test(Sim a, RabbitHole target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return TurnInAtRabbitHole.Definition.StaticTestForTurnInAtRabbitHole(a, target,
					ref greyedOutTooltipCallback, mOpportunity);
			}

			public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters,
				out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
			{
				Sim sim = parameters.Actor as Sim;
				if (sim.IsInGroupingSituation())
				{
					listObjs = null;
					headers = null;
					NumSelectableRows = 0;
				}
				else
				{
					NumSelectableRows = 1;
					PopulateSimPicker(ref parameters, out listObjs, out headers, sim.Household.Pets, includeActor: false);
				}
			}

			public override string GetInteractionName(Sim actor, RabbitHole target, InteractionObjectPair iop)
			{
				return mOpportunity.TargetInteractionName;
			}
		}

		public float mTimeToWaitInside;

		public Sim mPet;

		public static InteractionDefinition Singleton = new Definition();

		public override string GetSlaveInteractionName()
		{
			return GetInteractionName();
		}

		public override void Init(ref InteractionInstanceParameters parameters)
		{
			base.Init(ref parameters);
			Definition definition = parameters.InteractionDefinition as Definition;
			mTimeToWaitInside = definition.TimeToWaitInside;
		}

		public override void ConfigureInteraction()
		{
			base.ConfigureInteraction();
			TimedStage timedStage = new TimedStage(GetInteractionName(), mTimeToWaitInside,
				showCompletionTime: false, selectable: true, visibleProgress: true);
			base.Stages = new List<Stage>(new Stage[1] {
			timedStage
		});
			ActiveStage = timedStage;
		}

		public override bool Run()
		{
			mPet = (GetSelectedObject() as Sim);
			if (mPet == null || mPet.HasBeenDestroyed)
			{
				return false;
			}
			if (mPet.IsHorse)
			{
				List<Sim> list = new List<Sim>();
				list.Add(mPet);
				Route val = Actor.CreateRoute();
				Slot nearestRoutingSlot = Target.GetNearestRoutingSlot(mPet.Position);
				val.PlanToPointRadialRange(Target.GetPositionOfSlot(nearestRoutingSlot), 0f, 10f);
				if (!Actor.DoRouteWithFollowers(val, list))
				{
					return false;
				}
			}
			if (!mPet.IsHorse || Actor.Posture.Container != mPet)
			{
				AddFollower(mPet);
			}
			return base.Run();
		}

		public override bool InRabbitHole()
		{
			StartStages();
			bool result = DoLoop(ExitReason.Default);
			if (Actor.HasExitReason(ExitReason.StageComplete))
			{
				EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
			}
			return result;
		}
	}
}
