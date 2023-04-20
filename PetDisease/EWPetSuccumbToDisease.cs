using Echoweaver.Sims3Game.PetDisease;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.ActorSystems.PetSurfacePosture;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using Sims3.Gameplay.CAS;

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
            if (Loader.kAllowPetDiseaseDeath)
            {
                // TODO: Localize!
                if (!Target.IsSleeping)
                {   
                    EnterStateMachine("PetPassOut", "Enter", "x");
                    AnimateSim("PassOutLoop");
                }

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
                // TODO: Needs an origin for diseases
                // I guess we need a copy of the recuperate moodlet.
                //Target.BuffManager.AddElement(BuffEWRecuperateCat.StaticGuid,
                //    Origin.FromFight);

                Target.Motives.FreezeDecay(CommodityKind.Hunger, false);
                Target.Motives.FreezeDecay(CommodityKind.Energy, true);

                Target.BuffManager.RemoveElement(Buffs.BuffEWPetGermy.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWPetPneumonia.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWPetstilence.buffName);
                Target.BuffManager.RemoveElement(Buffs.BuffEWTummyTrouble.buffName);

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
