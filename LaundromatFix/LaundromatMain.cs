using System;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Miscellaneous;

//Template Created by Battery

namespace Echoweaver.Sims3.LaundromatFix
{
	public class Main
	{
		[Tunable]
		protected static bool init;
		[Tunable]
		public static bool kLaundromatDebug = false;

		static Main()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
		}

		static void OnWorldLoaded(object sender, EventArgs e)
		{
			foreach (WashingMachine w in Queries.GetObjects<WashingMachine>())
			{
				w.RemoveInteractionByType(WashingMachine.DoLaundry.Singleton);
				w.AddInteraction(EWDoLaundry.Singleton, true);
			}
			WashingMachine.DoLaundry.Singleton = new EWDoLaundry.Definition();
			AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
				"Laundromat Fix Debug Note", AlarmType.NeverPersisted, null);
		}

		public static void Initialize()
		{
			DebugNote("Laundromat Fix: Debug Mode On");
		}

		public static void DebugNote(string str)
		{
			if (kLaundromatDebug)
			{
				StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
					.NotificationStyle.kDebugAlert));
			}
		}

	}
}