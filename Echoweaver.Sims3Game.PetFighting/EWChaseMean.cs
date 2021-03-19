using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{

    public class EWChaseMean : ChaseBaseClass
    {
        public enum OutcomeType
        {
            Fight,
            ChaseAgain,
            ReverseRoles,
            Scold
        }

        public class EWChaseMeanDefinition : Definition
        {
            public EWChaseMeanDefinition()
                : base("Chase Mean", new string[0], null, initialGreet: false)
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                ChaseBaseClass chaseBaseClass = new EWChaseMean();
                chaseBaseClass.Init(ref parameters);
                chaseBaseClass.IsMeanChase = true;
                return chaseBaseClass;
            }

            public override string[] GetPath(bool isFemale)
            {
                return new string[1] {
                Localization.LocalizeString (ActionData.GetParentMenuLocKey (ActionDataBase.ParentMenuType.Mean))
            };
            }

            public override float CalculateScore(InteractionObjectPair interactionObjectPair, Sims3.Gameplay.Autonomy.Autonomy autonomy)
            {
                return CalculateScoreWithInteractionTuning(interactionObjectPair, autonomy, kSocialTuningScoreWeight, kInteractionTuningScoreWeight);
            }

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                //IL_002d: Unknown result type (might be due to invalid IL or missing references)
                //IL_0033: Invalid comparison between Unknown and I4
                if (Boat.IsEitherSimInABoat(actor, target))
                {
                    return false;
                }
                if (!actor.IsRaccoon && !target.IsRaccoon)
                {
                    Relationship relationship = Relationship.Get(actor, target, createIfNone: false);
                    if (relationship == null || (int)relationship.LTR.CurrentLTR == 1)
                    {
                        return false;
                    }
                    if (actor.NeedsToBeGreeted(target))
                    {
                        return false;
                    }
                    if (SocialComponent.IsInServicePreventingSocialization(actor) || SocialComponent.IsInServicePreventingSocialization(target))
                    {
                        return false;
                    }
                }
                return base.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
            }
        }

        [Tunable]
        [TunableComment("Base Weighting for these outcomes [Fight, ChaseAgain, ReverseRole, Scold], Note Humans can only chase again or scold and pets wont scold.")]
        public static float[] kBaseOutcomeWeights = new float[4] {
        1f,
        1f,
        1f,
        1f
    };

        [TunableComment("Trait overrides for Fight outcome.")]
        [Tunable]
        public static TraitNames[] kFightOutcomeOverrideTraits = new TraitNames[4] {
        TraitNames.AggressivePet,
        TraitNames.MeanPet,
        TraitNames.ShyPet,
        TraitNames.SkittishPet
    };

        [TunableComment("Weighting override for fight outcome for coresponding tuned traits.")]
        [Tunable]
        public static float[] kFightOutcomeOverrideWeights = new float[4] {
        3f,
        3f,
        0f,
        0f
    };

        [TunableComment("Trait overrides for Chase again outcome.")]
        [Tunable]
        public static TraitNames[] kChaseAgainOutcomeOverrideTraits = new TraitNames[3] {
        TraitNames.ShyPet,
        TraitNames.Coward,
        TraitNames.Brave
    };

        [Tunable]
        [TunableComment("Weighting override for Chase again outcome for coresponding tuned traits.")]
        public static float[] kChaseAgainOutcomeOverrideWeights = new float[3] {
        3f,
        3f,
        0f
    };

        [Tunable]
        [TunableComment("Trait overrides for ReverseRole outcome.")]
        public static TraitNames[] kReverseRoleOutcomeOverrideTraits = new TraitNames[2] {
        TraitNames.AggressivePet,
        TraitNames.ShyPet
    };

        [TunableComment("Weighting override for Reverse Role outcome for coresponding tuned traits.")]
        [Tunable]
        public static float[] kReverseRoleOutcomeOverrideWeights = new float[2] {
        2f,
        1f
    };

        [Tunable]
        [TunableComment("Trait overrides for Scold outcome.")]
        public static TraitNames[] kScoldOutcomeOverrideTraits = new TraitNames[2] {
        TraitNames.Coward,
        TraitNames.Brave
    };

        [TunableComment("Weighting override for scold outcome for coresponding tuned traits.")]
        [Tunable]
        public static float[] kScoldOutcomeOverrideWeights = new float[2] {
        0f,
        2f
    };

        [TunableComment("Max number of times this interaction can be pushed again.")]
        [Tunable]
        public static int kMaxNumLoops = 5;

        [Tunable]
        [TunableComment("Base chanse of pushing mean chase social after Bark AT, Hiss, Growl At, Pounce.")]
        public static int kBaseChancePushMeanChase = 30;

        [TunableComment("Trait overrides for Chance of pushing mean chase social after Bark AT, Hiss, Growl AT, Pounce.")]
        [Tunable]
        public static TraitNames[] kChancePushMeanChaseOverrideTraits = new TraitNames[6] {
        TraitNames.FriendlyPet,
        TraitNames.SkittishPet,
        TraitNames.ShyPet,
        TraitNames.AggressivePet,
        TraitNames.MeanPet,
        TraitNames.HunterPet
    };

        [Tunable]
        [TunableComment("Range 0-100: Chance override for pusing for mean chase for coresponding tuned traits.")]
        public static int[] kChancePushMeanChaseOverrideChances = new int[6] {
        0,
        0,
        0,
        50,
        50,
        30
    };

        [Tunable]
        [TunableComment("Weighting of Social tuning when autonomously choosing to do this interaction")]
        public static float kSocialTuningScoreWeight = 1f;

        [Tunable]
        [TunableComment("Weighting of Interaction tuning tool tuning when autonomously choosing to do this interaction")]
        public static float kInteractionTuningScoreWeight = 1f;

        public static InteractionDefinition Singleton = new EWChaseMeanDefinition();

        public override string SocialName => "Chase Mean";

        public override WalkStyle SimAWalkStyle => WalkStyle.PetRun;

        public override WalkStyle SimBWalkStyle
        {
            get
            {
                if (!Target.IsHuman)
                {
                    return WalkStyle.PetRun;
                }
                return WalkStyle.MeanChasedRun;
            }
        }

        public override int MaxNumLoops => kMaxNumLoops;

        public override void RunPostChaseBehavior()
        {
            OutcomeType outcomeType = OutcomeType.ChaseAgain;
            if (PreviouslyAccepted)
            {
                outcomeType = (OutcomeType)RandomUtil.GetWeightedIndex(GetOutcomeWeights());
            }
            switch (outcomeType)
            {
                case OutcomeType.Fight:
                    if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default))
                    {
                        FightPet continuation = FightPet.Singleton.CreateInstance(Target, Actor, new InteractionPriority(InteractionPriorityLevel.High), base.Autonomous, cancellableByPlayer: true) as FightPet;
                        Actor.InteractionQueue.TryPushAsContinuation(this, continuation);
                    }
                    break;
                case OutcomeType.ChaseAgain:
                    PlayFaceoffAnims(reverseRoles: false);
                    if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default) && base.NumLoops > 0)
                    {
                        EWChaseMean chaseMean2 = InteractionUtil.CreateInstance(this, Singleton, Target, Actor) as EWChaseMean;
                        if (chaseMean2 != null)
                        {
                            chaseMean2.PreviouslyAccepted = true;
                            chaseMean2.NumLoops = base.NumLoops - 1;
                            Actor.InteractionQueue.TryPushAsContinuation(this, chaseMean2);
                        }
                    }
                    break;
                case OutcomeType.ReverseRoles:
                    PlayFaceoffAnims(reverseRoles: true);
                    if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default) && base.NumLoops > 0)
                    {
                        EWChaseMean chaseMean = InteractionUtil.CreateInstance(this, Singleton, Actor, Target) as EWChaseMean;
                        if (chaseMean != null)
                        {
                            chaseMean.PreviouslyAccepted = true;
                            chaseMean.NumLoops = base.NumLoops - 1;
                            Target.InteractionQueue.TryPushAsContinuation(this, chaseMean);
                        }
                    }
                    break;
                case OutcomeType.Scold:
                    PlayScoldAnims();
                    break;
            }
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "Chase Mean", wasRecipient: false, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "Chase Mean", wasRecipient: true, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
        }

        public void PlayFaceoffAnims(bool reverseRoles)
        {
            Sim sim = Actor;
            Sim sim2 = Target;
            if (reverseRoles)
            {
                sim = Target;
                sim2 = Actor;
            }
            mCurrentStateMachine = StateMachineClient.Acquire((IHasScriptProxy)(object)sim, "ChaseMean", (AnimationPriority)(-2));
            mCurrentStateMachine.SetActor("x", (IHasScriptProxy)(object)sim);
            mCurrentStateMachine.SetActor("y", (IHasScriptProxy)(object)sim2);
            mCurrentStateMachine.EnterState("x", "Enter");
            mCurrentStateMachine.EnterState("y", "Enter");
            BeginCommodityUpdates();
            AnimateJoinSims("Face Off");
            AnimateJoinSims("Exit");
            EndCommodityUpdates(succeeded: true);
        }

        public void PlayScoldAnims()
        {
            mCurrentStateMachine = StateMachineClient.Acquire((IHasScriptProxy)(object)Target, "ChaseMean", (AnimationPriority)(-2));
            mCurrentStateMachine.SetActor("x", (IHasScriptProxy)(object)Target);
            mCurrentStateMachine.SetActor("y", (IHasScriptProxy)(object)Actor);
            mCurrentStateMachine.EnterState("x", "Enter");
            mCurrentStateMachine.EnterState("y", "Enter");
            BeginCommodityUpdates();
            AnimateJoinSims("Scold");
            AnimateJoinSims("Exit");
            EndCommodityUpdates(succeeded: true);
        }

        public float[] GetOutcomeWeights()
        {
            float[] array = new float[4];
            for (int i = 0; i < 4; i++)
            {
                array[i] = kBaseOutcomeWeights[i];
            }
            for (int j = 0; j < kChaseAgainOutcomeOverrideTraits.Length; j++)
            {
                if (Target.HasTrait(kChaseAgainOutcomeOverrideTraits[j]))
                {
                    array[1] = kChaseAgainOutcomeOverrideWeights[j];
                }
            }
            if (Target.IsHuman)
            {
                array[0] = 0f;
                array[2] = 0f;
                for (int k = 0; k < kScoldOutcomeOverrideTraits.Length; k++)
                {
                    if (Target.HasTrait(kScoldOutcomeOverrideTraits[k]))
                    {
                        array[3] = kScoldOutcomeOverrideWeights[k];
                    }
                }
            }
            else
            {
                array[3] = 0f;
                for (int l = 0; l < kFightOutcomeOverrideTraits.Length; l++)
                {
                    if (Target.HasTrait(kFightOutcomeOverrideTraits[l]))
                    {
                        array[0] = kFightOutcomeOverrideWeights[l];
                    }
                }
                for (int m = 0; m < kReverseRoleOutcomeOverrideTraits.Length; m++)
                {
                    if (Target.HasTrait(kReverseRoleOutcomeOverrideTraits[m]))
                    {
                        array[2] = kReverseRoleOutcomeOverrideWeights[m];
                    }
                }
            }
            return array;
        }

        public static void RollDiceToPushMeanChase(Sim actor, Sim target, bool isAutonomous)
        {
            int num = kBaseChancePushMeanChase;
            for (int i = 0; i < kChancePushMeanChaseOverrideTraits.Length; i++)
            {
                if (actor.HasTrait(kChancePushMeanChaseOverrideTraits[i]))
                {
                    num = kChancePushMeanChaseOverrideChances[i];
                }
            }
            if (RandomUtil.RandomChance(num))
            {
                EWChaseMean instance = Singleton.CreateInstance(target, actor, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous, cancellableByPlayer: true) as EWChaseMean;
                actor.InteractionQueue.AddNextIfPossible(instance);
            }
        }
    }
}
