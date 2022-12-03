﻿using Echoweaver.Sims3Game.PetDisease;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.ActorSystems.PetSurfacePosture;

namespace Echoweaver.Sims3Game.PetDisease
{
    public class EWPetSuccumbToDisease : Interaction<Sim, Sim>
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWPetSuccumbToDisease>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                //return Localization.LocalizeString("Echoweaver/PetDisease:Succumb");
                return "Succumb to Disease (no STBL)";
            }
        }

        public static InteractionDefinition Singleton = new Definition();


        public override bool Run()
        {
            if (Loader.kAllowPetDeath)
            {
                // TODO: Localize!
                //StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString("Echoweaver/PetDisease:PetDie",
                //    Target.Name), StyledNotification.NotificationStyle.kGameMessageNegative));
                StyledNotification.Show(new StyledNotification.Format("You have died of disentery",
                    StyledNotification.NotificationStyle.kGameMessageNegative));
                Target.Kill(Loader.diseaseDeathType);
            }
            else
            {
                if (!Target.IsSleeping)
                {
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                    Target.SetIsSleeping(value: true);
                }

                // TODO: Localize!
                //StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString("Echoweaver/PetDisease:PetRecuperate",
                //    Target.Name), StyledNotification.NotificationStyle.kGameMessageNegative));
                StyledNotification.Show(new StyledNotification.Format("You are only mostly dead.",
                    StyledNotification.NotificationStyle.kGameMessageNegative));
                // TODO: Needs an origin for diseases
                // I guess we need a copy of the recuperate moodlet.
                //Target.BuffManager.AddElement(BuffEWRecuperateCat.StaticGuid,
                //    Origin.FromFight);

                Target.Motives.FreezeDecay(CommodityKind.Hunger, false);
                Target.Motives.FreezeDecay(CommodityKind.Energy, true);

                // TODO: This should be tunable
                float passOutMinutes = 720f;
                ExitReason acceptedExitReasons = ~(ExitReason.Finished);
                float startTime = SimClock.ElapsedTime(TimeUnit.Minutes);
                while (!Target.WaitForExitReason(1f, acceptedExitReasons))
                {
                    float currentTime = SimClock.ElapsedTime(TimeUnit.Minutes);
                    if (currentTime - startTime > passOutMinutes)
                    {
                        AnimateSim(kJazzStateSleep);
                        break;
                    }
                }
                Target.Motives.RestoreDecay(CommodityKind.Hunger);
                Target.Motives.RestoreDecay(CommodityKind.Energy);
                AnimateSim("Exit");
                Target.SetIsSleeping(false);
            }

            return true;
        }

    }
}