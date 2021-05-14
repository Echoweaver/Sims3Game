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
                if (isAutonomous)
                {
                    return a.TraitManager.HasAnyElement(kTraitsCanFightAutonomously);
                }
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

        public static InteractionDefinition Singleton = new EWPetAttackSimDefinition();

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
                skillTarget = Actor.SkillManager.AddElement(SkillNames.MartialArts) as MartialArts;
                if (skillTarget == null)
                {
                    return false;
                }
            }

            // TODO: There are accelerated gain rates for Hunter and Aggressive pets. Possibly slower for Nonaggressive and Skittish?
            skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
            skillTarget.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);

            float distanceToObject = Actor.GetDistanceToObject(Target);
            if (distanceToObject > kDistanceForPetFight)
            {
                Actor.RequestWalkStyle(WalkStyle.PetRun);
                Actor.RouteToObjectRadius(Target, kDistanceForPetFight);
                Actor.UnrequestWalkStyle(WalkStyle.PetRun);
            }

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

            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, GetInteractionName(), allowCarryChild: false), pairedSocial: true, doCallOver: false))
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
            StartSocial("EWPetAttackSim");
            BeginCommodityUpdates();
            ((SocialInteraction)LinkedInteractionInstance).Rejected = Rejected;
            mPetFightNoiseBroadcast = new ReactionBroadcaster(Actor, kPetFightLoudBroadcastParams, FightBroadcastCallback);
            PetStartleBehavior.CheckForStartle(Actor, StartleType.Fight);
            StandardEntry(addToUseList: false);
            EnterStateMachine("PetAttackSim", "Enter", "x");
            SetActor("y", Target);
            EnterState("x", "Enter");
            bool flag = true;
            //bool flag = DoTimedLoop(RandomUtil.GetFloat(kFightTimeMinMax[0], kFightTimeMinMax[1]), ExitReason.Default);
            AnimateSim("Exit");
            StandardExit(removeFromUseList: false);
            StyledNotification.Show(new StyledNotification.Format("Fight is over ",
                StyledNotification.NotificationStyle.kDebugAlert));

            //StartSocial("EWPetAttackSim");
            //BeginCommodityUpdates();
            //((SocialInteraction)LinkedInteractionInstance).Rejected = Rejected;
            //mPetFightNoiseBroadcast = new ReactionBroadcaster(Actor, kPetFightLoudBroadcastParams, FightBroadcastCallback);
            //PetStartleBehavior.CheckForStartle(Actor, StartleType.Fight);
            //StandardEntry(addToUseList: false);
            //EnterStateMachine("social_generic", "Enter", "x");
            //SetActor("y", Target);
            //SetParameter("AnimationSuite", "attackSim");
            //SetParameter("AnimX", "ac2a_soc_neutral_attackSim_insulting_neutral_x");
            //SetParameter("AnimY", "ac2a_soc_neutral_attackSim_insulting_neutral_y");
            //EnterState("x", "Enter");
            //AnimateSim("social");
            //StandardExit(removeFromUseList: false);
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
            EndCommodityUpdates(flag);
            LinkedInteractionInstance.EndCommodityUpdates(flag);
            //bool actorWon = DoesActorWinFight();
            bool actorWon = true;
            if (!actorWon)
            {
                // TODO: Victory/loss animation?
                //AnimateSim("Swap");
                skillActor.lostFight();
            }
            else
            {
                StyledNotification.Show(new StyledNotification.Format("The cat won.",
                    StyledNotification.NotificationStyle.kDebugAlert));
                skillActor.wonFight();
                

                // Drive target off the lot
                Target.RequestWalkStyle(WalkStyle.Run);
                if (Target.IsAtHome)
                {
                    List<Lot> list = LotManager.GetAllCommunityLots();
                    list.Remove(LotManager.ActiveLot);
                    Lot runLot = RandomUtil.GetRandomObjectFromList(list);
                    Target.RouteToLot(runLot.LotId);
                }
                else
                {
                    Target.RouteToLot(Target.LotHome.LotId);
                }
            }
