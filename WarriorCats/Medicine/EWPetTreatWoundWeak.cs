using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Sims3.UI.ObjectPicker;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.WarriorCats.Medicine
{
    public class EWPetTreatWound : EWAbstractPetTreatPlantable
	{
		public class Definition : InteractionDefinition<Sim, GameObject, EWPetTreatWound>
		{
			public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
			{
				// Localize!
				return "Localize - Treat Wound" + Localization.Ellipsis;
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
				if (ingredient.IngredientKey != "Greenleaf" && ingredient.IngredientKey != "Garlic")
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
					if (s != actor && s.BuffManager.HasAnyElement(Loader.woundBuffList))
					{
						Lazy.Add(ref list, s);
					}
				}
			}

			Sim[] objects = Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
			foreach (Sim sim in objects)
			{
				if (sim != actor && sim.BuffManager.HasAnyElement(Loader.woundBuffList)
                    && !Lazy.Contains(list, sim))
				{
					Lazy.Add(ref list, sim);
				}
			}
			return list;
		}

		public override bool isSuccessfulTreatment(Sim simToPresentTo)
		{
			badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNameGraveWound);
			if (badBuff == null)
			{
				badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNameSeriousWound);
			}
			if (badBuff == null)
			{
				badBuff = simToPresentTo.BuffManager.GetElement(Loader.buffNameMinorWound);
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
			bool success = skill.TreatSim(simToPresentTo, badBuff, Target.GetLocalizedName());
			if (success && badBuff.BuffGuid != (ulong)Loader.buffNameMinorWound)
			{
                if (skill.SkillLevel >= 8)
                {
                    // Skill levels 8 and above have a chance of completely removing the buff
                    if (RandomUtil.RandomChance(6.25f * skill.SkillLevel))
                    {
                        return true;
                    }
                }
				if (badBuff.BuffGuid == (ulong)Loader.buffNameGraveWound)
				{
                    // Add a wound a level lower. The original will be removed with treat.
                    simToPresentTo.BuffManager.AddElement(Loader.buffNameSeriousWound, badBuff.TimeoutCount,
                        badBuff.BuffOrigin);
                } else
				{
                    simToPresentTo.BuffManager.AddElement(Loader.buffNameMinorWound, badBuff.TimeoutCount,
                        badBuff.BuffOrigin);
                }
            }
			return success;
		}

	}
}

