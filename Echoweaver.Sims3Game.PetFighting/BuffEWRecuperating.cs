using System;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWRecuperateCat : Buff
	{
		private const ulong kEWRecuperateCatGuid = 0x8B1D57AADCBD08B4;
		public static ulong StaticGuid
		{
			get
			{
				return kEWRecuperateCatGuid;
			}
		}
		public BuffEWRecuperateCat(BuffData data) : base(data)
		{
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi,
			bool travelReaddition)
		{
			base.OnAddition(bm, bi, travelReaddition);
			StyledNotification.Show(new StyledNotification.Format("Recuperate: "
				+ bm.Actor.Name, StyledNotification.NotificationStyle.kDebugAlert));

			bm.RemoveElement(BuffEWGraveWound.StaticGuid);
			bm.RemoveElement(BuffEWSeriousWound.StaticGuid);
			bm.RemoveElement(BuffEWMinorWound.StaticGuid);

			bm.Actor.Motives.SetMax(CommodityKind.Energy);
			bm.Actor.Motives.SetMax(CommodityKind.Hunger);
		}
	}
}
