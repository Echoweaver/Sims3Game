using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.SimIFace.Route;
using static Sims3.UI.ObjectPicker;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWCarryFish : Interaction<Sim, GameObject>
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWCarryFish>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				return "EWCarryFishTest" + Localization.Ellipsis;
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (target.CatHuntingComponent == null)
				{
					return false;
				}
				if (target.CatHuntingComponent.mPreyData.mPreyType != CatHuntingSkill.PreyType.Fish)
				{
					return false;
				}

				Fish fish = target as Fish;
				if (fish == null)
				{
					return false;
				}

				// TODO: Localize
				if (GetTreatableSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("Localize - No cats nearby");
					return false;
				}
				return true;
			}

			public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters,
				out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
			{
				Sim sim = parameters.Actor as Sim;
				NumSelectableRows = 1;
				PopulateSimPicker(ref parameters, out listObjs, out headers,
					GetTreatableSims(sim, sim.LotCurrent),
					includeActor: false);
			}
		}

		[TunableComment("The distance the cat should get to the Sim before dropping the object on the ground.")]
		[Tunable]
		public static float kDistanceFromSimToPresent = 2f;

		[TunableComment("The radius around the cat to look for valid sims.")]
		[Tunable]
		public static float kRadiusForValidSims = 10f;

		[TunableComment("The modifier applied to the base chance, if the relationship is higher than the kCatRequiredRelationshipToQualifyForModifier")]
		[Tunable]
		public static float kCatRelationshipModifier = 0.3f;

		public Sim mSimToPresent;

		public static InteractionDefinition Singleton = new Definition();

		public static List<Sim> GetTreatableSims(Sim actor, Lot lot)
		{
			List<Sim> list = null;
			if (!lot.IsWorldLot)
			{
				foreach (Sim s in lot.GetAllActors())
				{
					if (s != actor && s.IsPet)
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.IsPet && !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public override bool Run()
		{
			if (mSimToPresent == null)
			{
				mSimToPresent = (GetSelectedObject() as Sim);
				if (mSimToPresent == null)
					return false;
			}

			Fish fish = Target as Fish;
			//fish.UpdateVisualState(Sims3.Gameplay.ObjectComponents.CatHuntingComponent.CatHuntingModelState.InInventory);
			if (!HerbLore.EWPetPickUpPlantable.PickUpFromSimInventory(Actor, Target, fish.MedatorName, true))
				return false;

			if (mSimToPresent != null && !mSimToPresent.HasBeenDestroyed)
			{
				mSimToPresent.InteractionQueue.CancelAllInteractions();
				EWWait.Definition waitDefinition = new EWWait.Definition();
				EWWait waitInstance = waitDefinition.CreateInstance(mSimToPresent, mSimToPresent,
					new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
					CancellableByPlayer) as EWWait;
				waitInstance.SetInteractionName("Wait for Fish");
				mSimToPresent.InteractionQueue.AddNext(waitInstance);

				Route val = Actor.CreateRoute();
				val.DoRouteFail = true;
				val.SetOption(RouteOption.MakeDynamicObjectAdjustments, true);
				val.PlanToPointRadialRange(mSimToPresent, mSimToPresent.Position, kDistanceFromSimToPresent,
					kDistanceFromSimToPresent, Vector3.UnitZ, 360f, RouteDistancePreference.PreferNearestToRouteDestination,
					RouteOrientationPreference.TowardsObject, mSimToPresent.LotCurrent.LotId,
					new int[1]
					{
						mSimToPresent.RoomId
					});
				if (Actor.DoRoute(val))
				{
					val.SetOption(RouteOption.MakeDynamicObjectAdjustments, false);
					val.PlanToPointRadialRange(mSimToPresent, mSimToPresent.Position,
						kDistanceFromSimToPresent, kDistanceFromSimToPresent, Vector3.UnitZ, 360f,
						RouteDistancePreference.PreferNearestToRouteDestination, RouteOrientationPreference.TowardsObject,
						mSimToPresent.LotCurrent.LotId, new int[1]
						{
							mSimToPresent.RoomId
						});
					Actor.DoRoute(val);
				}
				waitInstance.waitComplete = true;
			}

			HerbLore.EWPetPickUpPlantable.PutDownOnFloor(Actor);
			if (Actor.HasExitReason())
			{
				return false;
			}

			return true;
		}

		public override bool RunFromInventory()
		{
			return Run();
		}
	}
}
