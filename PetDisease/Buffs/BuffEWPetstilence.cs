using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Actors.Sim;
using static Echoweaver.Sims3Game.PetDisease.Loader;


//Template Created by Battery

// Petstilence (Carrionplace Disease)
//   -- Generated from fights or hunting (maybe just hunting rodents), getting fleas
//   -- Bloodborne, transmitted by fighting, woohoo
//   -- Symptoms: passing out and vomiting, moments of psychosis, drooling
//   -- Frequently lethal.

// drool effect codes:
// ep5catdrool
// ep5dogdrool
// ep5dogdrooljaw
// ep5doglittledrool
// ep5doglittledrooljaw

// ep5DogDroolSpitUp
// ep5DogLittleDroolSpitUp

namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
    //XMLBuffInstanceID = 5522594682370665020ul
    public class BuffEWPetstilence : Buff
	{
		public const ulong mGuid = 0x7768716F913C2054ul;
        public const BuffNames buffName = (BuffNames)mGuid;

        [Tunable]
        [TunableComment("Range: Sim minutes.  Description:  Min time between symptoms.")]
        public static float kMinTimeBetweenSymptoms = 120f;

        [TunableComment("Range: Sim minutes.  Description:  Max time between symptoms.")]
        [Tunable]
        public static float kMaxTimeBetweenSymptoms = 360f;

        [TunableComment("Min petstilence duration (Hours)")]
        [Tunable]
        public static float kMinDuration = 48f; // 2 days

        [TunableComment("Max petstilence duration (Hours)")]
        [Tunable]
        public static float kMaxDuration = 96f; // 4 days

        [TunableComment("Chance of catching from stranger contact")]
        [Tunable]
        public static float kAmbientSicknessOdds = 0.01f; 

        public class BuffInstanceEWPetstilence : BuffInstance
        {
            public SimDescription mSickSim;
            public override SimDescription TargetSim => mSickSim;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
            public float mStageLength = 24f;  // Length of disease stage in hours
            public int mStage = 0;

            public VisualEffect mDroolEffect;
            public VisualEffect mDroolEffectJaw;
            public VisualEffect mFlyEffect;

            public BuffInstanceEWPetstilence()
            {
            }

            public BuffInstanceEWPetstilence(Buff buff, BuffNames buffGuid, int effectValue,
                float timeoutCount)
                : base(buff, buffGuid, effectValue, timeoutCount)
            {
            }

            public override BuffInstance Clone()
            {
                BuffInstanceEWPetstilence buffInstance = new BuffInstanceEWPetstilence(mBuff, mBuffGuid,
                    mEffectValue, mTimeoutCount);
                buffInstance.mSickSim = mSickSim;
                return buffInstance;
            }

            public override void SetTargetSim(SimDescription targetSim)
            {
                mSickSim = targetSim;
            }

            public void StartFx(Sim owner)
            {
                DebugNote("Start Petstilence drooling: " + owner.FullName);

                if (owner.IsCat)
                {
                    mDroolEffect = VisualEffect.Create("ep5catdrool");
                    mDroolEffect.ParentTo(owner, Sim.FXJoints.Mouth);
                }
                else if (owner.IsLittleDog)
                {
                    mDroolEffect = VisualEffect.Create("p5doglittledrool");
                    mDroolEffect.ParentTo(owner, Sim.FXJoints.Mouth);
                    mDroolEffectJaw = VisualEffect.Create("ep5doglittledrooljaw");
                    mDroolEffectJaw.ParentTo(owner, Sim.FXJoints.Mouth);
                }
                else
                {
                    mDroolEffect = VisualEffect.Create("p5dogdrool");
                    mDroolEffect.ParentTo(owner, Sim.FXJoints.Mouth);
                    mDroolEffectJaw = VisualEffect.Create("ep5dogdrooljaw");
                    mDroolEffectJaw.ParentTo(owner, Sim.FXJoints.Mouth);
                }

                    mFlyEffect = VisualEffect.Create("ep7BuffSickandTired_main");
                    mFlyEffect.ParentTo(owner, Sim.FXJoints.Pelvis);
                    mFlyEffect.Start();
                    //mFlyEffect.mDisgustBroadcaster = new ReactionBroadcaster(sim, DisgustSimsBroadcasterParams, DisgustSims);
                }


            public void StopFx()
            {
                if (mDroolEffect != null)
                {
                    mDroolEffect.Stop(VisualEffect.TransitionType.HardTransition);
                    mDroolEffect.Dispose();
                    mDroolEffect = null;
                }
                if (mDroolEffectJaw != null)
                {
                    mDroolEffectJaw.Stop(VisualEffect.TransitionType.HardTransition);
                    mDroolEffectJaw.Dispose();
                    mDroolEffectJaw = null;
                }
                if (mFlyEffect != null)
                {
                    mFlyEffect.Stop(VisualEffect.TransitionType.HardTransition);
                    mFlyEffect.Dispose();
                    mFlyEffect = null;
                }
            }

            public override void Dispose(BuffManager bm)
            {
                StopFx();
                if (mSymptomAlarm != AlarmHandle.kInvalidHandle)
                {
                    bm.Actor.RemoveAlarm(mSymptomAlarm);
                    bm.Actor.RemoveAlarm(mAdvanceDiseaseAlarm);
                    mSymptomAlarm = AlarmHandle.kInvalidHandle;
                    mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
                }
            }

            public void DoSymptom()
            {
                int symptomType = 0;

                DebugNote("Petstilence Level " + " + " + mStage + " symptom: " + mSickSim.FullName);

                switch (mStage)
                {
                    case 0:
                        // First stage, mildest symptoms. Maybe just a fuzzy feeling
                        symptomType = RandomUtil.GetInt(1, 3);
                        if (symptomType == 1)
                        {
                            RandomPetStartle action = RandomPetStartle.Singleton.CreateInstance(mSickSim.CreatedSim,
                                mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                false, false) as RandomPetStartle;
                            mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                        }
                        // a_react_stand_scaredShiver_x
                        // catfreakoutobject
                        break;
                    case 1:
                        symptomType = RandomUtil.GetInt(1, 3);
                        if (symptomType == 1)
                        {
                            ActWacky action = ActWacky.Singleton.CreateInstance(mSickSim.CreatedSim,
                                mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                false, false) as ActWacky;
                            mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                        }
                        else if (symptomType == 2)
                        {
                            BeSkittishAboutFullMoon action = BeSkittishAboutFullMoon.Singleton.CreateInstance(mSickSim.CreatedSim,
                                mSickSim.CreatedSim,new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                false, false) as BeSkittishAboutFullMoon;
                            mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                        }
                        else
                        {
                            // Hiss/growl at random sim
                            List<Sim> list = new List<Sim>(mSickSim.CreatedSim.LotCurrent.GetSims());
                            list.Remove(mSickSim.CreatedSim);
                            foreach (Sim s in list)
                            {
                                if (s.SimDescription.ChildOrBelow)
                                {
                                    list.Remove(s);
                                }
                                else if (s.IsHorse)
                                {
                                    list.Remove(s);
                                }
                            }
                            Sim target = null;
                            if (list.Count > 0)
                            {
                                target = RandomUtil.GetRandomObjectFromList<Sim>(list);
                            } else
                            {
                                DebugNote("Petstilence attempted at random sim but no targets available");
                                return;
                            }
                            DebugNote("Petstilence Hiss/Growl at random sim " + target.FullName);
                            if (mSickSim.IsCat)
                            {
                                InteractionInstance action = new SocialInteractionA.Definition("Cat Hiss",
                                    new string[0], null, initialGreet: false).CreateInstance(target,
                                    mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                    false, false);
                                mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                            } else
                            {
                                InteractionInstance action = new SocialInteractionA.Definition("Growl At",
                                    new string[0], null, initialGreet: false).CreateInstance(target,
                                    mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                    false, false);
                                mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                            }
                        }
                        break;
                    case 2:
                        // Final stage, fly swarms (?), hygiene drops, random mean chase or fight 
                        // Drop hygiene to 0 with fly swarm
                        // Attack/chase random sim
                        symptomType = RandomUtil.GetInt(1, 3);
                        if (symptomType == 1)
                        {
                            BuffExhausted.PassOut action = BuffExhausted.PassOut.Singleton.CreateInstance(mSickSim.CreatedSim,
                                mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.High),
                                false, false) as BuffExhausted.PassOut;
                            mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                        } else if (symptomType == 2)
                        {
                            List<Sim> list = new List<Sim>(mSickSim.CreatedSim.LotCurrent.GetSims());
                            list.Remove(mSickSim.CreatedSim);

                            foreach (Sim s in list)
                            {
                                if (s.SimDescription.ChildOrBelow)
                                {
                                    list.Remove(s);
                                } else if (s.IsHorse)
                                {
                                    list.Remove(s);
                                }
                            }
                            Sim target = null;
                            if (list.Count > 0)
                            {
                                target = RandomUtil.GetRandomObjectFromList<Sim>(list);
                            }
                            else
                            {
                                DebugNote("Petstilence attempted fight/chase random sim but no targets available");
                                return;
                            }
                            DebugNote("Petstilence fight/chase random sim " + target.FullName);
                            if (target.IsCat || target.IsADogSpecies)
                            {
                                FightPet action = FightPet.Singleton.CreateInstance(target, mSickSim.CreatedSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as FightPet;
                                mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                            } else
                            {
                                ChaseMean action = ChaseMean.Singleton.CreateInstance(target, mSickSim.CreatedSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as ChaseMean;
                                mSickSim.CreatedSim.InteractionQueue.AddNext(action);
                            }
                        } else
                        {
                            
                        }
                        break;
                    default:
                        break;
                }

                mSymptomAlarm = mSickSim.CreatedSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetstilence: Time until next symptom",
                    AlarmType.DeleteOnReset);
            }

            public void AdvanceDisease()
            {
                if (mStage < 3)
                {
                    mStage++;
                    mAdvanceDiseaseAlarm = mSickSim.CreatedSim.AddAlarm(mStageLength, TimeUnit.Minutes,
                        AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                        AlarmType.DeleteOnReset);
                    if (mStage == 1)
                    {
                        // Start drooling
                    }
                    DoSymptom();
                } else if (mStage >= 3)
                {
                    // This disease is lethal if not cured
                    if (Loader.kAllowPetDiseaseDeath)
                    {
                        mSickSim.CreatedSim.Kill(Loader.diseaseDeathType);
                    } else
                    {
                        EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(mSickSim.CreatedSim,
                            mSickSim.CreatedSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false)
                            as EWPetSuccumbToDisease;
                        mSickSim.CreatedSim.InteractionQueue.AddNext(die);
                    }
                }
            }
        }

        public class ActWacky : Interaction<Sim, Sim>
        {
            public class Definition : SoloSimInteractionDefinition<ActWacky>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return a.IsCat || a.IsADogSpecies;
                }
            }

            [TunableComment("Min/Max Num sim minutes cat should animate randomly picked between these values")]
            [Tunable]
            public static int[] kMinMaxMinsToLoop = new int[2] { 10, 15 };

            public static ISoloInteractionDefinition Singleton = new Definition();

            public override void ConfigureInteraction()
            {
                base.ConfigureInteraction();
                base.Hidden = true;
            }

            public override bool Run()
            {
                StandardEntry();
                EnterStateMachine("wacky_pet", "Enter", "x");
                AnimateSim("Loop");
                BeginCommodityUpdates();
                bool flag = DoTimedLoop(RandomUtil.GetInt(kMinMaxMinsToLoop[0], kMinMaxMinsToLoop[1]));
                EndCommodityUpdates(flag);
                AnimateSim("Exit");
                StandardExit();
                return flag;
            }
        }

        public static void CheckAmbientContagion(Sim s)
        {
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(kAmbientSicknessOdds))
            // This ambient check is 1%. It's rare to catch.
            {
                DebugNote("Sim Caught Petstilence: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckContactContagion(Sim s)
        {
            // Contact with subject that always has a chance of transmitting Petstilence
            // (Probably raccoon)
            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager.kInteractSicknessOdds))
            {
                DebugNote("Sim Caught Petstilence: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
        }

        public static void CheckBloodborneContagion(Sim s)
        {
            float test = RandomUtil.GetFloat(0f, 1f);

            if (!s.BuffManager.HasElement(buffName) && RandomUtil.RandomChance01(HealthManager
                .kRomanticSicknessOdds))
            // Woohoo/blood contact is 60%
            {
                // Get Sick
                DebugNote("Sim Caught Petstilence: " + s.Name);
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            } else
            {
                if (s.BuffManager.HasElement(buffName))
                {
                    DebugNote("Sim already has petstilence: " + s.Name);
                }
                else
                {
                    DebugNote("Sim did not catch pestilence: " + s.Name);
                }
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

        public BuffEWPetstilence(BuffData info) : base(info)
		{
			
		}
		
		public override bool ShouldAdd(BuffManager bm, MoodAxis axisEffected, int moodValue)
		{
            return (bm.Actor.IsADogSpecies || bm.Actor.IsCat) && bm.Actor.SimDescription.AdultOrAbove;
        }

        public override BuffInstance CreateBuffInstance()
        {
            return new BuffInstanceEWPetstilence(this, base.BuffGuid, base.EffectValue, base.TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            buffInstance.mSickSim = bm.Actor.SimDescription;
            buffInstance.mStageLength = bi.TimeoutCount / 3;
            buffInstance.mAdvanceDiseaseAlarm = bm.Actor.AddAlarm(buffInstance.mStageLength,
                TimeUnit.Minutes, buffInstance.AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                AlarmType.DeleteOnReset);
            buffInstance.DoSymptom();
            base.OnAddition(bm, bi, travelReaddition);

            // When complete, this effect will start after first stage advance
            buffInstance.StartFx(buffInstance.mSickSim.CreatedSim);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            //BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            base.OnRemoval(bm, bi);
        }

    }

    public class EWTestAnim : ImmediateInteraction<Sim, Sim>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, Sim, EWTestAnim>
        {
            public static InteractionDefinition Singleton = new Definition();

            public override bool Test(Sim actor, Sim target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "TestAnim";
            }
        }

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            Actor.InteractionQueue.AddNext(BuffEWPetGermy.Cough.Singleton.CreateInstance(Actor,
                Actor, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                isAutonomous: true, cancellableByPlayer: false));
            return true;
        }
    }
}