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
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Controller;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;
using System.Collections.Generic;
using static Echoweaver.Sims3Game.PetDisease.Loader;

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
                    greyedOutTooltipCallback = CreateTooltipCallback(LocalizeStr("NoHospital"));
                    return false;
                }
                if (a.LotCurrent != target.LotCurrent)
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(LocalizeStr("PetNotOnLot"));
                    return false;
                }
                return true;
            }

            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
            {
                return LocalizeStr("TakeToVetCost", PetDiseaseManager.kPetCureCost);
            }

            public bool IsSick(Sim pet)
            {
                return pet.BuffManager.HasAnyElement(PetDiseaseManager.CurableDiseases);
            }
        }

        public static int kLTRBoostOfVetVisit = 20;

        public static InteractionDefinition Singleton = new Definition();

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

        public static InteractionDefinition Singleton = new Definition();

        public static string LocalizeString(string name, params object[] parameters)
        {
            return LocalizeStr(name, parameters);
        }

        public override string GetSlaveInteractionName()
        {
            return LocalizeStr("BeTakenToVet");
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
                if (Actor.FamilyFunds > PetDiseaseManager.kPetCureCost)
                {
                    Actor.ShowTNSIfSelectable(LocalizeString("VetCureBill", mPet.Name, PetDiseaseManager
                        .kPetCureCost), StyledNotification.NotificationStyle.kGameMessagePositive);
                    Actor.ModifyFunds(-PetDiseaseManager.kPetCureCost);
                }
                else if (!GameUtils.IsFutureWorld())
                {
                    Actor.ShowTNSIfSelectable(LocalizeString("NoMoneyVetCure", mPet.Name, PetDiseaseManager
                        .kPetCureCost), StyledNotification.NotificationStyle.kGameMessageNegative);
                    Actor.UnpaidBills += PetDiseaseManager.kPetCureCost;
                }
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetGermy.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetPneumonia.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWPetstilence.buffName);
                mPet.BuffManager.RemoveElement(Buffs.BuffEWTummyTrouble.buffName);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHole, mPet, Target);
            }
            if (goToHospital != null)
            {
                goToHospital.timeToGo = true;
            }
            return result;
        }

        public override bool AfterExitingRabbitHole()
        {
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

