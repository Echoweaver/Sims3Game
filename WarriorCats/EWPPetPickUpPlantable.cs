using System;
using Sims3.Gameplay;
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
using Sims3.UI;

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
				if (!a.SkillManager.HasElement(EWHerbLoreSkill.SkillNameID))
                {
					return false;
                }
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
				return !target.InUse && CarrySystem.CouldPickUp(target as ICarryable);
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
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
			{
				// Unable to use PetCarrySystem because plantable not recognized as IPetCarryable
				// Not clear on how this interface works.
				// Fortunately the CarryUtils methods used by both carry systems are less picky.
				if (Actor.RouteToObjectRadius(Target, 0.3f))
				{
					if (Target.Plantable.PlantDef.GetModelName(out string modelname))
					{
						CarryUtils.Acquire(Actor, Target);
						Actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
						Enter(Actor, Target, modelname);
						CarryUtils.Request(Actor, "PickUp");
						CarryUtils.Request(Actor, "Carry");
						AnimateIntoSimInventory(Actor);
						//CarryUtils.VerifyAnimationParent(Target, Actor);
						// Note: PutInSimInventory includes ExitCarry. This means the
						// state machine has exited and can't be used again without Acquire
						bool success = CarryUtils.PutInSimInventory(Actor);
						skill.StopSkillGain();
						skill.AddPoints(200);  // This takes some know-how for a pet
						return success;
					}
				}
			}
			return false;
		}

		public static void Enter(Sim a, GameObject target, string modelname)
		{
			a.CarryStateMachine.SetActor("x", a);
			a.CarryStateMachine.SetActor("object", target);
			a.CarryStateMachine.SetParameter("model", "catToy", ProductVersion.EP5);
			a.CarryStateMachine.SetParameter("NamespaceMap0From", modelname);
			a.CarryStateMachine.SetParameter("NamespaceMap0To", "object");
			a.CarryStateMachine.EnterState("x", "Enter");
		}

		public static void AnimateIntoSimInventory(Sim actor)
		{
			actor.CarryStateMachine.SetParameter("model", "prey", ProductVersion.EP5);
			actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.SimInventory);
			actor.CarryStateMachine.RequestState("x", "PutDown");
		}

		public static bool PickUpFromSimInventory(Sim a, GameObject target, string modelname, bool removeFromInventory)
		{
			if (removeFromInventory && !a.Inventory.TryToRemove(target))
			{
				return false;
			}
			target.AddToWorld();
			CarryUtils.Acquire(a, target);
			a.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
			Enter(a,target, modelname);
            CarryUtils.Request(a, "PickUp");
            target.SetHiddenFlags(HiddenFlags.Nothing);
			CarryUtils.Request(a, "Carry");
            return true;
		}

		public static bool PutDownOnFloor(Sim actor)
        {
			GameObject objectInMouth = actor.GetObjectInMouth();
			GameObject gameObject = GlobalFunctions.CreateJigFromObject(objectInMouth) as GameObject;
			actor.CarryStateMachine.SetParameter("model", "prey", ProductVersion.EP5);
			actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
			Vector3 position2 = actor.Position;
			Vector3 forward2 = actor.ForwardVector;
			GlobalFunctions.FindGoodLocationNearby(gameObject, ref position2, ref forward2, 0.7f,
				GlobalFunctions.FindGoodLocationStrategies.All, (FindGoodLocationBooleans)20);
			CarryUtils.Request(actor, "PutDown");
			CarryUtils.ExitCarry(actor);
			gameObject.SetPosition(World.SnapToFloor(position2));
			gameObject.SetForward(forward2);
			CarryUtils.VerifyAnimationUnParent(objectInMouth, actor, gameObject);
			return true;
		}
	}
}
