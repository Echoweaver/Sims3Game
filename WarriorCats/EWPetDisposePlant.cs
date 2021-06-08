using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.Miscellaneous;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Objects.Gardening.Plant;
using static Sims3.SimIFace.Simulator;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public class EWPetDisposePlant : Interaction<Sim, Plant>, IDisposePlantInteraction
	{
		public class Definition : InteractionDefinition<Sim, Plant, EWPetDisposePlant>
		{
			public override string GetInteractionName(Sim a, Plant target, InteractionObjectPair interaction)
			{
				return LocalizeString("DisposeAlivePlant");
			}

			public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				// TODO: Check for HerbLore skill
				return target.GardenInteractionLotValidityTest(a) && a.IsCat;
			}
		}

		public const string sLocalizationKey = "Gameplay/Objects/Gardening/Plant/DisposeAlivePlant";

		public TrashPile mTrashPile;

		public bool mPlantDeletePointOfNoReturn;

		public bool mTrashPilePlaceInTrashCanFailure;

		public List<InteractionObjectPair> mTrashPileInteractions;

		public static InteractionDefinition Singleton = new Definition();

		public static string LocalizeString(string name, params object[] parameters)
		{
			return Localization.LocalizeString("Gameplay/Objects/Gardening/Plant/DisposeAlivePlant:" + name, parameters);
		}

		public override void Cleanup()
		{
			DisposeInteractionCleanup(this, ref mTrashPile, ref mTrashPileInteractions, mPlantDeletePointOfNoReturn, mTrashPilePlaceInTrashCanFailure);
			base.Cleanup();
		}

		public void SetTrashPile(TrashPile trashPile)
		{
			mTrashPile = trashPile;
		}

		public void RemoveTrashPileInteractions()
		{
			mTrashPileInteractions = new List<InteractionObjectPair>(mTrashPile.Interactions);
			mTrashPile.RemoveAllInteractions();
		}

		public void ReachedPlantDeletePointOfNoReturn()
		{
			mPlantDeletePointOfNoReturn = true;
		}

		public void SetTrashPilePlaceInTrashCanFailure(bool trashPilePlaceInTrashCanFailure)
		{
			mTrashPilePlaceInTrashCanFailure = trashPilePlaceInTrashCanFailure;
		}

		public override bool Run()
		{
			if (Target.RouteSimToMeAndCheckInUse(Actor))
			{
				//				Plant.TryConfigureTendGardenInteraction(Actor.CurrentInteraction);
				return DoDispose(Actor, Target, this);
			}
			else return false;
		}

		public bool DoDispose(Sim Actor, Plant Target, InteractionInstance interaction)
		{
			IDisposePlantInteraction disposePlantInteraction = interaction as IDisposePlantInteraction;
			if (disposePlantInteraction == null)
			{
				return false;
			}

			disposePlantInteraction.ReachedPlantDeletePointOfNoReturn();
			interaction.StandardEntry();
			interaction.BeginCommodityUpdates();
            AcquireStateMachine("eatharvestablepet");
			mCurrentStateMachine.SetActor("x", Actor);
			mCurrentStateMachine.EnterState("x", "Enter");
			SetParameter("IsEatingOnGround", paramValue: true);

			uint footprintHash = Target.GetFootprintHash();
			Target.DisableFootprint(footprintHash);
			Target.GetSoil().DisableFootprint(1478897068u);

			AnimateSim("EatHarvestable");
			AnimateSim("Exit");
			interaction.EndCommodityUpdates(true);
			interaction.StandardExit();
			return true;
		}

		public override ThumbnailKey GetIconKey()
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (Target == null && mTrashPile != null)
			{
				return mTrashPile.GetThumbnailKey();
			}
			return base.GetIconKey();
		}
	}
}
