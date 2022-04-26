using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;
using Sims3.UI.Hud;
using Sims3.Gameplay;
using Sims3.UI.CAS;
using Sims3.Gameplay.CAS;

namespace Echoweaver.Sims3Game.AlienEyeFixer
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
            Sim[] sims = Queries.GetObjects<Sim>();
            foreach (Sim s in sims)
            {
                if (s.SimDescription.IsAlien)
                {
                    World.ObjectRemoveVisualOverride(s.ObjectId, eVisualOverrideTypes.Alien);
                    //Responder.Instance.CASModel.SetVisualOverride(OccultTypes.None);
                }
            }
            EventTracker.AddListener(EventTypeId.kSimInstantiated, new ProcessEventDelegate(OnSimInstantiated));
             //Responder.Instance.CASModel.ShowUI += OnShowUIEW;
            //Responder.Instance.CASModel.OnSimPreviewChange += OnEWSimPreviewChange;
        }

        // Attempts to 
        //public static void OnEWSimPreviewChange(int simIndex)
        //{
        //    if (Responder.Instance.CASModel.CurrentSimDescription.IsAlien && Responder.Instance.InCasMode)
        //    {
        //        CASLogic.Instance.PreviewSim.SetVisualOverride(OccultTypes.None, false);
        //    }
        //}

        public static ListenerAction OnSimInstantiated(Event e)
        {
            Sim s = e.TargetObject as Sim;
            if (s.SimDescription.IsAlien)
            {
                World.ObjectRemoveVisualOverride(s.ObjectId, eVisualOverrideTypes.Alien);
            }

            return ListenerAction.Keep;
        }
    }
}