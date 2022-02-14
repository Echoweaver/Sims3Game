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
		public bool treatmentComplete = false;

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

			bool flag = SharedNearDistanceBehavior(PetEatPrey.kCatEatingDistance, 1f);
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

		public bool SharedNearDistanceBehavior(float routingDistance, float loopTime)
		{
			EWWait.Definition waitDefinition = new EWWait.Definition();
			EWWait waitInstance = waitDefinition.CreateInstance(mMedicineCat, mMedicineCat,
				new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
				CancellableByPlayer) as EWWait;
			waitInstance.SetInteractionName("Treat " + Actor.Name);
			mMedicineCat.InteractionQueue.AddNext(waitInstance);

			if (!Actor.RouteToObjectRadius(Target, routingDistance))
			{
				waitInstance.waitComplete = true;
				return false;
			}
			EnterStateMachine("eatofffloor", "Enter", "x");
			SetParameter("isFish", false);
			BeginCommodityUpdates();
			AnimateSim("EatOffFloorLoop");
			bool flag = DoTimedLoop(loopTime, ExitReason.Default);
			EndCommodityUpdates(flag);
			mDestroyPrey = true;
			AnimateSim("Exit");
			waitInstance.waitComplete = true;

			if (mSuccess)
            {
				Actor.ShowTNSIfSelectable("EWLocalize - Successful treatment",
					NotificationStyle.kGameMessagePositive);
				Actor.BuffManager.RemoveElement(mBuffID);
				if (Actor.GetDistanceToObjectSquared(mMedicineCat) <= kMaxDistanceForSimToReact
								* kMaxDistanceForSimToReact)
				{
					// Say thank you
					SocialInteractionA.Definition definition2 = new SocialInteractionA.Definition("Nuzzle Auto Accept",
						new string[0], null, initialGreet: false);
					InteractionInstance nuzzleInteraction = definition2.CreateInstance(mMedicineCat, Actor,
						new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
						true);
					Actor.InteractionQueue.TryPushAsContinuation(this, nuzzleInteraction);
				}
				DoLtrAdjustment(goodReaction: true);
			}
			else
            {
				Actor.ShowTNSIfSelectable("EWLocalize - Failed treatment",
					NotificationStyle.kGameMessagePositive);
				DoLtrAdjustment(goodReaction: false);
			}
			treatmentComplete = true;
			return flag;
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

	}

}
