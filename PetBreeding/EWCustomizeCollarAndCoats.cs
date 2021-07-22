using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetBreedfix
{
	public class EWCustomizeCollarAndCoats : CustomizeCollarAndCoats
	{
		public new class Definition : InteractionDefinition<Sim, Sim, EWCustomizeCollarAndCoats>
		{
			public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (target.IsSelectable)
				{
					if (a.IsCat || a.IsADogSpecies)
					{
						return a == target;
					}
					if (a.IsHuman && (target.IsADogSpecies || target.IsCat))
					{
						return target.SimDescription.AdultOrAbove;
					}
				}
				return false;
			}
		}

		public static new InteractionDefinition Singleton = new Definition();

		public override bool Run()
		{
			// This only exists to preserve the breed when you customize.
			string breedName = Target.SimDescription.PetManager.BreedName;
			bool origCustomize = base.Run();
			Target.SimDescription.PetManager.BreedName = breedName;
			return origCustomize;
		}
	}
}