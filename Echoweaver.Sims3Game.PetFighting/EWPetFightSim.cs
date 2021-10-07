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
                return "Fight";
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

        bool targetRunOnLose = false;
        bool actorRunOnLose = false;


        public void SetParams(bool pTargetRunOnLose, bool pActorRunOnLose)
        {
            targetRunOnLose = pTargetRunOnLose;
            actorRunOnLose = pActorRunOnLose;
        }

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
            mCurrentStateMachine = StateMachineClient.Acquire(Actor, "ChaseMean", AnimationPriority.kAPDefault);
            mCurrentStateMachine.SetActor("x", Actor);
            mCurrentStateMachine.SetActor("y", Target);
            mCurrentStateMachine.EnterState("x", "Enter");
            mCurrentStateMachine.EnterState("y", "Enter");
            AnimateJoinSims("Face Off");
            bool flag = true;
            //            bool flag = DoTimedLoop(RandomUtil.GetFloat(kFightTimeMinMax[0], kFightTimeMinMax[1]), ExitReason.Default);
            AnimateJoinSims("Face Off");
            AnimateJoinSims("Face Off");
            AnimateJoinSims("Exit");
            EndCommodityUpdates(flag);
            LinkedInteractionInstance.EndCommodityUpdates(flag);
            bool actorWon = DoesActorWinFight();
            if (!actorWon)
            {
                skillActor.lostFight();
                Actor.BuffManager.AddElement(BuffNames.CatScratch, Origin.FromFight);
            }
            else
            {
                skillActor.wonFight();
                Target.BuffManager.AddElement(BuffNames.ShreddedDignity, Origin.FromFight);
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
                }
                else
                {
                    Target.RequestWalkStyle(WalkStyle.PetRun);
                }
                MakeSimGoHome(Target, false);
            } else if (!actorWon)
            {
                InteractionInstance test = RunAwayFromSim.Singleton.CreateInstance(Actor,
                    Target, new InteractionPriority(InteractionPriorityLevel.High), true, false);
                Target.InteractionQueue.TryPushAsContinuation(this, test);
            }
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
