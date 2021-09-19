using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.InteractionsShared;
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
    public class EWPetFightSim : SocialInteractionA
    {
        public class EWPetFightSimDefinition : Definition
        {
            public EWPetFightSimDefinition()
                : base("EWPetFightSim", new string[0], null, initialGreet: false)
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                SocialInteractionA socialInteractionA = new EWPetFightSim();
                socialInteractionA.Init(ref parameters);
                return socialInteractionA;
            }

            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (isAutonomous)
                {
                    return a.TraitManager.HasAnyElement(kTraitsCanFightAutonomously);
                }
                return true;
            }

            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                    Localization.LocalizeString (ActionData.GetParentMenuLocKey
                    (ActionDataBase.ParentMenuType.Mean))
                };
            }

            public override float CalculateScore(InteractionObjectPair interactionObjectPair,
                Autonomy autonomy)
            {
                return CalculateScoreWithInteractionTuning(interactionObjectPair, autonomy,
                    kSocialTuningScoreWeight, kInteractionTuningScoreWeight);
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "EWPetFightSim";
            }
        }

        [TunableComment("Min/Max time fight lasts, actual time randomly picked between the 2 values")]
        [Tunable]
        public static float[] kFightTimeMinMax = new float[2] {
            5f,
            10f
        };

        [Tunable]
        [TunableComment("Base chance that Sim A wins the fight")]
        public static int kBaseWinChance = 50;

        [TunableComment("Win fight Chance increase/decrease per difference in hunting skill level between the 2 sims")]
        [Tunable]
        public static int kWinChanceBonusPerSkillLevelDiff = 2;

        [TunableComment("Fight win chance trait modifiers, if sim has this trait their chance of winning will be modified by the matching amount")]
        [Tunable]
        public static TraitNames[] kWinChanceModifyTraits = new TraitNames[6] {
            TraitNames.HunterPet,
            TraitNames.AggressivePet,
            TraitNames.DestructivePet,
            TraitNames.FriendlyPet,
            TraitNames.SkittishPet,
            TraitNames.NonDestructivePet
        };

        [Tunable]
        [TunableComment("Fight win chance increase/decrease matching coresponding trait list")]
        public static int[] kWinChanceModifyValues = new int[6] {
            10,
            10,
            10,
            -10,
            -10,
            -10
        };

        [Tunable]
        [TunableComment("Traits that are allowed to fight autonomously")]
        public static TraitNames[] kTraitsCanFightAutonomously = new TraitNames[2] {
            TraitNames.AggressivePet,
            TraitNames.MeanPet
        };

        [Tunable]
        [TunableComment("Weighting of Social tuning when autonomously choosing to do this interaction")]
        public static float kSocialTuningScoreWeight = 1f;

        [Tunable]
        [TunableComment("Weighting of Interaction tuning tool tuning when autonomously choosing to do this interaction")]
        public static float kInteractionTuningScoreWeight = 1f;

        [TunableComment("Broadcaster for pets fighting noise reactions.")]
        [Tunable]
        public static ReactionBroadcasterParams kPetFightLoudBroadcastParams = new ReactionBroadcasterParams();

        [Tunable]
        [TunableComment("Base chance either pet sim is wounded in fight")]
        public static int kBaseWoundChance = 75;

        [TunableComment("Wound chance reducion with each level of fight skill")]
        [Tunable]
        public static int kWoundChanceAdjPerSkillLevel = 7;

        // As of now, no traits affect the chance of being wounded. This could change. Maybe skittish reduces chance? Aggressive might
        // increase.

        public ReactionBroadcaster mPetFightNoiseBroadcast;

        public static InteractionDefinition Singleton = new EWPetFightSimDefinition();

        public override bool Run()
        {
            if (!SafeToSync())
            {
                return false;
            }

            EWPetFightingSkill skillActor = Actor.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
            if (skillActor == null)
            {
                skillActor = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                if (skillActor == null)
                {
                    return false;
                }
            }

            MartialArts skillTarget = Target.SkillManager.GetSkill<MartialArts>(SkillNames.MartialArts);
            if (skillTarget == null)
            {
                skillTarget = Actor.SkillManager.AddElement(SkillNames.MartialArts)
                    as MartialArts;
                if (skillTarget == null)
                {
                    return false;
                }
            }

            // TODO: There are accelerated gain rates for Hunter and Aggressive pets.
            // Possibly slower for Nonaggressive and Skittish?
            skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
            skillTarget.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);

            if (Actor.IsCat)
            {
                PouncePosture pouncePosture = PouncePosture.Create(Actor);
                pouncePosture.EnterPounce();
                Actor.Posture = pouncePosture;
                RequestWalkStyle(WalkStyle.CatStalk);
            }
            else if (Actor.IsADogSpecies)
            {
                RequestWalkStyle(WalkStyle.PetRun);
            }
            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, GetInteractionName(),
                allowCarryChild: false), pairedSocial: true, doCallOver: false))
            {
                if (Actor.IsCat)
                {
                    UnrequestWalkStyle(WalkStyle.CatStalk);
                    PouncePosture pouncePosture2 = Actor.Posture as PouncePosture;
                    if (pouncePosture2 != null)
                    {
                        pouncePosture2.ExitPounce();
                        Actor.PopPosture();
                    }
                }
                return false;
            }
            StandardEntry(addToUseList: false);
            StartSocial("EWPetFightSim");
            BeginCommodityUpdates();
            ((SocialInteraction)LinkedInteractionInstance).Rejected = Rejected;
            mPetFightNoiseBroadcast = new ReactionBroadcaster(Actor, kPetFightLoudBroadcastParams,
                FightBroadcastCallback);
            mCurrentStateMachine = StateMachineClient.Acquire(Actor, "ChaseMean", AnimationPriority.kAPDefault);
            mCurrentStateMachine.SetActor("x", Actor);
            mCurrentStateMachine.SetActor("y", Target);
            mCurrentStateMachine.EnterState("x", "Enter");
            mCurrentStateMachine.EnterState("y", "Enter");
            AnimateJoinSims("Face Off");
            AnimateJoinSims("Exit");
            if (Actor.IsCat)
            {
                UnrequestWalkStyle(WalkStyle.CatStalk);
                if (Actor.Posture is PouncePosture)
                {
                    Actor.PopPosture();
                }
            }
            // A fight should reduce fatigue
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
            bool flag = DoTimedLoop(RandomUtil.GetFloat(kFightTimeMinMax[0], kFightTimeMinMax[1]), ExitReason.Default);
            EndCommodityUpdates(flag);
            LinkedInteractionInstance.EndCommodityUpdates(flag);
            bool actorWon = DoesActorWinFight();
            if (!actorWon)
            {
                //AnimateSim("Swap");
                //SetActor("x", (IHasScriptProxy)(object)Target);
                //SetActor("y", (IHasScriptProxy)(object)Actor);
                //skillTarget.wonFight();
                skillActor.lostFight();
            }
            else
            {
                skillActor.wonFight();
                //skillTarget.lostFight();
            }
            AnimateSim("Exit");
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "EWPetFightSim", wasRecipient: false, wasAccepted: true, actorWon, CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "EWPetFightSim", wasRecipient: true, wasAccepted: true, !actorWon, CommodityTypes.Undefined));

            if (mPetFightNoiseBroadcast != null)
            {
                mPetFightNoiseBroadcast.EndBroadcast();
                mPetFightNoiseBroadcast = null;
            }
            FinishSocial("EWPetFightSim", bApplySocialEffect: true);
            skillActor.StopSkillGain();
            skillTarget.StopSkillGain();
            FinishLinkedInteraction();
            WaitForSyncComplete();
            StandardExit(removeFromUseList: false);
            AssignFightWounds();
            //if (DoesLoserDie())
            //{
            //    Target.Kill(Sims3.Gameplay.CAS.SimDescription.DeathType.BluntForceTrauma);
            //}
            return flag;
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

        public bool DoesActorWinFight()
        {
            int winChance = kBaseWinChance;
            int actorSkill = Math.Max(0, Actor.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
            int targetSkill = Math.Max(0, Target.SkillManager.GetSkillLevel(SkillNames.MartialArts));
            winChance += (actorSkill - targetSkill) * kWinChanceBonusPerSkillLevelDiff;
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
            winChance = MathUtils.Clamp(winChance, 0, 100);
            return RandomUtil.RandomChance(winChance);
        }

        public void AssignFightWounds()
        {
            //foreach (Sim fighter in new Sim[] { Actor, Target })
            //{
            //    // Chance of being wounded is calculated based on the sim's fight skill.
            //    // Could eventually take into account opponent sim or win/loss
            //    int wound_chance = kBaseWoundChance;
            //    int fight_level = Math.Max(0, fighter.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
            //    wound_chance -= kWoundChanceAdjPerSkillLevel * fight_level;
            //    wound_chance = MathUtils.Clamp(wound_chance, 10, 90);
            //    if (true)  // TODO: Replace debugging code
            //               //                if (RandomUtil.RandomChance(wound_chance))
            //    {
            //        // Determine wound severity
            //        // Would severity is also offset by skill. This may not be the best way to do it.

            //        int light_wound_chance = 10 + (2 * fight_level);
            //        int medium_wound_chance = 10 + fight_level;
            //        int grave_wound_chance = 10;
            //        int wound_type = RandomUtil.GetInt(light_wound_chance + medium_wound_chance + grave_wound_chance);
            //        if (wound_type <= light_wound_chance)
            //        {
            //            fighter.BuffManager.AddElement(BuffEWMinorWound.StaticGuid,
            //                (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
            //        }
            //        else if (wound_type <= (light_wound_chance + medium_wound_chance))
            //        {
            //            fighter.BuffManager.AddElement(BuffEWSeriousWound.StaticGuid,
            //                (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
            //        }
            //        else
            //        {
            //            fighter.BuffManager.AddElement(BuffEWGraveWound.StaticGuid,
            //                (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
            //        }
            //    }
            //}
        }

        public bool DoesLoserDie()
        {
            // TODO: If the loser has Grave Wound moodlet or runs out of fatigue, they die
            return false;
        }

        public static void FightBroadcastCallback(Sim s, ReactionBroadcaster rb)
        {
            ReactToDisturbance.NoiseBroadcastCallback(s, rb.BroadcastingObject as GameObject, rb.IsFirstTime(s),
                Origin.FromPetsFighting, isCryingBabyBuffInfinite: false);
        }
    }
}
