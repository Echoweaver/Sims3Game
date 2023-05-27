using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using static Echoweaver.Sims3Game.PetDisease.PetDiseaseManager;


//Template Created by Battery
// Stomach Flu
//   -- Generated from Contact with non-resident animals, drink/play in toilet
//   -- Transmitted by proximity to vomit piles or vomiting
//   -- Is it possible to tag vomit piles as diseased?
//   -- Symptoms: random nausea moodlets
//   -- Called Tummy Trouble, looks identical to Food Poisoning
// Food Poisoning
//   -- Generated from eating, likely with spoiled food
//   -- Also generated by from some kinds of berries?
//      -- (holly, juniper, nightshade, dogwood, poke, mistletoe, elderberry,  gooseberries,
//      -- marionberries, salmonberries, cherries, and serviceberries, baneberries, *tomatoes,
//      -- *onions, *garlic, rubarb, tree nuts, aloe (cats), parsley (cats))
//      -- Proposed: Cherry, tomato, onion, garlic
//   -- Not contagious
//   -- Symptoms: random nausea moodlets
//   -- Called Tummy Trouble, looks identical to Stomach Flu


namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
    //XMLBuffInstanceID = 5522594682370665020ul
	public class BuffEWTummyTrouble : Buff
	{
		public const ulong mGuid = 0xDFF72BA95943E99Dul;
		public const BuffNames buffName = (BuffNames)mGuid;

        public static ReactionBroadcasterParams kSickBroadcastParams = new ReactionBroadcasterParams();

        public class BuffInstanceEWTummyTrouble : BuffInstance
		{
			public Sim mSickSim;
			public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public bool isFlu = false;
            public ReactionBroadcaster NauseaContagionBroadcaster;

			public BuffInstanceEWTummyTrouble()
			{
			}

			public BuffInstanceEWTummyTrouble(Buff buff, BuffNames buffGuid, int effectValue,
                float timeoutCount)
				: base(buff, buffGuid, effectValue, timeoutCount)
			{
			}

			public override BuffInstance Clone()
			{
				BuffInstanceEWTummyTrouble buffInstance = new BuffInstanceEWTummyTrouble(mBuff, mBuffGuid,
                    mEffectValue, mTimeoutCount);
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

            public void SetIsFlu(bool p_isFlu)
            {
                isFlu = p_isFlu;
                if (isFlu)
                {
                    NauseaContagionBroadcaster = new ReactionBroadcaster(mSickSim,
                        kSickBroadcastParams, NauseaContagionCallback);
                }
            }

            public void DoSymptom()
			{
                // TODO: Add correct buff origin (from Tummy Trouble)
                DebugNote("Tummy Trouble add nauseous buff: " + mSickSim.FullName);
                mSickSim.BuffManager.AddElement(BuffNames.NauseousPet, (Origin)ResourceUtils
                    .HashString64("FromTummyTrouble"));

                if (mSickSim.BuffManager.HasElement(buffName))
                {
                    mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenNauseaSymptoms,
                    kMaxTimeBetweenNauseaSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWTummyTrouble: Time until next symptom",
                    AlarmType.DeleteOnReset);
                }
                else
                {
                    DebugNote("Attempted TummyTrouble symptom but the moodlet has been removed: " +
                        mSickSim.FullName);
                }
			}

            public void NauseaContagionCallback(Sim s, ReactionBroadcaster rb)
            {
                if (s.SimDescription.AdultOrAbove && s.IsPet && !s.TraitManager.HasElement(TraitNames
                    .VomitImmunityPet))
                {
                    TraitManager traitManager = s.TraitManager;
                    float getSickOdds = kStomachFluProximityOdds;
                    if (traitManager.HasElement(TraitNames.PiggyPet))
                    {
                        getSickOdds *= 1.2f;
                    }

                    if (RandomUtil.RandomChance01(getSickOdds))
                    {
                        // Get Sick
                        if (s != null && mSickIncubationAlarm == AlarmHandle.kInvalidHandle &&
                            !s.BuffManager.HasElement(buffName))
                        {
                            mSickIncubationAlarm = s.AddAlarm(RandomUtil.GetFloat(kMaxStomachFluIncubationTime -
                                kMinStomachFluIncubationTime) + kMinStomachFluIncubationTime, TimeUnit.Hours,
                                new GetSick(s, true).Execute, "stomach flu incubation alarm",
                                AlarmType.AlwaysPersisted);
                        }
                    }
                }
            }
        }

        public static void CheckInteractionContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kInteractSicknessOdds))
            // kAmbientSicknessOdds = 5%, kInteract = 10%
            // (Interact is interacting with potentially contaminated objects)
            {
                DebugNote("Sim Caught stomach flu: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, true).Execute, "pet stomach flu incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckAmbientContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kAmbientSicknessOdds))
            // kAmbientSicknessOdds = 5%, kInteract = 10%
            {
                DebugNote("Sim Caught stomach flu: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, true).Execute, "pet stomach flu incubation alarm",
                    AlarmType.AlwaysPersisted);
            } else
            {
                DebugNote("Sim did not catch stomach flu: " + s.Name);
            }
        }

        public static void CheckEatContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kInteractSicknessOdds + HealthManager.kProximitySicknessOdds))
            // This should be 20% because c'mon, eating trash.
            {
                DebugNote("Sim Caught stomach flu: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, true).Execute, "pet stomach flu incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckAmbientPoisoning(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(kAmbientPoisonOdds))
            {
                DebugNote("Sim Caught stomach food poisoning: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, false).Execute, "pet food poisoning incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckFoodPoisoning(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kInteractSicknessOdds))
            {
                // Get Sick
                DebugNote("Sim Caught stomach food poisoning: " + s.Name);
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, false).Execute, "pet food poisoning incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckFoodPoisoningSpoiled(Sim s)
        {
            // This is from eating spoiled human food. Pet can get very sick.
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kRomanticSicknessOdds))
            {
                // Get Sick
                DebugNote("Sim Caught stomach food poisoning: " + s.Name);
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s, false).Execute, "pet food poisoning incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public class GetSick
        {
            Sim sickSim;
            bool isFlu;
            public GetSick(Sim sim, bool flu)
            {
                sickSim = sim;
                isFlu = flu;
            }

            public void Execute()
            {
                if (!sickSim.BuffManager.HasElement(buffName))
                {
                    sickSim.BuffManager.AddElement(buffName, Origin.None);
                    BuffInstanceEWTummyTrouble buffInstance = sickSim.BuffManager.GetElement(buffName)
                        as BuffInstanceEWTummyTrouble;
                    buffInstance.SetIsFlu(isFlu);  
                    
                    // TODO: Should we have a popup?
                    // createdSim.ShowTNSAndPlayStingIfSelectable("sting_get_sick", TNSNames.GotSickTNS, createdSim, null, null, null, new bool[1] { createdSim.IsFemale }, false, createdSim);
                }
            }
        }

        public BuffEWTummyTrouble(Buff.BuffData info) : base(info)
        {
        }

        public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
        {
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWTummyTrouble(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWTummyTrouble buffInstance = bi as BuffInstanceEWTummyTrouble;
            buffInstance.mSickSim = bm.Actor;
            buffInstance.TimeoutCount = RandomUtil.GetFloat(kMinNauseaDuration, kMaxNauseaDuration);
            buffInstance.mSymptomAlarm = buffInstance.mSickSim.AddAlarm(2f, TimeUnit.Minutes,
                buffInstance.DoSymptom, "BuffEWTummyTrouble: Time until next symptom", AlarmType.DeleteOnReset);
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWTummyTrouble buffInstance = bi as BuffInstanceEWTummyTrouble;
            if (buffInstance.NauseaContagionBroadcaster != null)
            {
                buffInstance.NauseaContagionBroadcaster.Dispose();
                buffInstance.NauseaContagionBroadcaster = null;
            }
            if (buffInstance.mSymptomAlarm != AlarmHandle.kInvalidHandle)
            {
                bm.Actor.RemoveAlarm(buffInstance.mSymptomAlarm);
                buffInstance.mSymptomAlarm = AlarmHandle.kInvalidHandle;
            }
            base.OnRemoval(bm, bi);
        }
    }

}