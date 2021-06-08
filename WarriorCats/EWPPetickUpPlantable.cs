using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetPickUpPlantable : Interaction<Sim, GameObject>
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetPickUpPlantable>
		{
			public override string GetInteractionName(Sim a, GameObject target, InteractionObjectPair interaction)
			{
				return LocalizeString("PickUpPlantable");
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				PlantableComponent plantable = target.Plantable;
				if (plantable == null)
				{
					return false;
				}
				if (plantable.PlantDef == null)
				{
					return false;
				}
				if (plantable.PlantDef.LimitedAvailability)
				{
					return false;
				}
				ITreasureSpawnableObject treasureSpawnableObject = target as ITreasureSpawnableObject;
				if (treasureSpawnableObject != null && treasureSpawnableObject.IsOnSpawner)
				{
					return false;
				}
				return a.IsCat && !target.InUse && CarrySystem.CouldPickUp(target as ICarryable);
			}
		}

		public const string sLocalizationKey = "Gameplay/ObjectComponents/PickUpPlantable";

		public static InteractionDefinition Singleton = new Definition();

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString("Gameplay/ObjectComponents/PickUpPlantable:" + name, parameters);
		}


		public override bool Run()
		{
			if (Actor.RouteToPointRadialRange(Target.Position, 0.3f, 0.5f))
			{
				CarryUtils.Acquire(Actor, Target);
				Actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
				bool gotmodel = Target.Plantable.PlantDef.GetModelName(out string modelname);
				Enter(Actor, Target, modelname);
				CarryUtils.Request(Actor, "PickUp");
				CarryUtils.Request(Actor, "Carry");
				//CarryUtils.VerifyAnimationParent(Target, Actor);
				bool success = CarryUtils.PutInSimInventory(Actor);
//				CarryUtils.ExitCarry(Actor);
				return gotmodel && success;
			}
			return false;
		}

		public static void Enter(Sim a, GameObject target, String modelname)
		{
			a.CarryStateMachine.SetActor("x", a);
			a.CarryStateMachine.SetActor("object", target);
			a.CarryStateMachine.SetParameter("model", "catToy", ProductVersion.EP5);
			a.CarryStateMachine.SetParameter("NamespaceMap0From", modelname);
			a.CarryStateMachine.SetParameter("NamespaceMap0To", "object");
			a.CarryStateMachine.EnterState("x", "Enter");
		}

	}
}
