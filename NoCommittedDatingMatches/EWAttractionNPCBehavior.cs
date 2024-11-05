using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Controllers;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects.Misc;
using Sims3.Gameplay.Opportunities;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game.NoCommittedDatingMatches
{
	public class EWAttractionNPCController
	{
        public AttractionNPCBehaviorController npcController;

        public EWAttractionNPCController(AttractionNPCBehaviorController npc)
        {
            npcController = npc;
        }

        public static bool isCurrentlyAvailable(Sim selectableSim, Sim otherSim)
        {
            // This whole class is basically created for this additional check.

            if (selectableSim.IsMarried)
            {
                if (!OnlineDatingManager.SimHasProfile(selectableSim.SimDescription
                    .SimDescriptionId))
                {
                    Main.DebugNote("Active sim is married and has no dating profile.");
                    return false;
                }
            }
            if (isCommittedNPC(otherSim))
            {
                return false;
            }
            Main.DebugNote("Sims are in suitable relationships for this action.");
            return true;
        }

        public static bool isCommittedNPC(Sim sim)
        {
            if (sim.TraitManager.HasElement(TraitNames.CommitmentIssues))
            {
                Main.DebugNote("Inactive sim has Committment Issues");
                return false;
            }

            if (sim.IsMarried || (Main.goingSteadyIsCommitted && sim.Partner != null))
            {
                Relationship marriageState = Relationship.Get(sim.SimDescription,
                    sim.Partner, createIfNone: true);
                if (marriageState.CurrentLTRLiking >= 50f)
                {
                    Main.DebugNote("Inactive sim is happily married");
                    return true;
                }
            }
            return false;
        }

        public void ResetDateAlarm()
        {
            // Reset attraction date calls to use the EA system
            if (npcController.mDateCallRejectCounter < AttractionNPCBehaviorController
                .kAttractionDateRejectThreshold)
            {
                DaysOfTheWeek currentDayOfWeek = SimClock.CurrentDayOfWeek;
                DaysOfTheWeek daysToTrigger = SimClock.DayAfter(currentDayOfWeek);
                float timeToCall = CalculateTimeToCall();
                npcController.mDateAlarmHandle = AlarmManager.Global
                    .AddAlarmDayNonRepeating(timeToCall, daysToTrigger, npcController.SetDateAlarm,
                    "Attraction Date Alarm", AlarmType.AlwaysPersisted, npcController);
            }
        }

        public void SetGiftAlarm()
        {
            npcController.mGiftAlarmHandle = AlarmManager.Global.AddAlarmDayNonRepeating(4f,
                SimClock.DayAfter(SimClock.CurrentDayOfWeek), AttractionGiftAlarm,
                "Attraction Gift Alarm", AlarmType.AlwaysPersisted, npcController);
        }

        public void AttractionGiftAlarm()
        {
            if (!npcController.IsValidRelationship())
            {
                npcController.Dispose();
                return;
            }

            Sim aSelectableSimIfAny = npcController.mParentRelationship.GetASelectableSimIfAny();
            Sim otherSim = npcController.mParentRelationship.GetOtherSim(aSelectableSimIfAny);
            Main.DebugNote("Attraction Gift check: " + otherSim.FullName + "->" +
                aSelectableSimIfAny.FullName);

            if (!isCurrentlyAvailable(aSelectableSimIfAny, otherSim))
            {
                return;
            }

            if (RandomUtil.InterpolatedChance(AttractionNPCBehaviorController.kAttractionLTRThresholds[0],
                AttractionNPCBehaviorController.kAttractionLTRThresholds[1], 0f, AttractionNPCBehaviorController
                .kAttractionGiftLetterMaxChancePerDay, npcController.mParentRelationship.LTR.Liking))
            {
                List<ulong> list = new List<ulong>();
                list.Add(otherSim.SimDescription.SimDescriptionId);
                AttractionGift attractionGift = GlobalFunctions.CreateObjectOutOfWorld("Package",
                    "Sims3.Gameplay.Objects.Misc.AttractionGift", null) as AttractionGift;
                if (!attractionGift.SetGiftParametersAndPutInMailbox(aSelectableSimIfAny.SimDescription,
                    list))
                {
                    attractionGift.Destroy();
                }
            }
            SetGiftAlarm();
        }

        public void SetDateAlarm()
        {
            if (npcController.mDateCallRejectCounter < AttractionNPCBehaviorController
                .kAttractionDateRejectThreshold)
            {
                DaysOfTheWeek currentDayOfWeek = SimClock.CurrentDayOfWeek;
                DaysOfTheWeek daysToTrigger = SimClock.DayAfter(currentDayOfWeek);
                float timeToCall = CalculateTimeToCall();
                npcController.mDateAlarmHandle = AlarmManager.Global
                    .AddAlarmDayNonRepeating(timeToCall, daysToTrigger, AttractionDateAlarm,
                    "Attraction Date Alarm", AlarmType.AlwaysPersisted, npcController);
            }
        }

        public float CalculateTimeToCall()
        {
            Sim aSelectableSimIfAny = npcController.mParentRelationship.GetASelectableSimIfAny();
            DaysOfTheWeek currentDayOfWeek = SimClock.CurrentDayOfWeek;
            float timeToCall = 0f;
            Occupation occupation = aSelectableSimIfAny.Occupation;
            if (occupation != null && occupation.IsFollowingDayInTheWorkDayList(currentDayOfWeek))
            {
                float finishTime = occupation.FinishTime;
                timeToCall = RandomUtil.GetFloat(finishTime + AttractionNPCBehaviorController
                    .kDateHoursToCallAfterWork[0], finishTime + AttractionNPCBehaviorController
                    .kDateHoursToCallAfterWork[1]);
            }
            else
            {
                timeToCall = RandomUtil.GetFloat(PhoneService.HourItsTooEarlyToCall, PhoneService
                    .HourItsTooLateToCall);
            }
            return timeToCall;
        }

        public void AttractionDateAlarm()
        {
            if (!npcController.IsValidRelationship())
            {
                npcController.Dispose();
                return;
            }

            Sim aSelectableSimIfAny = npcController.mParentRelationship.GetASelectableSimIfAny();
            Sim otherSim = npcController.mParentRelationship.GetOtherSim(aSelectableSimIfAny);
            Main.DebugNote("Attraction Date Call check: " + otherSim.FullName + "->" +
                aSelectableSimIfAny.FullName);

            if (!isCurrentlyAvailable(aSelectableSimIfAny, otherSim))
            {
                return;
            }

            if (RandomUtil.RandomChance(AttractionNPCBehaviorController.kAttractionPhoneCallChance))
            {
                if (otherSim == null || aSelectableSimIfAny.LotCurrent == otherSim.LotCurrent)
                {
                    SetDateAlarm();
                    return;
                }
                List<Lot> list = new List<Lot>();
                foreach (Lot allLotsWithoutCommonException in LotManager.AllLotsWithoutCommonExceptions)
                {
                    if (allLotsWithoutCommonException.LotType == LotType.Commercial)
                    {
                        list.Add(allLotsWithoutCommonException);
                    }
                }
                if (list.Count != 0)
                {
                    Lot randomObjectFromList = RandomUtil.GetRandomObjectFromList(list);
                    SimDescription otherSimDescription = npcController.mParentRelationship
                        .GetOtherSimDescription(aSelectableSimIfAny.SimDescription);
                    string dialogText = Localization.LocalizeString(aSelectableSimIfAny.IsFemale,
                        "Gameplay/Controllers/AttractionDateManager:AcceptCancelText", aSelectableSimIfAny,
                        otherSimDescription, randomObjectFromList.Name);
                    AttractionDateCall call = new AttractionDateCall(dialogText, aSelectableSimIfAny,
                        otherSimDescription, randomObjectFromList, npcController);
                    PhoneService.PlaceCall(call, OpportunityManager.PhoneCallTimeout);
                }
            }
            SetDateAlarm();
        }

        public void SetLoveLetterAlarm()
        {
            if (GameUtils.IsInstalled(ProductVersion.EP8))
            {
                npcController.mLoveLetterAlarmHandle = AlarmManager.Global.AddAlarmDayNonRepeating(4f,
                    SimClock.DayAfter(SimClock.CurrentDayOfWeek), AttractionLoveLetterAlarm,
                    "Attraction Love Letter Alarm", AlarmType.AlwaysPersisted, npcController);
            }
        }

        public void AttractionLoveLetterAlarm()
        {
            if (!GameUtils.IsInstalled(ProductVersion.EP8))
            {
                return;
            }

            if (!npcController.IsValidRelationship())
            {
                npcController.Dispose();
                return;
            }

            Sim aSelectableSimIfAny = npcController.mParentRelationship.GetASelectableSimIfAny();
            Sim otherSim = npcController.mParentRelationship.GetOtherSim(aSelectableSimIfAny);
            Main.DebugNote("Attraction Love Letter check: " + otherSim.FullName + "->" +
                aSelectableSimIfAny.FullName);

            if (!isCurrentlyAvailable(aSelectableSimIfAny, otherSim))
            {
                return;
            }


            if (RandomUtil.InterpolatedChance(AttractionNPCBehaviorController.kAttractionLTRThresholds[0],
                AttractionNPCBehaviorController.kAttractionLTRThresholds[1], 0f,
                AttractionNPCBehaviorController.kAttractionGiftLetterMaxChancePerDay,
                npcController.mParentRelationship.LTR.Liking))
            {
                LoveLetter.CreateLetterAndPlaceInMailbox(aSelectableSimIfAny,
                    otherSim.SimDescription.SimDescriptionId, isResponse: false);
            }
            SetLoveLetterAlarm();
        }
    }

}

