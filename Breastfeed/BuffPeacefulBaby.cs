using Sims3.Gameplay.ActorSystems;

namespace Echoweaver.Sims3Game.Breastfeed
{

	internal class BuffPeacefulBaby : Buff
	{
		private const ulong kNonaPeacefulBabyBuffGuid = 13647932526627158103uL;

		public static ulong StaticGuid => 13647932526627158103uL;

		public BuffPeacefulBaby(BuffData data)
			: base(data)
		{
		}
	}
}
