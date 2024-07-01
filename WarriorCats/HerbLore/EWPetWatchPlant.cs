using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.WarriorCats.HerbLore
{

    public class EWPetWatchPlant : Interaction<Sim, Plant>
	{
		public class Definition : InteractionDefinition<Sim, Plant, EWPetWatchPlant>
		{
			public override bool Test(Sim a, Plant target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
			{
				return true;
			}

			public override string GetInteractionName(Sim actor, Plant target, InteractionObjectPair iop)
			{
				return "Localize - Investigate";
			}
		}

		public static InteractionDefinition Singleton = new Definition();

		[TunableComment("Minutes between sims reacting to watching plants.")]
		[Tunable]
		public static float kTimeBetweenReactions = 5f;

		[Tunable]
		[TunableComment("Max time that a sim is watching a wild plant before it ends")]
		public static float kWatchPlantMaxTime = 20f;

		[Tunable]
		[TunableComment("Possible reactions to play on the human sim watching the plant.")]
		public static ReactionTypes[] kReactionList = new ReactionTypes[] {
			ReactionTypes.HissPet,
			ReactionTypes.BlankStarePet,
			ReactionTypes.PositivePetLoud,
			ReactionTypes.TerrifiedPet,
			ReactionTypes.Sniff,
			ReactionTypes.CatFlop
		};

		public override bool Run()
		{
			if (!Target.RouteSimToMeAndCheckInUse(Actor))
			{
				return false;
			}
			EWHerbLoreSkill skill = EWHerbLoreSkill.StartSkillGain(Actor);
			if (skill != null)
            {
				StandardEntry();
				BeginCommodityUpdates();
				bool flag = DoLoop(ExitReason.Default, LoopFunc, null, kTimeBetweenReactions);
				EndCommodityUpdates(flag);
				StandardExit();
				skill.StopSkillGain();
				return flag;
			}
			return false;
		}

		public void LoopFunc(StateMachineClient smc, LoopData ld)
		{
			if (ld.mLifeTime > kWatchPlantMaxTime)
			{
				Actor.AddExitReason(ExitReason.Finished);
				return;
			}
			ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(Target.GetThumbnailKey());
			balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
			balloonData.mPriority = ThoughtBalloonPriority.Low;
			balloonData.mFlags = ThoughtBalloonFlags.ShowIfSleeping;
			Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
			AcquireStateMachine("catdoginvestigate");
			EnterStateMachine("catdoginvestigate", "Enter", "x");
			AnimateSim("Investigate");
			AnimateSim("Exit");
			ReactionTypes reactionType = kReactionList[RandomUtil.GetInt(kReactionList.Length - 1)];
			Actor.PlayReaction(reactionType, ReactionSpeed.ImmediateWithoutOverlay);
		}
	}
}
