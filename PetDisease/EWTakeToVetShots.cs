using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;
using static Sims3.UI.ObjectPicker;
using static Echoweaver.Sims3Game.PetDisease.Loader;

namespace Echoweaver.Sims3Game.PetDisease
{
    public class EWVaccinatePet : RabbitHoleInteraction<Sim, Hospital>, IOverrideGetSlaveInteractionName
    {
        public class Definition : InteractionDefinition<Sim, Hospital, EWVaccinatePet>
        {
            public float TimeToWaitInside;

            public InteractionVisualTypes GetVisualType => InteractionVisualTypes.Opportunity;

            public override bool Test(Sim a, Hospital target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (a.IsHuman && a.Household.Pets.Count > 0)
                {
                    if (a.FamilyFunds < PetDiseaseManager.kPetVaccineCost)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(LocalizeStr("NoMoneyForVaccine"));
                        return false;
                    }
                    if (GetUnvaccinatedPets(a.Household).Count == 0)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(LocalizeStr("NoPetsNeedVaccine"));
                        return false;
                    }
                    return true;
                }
                else return false;
            }

            public override string GetInteractionName(Sim actor, Hospital target, InteractionObjectPair iop)
            {
                return LocalizeStr("VaccinatePet", PetDiseaseManager.kPetVaccineCost);
            }

            public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters,
                out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
            {
                Sim sim = parameters.Actor as Sim;
                NumSelectableRows = -1; 
                PopulateSimPicker(ref parameters, out listObjs, out headers, GetUnvaccinatedPets(sim.Household),
                    includeActor: false);
            }
        }

        [Tunable]
        public static int kSimMinutesForPetVaccine = 60;

        public List<Sim> mPetsToVaccinate = new List<Sim>();

        public static InteractionDefinition Singleton = new Definition();

        public override string GetSlaveInteractionName()
        {
            return LocalizeStr("BeTakenToVet");
        }

        public static List<Sim> GetUnvaccinatedPets(Household household)
        {
            List<Sim> list = new List<Sim>();
            foreach (Sim s in household.Pets)
            {
                if (!s.IsHorse && s.SimDescription.AdultOrAbove && !PetDiseaseManager.CheckForVaccination(s))
                {
                    Lazy.Add(ref list, s);
                }
            }
            return list;
        }

        public override void ConfigureInteraction()
        {
            base.ConfigureInteraction();
            TimedStage timedStage = new TimedStage(GetInteractionName(), kSimMinutesForPetVaccine,
                showCompletionTime: false, selectable: true, visibleProgress: true);
            base.Stages = new List<Stage>(new Stage[1] { timedStage });
            base.ActiveStage = timedStage;
        }

        public override bool Run()
        {
            mPetsToVaccinate = GetSelectedObjectsAsSims();
            if (mPetsToVaccinate.Count <= 1)
            {
                return false;
            }
            Sim firstPet = mPetsToVaccinate[1];
            Actor.RouteTurnToFace(firstPet.Position);
            EnterStateMachine("CallPet", "Enter", "x");
            AnimateSim("Call Pet");
            AnimateSim("Exit");
            foreach (Sim pet in mPetsToVaccinate)
            {
                if (pet == null || pet.HasBeenDestroyed || pet.IsHorse)
                {
                    continue;
                }
                if (Actor.Posture.Container != pet)
                {
                    AddFollower(pet);
                }
            }
            return base.Run();
        }

        List<EWGoToHospitalPet> goToHospital = new List<EWGoToHospitalPet>();

        public override bool BeforeEnteringRabbitHole()
        {
            // Get the dang pet into the rabbithole. Surprised this is not handled by following
            foreach (Sim pet in mPetsToVaccinate)
            {
                CarryingPetPosture carryingPet = Actor.Posture as CarryingPetPosture;
                if (carryingPet == null)
                {
                    EWGoToHospitalPet goPet = EWGoToHospitalPet.Singleton.CreateInstance(Target,
                        pet, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: false,
                        cancellableByPlayer: true) as EWGoToHospitalPet;
                    pet.InteractionQueue.Add(goPet);
                    goToHospital.Add(goPet);
                }
            }

            return base.BeforeEnteringRabbitHole();
        }

        public override bool InRabbitHole()
        {
            StartStages();
            bool result = DoLoop(ExitReason.Default);
            if (Actor.HasExitReason(ExitReason.StageComplete))
            {
                int vetBill = mPetsToVaccinate.Count * PetDiseaseManager.kPetVaccineCost;
                if (Actor.FamilyFunds > vetBill)
                {
                    if (mPetsToVaccinate.Count.Equals(1))
                    {
                        Actor.ShowTNSIfSelectable(LocalizeStr("VaccineChargeSingular", vetBill),
                            StyledNotification.NotificationStyle.kGameMessagePositive);
                    }
                    else
                    {
                        Actor.ShowTNSIfSelectable(LocalizeStr("VaccineChargePlural", vetBill, mPetsToVaccinate.Count),
                            StyledNotification.NotificationStyle.kGameMessagePositive);
                    }
                    Actor.ModifyFunds(-vetBill);
                }
                else if (!GameUtils.IsFutureWorld())
                {
                    // Shouldn't hit this because interaction requires funds, but just in case
                    Actor.ShowTNSIfSelectable(LocalizeStr("InsufficientFundsVaccine", vetBill),
                        StyledNotification.NotificationStyle.kGameMessageNegative);
                    Actor.UnpaidBills += vetBill;
                }
                foreach(EWGoToHospitalPet interaction in goToHospital)
                {
                    interaction.timeToGo = true;
                }
                foreach(Sim pet in mPetsToVaccinate)
                {
                    PetDiseaseManager.Vaccinate(pet);
                    EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
                    EventTracker.SendEvent(EventTypeId.kVisitedRabbitHole, pet, Target);
                }
            }
            return result;
        }
    }

    // Pet doesn't enter the rabbithole on its own without this bit, even if it is a follower
    public class EWGoToHospitalPet : RabbitHoleInteraction<Sim, RabbitHole>
    {
        public class Definition : InteractionDefinition<Sim, RabbitHole, EWGoToHospitalPet>
        {
            public override bool Test(Sim a, RabbitHole target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim actor, RabbitHole target, InteractionObjectPair iop)
            {
                return LocalizeStr("GoToVet");
            }
        }

        public bool timeToGo = false;

        public static InteractionDefinition Singleton = new Definition();


        public override bool InRabbitHole()
        {
            while (!Actor.WaitForExitReason(kWaitForExitReasonDefaultTime, ExitReason.Canceled)
                && !timeToGo)
            {
            }
            return true;
        }
    }
}
