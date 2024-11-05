using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.RabbitHoles;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Objects.Electronics.Computer;
using Queries = Sims3.Gameplay.Queries;

//Template Created by Battery

namespace Echoweaver.Sims3Game.NoCommittedDatingMatches
{
	public class Main
	{
		[Tunable] static protected bool init;
		[Tunable] public static bool datingDebug = true;
		[Tunable] public static bool goingSteadyIsCommitted = false;
		[Persistable] public static Dictionary<string, EWAttractionNPCController> npcControllers =
			new Dictionary<string, EWAttractionNPCController>();

		static Main()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
		}

		static void OnWorldLoaded(object sender, EventArgs e)
		{
			EventTracker.AddListener(EventTypeId.kMeetAttractiveSim, new ProcessEventDelegate(OnAttraction));
			EventTracker.AddListener(EventTypeId.kHouseholdSelected, new ProcessEventDelegate(OnHouseholdChanged));

			AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, new AlarmTimerCallback(Initialize),
				"Dating Mod Alarm", AlarmType.NeverPersisted, null);
		}

		public static void Initialize()
		{
			DebugNote("No Committed Dating Matches Debug ON");
            BrowseDatingProfiles.Singleton = EWBrowseDatingProfiles.newSingleton;
            DebugNote("Replaced browse interaction");
            if (datingDebug)
			{
                CityHall[] objects = Queries.GetObjects<CityHall>();
				foreach(CityHall rb in objects)
				{
					rb.AddInteraction(EWResetAttractionController.Singleton);
				}
            }
            SetAttractionAlarms();
		}

        public static ListenerAction OnHouseholdChanged(Event e)
        {
			SetAttractionAlarms();
            return ListenerAction.Keep;
        }

		public static void SetAttractionAlarms()
		{
			DebugNote("Setting NPC romance alarms for active household " + Household.ActiveHousehold.Name);
            foreach (Sim s in Household.ActiveHousehold.Sims)
            {
                if (s.SimDescription.TeenOrAbove)
                {
                    foreach (Relationship r in Relationship.Get(s))
                    {
                        if (r.AttractionNPCController != null)
                        {
                            DebugNote("Checking attraction alarms for " + r.SimDescriptionA.FullName
                                + " and " + r.SimDescriptionB.FullName);
                            if (!npcControllers.ContainsKey(r.SimDescriptionA.SimDescriptionId.ToString() + "-"
                                + r.SimDescriptionB.SimDescriptionId.ToString()))
                            {
                                DebugNote("Replacing attraction alarms for " + r.SimDescriptionA.FullName
                                    + " and " + r.SimDescriptionB.FullName);
                                EWAttractionNPCController npc = new EWAttractionNPCController(r.
                                    AttractionNPCController);
                                npc.SetDateAlarm();
                                npc.SetGiftAlarm();
                                npc.SetLoveLetterAlarm();
                                npcControllers.Add(r.SimDescriptionA.SimDescriptionId.ToString() + "-" +
                                    r.SimDescriptionB.SimDescriptionId.ToString(), npc);
                            }
                            else
                            {
                                DebugNote("Attraction alarms already replaced for " + r.SimDescriptionA.FullName
                                    + " and " + r.SimDescriptionB.FullName);
                            }
                        }
                    }
                }
            }
        }


        public static ListenerAction OnAttraction(Event e)
		{
			DebugNote(e.Actor.Name + " is attracted to " + e.TargetObject.GetLocalizedName());
			Sim sim = e.Actor as Sim;
			Sim otherSim = e.TargetObject as Sim;
			Relationship r = Relationship.Get(sim, otherSim, true, true);
			if (r.AttractionNPCController != null)
			{
				if (!npcControllers.ContainsKey(r.SimDescriptionA.SimDescriptionId.ToString() + "-"
					+ r.SimDescriptionB.SimDescriptionId.ToString()))
				{
					DebugNote("Replacing attraction alarms for " + r.SimDescriptionA.FullName
					+ " and " + r.SimDescriptionB.FullName);
					EWAttractionNPCController npc = new EWAttractionNPCController(r.AttractionNPCController);
					npc.SetDateAlarm();
					npc.SetGiftAlarm();
					npc.SetLoveLetterAlarm();
				}
				else
				{
					DebugNote("Attraction controller was already replaced");
				}
			}
			else
			{
				DebugNote("No attraction controller to replace");
			}
			return ListenerAction.Keep;
		}

		public static void DebugNote(string str)
		{
			if (datingDebug)
			{
				StyledNotification.Show(new StyledNotification.Format(str,
					StyledNotification.NotificationStyle.kDebugAlert));
			}
		}
	}
}