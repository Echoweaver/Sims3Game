using System;
using Sims3.Gameplay.ActorSystems;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWRecuperating : Buff
	{
		private const ulong kEWRecuperatingGuid = 0x07D8D834178B5247;
		public static ulong StaticGuid
		{
			get
			{
				return kEWRecuperatingGuid;
			}
		}
		public BuffEWRecuperating(BuffData data) : base(data)
		{
		}
	}
}
