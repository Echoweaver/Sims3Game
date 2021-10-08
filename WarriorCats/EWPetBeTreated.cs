using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.Controller;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using static Sims3.UI.StyledNotification;


// This is not intended to be a user interaction. This is for pets treated by a medical cat to eat their
// herb treatment.

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetBeTreated : Interaction<Sim, IGameObject>
	{
		public class Definition : InteractionDefinition<Sim, IGameObject, EWPetBeTreated>
		{
			public override bool Test(Sim a, IGameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}
		}

		public bool mSuccess;
		public BuffNames mBuffID;
		public Sim mMedicineCat;
		public bool mDestroyPrey;

		public static InteractionDefinition Singleton = new Definition();

		public void SetParams(bool pSuccess, BuffNames pBuffID, Sim pMedicineCat, bool pDestroyPrey)
		{
			mSuccess = pSuccess;
			mBuffID = pBuffID;
			mMedicineCat = pMedicineCat;
			mDestroyPrey = pDestroyPrey;
		}

		[TunableComment("The maximum distance the treated pet must be to interact with the Medicine Pet.")]
		[Tunable]
		public static float kMaxDistanceForSimToReact = 10f;

		[Tunable]
		[TunableComment("The LTR gain for a positive reaction from successful treatment.")]
		public static float kLtrGainForSuccess = 20f;

		[Tunable]
		[TunableComment("The LTR loss for a negative reaction from failed treatment.")]
		public static float kLtrLossForFail = 10f;


		public override bool Run()
		{
			bool flag = false;
			float distanceToObjectSquared = Actor.GetDistanceToObjectSquared(Target);
			StandardEntry();
			Target.DisableInteractions();
			CASAgeGenderFlags species = Actor.SimDescription.Species;
			if ((int)species <= 1024)
			{
				if ((int)species != 768)
				{
					if ((int)species == 1024)
					{
						flag = DogBehavior(distanceToObjectSquared);
					}
					flag = false;
					StandardExit();
					return flag;
				}
				flag = CatBehavior(distanceToObjectSquared);
			}
			else
			{
				if ((int)species == 1280)
				{
					flag = DogBehavior(distanceToObjectSquared);
				}
				if ((int)species != 1792)
				{
					flag = false;
					StandardExit();
					return flag;
				}
				flag = SharedNearDistanceBehavior(PetEatPrey.kCatEatingDistance, 3f);
			}
			StandardExit();
			return flag;
		}

		public override void Cleanup()
		{
			if (mDestroyPrey)
			{
				DestroyObject(Target);  
			}
			base.Cleanup();
		}

		public bool SharedFarDistanceBehavior(float routingDistance)
		{
			Actor.ShowTNSIfSelectable("Shared Far Distance Behavior",
				NotificationStyle.kGameMessagePositive);
			RequestWalkStyle(Sim.WalkStyle.PetRun);
			bool result = Actor.RouteToObjectRadius(Target, routingDistance);
			UnrequestWalkStyle(Sim.WalkStyle.PetRun);
			return result;
		}

		public bool SharedNearDistanceBehavior(float routingDistance, float loopTime)
		{
			if (!Actor.RouteTurnToFace(Target.Position))
			{
				return false;
			}
			MedicineCatIdle();
			EnterStateMachine("eatofffloor", "Enter", "x");
			SetParameter("isFish", false);
			BeginCommodityUpdates();
			AnimateSim("EatOffFloorLoop");
			bool flag = DoTimedLoop(loopTime, ExitReason.Default);
			EndCommodityUpdates(flag);
			mDestroyPrey = true;
			AnimateSim("Exit");

			if (mSuccess)
            {
				Actor.ShowTNSIfSelectable("EWLocalize - Successful treatment",
					NotificationStyle.kGameMessagePositive);
				Actor.ShowTNSIfSelectable("Remove buff " + mBuffID,
					NotificationStyle.kGameMessagePositive);
				Actor.BuffManager.RemoveElement(mBuffID);
				Actor.ShowTNSIfSelectable("Removed buff.",
					NotificationStyle.kGameMessagePositive);
				if (Actor.GetDistanceToObjectSquared(mMedicineCat) <= kMaxDistanceForSimToReact
								* kMaxDistanceForSimToReact)
				{
					Actor.ShowTNSIfSelectable("Say Thank you.",
						NotificationStyle.kGameMessagePositive);
					// Say thank you
					SocialInteractionA.Definition definition2 = new SocialInteractionA.Definition("Nuzzle Auto Accept",
						new string[0], null, initialGreet: false);
					InteractionInstance nuzzleInteraction = definition2.CreateInstance(Actor, mMedicineCat,
						new InteractionPriority(InteractionPriorityLevel.UserDirected), Autonomous,
						CancellableByPlayer);
					Actor.InteractionQueue.AddNextIfPossible(nuzzleInteraction);
				}
				Actor.ShowTNSIfSelectable("Adjust .",
					NotificationStyle.kGameMessagePositive);
				DoLtrAdjustment(goodReaction: true);
			}
			else
            {
				Actor.ShowTNSIfSelectable("EWLocalize - Failed treatment",
					NotificationStyle.kGameMessagePositive);
				DoLtrAdjustment(goodReaction: false);
			}
			return flag;
		}

		public bool DogBehavior(float distanceSquared)
		{
			// Keeping this around on the theory of offering ITUN option to enable for dogs

			bool flag = (int)Actor.SimDescription.Species == 1280;
			if (distanceSquared > PetEatPrey.kDistanceForDogSniffingBehavior
				* PetEatPrey.kDistanceForDogSniffingBehavior)
			{
				if (!SharedFarDistanceBehavior(PetEatPrey.kDistanceFromPreyForDogToSniffAir))
				{
					return false;
				}
				Actor.PlayReaction(ReactionTypes.Sniff, ReactionSpeed.ImmediateWithoutOverlay);
			}
			float num = 0f;
			num = ((!flag) ? (Actor.IsPuppy ? PetEatPrey.kPuppyEatingDistance : PetEatPrey.kDogEatingDistance)
				: (Actor.IsPuppy ? PetEatPrey.kLittlePuppyEatingDistance : PetEatPrey.kLittleDogEatingDistance));
			return SharedNearDistanceBehavior(num, PetEatPrey.kSimMinutesForDogToEatPrey);
		}

		public bool CatBehavior(float distanceSquared)
		{
			if (Target.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Fish
				&& distanceSquared > PetEatPrey.kDistanceForCatHuntingBehavior
				* PetEatPrey.kDistanceForCatHuntingBehavior)
			{
				if (!SharedFarDistanceBehavior(PetEatPrey.kDistanceFromPreyForCatToHunting))
				{
					return false;
				}
				CatchPrey catchPrey = CatchPrey.Singleton.CreateInstance(Target, Actor,
					GetPriority(), base.Autonomous, base.CancellableByPlayer) as CatchPrey;
				catchPrey.FromEatPreyInteraction = true;
				if (Actor.InteractionQueue.TryPushAsContinuation(this, catchPrey))
				{
					return true;
				}
			}
			return SharedNearDistanceBehavior(Actor.IsKitten ? PetEatPrey.kKittenEatingDistance
				: PetEatPrey.kCatEatingDistance, PetEatPrey.kSimMinutesForCatToEatPrey);
		}

		public void DoLtrAdjustment(bool goodReaction)
		{
			float num = !goodReaction ? (0f - kLtrLossForFail) : kLtrGainForSuccess;
			Relationship relationship = Relationship.Get(Actor, mMedicineCat,
				createIfNone: true);
			LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
			float currentLTRLiking = relationship.CurrentLTRLiking;
			relationship.LTR.UpdateLiking(num);
			LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
			float currentLTRLiking2 = relationship.CurrentLTRLiking;
			bool isPositive = currentLTRLiking2 >= currentLTRLiking;
			SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
				Actor, mMedicineCat, isPositive, 0, currentLTR, currentLTR2);
		}

		public void MedicineCatIdle()
		{
			ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(Actor.GetThumbnailKey());
			balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
			balloonData.mPriority = ThoughtBalloonPriority.Low;
			balloonData.mFlags = ThoughtBalloonFlags.ShowIfSleeping;
			Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
			AcquireStateMachine("catdoginvestigate");
			EnterStateMachine("catdoginvestigate", "Enter", "x");
			AnimateSim("Investigate");
			AnimateSim("Exit");
			if (mSuccess)
            {
				mMedicineCat.PlayReaction(ReactionTypes.PositivePet, ReactionSpeed.ImmediateWithoutOverlay);
            } else
            {
				mMedicineCat.PlayReaction(ReactionTypes.NegativePet, ReactionSpeed.ImmediateWithoutOverlay);
			}
			return;
		}
	}
}
