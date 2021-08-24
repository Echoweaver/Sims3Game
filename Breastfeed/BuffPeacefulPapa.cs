using Sims3.Gameplay.ActorSystems;

namespace Echoweaver.Sims3Game.Breastfeed
{
	internal class BuffPeacefulPapa : Buff
	{
		private const ulong kNonaPeacefulPapaBuffGuid = 12974700909910434075uL;

		public static ulong StaticGuid => 12974700909910434075uL;

		public BuffPeacefulPapa(BuffData data)
			: base(data)
		{
		}
	}
}
