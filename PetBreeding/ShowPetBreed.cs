using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.PetSystems;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetBreedfix
{
    public sealed class ShowPetBreed : ImmediateInteraction<Sim, Sim>
    {
        [DoesntRequireTuning]
        private sealed class Definition : ImmediateInteractionDefinition<Sim, Sim, ShowPetBreed>
        {
            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                    Localization.LocalizeString (Loader.sEWBreedLocalizeKey + "BreedMenu")
                };
            }

            public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
            {
                return Localization.LocalizeString(Loader.sEWBreedLocalizeKey + "ShowPetBreed");
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
            StyledNotification.Show(new StyledNotification.Format(Target.FullName
                + " " + Localization.LocalizeString(Loader.sEWBreedLocalizeKey + "BreedMenu")
                + " = " + breedName,
                StyledNotification.NotificationStyle.kGameMessagePositive));
            return true;
        }
    }
}
