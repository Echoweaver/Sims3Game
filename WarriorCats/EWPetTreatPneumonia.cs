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
using static Sims3.UI.ObjectPicker;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetTreatFeverish : EWAbstractPetTreatPlantable
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetTreatFeverish>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				return "EWPetFeverish" + Localization.Ellipsis;
			}

			public override bool Test(Sim a, GameObject target, bool isAutonomous,
				ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.SkillManager.GetSkillLevel(EWMedicineCatSkill.SkillNameID) < 4)
				{
					return false;
				}

				Ingredient ingredient = target as Ingredient;
				if (ingredient == null)
				{
					return false;
				}
				// TODO: Do I want to use non-herb stuff?
				if (ingredient.IngredientKey != "Cinnamon" && ingredient.IngredientKey != "Garlic")
				{
					return false;
				}
				// TODO: Localize
				if (GetTreatableSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
				{
					greyedOutTooltipCallback = CreateTooltipCallback("Localize - No sims with fever");
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
					if (s != actor && s.BuffManager.HasElement(Loader.buffNamePneumoniaPet))
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.BuffManager.HasElement(Loader.buffNamePneumoniaPet)
					&& !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public override bool isSuccessfulTreatment(Sim simToPresentTo)
		{
			badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNamePneumoniaPet);
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
