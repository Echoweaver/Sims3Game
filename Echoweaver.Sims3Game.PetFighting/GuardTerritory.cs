using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.Gameplay.Situations.PetGuardSitutation;
using Queries = Sims3.Gameplay.Queries;


namespace Echoweaver.Sims3Game.PetFighting
{
	public class GuardTerritory : Interaction<Sim, Sim>
	{
		public class Definition : SoloSimInteractionDefinition<GuardTerritory>
		{
			public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (isAutonomous && a.TraitManager.HasElement(TraitNames.LazyPet))
				{
					return false;
				}
				if (target.IsSleeping)
				{
					return false;
				}
				if (a.LotHome == null)
				{
					return false;
				}
				if (a.LotHome.HasVirtualResidentialSlots)
				{
					return false;
				}
				return true;
			}
		}

		[Tunable]
		[TunableComment("During the night, the interaction's autonomy score will be multiplied by this value.")]
		public static float kNightTimeAutonomyMultiplier = 10f;

		[Tunable]
		[TunableComment("Distance squared from a burglar that regular pets will detect a burglar.")]
		public static float kRegularBurglarSenseDistanceSquared = 60f;

		[TunableComment("Distance squared from a burglar that clueless pets will detect a burglar.")]
		[Tunable]
		public static float kCluelessBurglarSenseDistanceSquared = 60f;

		[Tunable]
		[TunableComment("Chance a burglar will get spawned while the pet is guarding.")]
		public static float kChanceToSpawnBurglar = 0.25f;

		[TunableComment("Delay in sim minutes between trying to roll to spawn a burglar.")]
		[Tunable]
		public static float kTimeDelayBetweenTryingToSpawnBurglar = 20f;

		[Tunable]
		[TunableComment("Time in sim minutes that a pet will wait in an area before routing to another area.")]
		public static float kTimeToWaitInOneArea = 15f;

		[TunableComment("Range a pet will route to the front door when starting the interaction.")]
		[Tunable]
		public static float[] kFrontDoorRadialRouteRange = new float[2] {
			0f,
			2f
		};

		[TunableComment("Radial range that a pet will route to when chasing a burglar.")]
		[Tunable]
		public static float[] kFollowBurglarRadialRouteRange = new float[2] {
			1f,
			5f
		};

		[TunableComment("Range that a pet will wander from after they're done waiting in an area.")]
		[Tunable]
		public static float[] kPatrolRouteRange = new float[2] {
			5f,
			10f
		};

		[TunableComment("Radius in which a pet will react to a random sim.")]
		[Tunable]
		public static float kSimReactionRadius = 30f;

		[TunableComment("Sims with an LTR lower than this to the pet will get barked at.")]
		[Tunable]
		public static float kSimReactionLTRThreshold = 10f;

		[Tunable]
		[TunableComment("Radius in which a pet will react to a random car.")]
		public static float kCarReactionRadius = 50f;

		[TunableComment("Size of the route discourage footprint that gets added while a pet is guarding.")]
		[Tunable]
		public static float kDiscourageAreaSize = 1.5f;

		[TunableComment("Chance a lazy pet will fall asleep after routing an area to guard.")]
		[Tunable]
		public static float kLazyPetSleepChance = 0.1f;

		[TunableComment("Chance a clueless pet will face the house and bark at it.")]
		[Tunable]
		public static float kCluelessPetBarkAtHouseChance = 0.75f;

		[Tunable]
		[TunableComment("Time in sim minutes that a burglar will freak out at a barking pet.")]
		public static float[] kBurglarFreakOutTimeRange = new float[2] {
			5f,
			10f
		};

		[Tunable]
		[TunableComment("Chance a burglar will freak out when a pet is chasing them.")]
		public static float kChanceForBurglarToFreakOutAtBarkingPet = 0.75f;

		[Tunable]
		[TunableComment("Chance a burglar will run away after playing the freak out animations at a pet.  Otherwise, they will continue stealing.")]
		public static float kChanceForBurglarToRunAwayAfterFreakOut = 0.75f;

		[Tunable]
		[TunableComment("Range: positive floats 1-100. When a pet is standing while guarding, this is the chance that the pet will sit incremented per guard loop. So if the pet has been standing for 3 loops, the chance they'll sit will br 3x the value here. That way the longer they're standing, the more likely they'll sit.")]
		public static float kChanceSitIncrementPerGuardStandLoop = 30f;

		[Tunable]
		[TunableComment("Range: positive floats 1-100. When a pet is sitting while guarding, this is the chance that the pet will stand incremented per guard loop. So if the pet has been sitting for 3 loops, the chance they'll stand will be 3x the value here. That way the longer they're sitting, the more likely they'll stand.")]
		public static float kChanceStandIncrementPerGuardSitLoop = 5f;

