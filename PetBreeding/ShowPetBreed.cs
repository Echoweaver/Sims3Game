using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.PetSystems;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetBreeding
{
    public sealed class ShowPetBreed : ImmediateInteraction<Sim, Sim>
    {
        [DoesntRequireTuning]
        private sealed class Definition : ImmediateInteractionDefinition<Sim, Sim, ShowPetBreed>
        {
            public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
            {
                return "Show Breed";
            }

            public override bool Test(Sim a, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return !isAutonomous;
            }
        }

        public static readonly InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            string breedName = Target.SimDescription.PetManager.BreedName;
            //string breedName = new HudModel().GetPetBreedName(Target.SimDescription.GetMiniSimDescription()); 
            if (breedName == null || breedName == string.Empty)
            {
                if (Target.IsADogSpecies)
                {
                    breedName = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:DogNoBreed");
                }
                else
                {
                    breedName = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:CatHorseNoBreed");
                }
            } else
            {
                breedName = StringTable.GetLocalizedString(breedName);
            }
            Actor.ShowTNSIfSelectable(breedName, StyledNotification.NotificationStyle.kSimTalking);
            return true;
        }
    }
}
