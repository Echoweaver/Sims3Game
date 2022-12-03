using System;
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
using Sims3.Store.Objects;
using Sims3.UI;
using Sims3.UI.Controller;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;
using System.Collections.Generic;

namespace Echoweaver.Sims3Game.PetDisease
{
    // This collection of interlocking interactions seems clunky. Maybe I'll revisit someday.

    public class EWTakeToVetDisease : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWTakeToVetDisease>
        {

            public override bool Test(Sim a, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!a.IsHuman)
                    return false;
                if (!(target.IsCat || target.IsADogSpecies) || target.SimDescription.ChildOrBelow)
                    return false;
                if (!IsSick(target))
                    return false;
                if (GetRabbitHolesOfType(RabbitHoleType.Hospital).Count <= 0)
                {
                    // TODO: Localize "There is no hospital to treat this pet."
                    greyedOutTooltipCallback = CreateTooltipCallback("Localize - No Hospital");
                    return false;
                }
                if (a.LotCurrent != target.LotCurrent)
                {
                    // TODO: Localize.
                    greyedOutTooltipCallback = CreateTooltipCallback("Localize - Pet not on Actor's lot");
                    return false;
                }
                return true;
            }

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
            {
                return LocalizeString("TakeToVetCost", target.Name, kCostOfVetVisit);
            }

            public bool IsSick(Sim pet)
            {
                return pet.BuffManager.HasAnyElement(PetDiseaseManager.CurableDiseases);
            }
        }

        public static string sLocalizeKey = "Echoweaver/PetDisease:";

        [Tunable]
        public static int kCostOfVetVisit = 200;
        public static int kLTRBoostOfVetVisit = 20;

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
                    ChildUtils.SetPosturePrecondition(interactionInstance, CommodityKind.CarryingPet,
                        new CommodityKind[0]);
                    PickUpPet pickUp = PickUpPet.Singleton.CreateInstance(Target, Actor,
                        new InteractionPriority(InteractionPriorityLevel.High),
                        false, true) as PickUpPet;
                    Actor.InteractionQueue.TryPushAsContinuation(this, pickUp);
                    Actor.InteractionQueue.TryPushAsContinuation(pickUp, interactionInstance);
                }
                else
                {
                    SocialInteractionA greetPet = new SocialInteractionA.Definition("Let Sniff Hand",
                        new string[0], null, false).CreateInstance(Target, Actor,
                        new InteractionPriority(InteractionPriorityLevel.High), false, true) as SocialInteractionA;
                    Actor.InteractionQueue.TryPushAsContinuation(this, greetPet);
                    Actor.InteractionQueue.TryPushAsContinuation(greetPet, interactionInstance);
                }
                return true;
            }
            else
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
                // Shouldn't actually see this but I defined it anyway.
                return "Take Pet to Vet";
            }
        }

        public Sim mPet;
        public bool timeToGo;

        [Tunable]
        public static float kSimMinutesForVet = 120f;

        [Tunable]
        public static int kCostOfVet = EWTakeToVetDisease.kCostOfVetVisit;

        public static InteractionDefinition Singleton = new Definition();

        public static string LocalizeString(string name, params object[] parameters)
        {
            return EWTakeToVetDisease.LocalizeString(name, parameters);
        }

        public override string GetSlaveInteractionName()
        {
            // TODO: Localize "Be Taken to Vet"
            return "Localize - Be taken to vet";
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
        EWGoToHospitalPet goToHospital = null;

        public override bool BeforeEnteringRabbitHole()
        {
            // Get the dang pet into the rabbithole. Surprised this is not handled by following
            CarryingPetPosture carryingPet = Actor.Posture as CarryingPetPosture;
            if (carryingPet == null)
            {
                goToHospital = EWGoToHospitalPet.Singleton.CreateInstance(Target, mPet,
                    new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: false,
                    cancellableByPlayer: true) as EWGoToHospitalPet;
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
                    Actor.ShowTNSIfSelectable(LocalizeString("VetCureBill", kCostOfVet),
                        StyledNotification.NotificationStyle.kGameMessagePositive);
                    Sim actor = Actor;
                    actor.UnpaidBills += kCostOfVet;
                }
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetGermy.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetPneumonia.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetstilence.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWTummyTrouble.buffName);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, mPet, Target);
            }
            if (goToHospital != null)
            {
                goToHospital.timeToGo = true;
            }
            return result;
        }

        public override bool AfterExitingRabbitHole()
        {
            // This seems awfully complicated. Do you need all this to update a
            // relationship and display the icon?
            Relationship relationship = Relationship.Get(Actor, mPet, createIfNone: true);
            LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
            relationship.LTR.UpdateLiking(EWTakeToVetDisease.kLTRBoostOfVetVisit);
            LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
            SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
                            Actor, mPet, true, 0, currentLTR, currentLTR2);
            return base.AfterExitingRabbitHole();
        }
    }

}

