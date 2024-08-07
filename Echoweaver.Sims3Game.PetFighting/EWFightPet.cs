﻿using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
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
    public class EWFightPet : FightPet
    {
        public enum WoundType
        {
            Mild,
            Serious,
            Grave
        }

        public class EWFightPetDefinition : FightPetDefinition
        {
            public EWFightPetDefinition()
                : base()
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                SocialInteractionA socialInteractionA = new EWFightPet();
                socialInteractionA.Init(ref parameters);
                return socialInteractionA;
            }
        }

        public static new InteractionDefinition Singleton = new EWFightPetDefinition();

        bool targetRunOnLose = false;
        bool actorRunOnLose = false;

        public void SetParams(bool pTargetRunOnLose, bool pActorRunOnLose)
        {
            targetRunOnLose = pTargetRunOnLose;
            actorRunOnLose = pActorRunOnLose;
        }

        [TunableComment("Fight win chance trait modifiers, if sim has this trait their chance of winning will be modified by the matching amount")]
        [Tunable]
        public static new TraitNames[] kWinChanceModifyTraits = new TraitNames[6] {
            TraitNames.HunterPet,
            TraitNames.AggressivePet,
            TraitNames.DestructivePet,
            TraitNames.FriendlyPet,
            TraitNames.SkittishPet,
            TraitNames.NonDestructivePet
        };

        [Tunable]
        [TunableComment("Fight win chance increase/decrease matching coresponding trait list")]
        public static new int[] kWinChanceModifyValues = new int[6] {
            5,
            5,
            5,
            -5,
            -5,
            -5
        };

        [Tunable]
        [TunableComment("Base chance either pet sim is wounded in fight")]
        public static int kBaseWoundChance = 75;

        [TunableComment("Wound chance reducion with each level of fight skill")]
        [Tunable]
        public static int kWoundChanceAdjPerSkillLevel = 7;

        [Tunable]
        [TunableComment("LTR penalty between two fighting sims.")]
        public static int kLikingPenaltyPetFight = -10;


        public static float[] kPetFightTimeMinMax = new float[2] {
            5f,
            10f
        };

        // As of now, no traits affect the chance of being wounded. This could change.
        // Maybe skittish reduces chance? Aggressive might
        // increase.

        EWPetFightingSkill skillActor;
        EWPetFightingSkill skillTarget;

        public override bool Run()
        {
            if (!SafeToSync())
            {
                return false;
            }

            skillActor = Actor.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
            if (skillActor == null)
            {
                skillActor = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID)
                    as EWPetFightingSkill;
                if (skillActor == null)
                {
                    return false;
                }
            }

            skillTarget = Target.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
            if (skillTarget == null)
            {
                skillTarget = Target.SkillManager.AddElement(EWPetFightingSkill.skillNameID)
                    as EWPetFightingSkill;
                if (skillTarget == null)
                {
                    return false;
                }
            }

            skillActor.StartSkillGain(skillActor.getSkillGainRate());
            skillTarget.StartSkillGain(skillTarget.getSkillGainRate());

            BeginCommodityUpdates();
            Actor.RequestWalkStyle(WalkStyle.PetRun);

            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, GetInteractionName(),
                allowCarryChild: false), pairedSocial: true, doCallOver: false))
            {
                if (Actor.IsCat)
                {
                    Actor.UnrequestWalkStyle(WalkStyle.CatStalk);
                    PouncePosture pouncePosture2 = Actor.Posture as PouncePosture;
                    if (pouncePosture2 != null)
                    {
                        pouncePosture2.ExitPounce();
                        Actor.PopPosture();
                    }
                } else
                    {
                    Actor.UnrequestWalkStyle(WalkStyle.PetRun);
                }
                EndCommodityUpdates(false);
                return false;
            }

            Actor.UnrequestWalkStyle(WalkStyle.PetRun);

            if (Actor.IsCat)
            {
                PouncePosture pouncePosture = PouncePosture.Create(Actor);
                pouncePosture.EnterPounce();
                Actor.Posture = pouncePosture;
            }

            StandardEntry(addToUseList: false);
            StartSocial("Fight Pet");
            ((SocialInteraction)LinkedInteractionInstance).Rejected = Rejected;
            mPetFightNoiseBroadcast = new ReactionBroadcaster(Actor, kPetFightLoudBroadcastParams,
                FightBroadcastCallback);
            PetStartleBehavior.CheckForStartle(Actor, StartleType.Fight);
            EnterStateMachine("PetFight", "Enter", "x");
            SetActor("y", Target);
            AnimateSim("Loop Fight");
            // TODO: A fight should reduce fatigue
            InteractionTuning tuning = InteractionObjectPair.Tuning;
            if (tuning != null && tuning.mTradeoff != null)
            {
                using (List<CommodityChange>.Enumerator enumerator = tuning.mTradeoff.mOutputs.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        CommodityChange current = enumerator.Current;
                        if (current.Commodity == CommodityKind.CatScratch)
                        {
                            LinkedInteractionInstance.BeginCommodityUpdate(current, 1f);
                        }
                    }
                }
            }

            bool success = DoTimedLoop(RandomUtil.GetFloat(kPetFightTimeMinMax[0], kPetFightTimeMinMax[1]),
                ExitReason.Default);
            EndCommodityUpdates(success);
            Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives.GetMotiveValue(CommodityKind.Energy - 200));
            LinkedInteractionInstance.EndCommodityUpdates(success);
            bool actorWon = DoesActorWinFight();
            if (!actorWon)
            {
                AnimateSim("Swap");
                SetActor("x", Target);
                SetActor("y", Actor);
                skillTarget.wonFight(Actor, Target.LotCurrent == Target.LotHome);
                skillActor.lostFight(Target);
                skillTarget.AddPoints(200f, true, true);
                Actor.ShowTNSIfSelectable(Localization.LocalizeString("Echoweaver/PetFighting/EWFightPet:PetFightLose",
                    Actor.Name), StyledNotification.NotificationStyle.kGameMessageNegative);
            }
            else
            {
                skillActor.wonFight(Target, Actor.LotCurrent == Actor.LotHome);
                skillTarget.lostFight(Actor);
                Actor.ShowTNSIfSelectable(Localization.LocalizeString("Echoweaver/PetFighting/EWFightPet:PetFightWin",
                    Actor.Name), StyledNotification.NotificationStyle.kGameMessagePositive);
            }
            AnimateSim("Exit");

            if (Actor.IsCat)
            {
                Actor.UnrequestWalkStyle(WalkStyle.CatStalk);
                if (Actor.Posture is PouncePosture)
                {
                    Actor.PopPosture();
                }
            } else
            {
                Actor.UnrequestWalkStyle(WalkStyle.PetRun);
            }

            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "Fight Pet", wasRecipient: false, wasAccepted: true, actorWon, CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "Fight Pet", wasRecipient: true, wasAccepted: true, !actorWon, CommodityTypes.Undefined));

            if (mPetFightNoiseBroadcast != null)
            {
                mPetFightNoiseBroadcast.EndBroadcast();
                mPetFightNoiseBroadcast = null;
            }
            skillActor.StopSkillGain();
            skillTarget.StopSkillGain();
            FinishSocial("Fight Pet", bApplySocialEffect: true);
            FinishLinkedInteraction();
            WaitForSyncComplete();
            StandardExit(removeFromUseList: false);

            LikingChange();

            // Check for death conditions BEFORE new wounds assigned
            CheckForDeath(actorWon);  
            AssignFightWounds();

            // If this is called from ChaseOffLot, then the target will flee if it loses
            if (!actorWon && actorRunOnLose && Actor.LotCurrent != Actor.LotHome)
            {
                Actor.PopPosture();
                Actor.RequestWalkStyle(WalkStyle.PetRun);
                MakeSimGoHome(Actor, false);
            }
            else if (actorWon && targetRunOnLose && Target.LotCurrent != Target.LotHome)
            {
                Target.PopPosture();
                if (Target.IsHuman)
                {
                    Target.RequestWalkStyle(WalkStyle.MeanChasedRun);
                } else
                {
                    Target.RequestWalkStyle(WalkStyle.PetRun);
                }
                MakeSimGoHome(Target, false);
            }

            return success;
        }

        public override void Cleanup()
        {
            if (mPetFightNoiseBroadcast != null)
            {
                mPetFightNoiseBroadcast.EndBroadcast();
                mPetFightNoiseBroadcast = null;
            }
            base.Cleanup();
        }

        public new bool DoesActorWinFight()
        {
            float winChance = kBaseWinChance;

            float actorSkillLevel = skillActor.getEffectiveSkillLevel(Actor.LotCurrent == Actor.LotHome, Target);

            float targetSkillLevel = skillTarget.getEffectiveSkillLevel(Target.LotCurrent == Target.LotHome, Actor);

            winChance += (actorSkillLevel - targetSkillLevel) * kWinChanceBonusPerSkillLevelDiff;

            for (int i = 0; i < kWinChanceModifyTraits.Length; i++)
            {
                if (Actor.HasTrait(kWinChanceModifyTraits[i]))
                {
                    winChance += kWinChanceModifyValues[i];
                }
                if (Target.HasTrait(kWinChanceModifyTraits[i]))
                {
                    winChance -= kWinChanceModifyValues[i];
                }
            }
            return RandomUtil.RandomChance(winChance);
        }

        public void AssignFightWounds()
        {
            foreach (Sim fighter in new Sim[] { Actor, Target })
            {
                // Chance of being wounded is calculated based on the sim's fight skill.
                // Could eventually take into account opponent sim or win/loss
                int wound_chance = kBaseWoundChance;
                int fight_level = Math.Max(0, fighter.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
                wound_chance -= kWoundChanceAdjPerSkillLevel * fight_level;
                wound_chance = MathUtils.Clamp(wound_chance, 10, 90);
                if (RandomUtil.RandomChance(wound_chance))
                {
                    // Determine wound severity
                    // Would severity is also offset by skill. This may not be the best way to do it.

                    float[] woundChances = new float[3];
                    woundChances[0] = 10 + (2 * fight_level);  // Chance of mild: wound
                    woundChances[1] = 10 + fight_level;  // Chance of serious wound
                    woundChances[2] = 10;  // Chance of serious wound

                    WoundType wound = (WoundType)RandomUtil.GetWeightedIndex(woundChances);

                    switch (wound)
                    {
                        case WoundType.Mild:
                            fighter.BuffManager.AddElement(BuffEWMinorWound.StaticGuid, Origin.FromFight);
                            break;
                        case WoundType.Serious:
                            fighter.BuffManager.AddElement(BuffEWSeriousWound.StaticGuid, Origin.FromFight);
                            break;
                        case WoundType.Grave:
                            fighter.BuffManager.AddElement(BuffEWGraveWound.StaticGuid, Origin.FromFight);
                            break;
                    }
                }
            }
        }

        public void CheckForDeath(bool actorWins)
        {
            if (actorWins && Target.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
            {
                EventTracker.SendEvent(EventTypeId.kSimPassedOut, Target);
            } else if (!actorWins && Actor.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
            {
                EventTracker.SendEvent(EventTypeId.kSimPassedOut, Actor);
            }
        }

        public void LikingChange()
        {
            // This seems awfully complicated. Do you need all this to update a
            // relationship and display the icon?
            Relationship relationship = Relationship.Get(Actor, Target, createIfNone: true);
            LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
            relationship.LTR.UpdateLiking(kLikingPenaltyPetFight);
            LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
            SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
                            Actor, Target, true, 0, currentLTR, currentLTR2);
        }
    }
}