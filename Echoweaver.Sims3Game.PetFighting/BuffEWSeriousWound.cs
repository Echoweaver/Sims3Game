using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetFighting
{
	internal class BuffEWSeriousWound : Buff
	{
		private const ulong kEWSeriousWoundGuid = 0xAE4D28F1BCEC603D;

		public static float kEnergyDecayPerHour = -10f;
		public static float kHungerDecayPerHour = -10f;

		public static ulong StaticGuid
		{
			get
			{
				return kEWSeriousWoundGuid;
			}
		}

		public BuffEWSeriousWound(BuffData data) : base(data)
		{
		}

		public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
		{
			base.OnAddition(bm, bi, travelReaddition);
			Sim actor = bm.Actor;
			actor.Motives.GetMotive(CommodityKind.Energy).Decay += kEnergyDecayPerHour;
			actor.RequestWalkStyle(Sim.WalkStyle.PetStumbleWalk);
		}

		public override void OnRemoval(BuffManager bm, BuffInstance bi)
		{
			Sim actor = bm.Actor;
			actor.UnrequestWalkStyle(Sim.WalkStyle.PetStumbleWalk);
			actor.Motives.GetMotive(CommodityKind.Energy).Decay -= kEnergyDecayPerHour;
			// Grave wound becomes serious becomes mild before disappearing
			actor.BuffManager.AddElement(BuffEWMinorWound.StaticGuid,
				(Origin)ResourceUtils.HashString64("FromFightWithAnotherPet"));
			base.OnRemoval(bm, bi);
		}
	}
}
