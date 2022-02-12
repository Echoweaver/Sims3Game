using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.WarriorCats
{
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
