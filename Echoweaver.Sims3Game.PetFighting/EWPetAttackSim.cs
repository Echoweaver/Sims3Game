using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.InteractionsShared;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWPetAttackSim : SocialInteractionA
    {
        [Tunable]
        [TunableComment("Distance attacking pet must be from target pet before initiating attack animations.")]
        public static float kDistanceForPetFight = CatHuntingComponent.PetEatPrey.kDistanceFromPreyForCatToHunting;

        public class EWPetAttackSimDefinition : Definition
        {
            public EWPetAttackSimDefinition()
                : base("EWPetAttackSim", new string[0], null, initialGreet: false)
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                SocialInteractionA socialInteractionA = new EWPetAttackSim();
                socialInteractionA.Init(ref parameters);
                return socialInteractionA;
            }

            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                    Localization.LocalizeString (ActionData.GetParentMenuLocKey (ActionDataBase.ParentMenuType.Mean))
                };
            }

            public override float CalculateScore(InteractionObjectPair interactionObjectPair, Sims3.Gameplay.Autonomy.Autonomy autonomy)
            {
                return CalculateScoreWithInteractionTuning(interactionObjectPair, autonomy,
                    EWFightPet.kSocialTuningScoreWeight, EWFightPet.kInteractionTuningScoreWeight);
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                // TODO: Localize
                return "EWPetAttackSim";
            }
        }

        bool targetRunOnLose = false;
        bool actorRunOnLose = false;


        public void SetParams(bool pTargetRunOnLose, bool pActorRunOnLose)
        {
            targetRunOnLose = pTargetRunOnLose;
            actorRunOnLose = pActorRunOnLose;
        }

        public static InteractionDefinition Singleton = new EWPetAttackSimDefinition();

		public static void OnAfterAttack(Sim actor, Sim target, string interaction, ActiveTopic topic, InteractionInstance i)
        {
            StyledNotification.Show(new StyledNotification.Format("Custom OnAfterAttack",
                StyledNotification.NotificationStyle.kDebugAlert));

            if (actor.TraitManager.HasElement(TraitNames.AggressivePet))
            {
                actor.Motives.SetDecay(CommodityKind.Fun, decay: true);
                actor.Motives.SetValue(CommodityKind.Fun, actor.Motives.GetValue(CommodityKind.Fun) + PetSocialTunables.kAttackShredFunUpdate);
            }
            target.BuffManager.AddElement(BuffNames.Backache, Origin.FromCatAttack);
        }
    }
}
