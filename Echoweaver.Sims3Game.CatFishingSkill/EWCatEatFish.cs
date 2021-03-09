using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
namespace Echoweaver.Sims3Game
{
    public class EWCatEatFish : Interaction<Sim, ICatPrey>
	{
		public class Definition : InteractionDefinition<Sim, ICatPrey, EWCatEatFish>
		{
			public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if(a.SimDescription.Species != CASAgeGenderFlags.Cat)  // Interaction for cats only. 
                {
					return false;
				}
				if (target.CatHuntingComponent == null)
				{
					return false;
				}
				if (target.CatHuntingComponent.mPreyData.mPreyType != CatHuntingSkill.PreyType.Fish)
				{
					return false;
				}
				if (target.Parent != null)
				{
					return false;
				}
				return true;
			}

			public override string GetInteractionName(Sim a, ICatPrey target, InteractionObjectPair iop)
			{
				return Localization.LocalizeString("Echoweaver/Interactions:EWCatEatFish");
			}
		}

		public bool mDestroyPrey;
		public SimDescription mFishCatcher;

		public static InteractionDefinition Singleton = new Definition();

		public override bool Run()
		{
			float distanceToObjectSquared = Actor.GetDistanceToObjectSquared(Target);
			StandardEntry();
			Target.DisableInteractions();
			bool flag = CatBehavior(distanceToObjectSquared);
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
			RequestWalkStyle(Sim.WalkStyle.PetRun);
			bool result = Actor.RouteToObjectRadius(Target, routingDistance);
			UnrequestWalkStyle(Sim.WalkStyle.PetRun);
			return result;
		}

		public bool SharedNearDistanceBehavior(float routingDistance, float loopTime)
		{
			if (!Actor.RouteToObjectRadius(Target, routingDistance))
			{
				return false;
			}
			if (EWCatFishingSkill.sGourmetSimIDs.Contains(Target.CatHuntingComponent.mCatcherId))
			{
				// Catcher of the prey is a SeafoodGourmet. Add Hunger multiplier.
				InteractionTuning tuning = InteractionObjectPair.Tuning;
				foreach (CommodityChange mOutput in tuning.mTradeoff.mOutputs)
				{
					if (mOutput.Commodity == CommodityKind.Hunger)
					{
						if (mOutput.mMultiplier > 0)
						{
							// I don't know if 0 is a possibility, but lets just rule it out.
							mOutput.mMultiplier *= EWCatFishingSkill.kSeafoodGourmetHungerMultiplier;
						}
						else
						{
							mOutput.mMultiplier = EWCatFishingSkill.kSeafoodGourmetHungerMultiplier;
						}
					}
				}
			}
			EnterStateMachine("eatofffloor", "Enter", "x");
			SetParameter("isFish", true);
			BeginCommodityUpdates();
			AnimateSim("EatOffFloorLoop");
			bool flag = DoTimedLoop(loopTime, ExitReason.Default);
			EndCommodityUpdates(flag);
			mDestroyPrey = true;
			AnimateSim("Exit");
			Actor.BuffManager.AddElement(BuffNames.Tasty, Origin.FromEatingFish);
			EventTracker.SendEvent(EventTypeId.kAteFish, Actor, Target);
			return flag;
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
				CatchPrey catchPrey = CatchPrey.Singleton.CreateInstance(Target, Actor, GetPriority(),
					base.Autonomous, base.CancellableByPlayer) as CatchPrey;
				catchPrey.FromEatPreyInteraction = true;
				if (Actor.InteractionQueue.TryPushAsContinuation(this, catchPrey))
				{
					return true;
				}
			}
			return SharedNearDistanceBehavior(Actor.IsKitten ? PetEatPrey.kKittenEatingDistance :
				PetEatPrey.kCatEatingDistance, PetEatPrey.kSimMinutesForCatToEatPrey);
		}
	}
}
