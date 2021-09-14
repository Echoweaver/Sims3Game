using Sims3.Gameplay.ActorSystems;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWMinorWound : Buff
	{
		//		private const ulong kEWMinorWoundGuid = 0x452F9E53F8EC7FCE;
		private const ulong kEWMinorWoundGuid = 0x3BE0F368D4653A9E; 
		public static ulong StaticGuid
		{
			get
			{
				return kEWMinorWoundGuid;

			}
		}
		public BuffEWMinorWound(BuffData data) : base(data)
		{
		}
	}
}
