using Sims3.Gameplay.ActorSystems;
using System;

namespace Echoweaver.Sims3Game
{
	internal class BuffEWSeriousWound : Buff
	{
		private const ulong kEWSeriousWoundGuid = 0xAE4D28F1BCEC603D;
		public static ulong StaticGuid
		{
			get
			{
				return kEWSeriousWoundGuid;

			}
		}
		public BuffEWSeriousWound(Buff.BuffData data) : base(data)
		{
		}
	}
}
