using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Opportunities;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using static Sims3.Gameplay.Abstracts.RabbitHole;
using static Sims3.Gameplay.Actors.Sim;
using static Sims3.UI.ObjectPicker;
using Queries = Sims3.Gameplay.Queries;

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
                if (a.IsHuman)
                {
                    if (a.FamilyFunds < kCostOfPetVaccine)
                    {
                        greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback("Localize - Can't afford vaccine");
                        return false;
                    }
                    if (GetUnvaccinatedPets(a.Household).Count == 0)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback("Localize - No pets need vaccines");
                        return false;
                    }
                    return true;
                }
                else return false;
            }

            public override string GetInteractionName(Sim actor, Hospital target, InteractionObjectPair iop)
            {
                return "Localize - Vaccinate Pet §" + kCostOfPetVaccine;
            }

            public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters,
                out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
            {
                Sim sim = parameters.Actor as Sim;
                NumSelectableRows = 1;  // TODO: Try to select multiple pets?
                PopulateSimPicker(ref parameters, out listObjs, out headers, GetUnvaccinatedPets(sim.Household),
                    includeActor: false);
            }
        }

        public const string sLocalizationKey = "Echoweaver/PetDisease/Vaccinate:";

        [Tunable]
        public static int kSimMinutesForPetVaccine = 60;

        [Tunable]
        public static int kCostOfPetVaccine = 200;

        public Sim mPetToVaccinate;

        public static InteractionDefinition Singleton = new Definition();

        public override string GetSlaveInteractionName()
        {
            return "Localize - Be Taken To Vet";
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
            mPetToVaccinate = GetSelectedObject() as Sim;
            if (mPetToVaccinate == null || mPetToVaccinate.HasBeenDestroyed)
            {
                return false;
            }
            if (!mPetToVaccinate.IsHorse || Actor.Posture.Container != mPetToVaccinate)
            {
                AddFollower(mPetToVaccinate);
            }
            return base.Run();
        }

        EWGoToHospitalPet goToHospital = null;

        public override bool BeforeEnteringRabbitHole()
        {
            // Get the dang pet into the rabbithole. Surprised this is not handled by following
            CarryingPetPosture carryingPet = Actor.Posture as CarryingPetPosture;
            if (carryingPet == null)
            {
                goToHospital = EWGoToHospitalPet.Singleton.CreateInstance(Target,
                    mPetToVaccinate, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: false,
                    cancellableByPlayer: true) as EWGoToHospitalPet;
                mPetToVaccinate.InteractionQueue.Add(goToHospital);
            }

            return base.BeforeEnteringRabbitHole();
        }

        public override bool InRabbitHole()
        {
            StartStages();
            bool result = DoLoop(ExitReason.Default);
            if (Actor.HasExitReason(ExitReason.StageComplete))
            {
                if (Actor.FamilyFunds > kCostOfPetVaccine)
                {
                    Actor.ShowTNSIfSelectable("Localize - You were charged §" + kCostOfPetVaccine,
                        StyledNotification.NotificationStyle.kGameMessagePositive);
                    Actor.ModifyFunds(-kCostOfPetVaccine);
                }
                else if (!GameUtils.IsFutureWorld())
                {
                    // Shouldn't hit this because interaction requires funds, but just in case
                    Actor.ShowTNSIfSelectable("Localize - Insufficient funds billed §" + kCostOfPetVaccine,
                        StyledNotification.NotificationStyle.kGameMessageNegative);
                    Actor.UnpaidBills += kCostOfPetVaccine;
                }
                if (goToHospital != null)
                {
                    goToHospital.timeToGo = true;
                }
                PetDiseaseManager.Vaccinate(mPetToVaccinate);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHoleWithPet, Actor, Target);
                EventTracker.SendEvent(EventTypeId.kVisitedRabbitHole, mPetToVaccinate, Target);
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
                return "Localize - See Vet";
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
