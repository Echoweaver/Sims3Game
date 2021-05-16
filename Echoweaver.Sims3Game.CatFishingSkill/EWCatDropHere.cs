using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;

namespace Echoweaver.Sims3Game.CatFishing
{
	public class EWCatDropHere : Interaction<Sim, ICatPrey>
	{
		public class Definition : InteractionDefinition<Sim, ICatPrey, EWCatDropHere>
		{

			public override string GetInteractionName(Sim actor, ICatPrey target, InteractionObjectPair iop)
			{
                return Localization.LocalizeString("Echoweaver/Interactions:EWDropHere");
            }

			public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if(!a.IsPet)
                {
					return false;
                }
                if (!target.InInventory && target.Parent != a)
                {
                    return false;
                }
                if (a.LotCurrent.IsWorldLot)
                {
                    return false;
                }
                if (isAutonomous && a.LotCurrent != a.LotHome)
                {
                    return false;
                }
                return true;
			}
		}

		public static InteractionDefinition Singleton = new Definition();

		public override bool RunFromInventory()
		{
			// We'll assume that if the cat can stand in the location, it's valid to drop their prey?
			Target.UpdateVisualState(CatHuntingModelState.Carried);
			PetCarrySystem.PickUpFromSimInventory(Actor, Target, removeFromInventory: true);
			PetCarrySystem.PutDownOnFloor(Actor);
			Target.UpdateVisualState(CatHuntingModelState.InWorld);
			return true;
		}

    }
}