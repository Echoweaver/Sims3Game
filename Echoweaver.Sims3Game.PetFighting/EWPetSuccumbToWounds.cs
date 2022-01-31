using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.ActorSystems.PetSurfacePosture;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWPetSuccumbToWounds : Interaction<Sim, Sim>
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWPetSuccumbToWounds>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                // TODO: Localize
                return "Succumb";
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
                // TODO: We want a dog icon too
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWRecuperateCat"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
        }
            
        public override bool Run()
        {
            //mPriority = new InteractionPriority(InteractionPriorityLevel.MaxDeath);
            //CancellableByPlayer = false;

            Target.BuffManager.RemoveElement(BuffEWGraveWound.buffName);
            Target.BuffManager.RemoveElement(BuffEWSeriousWound.buffName);
            Target.BuffManager.RemoveElement(BuffEWMinorWound.buffName);
            if (Loader.kAllowPetDeath)
            {
                // TODO: LOCALIZE!
                StyledNotification.Show(new StyledNotification.Format(Target.Name +
                    " fought bravely but in the end, it was all too much. They will be missed.",
                    StyledNotification.NotificationStyle.kGameMessageNegative));
                Target.Kill(Loader.fightDeathType);
            }
            else
            {
                if (!Target.IsSleeping)
                {
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                    Target.SetIsSleeping(value: true);
                }

                StyledNotification.Show(new StyledNotification.Format(Target.Name +
                    " can do no more until they have a long time to recover.",
                    StyledNotification.NotificationStyle.kGameMessageNegative));
                // TODO: Needs an origin for succumb to wounds
                Target.BuffManager.AddElement(BuffEWRecuperateCat.StaticGuid,
                    Origin.FromFight);

                Target.Motives.FreezeDecay(CommodityKind.Hunger, false);
                Target.Motives.FreezeDecay(CommodityKind.Energy, true);

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
