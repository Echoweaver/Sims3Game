using System;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Objects.Electronics.Computer;

//Template Created by Battery

namespace Echoweaver.Sims3Game.NoCommittedDatingMatches
{
	public class Main
	{
		[Tunable] static protected bool init;
		
		static Main()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
		}
		
		static void OnWorldLoaded(object sender, EventArgs e)
		{
			BrowseDatingProfiles.Singleton = EWBrowseDatingProfiles.Singleton;
			AlarmManager.Global.AddAlarm(1f,TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
				"Dating Mod Alarm",AlarmType.NeverPersisted,null);
		}
		
		public static void Initialize()
		{
			DebugNote("Dating Profiles Mod ON");
		}

		public static void DebugNote(string str)
		{
            StyledNotification.Show(new StyledNotification.Format(str,
				StyledNotification.NotificationStyle.kDebugAlert));
        }
    }
}