//            AnimateSim("Exit");
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "Fight Pet", wasRecipient: false, wasAccepted: true, actorWon, CommodityTypes.Undefined));
            EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "Fight Pet", wasRecipient: true, wasAccepted: true, !actorWon, CommodityTypes.Undefined));

            if (mPetFightNoiseBroadcast != null)
            {
                mPetFightNoiseBroadcast.EndBroadcast();
                mPetFightNoiseBroadcast = null;
            }
            FinishSocial("EWPetAttackSim", bApplySocialEffect: true);
            skillActor.StopSkillGain();
            skillTarget.StopSkillGain();
            FinishLinkedInteraction();
            WaitForSyncComplete();

            // Must Assign wounds after checking for death because only a preexisting
            // grave wound should cause death upon losing.
            //AssignFightWounds(Actor, actorWon);
            //AssignFightWounds(Target, !actorWon);
            //return flag;
            return true;
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
            int actorSkillLevel = Math.Max(0, Actor.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
            int targetSkillLevel = Math.Max(0, Target.SkillManager.GetSkillLevel(SkillNames.MartialArts));
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
            winChance = MathUtils.Clamp(winChance, 0, 100);
            return RandomUtil.RandomChance(winChance);
        }

        public void AssignFightWounds(Sim fighter, bool wonFight)
        {
            if (!wonFight && fighter.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
            {
                // If the loser has Grave Wound moodlet or runs out of fatigue, they die 
                BuffEWGraveWound.Succumb(fighter);
            }

            // Chance of being wounded is calculated based on the sim's fight skill.
            // Could eventually take into account opponent sim or win/loss
            int wound_chance = kBaseWoundChance;
            int fight_level = Math.Max(0, fighter.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
            wound_chance -= kWoundChanceAdjPerSkillLevel * fight_level;
            wound_chance = MathUtils.Clamp(wound_chance, 10, 90);
            if (RandomUtil.RandomChance(wound_chance))
            {
                // Determine wound severity
                // Wound severity is also offset by skill. This may not be the best way to do it.

                int light_wound_chance = 10 + (2 * fight_level);
                int medium_wound_chance = 10 + fight_level;
                int grave_wound_chance = 10;
                int wound_type = RandomUtil.GetInt(light_wound_chance + medium_wound_chance + grave_wound_chance);
                if (wound_type <= light_wound_chance)
                {
                    fighter.BuffManager.AddElement(BuffEWMinorWound.StaticGuid,
                        (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
                }
                else if (wound_type <= (light_wound_chance + medium_wound_chance))
                {
                    fighter.BuffManager.AddElement(BuffEWSeriousWound.StaticGuid,
                        (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
                }
                else
                {
                    fighter.BuffManager.AddElement(BuffEWGraveWound.StaticGuid,
                        (Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
                }
            }

        }

        public static void FightBroadcastCallback(Sim s, ReactionBroadcaster rb)
        {
            ReactToDisturbance.NoiseBroadcastCallback(s, rb.BroadcastingObject as GameObject, rb.IsFirstTime(s),
                Origin.FromPetsFighting, isCryingBabyBuffInfinite: false);
        }
    }
}

//using System;
//using Sims3.Gameplay.Abstracts;
//using Sims3.Gameplay.Actors;
//using Sims3.Gameplay.ActorSystems;
//using Sims3.Gameplay.Autonomy;
//using Sims3.Gameplay.Core;
//using Sims3.Gameplay.EventSystem;
//using Sims3.Gameplay.Interactions;
//using Sims3.Gameplay.InteractionsShared;
//using Sims3.Gameplay.ObjectComponents;
//using Sims3.Gameplay.Services;
//using Sims3.Gameplay.Skills;
//using Sims3.Gameplay.Socializing;
//using Sims3.Gameplay.Utilities;
//using Sims3.SimIFace;
//using static Sims3.Gameplay.Actors.Sim;

//namespace Echoweaver.Sims3Game.PetFighting
//{
//    public class EWPetAttackSim : SocialInteractionA
//    {
//		[TunableComment("Fight win chance trait modifiers for human to increase or decrease chance of winning against pet")]
//		[Tunable]
//		public static TraitNames[] kHumanWinChanceModifyTraits = new TraitNames[8] {
//			TraitNames.Athletic,
//			TraitNames.Brave,
//			TraitNames.MeanSpirited,
//			TraitNames.Disciplined,
//			TraitNames.LycanthropyWerewolf,  // I'm going to say werewolf form is better at fighting with animals!
//			TraitNames.Clumsy,
//			TraitNames.Coward,
//			TraitNames.CouchPotato
//		};

//		[Tunable]
//		[TunableComment("Fight win chance for human increase/decrease matching coresponding trait list")]
//		public static int[] kHumanWinChanceModifyValues = new int[8] {
//			10,
//			10,
//			10,
//			10,
//			10,
//			-10,
//			-10,
//			-10
//		};

//		[Tunable]
//		[TunableComment("Distance pet must be from Human before initiating attack animations.")]
//		public static float kDistanceForAttack = CatHuntingComponent.PetEatPrey.kDistanceFromPreyForCatToHunting;

//		Sim petSim = new Sim();
//		Sim humanSim = new Sim();

//		public class EWPetAttackSimDefinition : Definition
//        {
//			public EWPetAttackSimDefinition()
//				: base("EWPetAttackSim", new string[0], null, initialGreet: false)
//			{
//			}
//			public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
//			{
//				SocialInteractionA socialInteractionA = new EWPetAttackSim();
//				socialInteractionA.Init(ref parameters);
//				return socialInteractionA;
//			}

//			public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//			{
//				if (isAutonomous)
//				{
//					return a.TraitManager.HasAnyElement(EWPetAttackSim.kTraitsCanFightAutonomously);
//				}
//				return true;
//			}

//			public override string[] GetPath(bool isFemale)
//			{
//				return new string[1] {
//					Localization.LocalizeString (ActionData.GetParentMenuLocKey (ActionDataBase.ParentMenuType.Mean))
//				};
//			}

//			public override float CalculateScore(InteractionObjectPair interactionObjectPair, Sims3.Gameplay.Autonomy.Autonomy autonomy)
//			{
//				return CalculateScoreWithInteractionTuning(interactionObjectPair, autonomy,
//					EWPetAttackSim.kSocialTuningScoreWeight, EWPetAttackSim.kInteractionTuningScoreWeight);
//			}

//			public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
//			{
//				// TODO: Localize
//				return "EWPetAttackSim";
//			}
//		}

//		public static InteractionDefinition Singleton = new EWPetAttackSimDefinition();

//		public override bool Run()
//		{
//			if (!SafeToSync())
//			{
//				return false;
//			}

//			if (Actor.IsPet)
//			{
//				petSim = Actor;
//				humanSim = Target;
//			}
//			else
//			{
//				petSim = Target;
//				humanSim = Actor;
//			}

//			float distanceToObjectSquared = Actor.GetDistanceToObjectSquared(Target);

//            EWPetFightingSkill petSkill = petSim.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
//			if (petSkill == null)
//			{
//				petSkill = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
//				if (petSkill == null)
//				{
//					return false;
//				}
//			}

//			MartialArts humanSkill = Target.SkillManager.GetSkill<MartialArts>(SkillNames.MartialArts);
//			if (humanSkill == null)
//			{
//				humanSkill = humanSim.SkillManager.AddElement(SkillNames.MartialArts) as MartialArts;
//				if (humanSkill == null)
//				{
//					return false;
//				}
//			}

//			// TODO: There are accelerated gain rates for Hunter and Aggressive pets.
//			// Possibly slower for Nonaggressive and Skittish?
//			petSkill.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
//			humanSkill.StartSkillGain(MartialArts.kMinimumMartialArtsSkillGainRateWhileSparring);

//			// Run until close. Then, if cat, start stalking and pounce.
//			petSim.RequestWalkStyle(WalkStyle.PetRun);
//			petSim.RouteToObjectRadius(Target, kDistanceForAttack);

//			if (petSim.IsCat)
//			{
//				petSim.UnrequestWalkStyle(WalkStyle.PetRun);
//				PouncePosture pouncePosture = PouncePosture.Create(petSim);
//				pouncePosture.EnterPounce();
//				petSim.Posture = pouncePosture;
//				petSim.RequestWalkStyle(WalkStyle.CatStalk);
//			}

//			if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, GetInteractionName(), allowCarryChild: false), pairedSocial: true, doCallOver: false))
//			{
//				if (petSim.IsCat)
//				{
//					UnrequestWalkStyle(petSim, WalkStyle.CatStalk);
//					PouncePosture pouncePosture2 = petSim.Posture as PouncePosture;
//					if (pouncePosture2 != null)
//					{
//						pouncePosture2.ExitPounce();
//						petSim.PopPosture();
//					}
//				}
//				return false;
//			}

//            StartSocial("EWPetAttackSim");
//			StandardEntry(addToUseList: false);
//			((SocialInteraction)LinkedInteractionInstance).Rejected = Rejected;
//            SocialBroadcaster = new ReactionBroadcaster(Actor, ReactToFightBroadcaster, ReactToFight);
//            PetStartleBehavior.CheckForStartle(Actor, StartleType.Fight);
//			EnterStateMachine("PetAttackSim", "Enter", "x");
//			SetActor("y", Target);
//			EnterState("x", "Enter");
//			AnimateSim("Exit");

//			//SetParameter("AnimationSuite", "attackSim");
//			//SetParameter("StartStanceX", "neutral");
//			//SetParameter("StartStanceY", "neutral");
//			//SetParameter("EndStanceX", "neutral");
//			//SetParameter("EndStanceY", "neutral");
//			//SetParameter("STEffectCommodity", "insulting");
//			//AnimateSim("social_complex");

//			//SetParameter("AnimX", "ac2a_soc_neutral_attackSim_insulting_neutral_x");
//			//SetParameter("AnimY", "ac2a_soc_neutral_attackSim_insulting_neutral_y");
//			//SetParameter("AnimZ", "ac2a_soc_neutral_attackSim_insulting_neutral_x");
//			//SetParameter("AnimW", "ac2a_soc_neutral_attackSim_insulting_neutral_y");
//			//AnimateSim("social");

//			//ActorWinsFight = DoesActorWinFight();
//			petSkill.StopSkillGain();
//			humanSkill.StopSkillGain();
//			StandardExit();
//			FinishSocial("EWPetAttackSim", bApplySocialEffect: true);
//			return true;
//		}

//		public bool DoesActorWinFight()
//		{

//			// Probably don't need this, but hey Bonehilda is a badass
//			if (Actor.SimDescription.IsBonehilda)
//			{
//				return true;
//			}
//			if (Target.SimDescription.IsBonehilda)
//			{
//				return false;
//			}

//			int petSkill = Math.Max(0, petSim.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
//			int humanSkill = Math.Max(0, humanSim.SkillManager.GetSkillLevel(SkillNames.MartialArts));
//			if (humanSim.SkillManager.HasElement(SkillNames.Athletic))
//			{
//				// Having Athletic skill grants a combat bonus -- maybe tunable
//				// TODO: Should it be tunable to use Athletic entirely for folks w/o EP1?
//				humanSkill += humanSim.SkillManager.GetSkillLevel(SkillNames.Athletic) / 2;
//			}

//			int winChance = EWPetAttackSim.kBaseWinChance;
//			winChance += (petSkill - humanSkill) * EWPetAttackSim.kWinChanceBonusPerSkillLevelDiff;

//			for (int index = 0; index < EWPetAttackSim.kWinChanceModifyTraits.Length; index++)
//			{
//				if (petSim.HasTrait(EWPetAttackSim.kWinChanceModifyTraits[index]))
//				{
//					winChance += EWPetAttackSim.kWinChanceModifyValues[index];
//				}
//			}
//			for (int index = 0; index < kHumanWinChanceModifyTraits.Length; index++)
//			{
//				if (humanSim.HasTrait(kHumanWinChanceModifyTraits[index]))
//				{
//					winChance -= kHumanWinChanceModifyValues[index];
//				}
//			}
//			winChance = MathUtils.Clamp(winChance, 0, 100);
//			bool petWinsFight = RandomUtil.RandomChance(winChance);
//			if (Actor.IsPet && petWinsFight)
//			{
//				return true;
//			} else if (Target.IsPet && !petWinsFight)
//            {
//				return true;
//            } else
//            {
//				return false;
//            }
//		}

//		public void AssignFightWoundsHuman(Sim fighter, bool wonFight)
//		{
//		}


//		private static void ReactToFight(Sim s, ReactionBroadcaster broadcaster)
//        {
//			// TODO: Is this the right reaction for a human attacked by a pet?
//			ReactToDisturbance.NoiseBroadcastCallback(s, broadcaster.BroadcastingObject as GameObject, broadcaster.IsFirstTime(s),
//				Origin.FromPetsFighting, isCryingBabyBuffInfinite: false);
//		}
//	}
//}
