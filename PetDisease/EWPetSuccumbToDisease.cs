using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using static Echoweaver.Sims3Game.PetDisease.PetDiseaseManager;
using System.Collections.Generic;

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
                if (!kAllowPetDiseaseDeath)
                {
                    return Localization.LocalizeString("Echoweaver/PetDisease:RecuperateName");
                }
                return Localization.LocalizeString("Echoweaver/PetDisease:Succumb");
            }
        }

        public static InteractionDefinition Singleton = new Definition();

        public override ThumbnailKey GetIconKey()
        {
            if (kAllowPetDiseaseDeath)
            {
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWDeath"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
            else if (Target.IsCat)
            {
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWRecuperateCat"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
            else
            {
                // TODO: We want a dog icon too
                return new ThumbnailKey(new ResourceKey(ResourceUtils.HashString64("moodlet_EWRecuperateCat"),
                    0x2F7D0004, 0u), ThumbnailSize.Medium);
            }
        }

        public static string LocalizeString(string name, bool isFemale, params object[] parameters)
        {
            return Localization.LocalizeString(isFemale, "Echoweaver/PetDisease/Succumb:"
                + name, parameters);
        }

        string diseaseName = "Disease";

        public void SetDiseaseName(string dName)
        {
            diseaseName = dName;
        }

        public override bool Run()
        {
            if (kAllowPetDiseaseDeath)
            {
                if (!Target.IsSleeping)
                {   
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                }
                World.ObjectSetGhostState(Target.ObjectId, (uint)kDiseaseDeathType,
                    (uint)Target.SimDescription.AgeGenderSpecies);
                AnimateSim("Exit");

                Target.BuffManager.RemoveAllElements();
                StyledNotification.Show(new StyledNotification.Format(LocalizeString("Die", Target.IsFemale,
                    diseaseName, Target.Name), StyledNotification.NotificationStyle.kGameMessageNegative));
                Target.Kill(kDiseaseDeathType);
            }
            else
            {
                if (!Target.IsSleeping)
                {
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                    Target.SetIsSleeping(value: true);
                }

                StyledNotification.Show(new StyledNotification.Format(LocalizeString("Recuperate", Target.IsFemale,
                    diseaseName, Target.Name), StyledNotification.NotificationStyle.kGameMessageNegative));

                TimedStage timedStage = new TimedStage(GetInteractionName(), kRecuperateTime,
                    showCompletionTime: false, selectable: true, visibleProgress: true);
                Stages = new List<Stage>(new Stage[1] { timedStage });
                ActiveStage = timedStage;

                StartStages();
                VisualEffect mSleepZs;
                mSleepZs = VisualEffect.Create("zzz");
                mSleepZs.ParentTo(Target, Sim.FXJoints.Mouth);
                mSleepZs.Start();

                Target.Motives.FreezeDecay(CommodityKind.Hunger, false);
                Target.Motives.FreezeDecay(CommodityKind.Energy, true);

                Target.BuffManager.RemoveElement(Buffs.BuffEWPetGermy.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWPetPneumonia.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWPetstilence.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWTummyTrouble.buffName);

                // Remove wound buffs if Fighting is installed
                Target.BuffManager.RemoveElement(buffNameMinorWound);
                Target.BuffManager.RemoveElement(buffNameSeriousWound);
                Target.BuffManager.RemoveElement(buffNameGraveWound);

                DoLoop(ExitReason.StageComplete);
                Target.Motives.RestoreDecay(CommodityKind.Hunger);
                Target.Motives.RestoreDecay(CommodityKind.Energy);
                mSleepZs.Stop();
                AnimateSim("Exit");
                Target.SetIsSleeping(false);
            }

            return true;
        }

    }
}
