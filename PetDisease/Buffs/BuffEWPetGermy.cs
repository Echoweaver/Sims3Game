using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.PetDisease.Loader;


//Template Created by Battery

// Germy (Whitecough) -- common cold.
//   -- Generated from weather changes
// Events:
// 	kWeatherStarted,
//	kWeatherStopped,
//	kChangedInsideOutsideStatus,
//   -- Symptoms: Coughing, energy reduction

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetGermy : Buff
	{
		public const ulong mGuid = 0x9086F0050AC3673Dul;
        public const BuffNames buffName = (BuffNames)mGuid;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenSymptoms = 60f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenSymptoms = 120f;

        [TunableComment("Min cold duration (Hours)")]
        [Tunable]
        public static float kMinDuration = 48f;

        [TunableComment("Max cold duration (Hours)")]
        [Tunable]
        public static float kMaxDuration = 96f;

        [TunableComment("1 in x chance cold will become pneumonia if left untreated")]
        [Tunable]
        public static float kChanceOfPneumonia = 4f;


        public class BuffInstanceEWPetGermy : BuffInstance
        {
            public ReactionBroadcaster PetGermyContagionBroadcaster;
            public SimDescription mSickSim;
            public override SimDescription TargetSim => mSickSim;
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
                mSickSim = targetSim;
            }

            public override void Dispose(BuffManager bm)
            {
                if (mSymptomAlarm != AlarmHandle.kInvalidHandle)
                {
                    bm.Actor.RemoveAlarm(mSymptomAlarm);
                    mSymptomAlarm = AlarmHandle.kInvalidHandle;
                }
            }

            public void DoSymptom()
            {
                DebugNote("Germy symptom: " + mSickSim.FullName);

                // Currently only symptom is coughing.
                mSickSim.CreatedSim.InteractionQueue.AddNext(Cough.Singleton.CreateInstance(mSickSim.CreatedSim,
                    mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                    isAutonomous: true, cancellableByPlayer: false));


                mSymptomAlarm = mSickSim.CreatedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetGermy: Time until next symptom",
                    AlarmType.DeleteOnReset);
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

        public static void CheckWeatherContagion(Sim s)
        {
            // Check to see if sim catches a cold.
            // TODO: Should there be a greater chance if it's cold? Or strong wind?
            // TODO: Should there be a cooldown timer/buff?
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kInteractSicknessOdds))
            // kAmbientSicknessOdds = 5%, kInteract = 10%
            {
                DebugNote("Sim Caught Germy: " + s.Name);

                // Get Sick with incubation time
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "pet germy incubation alarm",
                    AlarmType.AlwaysPersisted);
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
                    sickSim.BuffManager.AddElement(buffName, RandomUtil.GetFloat(kMinDuration,
                    kMaxDuration), Origin.None);
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
            buffInstance.mSickSim = bm.Actor.SimDescription;
            buffInstance.willBecomePnumonia = RandomUtil.RandomChance(kChanceOfPneumonia);
            if (buffInstance.willBecomePnumonia)
            {
                DebugNote("Germy can turn into pneumonia: " + buffInstance.mSickSim.FullName);
            }
            buffInstance.PetGermyContagionBroadcaster = new ReactionBroadcaster(bi.TargetSim.CreatedSim,
                BuffGermy.kSickBroadcastParams, PetGermyContagionCallback);
            buffInstance.DoSymptom();
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetGermy buffInstance = bi as BuffInstanceEWPetGermy;
            // Check to see if this turns into Pneumonia and add buff if applicable.
            // TODO: when the buff is removed from treatment, make sure to set the
            // pneumonia check to false.
            if (buffInstance.willBecomePnumonia)
            {
                bm.AddElement(BuffEWPetPneumonia.buffName, Origin.None);
            }
            if (buffInstance.PetGermyContagionBroadcaster != null)
            {
                buffInstance.PetGermyContagionBroadcaster.Dispose();
                buffInstance.PetGermyContagionBroadcaster = null;
            }
            base.OnRemoval(bm, bi);
        }

        public static void CheckAmbientContagion(Sim s)
        {
            // Humans can catch from cats but not the reverse unless I can figure
            // out some broadcaster magic.
            if ((s.IsADogSpecies || s.IsCat) && s.SimDescription.AdultOrAbove)
            {
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
                if (!s.BuffManager.HasElement(buffName) && RandomUtil
                    .RandomChance01(HealthManager.kProximitySicknessOdds))
                {
                    // Get Sick
                    DebugNote("Sim Caught Germy: " + s.Name);
                    s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                        HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                        TimeUnit.Hours, new GetSick(s).Execute, "pet germy incubation alarm",
                        AlarmType.AlwaysPersisted);
                }
            }
        }

    }

}