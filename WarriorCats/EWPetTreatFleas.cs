using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.UI.Controller;
using System.Collections.Generic;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using static Sims3.Gameplay.Skills.CatHuntingSkill;
using static Sims3.SimIFace.Route;
using static Sims3.UI.ObjectPicker;
using static Sims3.UI.StyledNotification;
using Queries = Sims3.Gameplay.Queries;


namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWPetTreatFleas : Interaction<Sim, ICatPrey>
	{
		public class Definition : InteractionDefinition<Sim, ICatPrey, EWPetTreatFleas>
		{
			public override string GetInteractionName(Sim actor, ICatPrey target, InteractionObjectPair iop)
			{
				return "EWPetTreatFleas" + Localization.Ellipsis;
			}

			public override bool Test(Sim a, ICatPrey target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWMedicineCatSkill.SkillNameID) < 1)
                {
					return false;
                }
				CatHuntingComponent catHuntingComponent = target.CatHuntingComponent;
				if (catHuntingComponent == null)
				{
					return false;
				}
				if(catHuntingComponent.mPreyData.mPreyType != PreyType.Rodent)
                {
					return false;
                }
				if (!catHuntingComponent.HasBeenCaught)
				{
					return false;
				}
				if (GetTreatableSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("Localize - No sims with fleas");
					return false;
				}
				if (catHuntingComponent.mHasBeenPresented)
				{
					greyedOutTooltipCallback = CreateTooltipCallback(LocalizeString(a.IsFemale, "AlreadyPresented"));
					return false;
				}
				return true;
			}

			public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters,
				out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
			{
				Sim sim = parameters.Actor as Sim;
				ICatPrey catPrey = parameters.Target as ICatPrey;
				NumSelectableRows = 1;
				PopulateSimPicker(ref parameters, out listObjs, out headers,
					GetTreatableSims(sim, catPrey.InInventory ? sim.LotCurrent : catPrey.LotCurrent),
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
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			List<Sim> list = null;
			if (!lot.IsWorldLot)
			{
				foreach (Sim s in lot.GetAllActors())
				{
					if (s != actor && s.BuffManager.HasElement(BuffNames.GotFleasPet))
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.BuffManager.HasElement(BuffNames.GotFleasPet)
					&& !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public bool isSuccessfulTreatment(Sim simToPresentTo)
		{
			BuffInstance fleaBuff = simToPresentTo.BuffManager.GetElement(BuffNames.GotFleasPet);
			if (fleaBuff == null)
            {
				return false;
			}
			EWMedicineCatSkill skill = Actor.SkillManager.GetSkill<EWMedicineCatSkill>(EWMedicineCatSkill.SkillNameID);
			if (skill == null)
			{
				return false;
			}
			return skill.TreatSim(simToPresentTo, fleaBuff, Target.GetLocalizedName());
		}

		public override bool Run()
		{
			if (mSimToPresent == null)
			{
				mSimToPresent = (GetSelectedObject() as Sim);
				if (mSimToPresent == null)
				{
					return false;
				}
				if (!PetCarrySystem.PickUp(Actor, Target))
				{
					return false;
				}
				Target.UpdateVisualState(CatHuntingModelState.Carried);
			}

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
			PetCarrySystem.PutDownOnFloor(Actor);
			Target.UpdateVisualState(CatHuntingModelState.InWorld);
			Target.CatHuntingComponent.mHasBeenPresented = true;
			if (Actor.HasExitReason())
			{
				return false;
			}
			BeginCommodityUpdates();

			EWPetBeTreated.Definition treatDefinition = new EWPetBeTreated.Definition();
			EWPetBeTreated treatInstance = treatDefinition.CreateInstance(Target, mSimToPresent,
				new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
				CancellableByPlayer) as EWPetBeTreated;
			treatInstance.SetParams(isSuccessfulTreatment(mSimToPresent), BuffNames.GotFleasPet,
				Actor, true);
			mSimToPresent.InteractionQueue.AddNext(treatInstance);

			EndCommodityUpdates(succeeded: true);

			return true;
		}

		public override bool RunFromInventory()
		{
			mSimToPresent = (GetSelectedObject() as Sim);
			if (mSimToPresent == null)
			{
				return false;
			}
			Target.UpdateVisualState(CatHuntingModelState.Carried);
			if (!PetCarrySystem.PickUpFromSimInventory(Actor, Target, removeFromInventory: true))
			{
				Target.UpdateVisualState(CatHuntingModelState.InInventory);
				return false;
			}
			return Run();
		}

		public override void Cleanup()
		{
			if (Target.InInventory)
			{
				Target.UpdateVisualState(CatHuntingModelState.InInventory);
			}
			else
			{
				Target.UpdateVisualState(CatHuntingModelState.InWorld);
			}
			base.Cleanup();
		}
	}

	public class EWWait : Interaction<Sim, Sim>, IInteractionNameCanBeOverriden
	{
		public class Definition : InteractionDefinition<Sim, Sim, EWWait>
		{
			public static InteractionDefinition Singleton = new Definition();

			public override bool Test(Sim actor, Sim target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}
		}

		public static InteractionDefinition Singleton = new Definition();

		public string mOverrideInteractionName = "Wait";

		public bool waitComplete = false;

		public override bool Run()
		{
			while (!Actor.WaitForExitReason(Sim.kWaitForExitReasonDefaultTime, ExitReason.Canceled)
				&& !waitComplete)
			{
				Actor.LoopIdle();
			}
			return true;
		}

		public override string GetInteractionName()
		{
			return mOverrideInteractionName;
		}

		public void SetInteractionName(string interactionName)
		{
			mOverrideInteractionName = interactionName;
		}
	}
}
