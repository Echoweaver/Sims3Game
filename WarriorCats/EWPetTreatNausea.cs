using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.SimIFace.Route;
using static Sims3.UI.ObjectPicker;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetTreatNausea : Interaction<Sim, GameObject>
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetTreatNausea>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				return "EWPetTreatNausea" + Localization.Ellipsis;
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWMedicineCatSkill.SkillNameID) < 2)
				{
					return false;
				}

				Ingredient ingredient = target as Ingredient;
				if (ingredient == null)
				{
					return false;
				}
				// TODO: Do I want to use non-herb stuff?
				if (ingredient.IngredientKey != "Peppermint")
				{
					return false;
				}
				// TODO: Localize
				if (GetTreatableSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("Localize - No sims with nausea");
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

		public Sim mSimToPresent;

		public static InteractionDefinition Singleton = new Definition();

		public static List<Sim> GetTreatableSims(Sim actor, Lot lot)
		{
			List<Sim> list = null;
			if (!lot.IsWorldLot)
			{
				foreach (Sim s in lot.GetAllActors())
				{
					if (s != actor && s.BuffManager.HasAnyElement(Loader.nauseaBuffList))
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.BuffManager.HasAnyElement(Loader.nauseaBuffList)
					&& !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public bool isSuccessfulTreatment(Sim simToPresentTo)
		{
			BuffInstance badBuff = simToPresentTo.BuffManager.GetElement(BuffNames.NauseousPet);
			// Cure the nastiest buff first if there are multiple types of nausea.
			if (simToPresentTo.BuffManager.HasElement(Loader.buffNameStomachFluPet))
            {
				badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNameStomachFluPet);
			} else if (simToPresentTo.BuffManager.HasElement(Loader.buffNameTummyTrouble))
            {
				badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNameTummyTrouble);
			}
			if (badBuff == null)
			{
				return false;
			}
			EWMedicineCatSkill skill = Actor.SkillManager.GetSkill<EWMedicineCatSkill>(EWMedicineCatSkill.SkillNameID);
			if (skill == null)
			{
				return false;
			}
			return skill.TreatSim(simToPresentTo, badBuff, Target.GetLocalizedName());
		}

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
			treatInstance.SetParams(isSuccessfulTreatment(mSimToPresent), BuffNames.NauseousPet,
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
