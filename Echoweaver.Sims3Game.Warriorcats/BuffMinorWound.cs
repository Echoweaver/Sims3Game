﻿using Sims3.Gameplay.ActorSystems;
using System;

namespace Echoweaver.Sims3Game
{
	internal class BuffEWMinorWound : Buff
	{
		private const ulong kEWMinorWoundGuid = 0x452F9E53F8EC7FCE;
		public static ulong StaticGuid
		{
			get
			{
				return kEWMinorWoundGuid;

			}
		}
		public BuffEWMinorWound(Buff.BuffData data) : base(data)
		{
		}
	}
}
