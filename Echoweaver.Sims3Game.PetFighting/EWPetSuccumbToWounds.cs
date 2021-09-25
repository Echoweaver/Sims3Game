using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using static Sims3.Gameplay.ActorSystems.PetSurfacePosture;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWPetSuccumbToWounds : Interaction<Sim, Sim>
    {
        public class Definition : SoloSimInteractionDefinition<EWPetSuccumbToWounds>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (isAutonomous || !target.IsPet)
                    return false;
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                // TODO: Localize
                return "EWPetSuccumbToWounds";
            }
        }

        public static InteractionDefinition Singleton = new Definition();


        public override ThumbnailKey GetIconKey()
        {
            if (Loader.kAllowPetDeath)
            {
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWDeath"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
            else if (Target.IsCat)
            {
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWRecuperateCat"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            } else
            {
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWRecuperateCat"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
        }

        public override bool Run()
        {
            mPriority = new InteractionPriority(InteractionPriorityLevel.MaxDeath);
            CancellableByPlayer = false;
            if (!Target.IsSleeping)
            {
                EnterStateMachine("PetPassOut", "Enter", "x");
                AnimateSim("PassOutLoop");
                Target.SetIsSleeping(value: true);
            }
            if (Loader.kAllowPetDeath)
            {
                StyledNotification.Show(new StyledNotification.Format("Kill: "
                    + Target.Name, StyledNotification.NotificationStyle.kDebugAlert));
                AnimateSim("Exit");
                Target.Kill(Loader.fightDeathType);
                StyledNotification.Show(new StyledNotification.Format("Death Complete ",
                    StyledNotification.NotificationStyle.kDebugAlert));                
            }
            else
            {
                // TODO: Needs an origin for succumb to wounds
                // if target is cat
                Target.BuffManager.AddElement(BuffEWRecuperateCat.StaticGuid,
                    Origin.FromFight);

                VisualEffect mSleepFX = VisualEffect.Create(Target.OccultManager.GetSleepFXName());
                mSleepFX.ParentTo(Target, Sim.ContainmentSlots.Mouth);
                mSleepFX.Start();

                float passOutMinutes = 720f;
                ExitReason acceptedExitReasons = ~(ExitReason.Finished);
                float startTime = SimClock.ElapsedTime(TimeUnit.Minutes);
                while (!Actor.WaitForExitReason(1f, acceptedExitReasons))
                {
                    float currentTime = SimClock.ElapsedTime(TimeUnit.Minutes);
                    if (currentTime - startTime > passOutMinutes)
                    {
                        AnimateSim(kJazzStateSleep);
                        break;
                    }
                }
                AnimateSim("Exit");
                Target.SetIsSleeping(false);
            }

            return true;
        }

    }
}
