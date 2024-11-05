using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;   
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.CAS;
using Sims3.UI.OnlineDating;
using static Sims3.Gameplay.Objects.Electronics.Computer;
using static Sims3.Gameplay.Utilities.OnlineDatingManager;

namespace Echoweaver.Sims3Game.NoCommittedDatingMatches
{
    public class EWBrowseDatingProfiles : BrowseDatingProfiles
    //public class EWBrowseDatingProfiles : ComputerInteraction
    {
        public new class Definition : InteractionDefinition<Sim, Computer, EWBrowseDatingProfiles>
        {
            public override string[] GetPath(bool isFemale)
            {
                return new string[1] { LocalizeString("InteractionPath") };
            }

            public override bool Test(Sim a, Computer target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!isAutonomous && OnlineDatingManager.SimHasProfile(a.SimDescription.SimDescriptionId))
                {
                    return target.IsComputerUsable(a, checkBroken: true, checkExpensive: false, isAutonomous);
                }
                return false;
            }
            public override string GetInteractionName(Sim a, Computer c, InteractionObjectPair iop)
            {
                return "EWBrowse Dating Profiles";
            }
        }

        //[Tunable]
        //[TunableComment("Time it takes to write message in minutes")]
        //public static float kTimeToWriteMessage = 10f;

        //public static string LocalizeString(string name, params object[] parameters)
        //{
        //    return Localization.LocalizeString("Gameplay/Objects/Electronics/Computer/CreateDatingProfile:"
        //        + name, parameters);
        //}

        public static InteractionDefinition newSingleton = new Definition();

        public static Dictionary<ulong, DateAndTime> lastChecked = new Dictionary<ulong, DateAndTime>();

        public override bool Run()
        {

            Main.DebugNote("Start browse dating profiles.");
            StandardEntry();
            if (!Target.StartComputing(this, SurfaceHeight.Table, turnOn: true))
            {
                Main.DebugNote("Start computing failed.");
                StandardExit();
                return false;
            }
            if (!UIUtils.IsOkayToStartModalDialog())
            {
                Main.DebugNote("Can't open dialog");
                return false;
            }

            Target.StartVideo(VideoType.Browse);
            BeginCommodityUpdates();
            OnlineDatingManager instance = OnlineDatingManager.Instance;
            SimDescription simDescription = Actor.SimDescription;
            ulong simDescriptionId = simDescription.SimDescriptionId;
            OnlineDatingRecord recordForSim = instance.GetRecordForSim(Actor.SimDescription.SimDescriptionId);

            // I'm trying to deal with the possibility that matches were last generated without the
            // mod installed, which would mean they should be regenerated. I'm pretty sure this is a bad
            // way to do it, but it should work?
            if (!lastChecked.ContainsKey(simDescriptionId))
            {
                recordForSim.mLastTimeMatchesGenerated = DateAndTime.Invalid;
            } else if (lastChecked[simDescriptionId] < recordForSim.mLastTimeMatchesGenerated)
            {
                recordForSim.mLastTimeMatchesGenerated = lastChecked[simDescriptionId];
            }
            RefreshDatingMatches(instance, recordForSim);
            lastChecked[simDescriptionId] = recordForSim.mLastTimeMatchesGenerated;

            List <IDatingProfile> possibleMatchesForSim = instance.GetPossibleMatchesForSim(simDescriptionId);
            AnimateSim("GenericTyping");
            IDatingProfile val = BrowseDatingProfilesDialog.Show(possibleMatchesForSim, simDescription
                .GenderPreferenceIsFemale());
            if (val != null)
            {
                DoTimedLoop(kTimeToWriteMessage, ExitReason.Default);
                instance.AddSentMessage(simDescriptionId, val.OwnerId);
            }
            EventTracker.SendEvent(EventTypeId.kOnlineDatingBrowseProfiles, Actor);
            EndCommodityUpdates(true);
            Target.StopComputing(this, StopComputingAction.TurnOff, forceBreak: false);
            StandardExit();

            return true;
        }

