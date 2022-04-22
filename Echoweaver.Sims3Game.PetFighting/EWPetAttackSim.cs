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
using Sims3.UI.Controller;
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
                return base.Test(a, target, isAutonomous, ref greyedOutTooltipCallback);
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

            //public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            //{
            //    // TODO: Localize
            //    return "EWPetAttackSim";
            //}
        }

        public static InteractionDefinition Singleton = new EWPetAttackSimDefinition();


        [TunableComment("Fight win chance trait modifiers, if sim has this trait their chance of winning will be modified by the matching amount")]
        [Tunable]
        public static TraitNames[] kWinChanceModifyTraitsHuman = new TraitNames[10] {
            TraitNames.Brave,
            TraitNames.Athletic,
            TraitNames.Disciplined,
            TraitNames.CanApprehendBurglar,
            TraitNames.Lucky,
            TraitNames.HotHeaded,
            TraitNames.Loser,
            TraitNames.Coward,
            TraitNames.Clumsy,
            TraitNames.Unlucky
        };

        [Tunable]
        [TunableComment("Fight win chance increase/decrease matching coresponding trait list")]
        public static int[] kWinChanceModifyValuesHuman = new int[10] {
            10,
            10,
            10,
            10,
            10,
            10,
            -20,
            -10,
            -10,
            -10
        };


        [Tunable]
        [TunableComment("LTR penalty between two sims after they fight.")]
        public static int kLikingPenaltyPetHumanAttack = -10;

        bool targetRunOnLose = false;
        bool actorRunOnLose = false;

        public void SetParams(bool pTargetRunOnLose, bool pActorRunOnLose)
        {
            targetRunOnLose = pTargetRunOnLose;
            actorRunOnLose = pActorRunOnLose;
        }

        EWPetFightingSkill skillActor;
        MartialArts skillTarget;

        public override bool Run()
        {
            if (!SafeToSync())
            {
                return false;
            }
            StandardEntry(addToUseList: false);

            float num = GetSocialDistanceAndSetupJig();
            if (num < 0f)
            {
                Actor.AddExitReason(ExitReason.RouteFailed);
                return false;
            }

            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, GetInteractionName(),
                allowCarryChild: false), pairedSocial: true, doCallOver: false))
            {
                Actor.AddExitReason(ExitReason.FailedToStart);
                return false;
            }

            skillActor = Actor.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
            if (skillActor == null)
            {
                skillActor = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                if (skillActor == null)
                {
                    return false;
                }
            }

            skillTarget = Target.SkillManager.GetSkill<MartialArts>(SkillNames.MartialArts);
            if (skillTarget == null)
            {
                skillTarget = Actor.SkillManager.AddElement(SkillNames.MartialArts)
                    as MartialArts;
                if (skillTarget == null)
                {
                    return false;
                }
            }

            skillActor.StartSkillGain(skillActor.getSkillGainRate(Actor));
            skillTarget.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);

            UpdateConversationWhenSocialStarts(Actor, Target);
            mSmc = GetStateMachine();
            string jazzState = mTargetEffect.RHS.JazzState;
            jazzState = SetupAnimationParameters(false, false, false, jazzState);
            mSmc.RequestState(null, jazzState);

            skillActor.StopSkillGain();
            skillTarget.StopSkillGain();

            bool actorWon = DoesActorWinFight();

            if (actorWon)
            {
                skillActor.wonFight(Target, Actor.LotCurrent == Actor.LotHome);
                skillActor.AddPoints(200f, true, true);
                Actor.ShowTNSIfSelectable(Localization.LocalizeString("Echoweaver/PetFighting/EWPetAttackSim:PetAttackWin",
                    Actor.Name), StyledNotification.NotificationStyle.kGameMessagePositive);

                // TODO: Need appropriate origin for dog attack
                Target.BuffManager.AddElement(BuffNames.ShreddedDignity, Origin.FromFight);
                PlayHumanLossAnims();
            }
            else
            {
                skillActor.lostFight(Target);
                Actor.ShowTNSIfSelectable(Localization.LocalizeString("Echoweaver/PetFighting/EWPetAttackSim:PetAttackLose",
                    Actor.Name), StyledNotification.NotificationStyle.kGameMessageNegative);
                PlayScoldAnims();
            }

            AfterAttack();
            FinishLinkedInteraction();
            WaitForSyncComplete();
            if (targetRunOnLose && Target.LotCurrent != Target.LotHome)
            {
                // Success! Actor drove the unwanted sim off the lot.
                Target.RequestWalkStyle(WalkStyle.MeanChasedRun);
                MakeSimGoHome(Target, false);
            }
            else if (actorRunOnLose && Actor.LotCurrent != Actor.LotHome)
            {
                // Currently nothing calls this condition, but it seemed good to have it anyway.
                Actor.RequestWalkStyle(WalkStyle.PetRun);
                MakeSimGoHome(Actor, false);
            }

            StandardExit(removeFromUseList: false);
            return true;
        }

        public bool DoesActorWinFight()
        {
            float winChance = FightPet.kBaseWinChance;
            float actorSkillLevel = skillActor.getEffectiveSkillLevel(Actor.LotCurrent == Actor.LotHome, Target);
            int targetSkill = Math.Max(0, Target.SkillManager.GetSkillLevel(SkillNames.MartialArts));
            winChance += (actorSkillLevel - targetSkill) * FightPet.kWinChanceBonusPerSkillLevelDiff;
            for (int i = 0; i < EWFightPet.kWinChanceModifyTraits.Length; i++)
            {
                if (Actor.HasTrait(EWFightPet.kWinChanceModifyTraits[i]))
                {
                    winChance += EWFightPet.kWinChanceModifyValues[i];
                }
            }
            for (int i = 0; i < kWinChanceModifyTraitsHuman.Length; i++)
            {
                if (Target.HasTrait(kWinChanceModifyTraitsHuman[i]))
                {
                    winChance -= kWinChanceModifyValuesHuman[i];
                }
            }

            float probability = MathUtils.Clamp(winChance, 0, 100);
            return RandomUtil.RandomChance(probability);
        }

        public void PlayScoldAnims()
        {
            mCurrentStateMachine = StateMachineClient.Acquire(Target, "ChaseMean", AnimationPriority.kAPDefault);
            mCurrentStateMachine.SetActor("x", Target);
            mCurrentStateMachine.SetActor("y", Actor);
            mCurrentStateMachine.EnterState("x", "Enter");
            mCurrentStateMachine.EnterState("y", "Enter");
            AnimateJoinSims("Scold");
            AnimateJoinSims("Exit");
        }

        public void PlayHumanLossAnims()
        {
            //Target.PlaySoloAnimation("a_react_shocked_standing_x", true);
            Target.PlaySoloAnimation("a_react_whyMe_standing_x", true);
            //a_react_tantrum_intense_standing_x
            //a_react_tantrum_mild_standing_x
            //a_react_view_hate_x
            //EnterStateMachine("social_pet_tackle", "Enter", "x", "y");
            //AnimateJoinSims("tackle");
            //AnimateJoinSims("Exit");
        }

        public void AfterAttack()
        {
            // This seems awfully complicated. Do you need all this to update a
            // relationship and display the icon?
            Relationship relationship = Relationship.Get(Actor, Target, createIfNone: true);
            LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
            relationship.LTR.UpdateLiking(kLikingPenaltyPetHumanAttack);
            LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
            SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Insulting,
                            Actor, Target, false, 0, currentLTR, currentLTR2);

            if (Actor.TraitManager.HasElement(TraitNames.AggressivePet))
            {
                Actor.Motives.SetDecay(CommodityKind.Fun, decay: true);
                Actor.Motives.SetValue(CommodityKind.Fun, Actor.Motives.GetValue(CommodityKind.Fun) +
                    PetSocialTunables.kAttackShredFunUpdate);
            }
        }
    }
}
