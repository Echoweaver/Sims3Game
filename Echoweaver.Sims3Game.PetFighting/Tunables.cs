using System;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.PetFighting
{
	public class Tunables
	{

        [Tunable]
        public static bool kAllowPetDeath = true;

        [Tunable]
        public static int kGraveWoundDuration = 720;

        [Tunable]
        public static int kSeriousWoundDuration = 480;

        [Tunable]
        public static int kMinorWoundDuration = 240;

        [Tunable]
        public static int kRecuperateDuration = 720;

        [Tunable]
        public static int kCostOfVetVisit = 200;

        [Tunable]
        public static int kLTRBoostOfVetVisit = 20;

        public Tunables()
		{
		}
	}
}

