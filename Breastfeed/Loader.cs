using System;
using Sims3.Gameplay.Actors;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.Breastfeed
{
    public class Loader
    {
        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            foreach (Sim s in Sims3.Gameplay.Queries.GetObjects<Sim>())
            {
                if (s.SimDescription.ToddlerOrBelow)
                {
                    s.AddInteraction(AnimationTest.Singleton, true);
                }
            }
        }
    }
}