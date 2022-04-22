using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using static Sims3.SimIFace.Route;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public abstract class EWAbstractPetTreatPlantable : Interaction<Sim, GameObject>
	{
		[TunableComment("The distance the cat should get to the Sim before dropping the object on the ground.")]
		[Tunable]
		public static float kDistanceFromSimToPresent = 2f;

		[TunableComment("The radius around the cat to look for valid sims.")]
		[Tunable]
		public static float kRadiusForValidSims = 10f;

		public Sim mSimToPresent;

		public BuffInstance badBuff;

		public abstract bool isSuccessfulTreatment(Sim simToPresentTo);

		public override bool Run()
		{
			if (mSimToPresent == null)
			{
				mSimToPresent = (GetSelectedObject() as Sim);
				if (mSimToPresent == null)
					return false;
			}

			string modelname;
			if (Target.Plantable.PlantDef.GetModelName(out modelname))
			{
				if (!EWPetPickUpPlantable.PickUpFromSimInventory(Actor, Target, modelname, true))
					return false;
			}
			else return false;

			if (mSimToPresent != null && !mSimToPresent.HasBeenDestroyed)
			{
				mSimToPresent.InteractionQueue.CancelAllInteractions();
				EWWait.Definition waitDefinition = new EWWait.Definition();
				EWWait waitInstance = waitDefinition.CreateInstance(mSimToPresent, mSimToPresent,
					new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
					CancellableByPlayer) as EWWait;
				waitInstance.SetInteractionName("Wait for Medicine");
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

			EWPetPickUpPlantable.PutDownOnFloor(Actor);
			if (Actor.HasExitReason())
			{
				return false;
			}
			BeginCommodityUpdates();

			EWPetBeTreated.Definition treatDefinition = new EWPetBeTreated.Definition();
			EWPetBeTreated treatInstance = treatDefinition.CreateInstance(Target, mSimToPresent,
				new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
				CancellableByPlayer) as EWPetBeTreated;
			treatInstance.SetParams(isSuccessfulTreatment(mSimToPresent), badBuff.Guid,
				Actor, true);
			mSimToPresent.InteractionQueue.AddNext(treatInstance);

			EndCommodityUpdates(succeeded: true);

			return true;
		}

		public override bool RunFromInventory()
		{
			return Run();
		}
	}
}
