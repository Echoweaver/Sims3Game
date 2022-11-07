using System;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery
// Pneumonia (Greencough)
//   -- Develops from Germy.
//   -- Symptoms: Fever, exhaustion, frequent coughing, delerium
//   -- Contagion by contact
//   -- Can be lethal.


namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetPneumonia : Buff
	{
		public const ulong mGuid = 0x904F100B14974699ul;
        public const BuffNames buffName = (BuffNames)mGuid;

        static bool once;
		
		public BuffEWPetPneumonia(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

    }		
}