using Sims3.Gameplay.ActorSystems;

namespace Echoweaver.Sims3Game.Breastfeed
{
	internal class BuffPeacefulMama : Buff
	{
		private const ulong kNonaPeacefulMamaBuffGuid = 4506494096455629351uL;

		public static ulong StaticGuid => 4506494096455629351uL;

		public BuffPeacefulMama(BuffData data)
			: base(data)
		{
		}
	}
}
