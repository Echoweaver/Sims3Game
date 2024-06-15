using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWDebugClearApprentice : ImmediateInteraction<Sim, Sim>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, Sim, EWDebugClearApprentice>
        {
            public override bool Test(Sim actor, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (Config.kPetWarriorDebug)
                    return true;
                return false;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "Clear Apprentice State";
            }
        }

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            Config.ClearApprenticeState(Target);
            Config.DebugNote("Apprentice status cleared for " + Target.Name);
            return true;
        }
    }
}

