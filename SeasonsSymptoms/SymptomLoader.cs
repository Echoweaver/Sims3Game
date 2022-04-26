using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery

namespace Echoweaver.Sims3Game.SeasonsSymptoms
{
	public class Loader
	{
		[Tunable] protected static bool init;
		
		static Loader()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
			LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
		}

		static void OnWorldLoaded(object sender, EventArgs e)
		{
			EventTracker.AddListener(EventTypeId.kGotBuff, new ProcessEventDelegate(OnGotBuff));
			AlarmManager.Global.AddAlarm(1f,TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
				"Symptoms Loaded Alarm", AlarmType.NeverPersisted, null);
		}

        static void OnPreload()
        {
			Buffs.BuffEWGermy.LoadBuffXMLandParse(null);
			Buffs.BuffEWAllergies.LoadBuffXMLandParse(null);
        }

		public static void Initialize()
		{
			StyledNotification.Show(new StyledNotification.Format("Symptoms Loaded!",
				StyledNotification.NotificationStyle.kDebugAlert));
		}

		private static ListenerAction OnGotBuff(Event e)
		{
			Sim sim = e.Actor as Sim;
			if (sim != null)
			{
				if (sim.IsHuman)
					Simulator.AddObject(new OneShotFunctionWithParams(new FunctionWithParam(ProcessBuff), sim));
			}
			return ListenerAction.Keep;
		}

		private static void ProcessBuff(object obj)
		{
			Sim sim = obj as Sim;

			if (sim.BuffManager.HasElement(BuffNames.Germy))
            {
				if (!sim.BuffManager.HasElement(Buffs.BuffEWGermy.buffName))
                {
					sim.BuffManager.AddElement(Buffs.BuffEWGermy.buffName, Origin.None);
                }
            } else if (sim.BuffManager.HasElement(Buffs.BuffEWGermy.buffName))
			{
				if (!sim.BuffManager.HasElement(BuffNames.Germy))
				{
					sim.BuffManager.RemoveElement(Buffs.BuffEWGermy.buffName);
				}
			}

			if (sim.BuffManager.HasElement(BuffNames.AllergyHaze))
			{
				if (!sim.BuffManager.HasElement(Buffs.BuffEWAllergies.buffName))
				{
					sim.BuffManager.AddElement(Buffs.BuffEWAllergies.buffName, Origin.None);
				}
			}
			else if (sim.BuffManager.HasElement(Buffs.BuffEWAllergies.buffName))
			{
				if (!sim.BuffManager.HasElement(BuffNames.AllergyHaze))
				{
					sim.BuffManager.RemoveElement(Buffs.BuffEWAllergies.buffName);
				}
			}
		}
	}
}