using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using static Echoweaver.Sims3Game.PetDisease.PetDiseaseManager;


//Template Created by Battery
// Pneumonia (Greencough)
//   -- Develops from Germy.
//   -- Symptoms: Fever, exhaustion, frequent coughing
//   -- Contagion by contact
//   -- Can be lethal.

// Pant, feverish moodlet, pass out, cough
// Dog panting animation, coughing from EWPetGermy

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
	//XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWPetPneumonia : Buff
	{
		public const ulong mGuid = 0x904F100B14974699ul;
        public const BuffNames buffName = (BuffNames)mGuid;

        public class BuffInstanceEWPetPneumonia : BuffInstance
        {
            public Sim mSickSim;
            public override SimDescription TargetSim => mSickSim.SimDescription;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public bool isDeadly = false;

            public BuffInstanceEWPetPneumonia()
            {
            }

            public BuffInstanceEWPetPneumonia(Buff buff, BuffNames buffGuid, int effectValue,
                float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetPneumonia buffInstance = new BuffInstanceEWPetPneumonia(mBuff,
                    mBuffGuid, mEffectValue, mTimeoutCount);
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
                int symptomType = 0;
                InteractionPriority priority = new InteractionPriority(InteractionPriorityLevel.High);
                if (mSickSim.IsSleeping || mSickSim.SimInRabbitHolePosture || mSickSim.Posture is SittingInVehicle)
                {
                    // Don't force these situations to terminate just to run a symptom
                    priority = new InteractionPriority(InteractionPriorityLevel.UserDirected);
                }
                if (mSickSim.IsSleeping)
                {
                    // if sim is sleeping 50% nothing will happen
                    // Symptoms while sleeping will queue instead of interrupting
                    symptomType = RandomUtil.GetInt(1, 6);
                }
                else
                {
                    symptomType = RandomUtil.GetInt(1, 3);
                }
                DebugNote("Pneumonia symptom " + symptomType + ": " + mSickSim.FullName);

                if (symptomType == 1)
                {
                    mSickSim.InteractionQueue.AddNext(BuffEWPetGermy.Cough.Singleton.CreateInstance(mSickSim,
                        mSickSim, priority, isAutonomous: false, cancellableByPlayer: false));
                    // Additional energy loss for pneumonia
                    mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                            .GetMotiveValue(CommodityKind.Energy) - 10);
                }
                else if (symptomType == 2)
                {
                    mSickSim.InteractionQueue.AddNext(Wheeze.Singleton.CreateInstance(mSickSim,
                        mSickSim, priority, isAutonomous: false, cancellableByPlayer: false));
                    mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                            .GetMotiveValue(CommodityKind.Energy) - 20);
                }
                else if (symptomType == 3)
                {

                    mSickSim.BuffManager.AddElement(BuffNames.ExhaustedPet, 30f,
                        (Origin)ResourceUtils.HashString64("FromDisease"));
                    mSickSim.InteractionQueue.AddNext(BuffExhausted.PassOut.Singleton.CreateInstance(mSickSim,
                        mSickSim, priority, isAutonomous: false, cancellableByPlayer: false) as BuffExhausted.PassOut);
                    if (mSickSim.Motives.GetMotiveValue(CommodityKind.Energy) > 20)
                    {
                        mSickSim.Motives.SetValue(CommodityKind.Energy, 20);
                    }
                }

                if (mSickSim.BuffManager.HasElement(buffName))
                {
                    DebugNote("Set Symptom alarm: " + mSickSim.FullName);
                    mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenPneumoniaSymptoms,
                    kMaxTimeBetweenPneumoniaSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetPneumonia: Time until next symptom",
                    AlarmType.DeleteOnReset);
                }

            }
        }

        public class Wheeze : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, Wheeze>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return LocalizeString("Wheeze");
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Wheezing symptom: " + Actor.FullName);
                StandardEntry();

                Actor.PlaySoloAnimation("a_wheeze_x", yield: true, ProductVersion.EP5);

                Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives
                        .GetMotiveValue(CommodityKind.Energy) - 30);
                StandardExit();

                return true;
            }

        }

        public static string LocalizeString(string name, params object[] parameters)
        {
            return Localization.LocalizeString("Gameplay/ActorSystems/BuffEWPetPneumonia:"
                + name, parameters);
        }

        public BuffEWPetPneumonia(Buff.BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWPetPneumonia(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetPneumonia buffInstance = bi as BuffInstanceEWPetPneumonia;
            buffInstance.mSickSim = bm.Actor;
            buffInstance.isDeadly = RandomUtil.RandomChance(kChanceOfLethalPneumonia);
            buffInstance.TimeoutCount = RandomUtil.GetFloat(kMinPneumoniaDuration, kMaxPneumoniaDuration);
            if (buffInstance.isDeadly)
            {
                DebugNote("This pneumonia is deadly: " + buffInstance.mSickSim.FullName);
            } else
            {
                DebugNote("This pneumonia NOT is deadly: " + buffInstance.mSickSim.FullName);
            }
            buffInstance.mSymptomAlarm = buffInstance.mSickSim.AddAlarm(2f, TimeUnit.Minutes,
                buffInstance.DoSymptom, "BuffEWPetPneumonia: Time until next symptom", AlarmType.DeleteOnReset);
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetPneumonia buffInstance = bi as BuffInstanceEWPetPneumonia;
            if (buffInstance.mSymptomAlarm != AlarmHandle.kInvalidHandle)
            {
                bm.Actor.RemoveAlarm(buffInstance.mSymptomAlarm);
                buffInstance.mSymptomAlarm = AlarmHandle.kInvalidHandle;
            }
            base.OnRemoval(bm, bi);
        }

        public override void OnTimeout(BuffManager bm, BuffInstance bi, OnTimeoutReasons reason)
        {
            BuffInstanceEWPetPneumonia buffInstance = bi as BuffInstanceEWPetPneumonia;
            if (buffInstance.isDeadly)
            {
                EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(buffInstance.mSickSim,
                    buffInstance.mSickSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath),
                    false, false) as EWPetSuccumbToDisease;
                die.SetDiseaseName(Localization.LocalizeString(0x07AB0D4FA0FFBCC1));
                buffInstance.mSickSim.InteractionQueue.AddNext(die);
            }
            base.OnTimeout(bm, bi, reason);
        }
    }
}