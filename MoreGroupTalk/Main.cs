using System;
using Sims3.Gameplay.Objects.Beds.Mimics;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Objects.Miscellaneous.BubbleBar;

//Template Created by Battery

namespace Echoweaver.Sims3Game.MoreGroupTalk
{
	public class Main
	{
		[Tunable] static bool init;

        [Tunable]
        public static bool kTalkDebug = true;

        static Main()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
		}
		
		static void OnWorldLoaded(object sender, EventArgs e)
		{
			AlarmManager.Global.AddAlarm(1f,TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
				"Debug Mode Alarm",AlarmType.NeverPersisted,null);
			BlowBubbles.Singleton = EWBlowBubbles.Singleton;
		}
		
		public static void Initialize()
		{
            if (kTalkDebug)
				DebugNote("MoreGroupTalk Debug Mode ON");
		}

        public static void DebugNote(string str)
        {
            if (kTalkDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
                    .NotificationStyle.kDebugAlert));
            }
        }
    }
}