        public static void RefreshDatingMatches(OnlineDatingManager instance, OnlineDatingRecord recordForSim)
        {
            SimDescription sd = null;
            string check_name = "";
            Main.DebugNote("Entering RefreshDatingMatches");
            recordForSim.CleanUpPossibleMatches();

            if (recordForSim.mPossibleMatches == null || recordForSim.mLastTimeMatchesGenerated ==
                DateAndTime.Invalid
                || SimClock.ElapsedTime(TimeUnit.Days, recordForSim.mLastTimeMatchesGenerated) >= 1f)
            {
                if (recordForSim.mPossibleMatches == null)
                {
                    recordForSim.mPossibleMatches = new List<IDatingProfile>();
                }
                else
                {
                    int num = (int)(recordForSim.mPossibleMatches.Count * 0.5f);
                    for (int i = 0; i < num; i++)
                    {
                        recordForSim.mPossibleMatches.RemoveAt(RandomUtil.GetInt(recordForSim.mPossibleMatches.Count - 1));
                    }
                }

                List<SimDescription> allSims = new List<SimDescription>(Household.AllSimsLivingInWorld());
                Main.DebugNote("AllSims length " + allSims.Count);
                RandomUtil.RandomizeListOfObjects(allSims);
                int match_count = RandomUtil.GetInt(OnlineDatingManager.kMinMaxPossibleMatches[0],
                    OnlineDatingManager.kMinMaxPossibleMatches[1]);
                Main.DebugNote("Target match count " + match_count);
                try
                {
                    foreach (SimDescription sim in allSims)
                    {
                        ulong simDescriptionId = sim.SimDescriptionId;
                        sd = sim;
                        check_name = "Age/species";
                        if (simDescriptionId == recordForSim.mOwnerId || sim.TeenOrBelow || !sim.IsHuman || sim.IsEP11Bot
                            || recordForSim.mOwnerSimDescription.IsMemberOfMyHousehold(sim))
                        {
                            continue;
                        }
                        check_name = "Celebrity";
                        if (sim.IsCelebrity)
                        {
                            if (!recordForSim.mOwnerSimDescription.IsCelebrity)
                            {
                                continue;
                            }
                            int celebrityLevelDelta = sim.CelebrityManager.GetCelebrityLevelDelta(recordForSim.mOwnerSimDescription);
                            if (celebrityLevelDelta < 0)
                            {
                                continue;
                            }
                        }

                        // Here is the additional check for dating matches -- happily married sims should
                        // not be using online dating. For now, I'm going to limit this check to married
                        // sims. Sims who are in a committed relationship are still allowed to look for
                        // new dates.
                        check_name = "Marriage";
                        if (EWAttractionNPCController.isCommittedNPC(sim.CreatedSim))
                        {
                            continue;
                        }

                        // I'm not sure why we're looping back through the very list we're building
                        // here. Are we looking for duplicates? It seems like there ought to be a better
                        // way to do it, but I'm going to leave it here because I don't want to figure it out.

                        check_name = "Check for duplicates";
                        bool alreadyInList = false;
                        foreach (OnlineDatingProfile mPossibleMatch in recordForSim.mPossibleMatches)
                        {
                            if (mPossibleMatch.OwnerId == simDescriptionId)
                            {
                                Main.DebugNote(sim.FullName + " already in list");
                                alreadyInList = true;
                                break;
                            }
                        }
                        if (!alreadyInList)
                        {
                            check_name = "Liar Status";
                            bool liarStatus = OnlineDatingManager.Instance.GetLiarStatus(sim);
                            OnlineDatingProfile item = new OnlineDatingProfile(simDescriptionId, liarStatus);
                            if (recordForSim.mPossibleMatches == null)
                            {
                                recordForSim.mPossibleMatches = new List<IDatingProfile>();
                            }
                            recordForSim.mPossibleMatches.Add(item);
                            if (recordForSim.mPossibleMatches.Count >= match_count)
                            {
                                break;
                            }
                        }
                    }
                } catch (Exception e)
                {
                    string simid = "";
                    if (sd == null)
                    {
                        simid = "NULL";
                    } else
                    {
                        simid = sd.FullName;
                    }
                    Main.DebugNote("Error in Sim " + simid + " check " + check_name + ": " + e.Message);
                }
                Main.DebugNote("Total possible matches: " + recordForSim.mPossibleMatches.Count);
                recordForSim.mLastTimeMatchesGenerated = SimClock.CurrentTime();
            }
        }
    }

}