		[Tunable]
		[TunableComment("Time range in sim minutes to guard the house.")]
		public static float[] kTimeRangeToGuardHouse = new float[2] {
			60f,
			120f
		};

		public float mTimeInState;

		public float mTimeBetweenBurglarRolls;

		public GameObject mDiscourageArea;

		public Lot mLotToGuard;

		public bool mAnimating;

		public float mChanceChangePosture;

		public float mMaxGuardTime;

		public static ISoloInteractionDefinition Singleton = new Definition();

		[ScoringFunction]
		public static float GuardHouseScoringFunction(Sim Actor, InteractionObjectPair interactionObjectPair)
		{
			if (SimClock.IsNightTime())
			{
				return kNightTimeAutonomyMultiplier;
			}
			return 1f;
		}

		public override bool Run()
		{
			mLotToGuard = Actor.LotHome;
			if (mLotToGuard == null)
			{
				return false;
			}
			RouteToFrontDoor();
			SwitchToSitting(Actor);
			StandardEntry();
			BeginCommodityUpdates();
			CreateRouteDiscourageArea();
			mMaxGuardTime = RandomUtil.GetFloat(kTimeRangeToGuardHouse[0], kTimeRangeToGuardHouse[1]);
			bool flag = DoLoop(ExitReason.Default, GuardTheHouseLoop, null);
			EndCommodityUpdates(flag);
			SwitchToStanding(Actor);
			StandardExit();
			return flag;
		}

