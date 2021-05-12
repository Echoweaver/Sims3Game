using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using static Sims3.UI.CAS.CASController;

namespace Echoweaver.Sims3Game.PetBreeding
{
    public class SetPetBreed : ImmediateInteraction<Sim, Sim>
    {
        [DoesntRequireTuning]
        private sealed class Definition : ImmediateInteractionDefinition<Sim, Sim, SetPetBreed>
        {
            public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
            {
                return "Set Breed";
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
            if (breed != null)
            {
                Target.SimDescription.PetManager.BreedName = breed.BreedLocKey;
            } 
            return true;
        }
    }
}
