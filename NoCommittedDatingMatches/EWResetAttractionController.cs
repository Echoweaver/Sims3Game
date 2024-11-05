using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.HobbiesSkills;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.NoCommittedDatingMatches
{
    public class EWResetAttractionController : ImmediateInteraction<Sim, CityHall>
    {
        private class Definition : ImmediateInteractionDefinition<Sim, CityHall, EWResetAttractionController>
        {
            public override string GetInteractionName(Sim a, CityHall target, InteractionObjectPair interaction)
            {
                return "Reset NPC Attraction Behavior";
            }

            public override bool Test(Sim a, CityHall target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }
        }

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            foreach (EWAttractionNPCController controller in Main.npcControllers.Values)
            {
                AttractionNPCBehaviorController oldController = controller.npcController;
                if (oldController != null)
                {
                    oldController.SetDateAlarm();
                    oldController.SetGiftAlarm();
                    oldController.SetLoveLetterAlarm();
                } 
            }
            Main.npcControllers.Clear();
            return true;
        }

    }
}

