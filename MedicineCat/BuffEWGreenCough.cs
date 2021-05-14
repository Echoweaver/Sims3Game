using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.MedicineCat
{
	internal class BuffEWGreenCough : Buff
	{
		private const ulong kEWGraveWoundGuid = 0xE07FEE99F17FD43E;
		public static ulong StaticGuid
		{
			get
			{
				return kEWGraveWoundGuid;

			}
		}
		public BuffEWGreenCough(BuffData data) : base(data)
		{
		}

		public static void Succumb(Sim s)
        {
			if (Loader.kAllowPetDeath)
            {
				s.Kill(Loader.diseaseDeathType);
            }
			else
            {
				// TODO: Needs an origin for succumb to wounds
				s.BuffManager.AddElement(BuffEWRecuperating.StaticGuid,
					(Origin)ResourceUtils.HashString64("From??")); // TODO: Set Correct origin
            }
        }
	}
}
