using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWMinorWound : Buff
	{
		private const ulong kEWMinorWoundGuid = 0x3BE0F368D4653A9E; 
		public static ulong StaticGuid
		{
			get
			{
				return kEWMinorWoundGuid;

			}
		}

		public static float kMinorWoundHungerDecayMultiplier = 1.5f;
		public static float kMinorWoundEnergyDecayMultiplier = 1.5f;

		public BuffEWMinorWound(BuffData data) : base(data)
		{
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			base.OnAddition(bm, bi, travelReaddition);

			Sim actor = bm.Actor;
			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger, kMinorWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy, kMinorWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			base.OnRemoval(bm, bi);

			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger, kMinorWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy, kMinorWoundEnergyDecayMultiplier);
		}
	}
}
