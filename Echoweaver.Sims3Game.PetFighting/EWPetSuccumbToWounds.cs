using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

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
                return Localization.LocalizeString("Echoweaver/PetFighting/EWFightPet:Succumb");
            }
        }

        public static InteractionDefinition Singleton = new Definition();


        public override ThumbnailKey GetIconKey()
        {
            if (Tunables.kAllowPetDeath)
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
            Target.BuffManager.RemoveElement(BuffEWGraveWound.buffName);
            Target.BuffManager.RemoveElement(BuffEWSeriousWound.buffName);
            Target.BuffManager.RemoveElement(BuffEWMinorWound.buffName);

            if (Tunables.kAllowPetDeath)
            {

                if (!Target.IsSleeping)
                {
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                }

                StyledNotification.Show(new StyledNotification.Format(Localization
                    .LocalizeString("Echoweaver/PetFighting/EWFightPet:PetFightDie", Target.Name),
                    StyledNotification.NotificationStyle.kGameMessageNegative));

                // Add a ghost shader so the pet appears to die after falling unconscious. 
                World.ObjectSetGhostState(Target.ObjectId, (uint)Loader.fightDeathType,
                    (uint)Target.SimDescription.AgeGenderSpecies);
                AnimateSim("Exit");
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

                StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString("Echoweaver/PetFighting/EWFightPet:PetFightRecuperate",
                    Target.Name), StyledNotification.NotificationStyle.kGameMessageNegative));

                TimedStage timedStage = new TimedStage(GetInteractionName(), Tunables.kRecuperateDuration,
                    showCompletionTime: false, selectable: true, visibleProgress: true);
                Stages = new List<Stage>(new Stage[1] { timedStage });
                ActiveStage = timedStage;

                StartStages();
                VisualEffect mSleepZs;
                mSleepZs = VisualEffect.Create("zzz");
                mSleepZs.ParentTo(Target, Sim.FXJoints.Mouth);
                mSleepZs.Start();

                // Remove diseases from Pet Diseases mod if they are present
                Target.BuffManager.RemoveElement(Loader.buffNamePetGermy);
                Target.BuffManager.RemoveElement(Loader.buffNamePetPnumonia);
                Target.BuffManager.RemoveElement(Loader.buffNamePetstilence);
                Target.BuffManager.RemoveElement(Loader.buffNameTummyTrouble);

                Target.Motives.FreezeDecay(CommodityKind.Hunger, false);
                Target.Motives.FreezeDecay(CommodityKind.Energy, true);

                DoLoop(ExitReason.StageComplete);
                Target.Motives.RestoreDecay(CommodityKind.Hunger);
                Target.Motives.RestoreDecay(CommodityKind.Energy);
                mSleepZs.Stop();

                Target.Motives.RestoreDecay(CommodityKind.Hunger);
                Target.Motives.RestoreDecay(CommodityKind.Energy);
                AnimateSim("Exit");
                Target.SetIsSleeping(false);
            }

            return true;
        }

    }
}