		public void GuardTheHouseLoop(StateMachineClient smc, LoopData loopData)
		{
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0339: Unknown result type (might be due to invalid IL or missing references)
			if (mAnimating)
			{
				AnimateSim("Exit");
			}
			if (loopData.mLifeTime > mMaxGuardTime)
			{
				Actor.AddExitReason(ExitReason.Finished);
				return;
			}
			if (Actor.LotCurrent != mLotToGuard)
			{
				HideDiscourageAreaPosition();
				if (SwitchToStanding(Actor))
				{
					mChanceChangePosture = 0f;
				}
				Actor.RouteToLot(mLotToGuard.LotId);
			}
			bool flag = false;
			Sim burglarOnLot = mLotToGuard.GetBurglarOnLot();
			if (ShouldReactToBurglar(Actor, burglarOnLot))
			{
				BurglarSituation.SetToReactToPetState(burglarOnLot, Actor);
				if (burglarOnLot.LotCurrent == mLotToGuard && (!Actor.CanSeeObject(burglarOnLot) || Actor.GetDistanceToObjectSquared(burglarOnLot) > kFollowBurglarRadialRouteRange[1] * kFollowBurglarRadialRouteRange[1]))
				{
					HideDiscourageAreaPosition();
					RequestWalkStyle(Sim.WalkStyle.PetRun);
					Actor.RouteToObjectRadialRange(burglarOnLot, kFollowBurglarRadialRouteRange[0], kFollowBurglarRadialRouteRange[1]);
					UnrequestWalkStyle(Sim.WalkStyle.PetRun);
				}
				Actor.RouteTurnToFace(burglarOnLot.Position);
				ShowThoughtBalloon(burglarOnLot, isNegative: true);
				EventTracker.SendEvent(EventTypeId.kCatchBurglar, Actor);
				flag = true;
			}
			else
			{
				if (!Actor.IsOutside)
				{
					RouteToFrontDoor();
				}
				if (mTimeInState < kTimeToWaitInOneArea)
				{
					if (Actor.HasTrait(TraitNames.CluelessPet) && RandomUtil.RandomChance01(kCluelessPetBarkAtHouseChance))
					{
						if (SwitchToStanding(Actor))
						{
							mChanceChangePosture = 0f;
						}
						Actor.RouteTurnToFace(mLotToGuard.GetCenterPosition());
						flag = true;
					}
					else
					{
						GameObject objectToReactTo = GetObjectToReactTo();
						if (objectToReactTo != null)
						{
							if (SwitchToStanding(Actor))
							{
								mChanceChangePosture = 0f;
							}
							Actor.RouteTurnToFace(objectToReactTo.Position, 18014398509481984uL, 0uL);
							ShowThoughtBalloon(objectToReactTo, isNegative: true);
							flag = true;
							Sim sim = objectToReactTo as Sim;
							if (sim != null)
							{
								if (sim.IsHuman)
								{
									ReactionTypes randomObjectFromList = RandomUtil.GetRandomObjectFromList(GuardObject.kReactionsToBarkingPets);
									sim.OverlayComponent.PlayReaction(randomObjectFromList, Actor);
								}
								else
								{
									sim.PlayReaction(ReactionTypes.NegativePet, new InteractionPriority(InteractionPriorityLevel.CriticalNPCBehavior), Actor, ReactionSpeed.NowOrLater);
								}
								if (GuardObject.ShouldReactToGuardingPet(Actor, sim))
								{
									if (!sim.InteractionQueue.HasInteractionOfType(ReactToGuardingAnimal.Singleton))
									{
										InteractionInstance entry = ReactToGuardingAnimal.Singleton.CreateInstance(Actor, sim, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true, cancellableByPlayer: true);
										sim.InteractionQueue.AddNext(entry);
									}
								}
								else if (!sim.ThoughtBalloonManager.HasBallon)
								{
									ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(Actor.GetThumbnailKey());
									balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
									balloonData.LowAxis = ThoughtBalloonAxis.kDislike;
									balloonData.Duration = ThoughtBalloonDuration.Medium;
									sim.ThoughtBalloonManager.ShowBalloon(balloonData);
								}
							}
						}
					}
					mTimeInState += loopData.mDeltaTime;
				}
				else
				{
					if (Actor.HasTrait(TraitNames.LazyPet) && RandomUtil.RandomChance01(kLazyPetSleepChance))
					{
						GuardObject.AddReinforcementEventForLazyPet(Actor, GetInteractionName());
						PetSurfacePosture.LayDownHere.Definition definition = new PetSurfacePosture.LayDownHere.Definition(PetSurfacePosture.LayDownHere.LayDownType.Nap, Actor.Position);
						InteractionInstance continuation = definition.CreateInstance(Actor, Actor, GetPriority(), isAutonomous: true, base.CancellableByPlayer);
						Actor.InteractionQueue.TryPushAsContinuation(this, continuation);
						Actor.AddExitReason(ExitReason.Finished);
						return;
					}
					RouteToPatrol();
					mTimeInState = 0f;
				}
				mTimeBetweenBurglarRolls += loopData.mDeltaTime;
				if (mTimeBetweenBurglarRolls > kTimeDelayBetweenTryingToSpawnBurglar)
				{
					if (RandomUtil.RandomChance01(kChanceToSpawnBurglar))
					{
						mLotToGuard.SpawnBurglar();
					}
					mTimeBetweenBurglarRolls = 0f;
				}
			}
			UpdateDiscourageAreaPosition();
			if (flag)
			{
				if (SwitchToStanding(Actor))
				{
					mChanceChangePosture = 0f;
				}
				EnterStateMachine("GuardPet", "Enter", "x");
				AnimateSim("Grumble At");
			}
			else
			{
				mChanceChangePosture = SwitchPostureIfNecessary(Actor, mChanceChangePosture, kChanceStandIncrementPerGuardSitLoop, kChanceSitIncrementPerGuardStandLoop);
				EnterStateMachine("GuardPet", "Enter", "x");
				AnimateSim("Guarding Idles");
			}
			EventTracker.SendEvent(EventTypeId.kGuardHome, Actor);
			Actor.RemoveExitReason(ExitReason.RouteFailed);
			mAnimating = true;
		}

