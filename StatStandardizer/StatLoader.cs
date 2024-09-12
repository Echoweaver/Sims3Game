using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.SimIFace.UserToolUtils;
using static Sims3.SimIFace.World;
using Queries = Sims3.Gameplay.Queries;


//Template Created by Battery

namespace Echoweaver.Sims3Game.StatStandardizer
{
    public class Loader
    {
        [Tunable] protected static bool init;
        [Tunable] protected static bool kStatStandardizerDebug = true;

        static Loader()
        {
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
        }

        static void OnWorldLoaded(object sender, EventArgs e)
        {
            AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
                "Hello World Alarm", AlarmType.NeverPersisted, null);
            EventTracker.AddListener(EventTypeId.kEnterInWorldSubState,
                new ProcessEventDelegate(OnEnterWorldState));
            EventTracker.AddListener(EventTypeId.kExitInWorldSubState,
                new ProcessEventDelegate(OnExitWorldState));
            EventTracker.AddListener(EventTypeId.kBoughtObject, new ProcessEventDelegate(OnBoughtObject));
        }

        public static void Initialize()
        {
            DebugNote("Stat Standardizer Debug ON");
            foreach (Bed b in Queries.GetObjects<Bed>())
            {
                SetBedStats(b);
            }

            foreach (Stove s in Queries.GetObjects<Stove>())
            {
                SetStoveStats(s);
            }

            foreach (Fridge f in Queries.GetObjects<Fridge>())
            {
                SetFridgetStats(f);
            }
        }

        public static void SetBedStats(Bed b)
        {
            // Only mess with stats for buyable items
            if (b.Product.ShowInCatalog)
            {
                DebugNote("Recalculate bed className = " + b.GetClassName());
                bool success = ReplaceBuyPreview.CalculateBedStats(b.GetClassName(), b.Cost,
                    out int energy, out float multiplier, out bool cheap);
                b.TuningBed.GivesBadNightsSleep = cheap;
                b.TuningBed.EnergyMultiplier = multiplier;
            }
        }

        public static void SetStoveStats(Stove s)
        {
            if (s.Product.ShowInCatalog)
            {
                DebugNote("Recalculate stove className = " + s.GetClassName());
                bool success = ReplaceBuyPreview.CalculateStoveStats(s.GetClassName(), s.Cost,
                    out int hunger, out float speed, out float skillGain, out bool cheap);
                s.StoveTuning.CookSpeedMultiplier = speed;
                s.StoveTuning.ApplianceCookingSkillMultiplier = skillGain;
                if (hunger > 4)
                {
                    s.StoveTuning.ApplianceBonusFoodPoints = (hunger - 4) * 4;
                } else
                {
                    s.StoveTuning.ApplianceBonusFoodPoints = 0;
                }
            }
        }

        public static void SetFridgetStats(Fridge f)
        {
            if (f.Product.ShowInCatalog)
            {
                DebugNote("Recalculate fridge className = " + f.GetClassName());
                bool success = ReplaceBuyPreview.CalculateFridgeStats(f.GetClassName(), f.Cost,
                    out int hunger, out int spoilage, out bool cheap);
                f.FridgeTuning.SpoilageMultiplier = spoilage;
            }

        }

        public static ListenerAction OnBoughtObject(Event e)
        {
            IGameObject gameObject = e.TargetObject;
            if (gameObject is GameObject)
            {
                GameObject go = (GameObject)gameObject;
                if (go is Bed)
                {
                    SetBedStats((Bed)go);
                }
            }
            return ListenerAction.Keep;
        }

        public static ListenerAction OnEnterWorldState(Event e)
        {
            InWorldSubStateEvent inWorldSubStateEvent = e as InWorldSubStateEvent;
            if (inWorldSubStateEvent == null)
            {
                return ListenerAction.Keep;
            }
            InWorldSubState state = inWorldSubStateEvent.State;
            if (state.StateId == 2)  // Buy  Mode
            {
                Simulator.AddObject(new Sims3.Gameplay.OneShotFunctionTask(ReplaceBuyPreview.OnEnterBuyMode));
            }

            return ListenerAction.Keep;
        }

        public static ListenerAction OnExitWorldState(Event e)
        {
            InWorldSubStateEvent inWorldSubStateEvent = e as InWorldSubStateEvent;
            if (inWorldSubStateEvent == null)
            {
                return ListenerAction.Keep;
            }
            InWorldSubState state = inWorldSubStateEvent.State;
            if (state.StateId == 2)  // Buy  Mode
            {
                BuyController.sController.mCatalogPreviewController.ShowHide
                    -= ReplaceBuyPreview.OnShowHideBuyPreview;
            }
            return ListenerAction.Keep;
        }

        public static void DebugNote(string str)
        {
            if (kStatStandardizerDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
                    .NotificationStyle.kDebugAlert));
            }
        }


    }
}