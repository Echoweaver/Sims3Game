using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.PetDisease.Buffs.BuffEWPetPneumonia;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using static Echoweaver.Sims3Game.PetDisease.PetDiseaseManager;


//Template Created by Battery

// Germy (Whitecough) -- common cold.
//   -- Generated from weather changes
// Events:
// 	kWeatherStarted,
//	kChangedInsideOutsideStatus,
//   -- Symptoms: Coughing, energy reduction

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetGermy : Buff
	{
		public const ulong mGuid = 0x9086F0050AC3673Dul;
        public const BuffNames buffName = (BuffNames)mGuid;



        public class BuffInstanceEWPetGermy : BuffInstance
        {
            public ReactionBroadcaster PetGermyContagionBroadcaster;
            public Sim mSickSim;
            public override SimDescription TargetSim => mSickSim.SimDescription;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public bool willBecomePnumonia = false;

            public BuffInstanceEWPetGermy()
            {
            }

            public BuffInstanceEWPetGermy(Buff buff, BuffNames buffGuid, int effectValue, float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetGermy buffInstance = new BuffInstanceEWPetGermy(mBuff, mBuffGuid, mEffectValue, mTimeoutCount);
                buffInstance.mSickSim = mSickSim;
                return buffInstance;
            }

            public override void SetTargetSim(SimDescription targetSim)
            {
                mSickSim = targetSim.CreatedSim;
            }

            public override float UITimeoutCount
            {
                get
                {
                    if (kPetDiseaseDebug)
                    {
                        return this.TimeoutCount;
                    }
                    else return -1f;
                }
            }

            public void DoSymptom()
            {
                // Currently only symptom is coughing.
                mSickSim.InteractionQueue.AddNext(Cough.Singleton.CreateInstance(mSickSim, mSickSim,
                    new InteractionPriority(InteractionPriorityLevel.UserDirected),
                    isAutonomous: true, cancellableByPlayer: false));

                if (mSickSim.BuffManager.HasElement(buffName))
                {

                    mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenGermySymptoms,
                    kMaxTimeBetweenGermySymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetGermy: Time until next symptom",
                    AlarmType.DeleteOnReset);
                }
                else
                {
                    DebugNote("Attempted Pet Germy symptom, but moodlet was remmoved: " + mSickSim.FullName);
                }
            }
        }

        public static string LocalizeString(string name, params object[] parameters)
        {
            return Localization.LocalizeString("Gameplay/ActorSystems/BuffEWGermy:"
                + name, parameters);
        }

        public class Cough : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, Cough>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return "Localize - Cough";
                    //return LocalizeString("Cough");
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Coughing symptom: " + Actor.FullName);
                StandardEntry();
                if (Actor.Posture != Actor.PetSittingOnGround)
                {
                    PetSittingOnGroundPosture.SitOnGround(Actor);
                    Actor.Posture = Actor.PetSittingOnGround;
                }
                Actor.PlaySoloAnimation("a_idle_sit_hack_x", yield: true, ProductVersion.EP5);
                Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives
                        .GetMotiveValue(CommodityKind.Energy) - 20);
                StandardExit();

                return true;
            }

        }

        private static bool CheckContagionEligibility(Sim s)
        {
            if (s.BuffManager.HasElement(buffName))
            {
                DebugNote("Sim already has Germy: " + s.Name);
                return false;
            }
            if (s.BuffManager.HasElement(Buffs.BuffEWPetPneumonia.buffName))
            {
                DebugNote("Sim already has Pneumonia: " + s.Name);
                return false;
            }
            // Can't vaccinate against the common cold.
            return true;
        }

        public static void CheckWeatherContagion(Sim s)
        {
            // Check to see if sim catches a cold.
            // TODO: Should there be a greater chance if it's cold? Or strong wind?
            // TODO: Should there be a cooldown timer/buff?
            if (!CheckContagionEligibility(s))
                return;
            if (RandomUtil.RandomChance01(HealthManager.kInteractSicknessOdds))
            // kAmbientSicknessOdds = 5%, kInteract = 10%
            {
                DebugNote("Sim Caught Germy: " + s.Name);

                // Get Sick with incubation time
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "pet germy incubation alarm",
                    AlarmType.AlwaysPersisted);
            } else
            {
                DebugNote("Sim did NOT catch Germy: " + s.Name);
            }
        }

        public class GetSick
        {
            Sim sickSim;
            public GetSick(Sim sim)
            {
                sickSim = sim;
            }

            public void Execute()
            {
                if (!sickSim.BuffManager.HasElement(buffName) && !PetDiseaseManager.CheckForVaccination(sickSim))
                {
                    // TODO: Should check for cooldown buff so pet can't get the same disease
                    // immediately after?
                    sickSim.BuffManager.AddElement(buffName, Origin.None);
                }
            }
        }

        public BuffEWPetGermy(Buff.BuffData info) : base(info)
        {
        }
        public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
        {
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWPetGermy(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            buffInstance.mSickSim = bm.Actor;
            buffInstance.TimeoutCount = RandomUtil.GetFloat(kMinGermyDuration, kMaxGermyDuration);
            if (!PetDiseaseManager.CheckForVaccination(buffInstance.mSickSim))
            {
                buffInstance.willBecomePnumonia = RandomUtil.RandomChance(kChanceOfPneumonia);
                if (buffInstance.willBecomePnumonia)
                {
                    DebugNote("Germy will turn into pneumonia if untreated: " + buffInstance.mSickSim.FullName);
                }
                else
                {
                    DebugNote("Germy will NOT turn into pneumonia: " + buffInstance.mSickSim.FullName);
                }
            }
            else
            {
                buffInstance.willBecomePnumonia = false;
                DebugNote("Germy can't become pneumonia because sim is vaccinated: " + buffInstance.mSickSim.FullName);
            }

            buffInstance.PetGermyContagionBroadcaster = new ReactionBroadcaster(bm.Actor,
                BuffGermy.kSickBroadcastParams, PetGermyContagionCallback);
            base.OnAddition(bm, bi, travelReaddition);
            buffInstance.mSymptomAlarm = buffInstance.mSickSim.AddAlarm(2f, TimeUnit.Minutes,
                buffInstance.DoSymptom, "BuffEWPetGermy: Time until next symptom", AlarmType.DeleteOnReset);
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            // Check to see if this turns into Pneumonia and add buff if applicable.
            // TODO: when the buff is removed from treatment, make sure to set the
            // pneumonia check to false.
            if (buffInstance.PetGermyContagionBroadcaster != null)
            {
                buffInstance.PetGermyContagionBroadcaster.Dispose();
                buffInstance.PetGermyContagionBroadcaster = null;
            }
            if (buffInstance.mSymptomAlarm != AlarmHandle.kInvalidHandle)
            {
                bm.Actor.RemoveAlarm(buffInstance.mSymptomAlarm);
                buffInstance.mSymptomAlarm = AlarmHandle.kInvalidHandle;
            }
            base.OnRemoval(bm, bi);
        }

        public override void OnTimeout(BuffManager bm, BuffInstance bi, OnTimeoutReasons reason)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            if (buffInstance.willBecomePnumonia)
            {
                bm.AddElement(BuffEWPetPneumonia.buffName, RandomUtil.GetFloat(kMinPneumoniaDuration,
                    kMaxPneumoniaDuration), Origin.None);
            }
            base.OnTimeout(bm, bi, reason);
        }

        public static void CheckAmbientContagion(Sim s)
        {
            if (!CheckContagionEligibility(s))
                return;
            if (!s.BuffManager.HasElement(buffName) && RandomUtil
                .RandomChance01(HealthManager.kAmbientSicknessOdds))
            {
                // Get Sick
                DebugNote("Sim Caught Germy: " + s.Name);
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "pet germy incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
            else
            {
                DebugNote("Sim did NOT catch Germy: " + s.Name);
            }
        }

        public void PetGermyContagionCallback(Sim s, ReactionBroadcaster rb)
        {
            // Humans can catch from cats but not the reverse unless I can figure
            // out some broadcaster magic.
            if (s.SimDescription.IsHuman)
            {
                s.SimDescription.HealthManager?.PossibleProximityContagion();
            } else if ((s.IsADogSpecies || s.IsCat) && s.SimDescription.AdultOrAbove)
            {
                DebugNote("Check proximity contagion: " + s.Name);
                if (!CheckContagionEligibility(s))
                    return;
                if (!s.BuffManager.HasElement(buffName) && RandomUtil
                    .RandomChance01(HealthManager.kProximitySicknessOdds))
                {
                    // Get Sick
                    DebugNote("Sim Caught Germy (Proximity): " + s.Name);
                    s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                        HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                        TimeUnit.Hours, new GetSick(s).Execute, "pet germy incubation alarm",
                        AlarmType.AlwaysPersisted);
                }
                else
                {
                    DebugNote("Sim did NOT catch Germy (Proximity): " + s.Name);
                }
            }
        }
    }

}