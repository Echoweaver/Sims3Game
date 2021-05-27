using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.UI.CAS.CASController;

namespace Echoweaver.Sims3Game.PetBreeding
{
    public class SetPetBreed : ImmediateInteraction<Sim, Sim>
    {
        [DoesntRequireTuning]
        private sealed class Definition : ImmediateInteractionDefinition<Sim, Sim, SetPetBreed>
        {

            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                    Localization.LocalizeString (Loader.sEWBreedLocalizeKey + "BreedMenu")
                };
            }

            public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
            {
                return Localization.LocalizeString(Loader.sEWBreedLocalizeKey + "SetPetBreed");
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
            BreedOutfit breed = new BreedPickerDialog(Target.SimDescription.Species).Show();
            string breedName = "";
            if (breed == null || breed.BreedLocKey == string.Empty)
            {
                Target.SimDescription.PetManager.BreedName = "";
                if (Target.IsADogSpecies)
                {
                    breedName = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:DogNoBreed");
                }
                else
                {
                    breedName = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:CatHorseNoBreed");
                }
            }
            else
            {
                Target.SimDescription.PetManager.BreedName = breed.BreedLocKey;
                breedName = StringTable.GetLocalizedString(breed.BreedLocKey);
            }

            StyledNotification.Show(new StyledNotification.Format(Target.FullName
                + " " + Localization.LocalizeString(Loader.sEWBreedLocalizeKey + "SetPetBreed")
                + " = " + breedName, StyledNotification.NotificationStyle.kGameMessagePositive));
            return true;
        }
    }
}
