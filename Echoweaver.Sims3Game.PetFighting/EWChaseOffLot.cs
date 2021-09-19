using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{

    public class EWChaseOffLot : ChaseBaseClass
	{
		public enum OutcomeType
		{
			Succeed,
			Fight,
			Reversal,
			ChaseAgain
		}

		public class EWChaseOffLotDefinition : Definition
		{
			public EWChaseOffLotDefinition()
				: base("Chase Mean", new string[0], null, initialGreet: false)
			{
			}

			public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
			{
				ChaseBaseClass chaseBaseClass = new EWChaseOffLot();
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

			public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
			{
				return "EWChaseOffLot";
			}

			public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				// You can only chase a sim off your home lot
				if (actor.LotCurrent != actor.LotHome)
					return false;
				if (target.LotCurrent == target.LotHome)
					return false;
				if (!actor.IsRaccoon && !target.IsRaccoon)
				{
					if (SocialComponent.IsInServicePreventingSocialization(actor)
						|| SocialComponent.IsInServicePreventingSocialization(target))
					{
						return false;
					}
				}
				return base.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
			}
		}

		[TunableComment("Successful chance increase/decrease per difference in fight skill level between the 2 sims")]
		[Tunable]
		public static int kIntimidateChanceBonusPerSkillLevelDiff = 2;

		[Tunable]
		[TunableComment("Base Weighting for these outcomes [Fight, ChaseAgain, ReverseRole, Scold, Succeed], Note Humans can't reverse role and pets wont scold.")]
		public static float[] kBaseOutcomeWeights = new float[3] {
			1f,
			1f,
			1f
		};

		[TunableComment("Traits that make sim more likely to inimidate or be intimidated.")]
		[Tunable]
		public static TraitNames[] kIntimidateModifierTraits = new TraitNames[12] {
			TraitNames.AggressivePet,
			TraitNames.MeanPet,
			TraitNames.ShyPet,
			TraitNames.SkittishPet,
			TraitNames.Coward,
			TraitNames.Brave,
			TraitNames.HunterPet,
			TraitNames.Daredevil,
			TraitNames.EasilyImpressed,
			TraitNames.MeanSpirited,
			TraitNames.Rebellious,
			TraitNames.Shy
		};

		[TunableComment("Weight for how intimidating a sim is.")]
		[Tunable]
		public static float[] kIntimidateModifierWeights = new float[12] {
			1f,
			1f,
			-1f,
			-1f,
			-1f,
			1f,
			1f,
			1f,
			-1f,
			1f,
			1f,
			-1f
		};

		[TunableComment("Traits that make sim harder to intimidate but not more intimidating.")]
		[Tunable]
		public static TraitNames[] kIntimidateResistanceTraits = new TraitNames[4] {
			TraitNames.IndependentPet,
			TraitNames.GeniusPet,
			TraitNames.Genius,
			TraitNames.AnimalLover
		};
		[TunableComment("Traits that make sim harder to intimidate but not more intimidating.")]
		[Tunable]
		public static float[] kIntimidateResistanceWeights = new float[4] {
			1f,
			1f,
			1f,
			1f
		};
		// Cat/Dog Person for the appropriate animal.

		[TunableComment("Max number of times this interaction can be pushed again.")]
		[Tunable]
		public static int kMaxNumLoops = 5;

		public static InteractionDefinition Singleton = new EWChaseOffLotDefinition();

		public override string SocialName => "Chase Mean";

		public override WalkStyle SimAWalkStyle => WalkStyle.PetRun;

		EWPetFightingSkill skillActor;

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

        public override bool Run()
        {
			skillActor = Actor.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
			if (skillActor == null)
			{
				skillActor = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
				if (skillActor == null)
				{
					return false;
				}
			}
			skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
			bool returnVal = base.Run();
			skillActor.StopSkillGain();
			return returnVal;
		}

		public override void RunPostChaseBehavior()
		{
			skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
			OutcomeType outcomeType = OutcomeType.Succeed;
				outcomeType = (OutcomeType)RandomUtil.GetWeightedIndex(GetOutcomeWeights());
				StyledNotification.Show(new StyledNotification.Format("Outcome " + outcomeType,
					StyledNotification.NotificationStyle.kDebugAlert));
			PlayFaceoffAnims(false);
			switch (outcomeType)
			{
				case OutcomeType.Succeed:
                    if (Target.LotCurrent != Target.LotHome)
                    {
						if (Target.IsHuman)
                        {
							Target.RequestWalkStyle(WalkStyle.MeanChasedRun);
						} else
                        {
							Target.RequestWalkStyle(WalkStyle.PetRun);
						}
						// Success! Actor drove the unwanted sim off the lot.
						StyledNotification.Show(new StyledNotification.Format("GO home",
							StyledNotification.NotificationStyle.kDebugAlert));
						skillActor.AddPoints(200f, true, true);   // Successful driving off a foe gains skill
						MakeSimGoHome(Target, false);
                    }

					break;
				case OutcomeType.Fight:
                    if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default))
					{
						if (Target.IsHuman)
						{
							EWPetAttackSim continuation = EWPetAttackSim.Singleton.CreateInstance(Target, Actor,
								new InteractionPriority(InteractionPriorityLevel.High), Autonomous,
								cancellableByPlayer: true) as EWPetAttackSim;
							continuation.SetParams(true, false);
							Actor.InteractionQueue.TryPushAsContinuation(this, continuation);
						}
						else
						{ 
							EWFightPet continuation = EWFightPet.Singleton.CreateInstance(Target, Actor,
								new InteractionPriority(InteractionPriorityLevel.High), Autonomous,
								cancellableByPlayer: true) as EWFightPet;
							continuation.SetParams(true, false);
							Actor.InteractionQueue.TryPushAsContinuation(this, continuation);
						}
					}
					break;
				case OutcomeType.Reversal:
					// In this situation, a pet target will mean chase the actor and a human target will scold
					
					PlayFaceoffAnims(true);
					if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default))
					{
						if (Target.IsHuman)
                        {
							// Scold naughty actor
							PlayScoldAnims();
                        }
						else
                        {
                            EWChaseMean chaseMean = InteractionUtil.CreateInstance(this, Singleton, Actor, Target) as EWChaseMean;
							if (chaseMean != null)
							{
								chaseMean.PreviouslyAccepted = true;
								chaseMean.NumLoops = Math.Max(0, NumLoops - 1);
								Target.InteractionQueue.TryPushAsContinuation(this, chaseMean);
							}
						}
					}
					break;
				case OutcomeType.ChaseAgain:
					if (!Actor.HasExitReason(ExitReason.Default) && !Target.HasExitReason(ExitReason.Default) && base.NumLoops > 0)
					{
						EWChaseOffLot chaseMean = InteractionUtil.CreateInstance(this, Singleton, Target, Actor) as EWChaseOffLot;
						if (chaseMean != null)
						{
							chaseMean.PreviouslyAccepted = true;
							chaseMean.NumLoops = Math.Max(0, NumLoops - 1);
							Actor.InteractionQueue.TryPushAsContinuation(this, chaseMean);
						}
					}
					break;
			}
			EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Actor, Target, "Chase Mean", wasRecipient: false, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
			EventTracker.SendEvent(new SocialEvent(EventTypeId.kSocialInteraction, Target, Actor, "Chase Mean", wasRecipient: true, wasAccepted: true, actorWonFight: false, CommodityTypes.Undefined));
			skillActor.StopSkillGain();
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
			mCurrentStateMachine = StateMachineClient.Acquire(sim, "ChaseMean", AnimationPriority.kAPDefault);
			mCurrentStateMachine.SetActor("x", sim);
			mCurrentStateMachine.SetActor("y", sim2);
			mCurrentStateMachine.EnterState("x", "Enter");
			mCurrentStateMachine.EnterState("y", "Enter");
			BeginCommodityUpdates();
			AnimateJoinSims("Face Off");
			AnimateJoinSims("Exit");
			EndCommodityUpdates(succeeded: true);
		}

		public void PlayScoldAnims()
		{
			mCurrentStateMachine = StateMachineClient.Acquire(Target, "ChaseMean", AnimationPriority.kAPDefault);
			mCurrentStateMachine.SetActor("x", Target);
			mCurrentStateMachine.SetActor("y", Actor);
			mCurrentStateMachine.EnterState("x", "Enter");
			mCurrentStateMachine.EnterState("y", "Enter");
			BeginCommodityUpdates();
			AnimateJoinSims("Scold");
			AnimateJoinSims("Exit");
			EndCommodityUpdates(succeeded: true);
		}

		public float[] GetOutcomeWeights()
		{
			int actorSkill = Math.Max(0, Actor.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));

			int targetSkill = 0;
			if (Target.IsHuman)
			{
				targetSkill = Math.Max(0, Target.SkillManager.GetSkillLevel(SkillNames.MartialArts));
			} else if (Target.IsCat || Target.IsADogSpecies)
            {
				targetSkill = Math.Max(0, Target.SkillManager.GetSkillLevel(EWPetFightingSkill.skillNameID));
			}

			int intimidateModifier = (actorSkill - targetSkill) * kIntimidateChanceBonusPerSkillLevelDiff;
			int animalAffinity = 0;
			if (Actor.IsCat && Target.HasTrait(TraitNames.CatPerson))
            {
				animalAffinity = 1;
            }
			if (Actor.IsADogSpecies && Target.HasTrait(TraitNames.DogPerson))
            {
				animalAffinity = 1;
            }

			float[] chaseWeights = new float[4]; // Number of Outcome Types
			for (int i = 0; i < 3; i++)
			{
				chaseWeights[i] = kBaseOutcomeWeights[i];
			}

			// Chance of successfully chasing target off lot.
			chaseWeights[0] += intimidateModifier;
			for (int j = 0; j < kIntimidateModifierTraits.Length; j++)
			{
				if (Actor.HasTrait(kIntimidateModifierTraits[j]))
				{
					chaseWeights[0] += kIntimidateModifierWeights[j];
					chaseWeights[2] -= kIntimidateModifierWeights[j]; // Chance of switching roles

				}
				if (Target.HasTrait(kIntimidateModifierTraits[j]))
				{
					chaseWeights[0] -= kIntimidateModifierWeights[j];
					chaseWeights[1] += kIntimidateModifierWeights[j];  // Chance of fighting if chase unsuccesful
					chaseWeights[2] += kIntimidateModifierWeights[j]; 
				}
			}
			for (int j = 0; j < kIntimidateResistanceTraits.Length; j++)
			{
				if (Target.HasTrait(kIntimidateResistanceTraits[j]))
				{
					chaseWeights[0] -= kIntimidateResistanceWeights[j];
					chaseWeights[2] += kIntimidateResistanceWeights[j];  
				}
			}

			// Animal people less likely to run from or fight their animal and more likely to get the upper hand
			chaseWeights[0] -= animalAffinity;
			chaseWeights[1] -= animalAffinity;
			chaseWeights[2] += animalAffinity;

			chaseWeights[3] = 3f;  // Always the same chance for chase again, at least for now.

			StyledNotification.Show(new StyledNotification.Format("Succeed " + chaseWeights[0] + " Fight " + chaseWeights[1]
				+ " Reverse " + chaseWeights[2] + " Repeat " + chaseWeights[3],
				StyledNotification.NotificationStyle.kDebugAlert));

			return chaseWeights;
		}

	}


	//public class EWChaseOffLot : Interaction<Sim, Sim>
	//{
	//	public class Definition : InteractionDefinition<Sim, Sim, EWChaseOffLot>
	//	{
	//		public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
	//		{
	//			return ((a.LotCurrent == a.LotHome) && (target.LotCurrent != target.LotHome));
	//		}
	//	}

	//	public static InteractionDefinition Singleton = (InteractionDefinition)(object)new Definition();

	//	[TunableComment("The dog will chase the mailman for a random number of minutes within this range.  Note the mailmans route is not guaranteed to last this long, in which case the dog will stop earlier.")]
	//	[Tunable]
	//	public static float[] kChaseLength = new float[2] {
	//	3f,
	//	7f
	//};

	//	[Tunable]
	//	[TunableComment("How much the LTR Liking between the dog and mailman will go down after the chase.")]
	//	public static int kLTRLoss = 20;

	//	public Vector3 TargetDestination;

	//	public float TargetRouteTime;

	//	public Route r;

	//	public StateMachineClient mBarkOverlaySMC;

	//	public override bool Run()
	//	{

	//		r = Actor.CreateRoute();
	//		r.SetOption(RouteOption.EnablePlanningAsCar, false);
	//		r.PlanToPointRadialRange(TargetDestination, 1f, 5f);
	//		RoutePlanResult planResult = r.PlanResult;
	//		if (planResult.Succeeded())
	//		{
	//			string locomotionStateForWalkStyle = SimWalkStyleRules.GetLocomotionStateForWalkStyle(WalkStyle.PetRun);
	//			SimDescription simDescription = Actor.SimDescription;
	//			RouteAnimationSpec val = new RouteAnimationSpec(locomotionStateForWalkStyle, (uint)simDescription.Age,
	//                   (uint)simDescription.Gender, (uint)simDescription.Species);
	//			float estimatedTravelTime = r.GetEstimatedTravelTime(0u, Actor.ObjectId, val);
	//			float num = TargetRouteTime - estimatedTravelTime;
	//			float num2 = SimClock.ConvertFromTicks((long)Math.Abs(num), TimeUnit.Minutes);
	//			if (!(num > 0f))
	//			{
	//				_ = 0.1f;
	//			}
	//			BalloonData val2 = new BalloonData(Target.GetThumbnailKey());
	//			val2.BalloonType = ThoughtBalloonTypes.kScreamBalloon;
	//			val2.Duration = ThoughtBalloonDuration.Medium;
	//			val2.mPriority = ThoughtBalloonPriority.High;
	//			Actor.ThoughtBalloonManager.ShowBalloon(val2);
	//			BeginCommodityUpdates();
	//			mBarkOverlaySMC = StateMachineClient.Acquire(Actor, "dogbarkoverlay", AnimationPriority.kAPDefault);
	//			mBarkOverlaySMC.SetActor("x", (IHasScriptProxy)(object)base.Actor);
	//			mBarkOverlaySMC.EnterState("x", "Enter");
	//			mBarkOverlaySMC.RequestState("x", "Bark");
	//			RequestWalkStyle(WalkStyle.PetRun);
	//			AlarmManager.Global.AddAlarm(RandomUtil.GetFloat(kChaseLength[0], kChaseLength[1]), TimeUnit.Minutes,
	//				new AlarmTimerCallback(StopChase), "Dog Chase Mailman Alarm", AlarmType.NeverPersisted, Actor);
	//			Actor.DoRoute(r);
	//			EventTracker.SendEvent(EventTypeId.kChaseMailman, Actor);
	//			EndCommodityUpdates(true);
	//			Relationship val3 = Relationship.Get(Actor, Target, true);
	//			val3.LTR.UpdateLiking(-kLTRLoss);
	//			Actor.ThoughtBalloonManager.KillBalloon(val2);
	//			mBarkOverlaySMC.RequestState("x", "Exit");
	//		}
	//		if (Target.LotCurrent != Target.LotHome)
	//		{
	//			MakeSimGoHome(Target, false);
	//		}
	//		return true;
	//	}

	//	public void StopChase()
	//	{
	//		if (r != null && r.IsActive)
	//		{
	//			Actor.AddExitReason(ExitReason.Finished);
	//		}
	//	}
	//}
}
