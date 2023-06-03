using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Controller;
using System.Collections.Generic;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;
using static Echoweaver.Sims3Game.PetFighting.Tunables;

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
					greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString("Echoweaver/PetFighting/EWFightPet:PetFightLotFail"));
					return false;
				}
                return true;
            }

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
			{
				return LocalizeString("Echoweaver/PetFighting/EWFightPet:TakeToVetCost", target.Name, kCostOfVetVisit);
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
				} else
                {
					SocialInteractionA greetPet = new SocialInteractionA.Definition("Let Sniff Hand", new string[0],
						null, false).CreateInstance(Target, Actor, new InteractionPriority(InteractionPriorityLevel.High),
						false, true) as SocialInteractionA;
					Actor.InteractionQueue.TryPushAsContinuation(this, greetPet);
					Actor.InteractionQueue.TryPushAsContinuation(greetPet, interactionInstance);
                }
				return true; 
			} else
				return false;
		}		
	}

	public class EWGoToVet : RabbitHoleInteraction<Sim, Hospital>, IOverrideGetSlaveInteractionName
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
		public static InteractionDefinition Singleton = new Definition();


		public override string GetSlaveInteractionName()
		{
			return "Be Taken to Vet";
		}

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString(EWTakePetToVetWounds.sLocalizeKey + name, parameters);
		}

		public override bool Run()
		{
			timeToGo = false;
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
			AddFollower(mPet);
			return base.Run();
		}

        public override bool BeforeEnteringRabbitHole()
        {
            // Get the dang pet into the rabbithole. Surprised this is not handled by following
            CarryingPetPosture carryingPet = Actor.Posture as CarryingPetPosture;
            if (carryingPet == null)
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
				if (Actor.FamilyFunds > kCostOfVetVisit)
				{
                    Actor.ShowTNSIfSelectable(LocalizeString("Echoweaver/PetFighting/EWFightPet:WoundsCuredVet",
                        mPet.Name, kCostOfVetVisit), StyledNotification.NotificationStyle.kGameMessagePositive);
                    Actor.ModifyFunds(-kCostOfVetVisit);
				}
				else if (!GameUtils.IsFutureWorld())
				{
					Actor.ShowTNSIfSelectable(LocalizeString("Echoweaver/PetFighting/EWFightPet:TakeToVetBill",
						kCostOfVetVisit), StyledNotification.NotificationStyle.kGameMessageNegative);
					Sim actor = Actor;
					actor.UnpaidBills += kCostOfVetVisit;
				}
				mPet.BuffManager.RemoveElement(BuffEWGraveWound.buffName);
				mPet.BuffManager.RemoveElement(BuffEWMinorWound.buffName);
				mPet.BuffManager.RemoveElement(BuffEWSeriousWound.buffName);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
				EventTracker.SendEvent(EventTypeId.kVisitedRabbitHole, mPet, Target);
			}
			timeToGo = true;
			return result;
		}

		public override bool AfterExitingRabbitHole()
		{
			// This seems awfully complicated. Do you need all this to update a
			// relationship and display the icon?
			Relationship relationship = Relationship.Get(Actor, mPet, createIfNone: true);
			LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
			relationship.LTR.UpdateLiking(kLTRBoostOfVetVisit);
			LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
			SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
							Actor, mPet, true, 0, currentLTR, currentLTR2);
			return base.AfterExitingRabbitHole();
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

			public override string GetInteractionName(Sim actor, RabbitHole target, InteractionObjectPair iop)
			{
				return LocalizeString("Echoweaver/PetFighting/EWFightPet:SeeVet");
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
				while (!Actor.WaitForExitReason(kWaitForExitReasonDefaultTime, ExitReason.Canceled) && !goToVetInst.timeToGo)
				{
				}
				return true;
			}
			return false;
		}
	}

}
