using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.CatFishing
{

	public class EWCatInspectWater : ImmediateInteractionGameObjectHit<Sim, Terrain>
	{
		public class Definition : ImmediateInteractionDefinition<Sim, Terrain, EWCatInspectWater>
		{
			public override bool Test(Sim a, Terrain target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (a.IsCat)
				{
					EWCatFishingSkill skill = a.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
					if (skill != null && skill.CanCatchPreyFish())
					{
						return true;
					}
				}
				return false;
			}

			public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				if (((int)parameters.Hit.mType == 8 && PondManager.ArePondsLiquid()) || (int)parameters.Hit.mType == 9)
				{
					return base.Test(ref parameters, ref greyedOutTooltipCallback);
				}
				return InteractionTestResult.Gen_BadTerrainType;
			}

			public override string GetInteractionName(Sim a, Terrain target, InteractionObjectPair interaction)
			{
				return Localization.LocalizeString("Echoweaver/Interactions:EWCatInspectWater");
			}
		}

		public static InteractionDefinition Singleton = new Definition();

		public override bool Run()
		{
			Vector3 mPoint = Hit.mPoint;
			FishingData fishingData = FishingSpot.GetFishingData(mPoint, Hit.mType);
			FishingSpotData fishingSpotData = fishingData as FishingSpotData;
			string str = (fishingSpotData == null) ? Localization.LocalizeString("Gameplay/Objects/Fishing:EmptyWater")
				: ((!fishingSpotData.IsActive) ? Localization.LocalizeString("Gameplay/Objects/Fishing:InactiveWater")
				: Localization.LocalizeString("Gameplay/Objects/Fishing:ActiveWater"));
			str += "\n";
			List<FishType> fish = fishingData.GetFish();
			List<int> chances = fishingData.GetChances();
			EWCatFishingSkill skill = Actor.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
			for (int i = 0; i < chances.Count; i++)
			{
				if (chances[i] > 0)
				{
					FishType fishType = fish[i];
					if (fishType != FishType.None && fishType != FishType.Box)
					{
						// Only fish appropriate to skill or that sim "knows about" (has already caught) will be
						// displayed. This should be just like human fishing.
						str = str + "\n" + GetFishName(fish[i], skill);
						//str = str + "\n" + fish[i].ToString();
					}
				}
			}
			Show(new Format(str, NotificationStyle.kGameMessagePositive));
			return true;
		}

		public string GetFishName(FishType type, EWCatFishingSkill fishingSkill)
		{
			if (Fish.sFishData.TryGetValue(type, out FishData value) && fishingSkill != null && (fishingSkill.SkillLevel >= value.Level
				|| fishingSkill.KnowsAbout(type)))
			{
				return Localization.LocalizeString(value.StringKeyName);
			}
			return Localization.LocalizeString("Gameplay/Objects/Fishing:UnknownFish");
		}
	}
}
