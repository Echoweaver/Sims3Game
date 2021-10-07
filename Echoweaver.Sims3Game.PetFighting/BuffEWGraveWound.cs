using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWGraveWound : Buff
	{
		private const ulong kEWGraveWoundGuid = 0x384B537AE0B8F97A;
		public static ulong StaticGuid
		{
			get
			{
				return kEWGraveWoundGuid;

			}
		}
		public static BuffNames buffName = (BuffNames)kEWGraveWoundGuid;

		public static float kGraveWoundHungerDecayMultiplier = 3.0f;
		public static float kGraveWoundEnergyDecayMultiplier = 3.0f;

		public BuffEWGraveWound(BuffData data) : base(data)
		{
		}


		public override void OnAddition(BuffManager bm, BuffInstance bi,
			bool travelReaddition)
		{
			base.OnAddition(bm, bi, travelReaddition);
			Sim actor = bm.Actor;

			// This should increase hunger and energy decay.
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Hunger,
				kGraveWoundHungerDecayMultiplier);
			BuffBooter.addCommodityMultiplier(actor, CommodityKind.Energy,
				kGraveWoundEnergyDecayMultiplier);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			Sim actor = bm.Actor;
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Hunger,
				kGraveWoundHungerDecayMultiplier);
			BuffBooter.removeCommodityMultiplier(actor, CommodityKind.Energy,
				kGraveWoundEnergyDecayMultiplier);

			base.OnRemoval(bm, bi);
		}

		public static void Succumb(Sim s)
        {
			if (Loader.kAllowPetDeath)
            {
				s.Kill(Loader.fightDeathType);
            }
			else
            {
				// TODO: Needs an origin for succumb to wounds
				s.BuffManager.AddElement(BuffEWRecuperateCat.StaticGuid,
					Origin.FromFight);
            }
        }
	}
}
