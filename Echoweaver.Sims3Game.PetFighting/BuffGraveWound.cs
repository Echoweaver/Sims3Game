using Sims3.Gameplay.ActorSystems;
using System;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWGraveWound : Buff
	{
		private const ulong kEWGraveWoundGuid = 0xE07FEE99F17FD43E;
		public static ulong StaticGuid
		{
			get
			{
				return kEWGraveWoundGuid;

			}
		}
		public BuffEWGraveWound(Buff.BuffData data) : base(data)
		{
		}
	}
}
