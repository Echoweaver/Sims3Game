using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
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
            10,
            10,
            10,
            -10,
            -10,
            -10
        };

        [Tunable]
        [TunableComment("Base chance either pet sim is wounded in fight")]
        public static int kBaseWoundChance = 75;

        [TunableComment("Wound chance reducion with each level of fight skill")]
        [Tunable]
        public static int kWoundChanceAdjPerSkillLevel = 7;

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
                skillTarget = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID)
                    as EWPetFightingSkill;
                if (skillTarget == null)
                {
                    return false;
                }
            }

            // TODO: There are accelerated gain rates for Hunter and Aggressive pets.
            // Possibly slower for Nonaggressive and Skittish?
            if (skillActor.OppExperiencedFighterCompleted)
            {
                skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateExperienced);
            } else skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
            if (skillTarget.OppExperiencedFighterCompleted)
            {
                skillTarget.StartSkillGain(EWPetFightingSkill.kSkillGainRateExperienced);
            } else skillTarget.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);

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
            SetActor("y", (IHasScriptProxy)(object)Target);
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
            LinkedInteractionInstance.EndCommodityUpdates(success);
            bool actorWon = DoesActorWinFight();
            if (!actorWon)
            {
                AnimateSim("Swap");
                SetActor("x", Target);
                SetActor("y", Actor);
                skillTarget.wonFight(Actor, Target.LotCurrent == Target.LotHome);
                skillActor.lostFight(Target);
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
            FinishSocial("Fight Pet", bApplySocialEffect: true);
            skillActor.StopSkillGain();
            skillTarget.StopSkillGain();
            FinishLinkedInteraction();
            WaitForSyncComplete();
            StandardExit(removeFromUseList: false);

            // Check for death conditions BEFORE new wounds assigned
            CheckForDeath(actorWon);  
            AssignFightWounds();

            // If this is called from ChaseOffLot, then the target will flee if it loses
            if (!actorWon && actorRunOnLose && Actor.LotCurrent != Actor.LotHome)
            {
                // TODO: Walkstyle should be running fear
                Actor.PopPosture();
                Actor.RequestWalkStyle(WalkStyle.PetRun);
                MakeSimGoHome(Actor, false);
            }
            else if (actorWon && targetRunOnLose && Target.LotCurrent != Target.LotHome)
            {
                // TODO: Walkstyle should be running fear
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

            float actorSkillLevel = Math.Max(0, skillActor.SkillLevel);
            if (Actor.LotCurrent == Actor.LotHome && skillActor.OppHomeDefenderCompleted)
            {
                actorSkillLevel *= EWPetFightingSkill.kOppHomeDefenderBonus;
            }
            if (Target.IsHuman && skillActor.OppHumanFighterCompleted)
            {
                actorSkillLevel *= EWPetFightingSkill.kOppHumanFighterBonus;
            } else if (Target.IsFullSizeDog && skillActor.OppBigPetFighterCompleted)
            {
                actorSkillLevel *= EWPetFightingSkill.kOppBigPetFighterBonus;
            } else if ((Target.IsLittleDog || Target.IsCat || Target.IsRaccoon) && skillActor.OppSmallPetFighterCompleted)
            {
                actorSkillLevel *= EWPetFightingSkill.kOppSmallPetFighterBonus;
            }

            float targetSkillLevel = Math.Max(0, skillTarget.SkillLevel);
            if (Target.LotCurrent == Target.LotHome && skillTarget.OppHomeDefenderCompleted)
            {
                targetSkillLevel *= EWPetFightingSkill.kOppHomeDefenderBonus;
            }
            if (Actor.IsHuman && skillTarget.OppHumanFighterCompleted)
            {
                targetSkillLevel *= EWPetFightingSkill.kOppHumanFighterBonus;
            }
            else if (Actor.IsFullSizeDog && skillTarget.OppBigPetFighterCompleted)
            {
                targetSkillLevel *= EWPetFightingSkill.kOppBigPetFighterBonus;
            }
            else if ((Actor.IsLittleDog || Actor.IsCat || Actor.IsRaccoon) && skillTarget.OppSmallPetFighterCompleted)
            {
                targetSkillLevel *= EWPetFightingSkill.kOppSmallPetFighterBonus;
            }
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
    }

    public class EWKillNow : ImmediateInteraction<Sim, Sim>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, Sim, EWKillNow>
        {
            public static InteractionDefinition Singleton = new Definition();

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "KillNow";
            }
        }

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            // Mummy Curse: exotic gold-to-black shading
            // Mermaid Dehydration: Slowly pulsing yellow
            // Jellybean: Teal with barely-visible jellybean outlines floating above torso
            // Robot: Lovely green good for disease but difficult to use because it has no defined death process
            // Thirst: Red liquid with pulsing heart. Perfect for wounds, but concered that Bad Pet ghost is already
            // red.
            // Jetpack: Fast-moving clouds over gold ghost
            // HumanStatue: Invisible except for eyes!!

            if (Loader.kAllowPetDeath)
            {
                // TODO: LOCALIZE!
                Target.Kill(Loader.fightDeathType);
            }
            //Target.Kill(SimDescription.DeathType.HumanStatue);
            //if (Target.IsHuman)
            //{
            //    Target.PlaySoloAnimation("ad2ad_soc_neutral_fight_Loop1_y");
            //} else if (Target.IsLittleDog)
            //{
            //    Target.PlaySoloAnimation("al2a_soc_neutral_attackSim_insulting_neutral_x");
            //}

            return true;
        }
    }
}