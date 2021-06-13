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
		public static float kDistanceFromSimToPresent = 1f;

		[TunableComment("The maximum distance from the prey the target needs to be in order to react. Should be greater than kDistanceFromSimToPresent")]
		[Tunable]
		public static float kMaxDistanceForSimToReact = 10f;

		[TunableComment("The radius around the cat to look for valid sims.")]
		[Tunable]
		public static float kRadiusForValidSims = 10f;

		[TunableComment("The base chance for a cat sim to nuzzle the cat after being presented something.")]
		[Tunable]
		public static float kCatBaseChanceToNuzzleCat = 0.5f;

		[TunableComment("The cat traits that will get a modifier on the base chance to nuzzle a cat")]
		[Tunable]
		public static TraitNames[] kCatTraits = new TraitNames[2] {
			TraitNames.FriendlyPet,
			TraitNames.AggressivePet
		};

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

		public bool SuccessfulTreatment(Sim simToPresentTo)
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
					val.PlanToPointRadialRange((IHasScriptProxy)(object)mSimToPresent, mSimToPresent.Position,
						kDistanceFromSimToPresent, kDistanceFromSimToPresent, Vector3.UnitZ, 360f,
						RouteDistancePreference.PreferNearestToRouteDestination, RouteOrientationPreference.TowardsObject,
						mSimToPresent.LotCurrent.LotId, new int[1]
						{
							mSimToPresent.RoomId
						});
					Actor.DoRoute(val);
				}
			}
			PetCarrySystem.PutDownOnFloor(Actor);
			Target.UpdateVisualState(CatHuntingModelState.InWorld);
			if (Actor.HasExitReason())
			{
				return false;
			}
			BeginCommodityUpdates();
			Target.CatHuntingComponent.mHasBeenPresented = true;


			EWPetBeTreated.Definition treatDefinition = new EWPetBeTreated.Definition();
			EWPetBeTreated treatInstance = treatDefinition.CreateInstance(Target, mSimToPresent,
				new InteractionPriority(InteractionPriorityLevel.UserDirected), Autonomous,
				CancellableByPlayer) as EWPetBeTreated;
			treatInstance.SetParams(SuccessfulTreatment(mSimToPresent), BuffNames.GotFleasPet,
				Actor, true);
			//LinkedInteractionInstance = treatInstance;
			mSimToPresent.InteractionQueue.AddNext(treatInstance);
			//WaitForSyncComplete();

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
}
