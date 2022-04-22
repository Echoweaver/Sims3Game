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
	public class EWPetTreatWoundWeak : EWAbstractPetTreatPlantable
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetTreatWoundWeak>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				return "EWPetTreatWoundWeak" + Localization.Ellipsis;
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWMedicineCatSkill.SkillNameID) < 3)
				{
					return false;
				}

				Ingredient ingredient = target as Ingredient;
				if (ingredient == null)
				{
					return false;
				}
				// TODO: Do I want to use non-herb stuff?
				if (ingredient.IngredientKey != "Basil")
				{
					return false;
				}

				// TODO: Localize
				if (GetTreatableSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("Localize - No sims with wounds");
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

		public static InteractionDefinition Singleton = new Definition();

		public static List<Sim> GetTreatableSims(Sim actor, Lot lot)
		{
			List<Sim> list = null;
			if (!lot.IsWorldLot)
			{
				foreach (Sim s in lot.GetAllActors())
				{
					if (s != actor && s.BuffManager.HasAnyElement(LoadThis.woundBuffList))
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.BuffManager.HasElement(BuffNames.NauseousPet)
					&& !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public override bool isSuccessfulTreatment(Sim simToPresentTo)
		{
			badBuff = simToPresentTo.BuffManager.GetElement(LoadThis.buffNameGraveWound);
			if (badBuff == null)
			{
				badBuff = simToPresentTo.BuffManager.GetElement(LoadThis.buffNameSeriousWound);
			}
			else
			{
				// Add a wound a level lower. The original will be removed with treat.
				simToPresentTo.BuffManager.AddElement(LoadThis.buffNameSeriousWound, badBuff.TimeoutCount / 2,
					badBuff.BuffOrigin);
			}
			if (badBuff == null)
			{
				badBuff = simToPresentTo.BuffManager.GetElement(LoadThis.buffNameMinorWound);
			}
			else
			{
				// Add wound a level lower
				simToPresentTo.BuffManager.AddElement(LoadThis.buffNameMinorWound, badBuff.TimeoutCount / 2,
					badBuff.BuffOrigin);
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

	}
}