		public static bool ShouldReactToBurglar(Sim pet, Sim burglar)
		{
			if (pet == null || burglar == null)
			{
				return false;
			}
			if (!pet.HasTrait(TraitNames.GeniusPet))
			{
				float num = kRegularBurglarSenseDistanceSquared;
				float distanceToObjectSquared = pet.GetDistanceToObjectSquared(burglar);
				if (pet.HasTrait(TraitNames.CluelessPet))
				{
					num = kCluelessBurglarSenseDistanceSquared;
				}
				if (distanceToObjectSquared < num)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		public GameObject GetObjectToReactTo()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			List<GameObject> list = new List<GameObject>();
			Vector3 position = Actor.Position;
			Vehicle[] objects = Queries.GetObjects<Vehicle>(position, kCarReactionRadius);
			Vehicle[] array = objects;
			foreach (Vehicle vehicle in array)
			{
				if (vehicle.GetOwnerLot() != mLotToGuard && (!Actor.TraitManager.HasElement(TraitNames.CluelessPet) || Actor.CanSeeObject(vehicle)))
				{
					list.Add(vehicle);
				}
			}
			Sim[] objects2 = Queries.GetObjects<Sim>(position, kSimReactionRadius);
			Sim[] array2 = objects2;
			foreach (Sim sim in array2)
			{
				if (!(sim.Service is Burglar))
				{
					Relationship relationship = Actor.GetRelationship(sim, bCreateIfNotPresent: false);
					if (!Actor.IsRelatedOrSameHousehold(sim) && (relationship == null || relationship.CurrentLTRLiking < kSimReactionLTRThreshold) && (!Actor.TraitManager.HasElement(TraitNames.CluelessPet) || Actor.CanSeeObject(sim)))
					{
						list.Add(sim);
					}
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			return RandomUtil.GetRandomObjectFromList(list);
		}

		public bool RouteToFrontDoor()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			HideDiscourageAreaPosition();
			Door door = mLotToGuard.GetFrontDoor((eFrontDoorType)1);
			if (door == null)
			{
				Vector3 position = Actor.Position;
				if (mLotToGuard.IsEntranceFromTargetPoint())
				{
					position = mLotToGuard.GetEntraceTargetPoint();
				}
				door = mLotToGuard.FindOutsideDoor(position);
			}
			if (SwitchToStanding(Actor))
			{
				mChanceChangePosture = 0f;
			}
			Route r = Actor.CreateRoute();
			if (!mLotToGuard.RouteToDoor(door, Actor, kFrontDoorRadialRouteRange[0], kFrontDoorRadialRouteRange[1], ref door, wantToBeOutside: true, ref r, doRoute: true, wantToBeAsNearAsPossibleToDestination: true, null, float.MaxValue))
			{
				return false;
			}
			Vector3 val = Actor.Position - mLotToGuard.GetCenterPosition();
			Vector3 val2 = val.Normalize();
			bool result = Actor.RouteTurnToFace(Actor.Position + val2, 18014398509481984uL, 0uL);
			Actor.RemoveExitReason(ExitReason.RouteFailed);
			return result;
		}

		public bool RouteToPatrol()
		{
			HideDiscourageAreaPosition();
			if (SwitchToStanding(Actor))
			{
				mChanceChangePosture = 0f;
			}
			Actor.Wander(kPatrolRouteRange[0], kPatrolRouteRange[1], limitOutdoors: true, (RouteDistancePreference)0, doRoutFail: false);
			Vector3 val = Actor.Position - mLotToGuard.GetCenterPosition();
			Vector3 val2 = val.Normalize();
			return Actor.RouteTurnToFace(Actor.Position + val2, 18014398509481984uL, 0uL);
		}

		public void CreateRouteDiscourageArea()
		{
			mDiscourageArea = GlobalFunctions.CreateObject("DynamicSlot", ProductVersion.BaseGame, Actor.Position, Actor.Level, Actor.ForwardVector, null, null) as GameObject;
			// TODO Check original Guard interaction for hidden flags.
			mDiscourageArea.SetHiddenFlags(HiddenFlags.Nothing);
			Vector3 position = Actor.Position;
			Vector3 forwardVector = Actor.ForwardVector;
			Vector3 sideVector = Actor.SideVector;
			forwardVector.y = (sideVector.y = 0f);
			Vector3[] array = (Vector3[])(object)new Vector3[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = position;
			}
			Vector3 val = sideVector * kDiscourageAreaSize;
			Vector3 val2 = forwardVector * kDiscourageAreaSize;
			ref Vector3 reference = ref array[0];
			reference += val2 + val;
			ref Vector3 reference2 = ref array[1];
			reference2 += val2 - val;
			ref Vector3 reference3 = ref array[2];
			reference3 += -val2 - val;
			ref Vector3 reference4 = ref array[3];
			reference4 += -val2 + val;
			ObjectGuid objectId = mDiscourageArea.ObjectId;
			Route.AddDiscourageFootprint(objectId.Value, array, false, true);
		}

		public void HideDiscourageAreaPosition()
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (mDiscourageArea != null)
			{
				mDiscourageArea.SetPosition(Vector3.OutOfWorld);
			}
		}

		public void UpdateDiscourageAreaPosition()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			if (mDiscourageArea != null)
			{
				mDiscourageArea.SetPosition(Actor.Position);
			}
		}

		public void ShowThoughtBalloon(GameObject obj, bool isNegative)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (!Actor.ThoughtBalloonManager.HasBallon)
			{
				ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(obj.GetThumbnailKey());
				balloonData.Duration = ThoughtBalloonDuration.Short;
				balloonData.HighAxis = ((!isNegative) ? ThoughtBalloonAxis.kLike : ThoughtBalloonAxis.kDislike);
				Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
			}
		}

		public override void Cleanup()
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (mDiscourageArea != null)
			{
				ObjectGuid objectId = mDiscourageArea.ObjectId;
				Route.RemoveDiscourageFootprint(objectId.Value);
				mDiscourageArea.Destroy();
				mDiscourageArea = null;
			}
			base.Cleanup();
		}
	}
}
