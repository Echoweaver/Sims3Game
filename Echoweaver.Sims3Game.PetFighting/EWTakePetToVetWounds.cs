using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Controller;
using System.Collections.Generic;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{
	public class EWTakePetToVetWounds : SocialInteraction
	{
		public class Definition : InteractionDefinition<Sim, Sim, EWTakePetToVetWounds>
		{

			public override bool Test(Sim a, Sim target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
				if (!a.IsHuman)
					return false;
				if (!target.IsPet)
					return false;
                if (!PetHasWound(target))
                    return false;
				if (GetRabbitHolesOfType(RabbitHoleType.Hospital).Count <= 0)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("There is no hospital to treat this pet.");
					return false;
				}
				if (a.LotCurrent != target.LotCurrent)
				{
					// TODO: Localize
					greyedOutTooltipCallback = CreateTooltipCallback("Must be on same lot as pet");
					return false;
				}
                return true;
            }

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
			{
				return "Take " + target.Name + " to vet (§" + kCostOfVetVisit + ")";
//				return LocalizeString("InteractionName", kCostOfVetVisit);
			}

			private bool PetHasWound(Sim s)
            {
				if (!s.IsPet)
					return false;
				if (s.BuffManager.HasAnyElement(new BuffNames[] {BuffEWGraveWound.buffName,
					BuffEWSeriousWound.buffName, BuffEWMinorWound.buffName }))
					return true;
				else return false;
            }
		}

		public static string sLocalizeKey = "Echoweaver/PetFighting/TakeToVet:";

		[Tunable]
		public static int kCostOfVetVisit = 200;
		public static int kLTRBoostOfVetVisit = 100;

		public static InteractionDefinition Singleton = new Definition();

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(sLocalizeKey + name, parameters);
		}

        public override bool Run()
        {
			Hospital hospital = RabbitHole.GetClosestRabbitHoleOfType(RabbitHoleType.Hospital,
				Target.Position) as Hospital;

			if (hospital != null)
			{
				Actor.RouteTurnToFace(Target.Position);
				EWGoToVet interactionInstance = EWGoToVet.Singleton.CreateInstance(hospital, Actor,
					new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: false,
					cancellableByPlayer: true) as EWGoToVet;
				interactionInstance.mPet = Target;

				if ((Target.IsCat || Target.IsLittleDog))
                {
					ChildUtils.SetPosturePrecondition(interactionInstance, CommodityKind.CarryingPet, new CommodityKind[0]);
					PickUpPet pickUp = PickUpPet.Singleton.CreateInstance(Target, Actor, new InteractionPriority(InteractionPriorityLevel.High),
						false, true) as PickUpPet;
					Actor.InteractionQueue.TryPushAsContinuation(this, pickUp);
					Actor.InteractionQueue.TryPushAsContinuation(pickUp, interactionInstance);
				}

				return true; 
			} else
				return false;
		}		
	}

	public class EWGoToVet : RabbitHoleInteraction<Sim, Hospital>
	{
		public class Definition : InteractionDefinition<Sim, Hospital, EWGoToVet>
		{
			public override bool Test(Sim a, Hospital target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}

			public override string GetInteractionName(Sim actor, Hospital target, InteractionObjectPair iop)
			{
				return "Take Pet to Vet";
			}
		}

		public Sim mPet;
		public bool timeToGo;

		[Tunable]
		public static float kSimMinutesForVet = 120f;

		[Tunable]
		public static int kCostOfVet = EWTakePetToVetWounds.kCostOfVetVisit;

		public static InteractionDefinition Singleton = new Definition();

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(EWTakePetToVetWounds.sLocalizeKey + name, parameters);
		}

		public override bool Run()
		{
			timeToGo = false;
			StyledNotification.Show(new StyledNotification.Format("In GoToVet",
				StyledNotification.NotificationStyle.kGameMessagePositive));
			Target.RouteOutside(Actor, new List<Sim>());
			TimedStage timedStage = new TimedStage(GetInteractionName(), kSimMinutesForVet,
				showCompletionTime: false, selectable: true, visibleProgress: true);
			Stages = new List<Stage>(new Stage[1] {
				timedStage
			});
			ActiveStage = timedStage;
			if (mPet == null || mPet.HasBeenDestroyed)
			{
				return false;
			}
			if (!mPet.IsHorse || Actor.Posture.Container != mPet)
			{
				AddFollower(mPet);
			}
			return base.Run();

		}

		public override bool BeforeEnteringRabbitHole()
		{
			// Get the dang pet into the rabbithole. Surprised this is not handled by following
			if (Actor.Posture.Container != mPet)
			{
				EWGoToHospitalPet goToHospital = EWGoToHospitalPet.Singleton.CreateInstance(Target, mPet,
					new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: false,
					cancellableByPlayer: true) as EWGoToHospitalPet;
				goToHospital.goToVetInst = this;
				mPet.InteractionQueue.Add(goToHospital);
			}

			return base.BeforeEnteringRabbitHole();
		}

		public override bool InRabbitHole()
		{
			StartStages();
			bool result = DoLoop(ExitReason.Default);
			if (Actor.HasExitReason(ExitReason.StageComplete))
			{
				if (Actor.FamilyFunds > kCostOfVet)
				{
					Actor.ModifyFunds(-kCostOfVet);
				}
				else if (!GameUtils.IsFutureWorld())
				{
					// TODO: Add correct Localization
					Actor.ShowTNSIfSelectable("You will owe the vet §" + kCostOfVet + "for this visit",
						StyledNotification.NotificationStyle.kGameMessagePositive);
					Sim actor = Actor;
					actor.UnpaidBills += kCostOfVet;
				}
				// TODO: Add relationship boost
				EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
				EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, mPet, Target);
				// This seems awfully complicated. Do you need all this to update a
				// relationship and display the icon?
				Relationship relationship = Relationship.Get(Actor, mPet, createIfNone: true);
				LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
				relationship.LTR.UpdateLiking(EWTakePetToVetWounds.kLTRBoostOfVetVisit);
				LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
				SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
								Actor, mPet, true, 0, currentLTR, currentLTR2);
			}
			timeToGo = true;
			return result;
		}
	}

	public class EWGoToHospitalPet : RabbitHoleInteraction<Sim, RabbitHole>
	{
		public class Definition : InteractionDefinition<Sim, RabbitHole, EWGoToHospitalPet>
		{
			public override bool Test(Sim a, RabbitHole target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}
		}

		public EWGoToVet goToVetInst;

		public static InteractionDefinition Singleton = new Definition();

		public override bool Run()
		{
			return base.Run();
		}

		public override bool InRabbitHole()
		{
			if (goToVetInst != null)
			{
				goToVetInst.AddFollower(Actor);
				while (!Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Canceled) && !goToVetInst.timeToGo)
				{
				}
				Actor.BuffManager.RemoveElement(BuffEWGraveWound.buffName);
				Actor.BuffManager.RemoveElement(BuffEWSeriousWound.buffName);
				Actor.BuffManager.RemoveElement(BuffEWMinorWound.buffName);

				return true;
			}
			return false;
		}
	}

}
