using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Objects;
using Sims3.SimIFace;
using System.Collections.Generic;
using static Sims3.Gameplay.Actors.Sim;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.ThoughtBalloons;

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

        [TunableComment("Min petstilence duration (Minutes)")]
        [Tunable]
        public static float kMinDuration = 5760f;  // 4 days

        [TunableComment("Max petstilence duration (Minutes)")]
        [Tunable]
        public static float kMaxDuration = 10080f;  // 1 week

        [TunableComment("Chance of catching from stranger contact")]
        [Tunable]
        public static float kAmbientSicknessOdds = 0.05f; 

        public class BuffInstanceEWPetstilence : BuffInstance
        {
            public Sim mSickSim;
            public override SimDescription TargetSim => mSickSim.SimDescription;
            public AlarmHandle mSymptomAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
            public AlarmHandle mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
            public float mStageLength = 24f;  // Length of disease stage in hours
            public int mStage = 0;

            public VisualEffect mDroolEffect; 
            public VisualEffect mHazeEffect;
            public VisualEffect mHazeEffect2;

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
                mSickSim = targetSim.CreatedSim;
            }

            public void StartDroolingFx(Sim owner)
            {
                DebugNote("Start Petstilence drooling: " + owner.FullName);

                // Non-looping effects of interest
                //mDroolEffect = VisualEffect.Create("ep5smalldogdrinkponddrips");
                //mDroolEffect = VisualEffect.Create("ep5dogmouthdrips");

                if (owner.IsCat || owner.IsLittleDog)
                {
                    mDroolEffect = VisualEffect.Create("spongebathdripsshort");
                    mDroolEffect.ParentTo(owner, Sim.FXJoints.Mouth);
                    mDroolEffect.Start();
                }
                else
                {
                    mDroolEffect = VisualEffect.Create("spongebathdrips");
                    mDroolEffect.ParentTo(owner, Sim.FXJoints.Mouth);
                    mDroolEffect.Start();
                }
            }

            public void StartHazeFx()
            {
                DebugNote("Start Petstilence fly swarm: " + mSickSim.FullName);

                //mHazeEffect = VisualEffect.Create("ep11buffhealthyglowlrg_main");
                //mHazeEffect = VisualEffect.Create("ep5unicornblessblacksparklessmpet");
                mHazeEffect = VisualEffect.Create("ep7BuffSickandTired_main");
                //mHazeEffect2 = VisualEffect.Create("ep11buffhealthyglowlrg_main");
                mHazeEffect.ParentTo(mSickSim, Sim.FXJoints.Spine2);
                //mHazeEffect2.ParentTo(mSickSim, Sim.FXJoints.Crotch);
                //Vector3 fxColor = new Vector3(0.7f, 8f, 0f);
                //Vector3 fxColor = new Vector3(0.2f, 0.2f, 0.2f);
                //mHazeEffect.SetEffectColorScale(fxColor);
                //mHazeEffect2.SetEffectColorScale(fxColor);
                mHazeEffect.Start();
                //mHazeEffect2.Start();
            }

            public void StopFx()
            {
                if (mDroolEffect != null)
                {
                    mDroolEffect.Stop(VisualEffect.TransitionType.HardTransition);
                    mDroolEffect.Dispose();
                    mDroolEffect = null;
                }
                if (mHazeEffect != null)
                {
                    mHazeEffect.Stop(VisualEffect.TransitionType.HardTransition);
                    mHazeEffect.Dispose();
                    mHazeEffect = null;
                }
                if (mHazeEffect2 != null)
                {
                    mHazeEffect2.Stop(VisualEffect.TransitionType.HardTransition);
                    mHazeEffect2.Dispose();
                    mHazeEffect2 = null;
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

                switch (mStage)
                {
                    case 0:
                        // First stage, mildest symptoms. Maybe just a fuzzy feeling
                        symptomType = RandomUtil.GetInt(1, 3);
                        DebugNote("Petstilence stage 0 Symptom type " + symptomType + ": " + mSickSim.FullName);
                        if (symptomType == 1)
                        {

                            BeSkittish action = BeSkittish.Singleton.CreateInstance(mSickSim,
                                mSickSim, priority, false, false) as BeSkittish;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else if (symptomType == 2)
                        {
                            Shiver action = Shiver.Singleton.CreateInstance(mSickSim, mSickSim,
                                priority, false, false) as Shiver;
                            mSickSim.InteractionQueue.AddNext(action);
                        } else
                        {
                            Whine action = Whine.Singleton.CreateInstance(mSickSim, mSickSim, priority,
                                false, false) as Whine;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                            
                        break;
                    case 1:
                        symptomType = RandomUtil.GetInt(1, 3);
                        DebugNote("Petstilence stage 1 Symptom type " + " + " + symptomType + ": " + mSickSim.FullName);
                        mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                                .GetMotiveValue(CommodityKind.Energy) - 20);
                        if (symptomType == 1)
                        {
                            ActWacky action = ActWacky.Singleton.CreateInstance(mSickSim, mSickSim,
                                priority, false, false) as ActWacky;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else if (symptomType == 2)
                        {
                            Yowl action = Yowl.Singleton.CreateInstance(mSickSim,
                                mSickSim, priority, false, false) as Yowl;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else
                        {
                            // Hiss/growl at random sim
                            List<Sim> list = new List<Sim>(mSickSim.LotCurrent.GetSims());
                            list.Remove(mSickSim);
                            foreach (Sim s in list)
                            {
                                if (s.SimDescription.ChildOrBelow)
                                {
                                    list.Remove(s);
                                }
                            }
                            Sim target = null;
                            if (list.Count > 0)
                            {
                                target = RandomUtil.GetRandomObjectFromList(list);
                            } else
                            {
                                DebugNote("Petstilence hiss/growl - no targets available");
                                return;
                            }
                            DebugNote("Petstilence Hiss/Growl at random sim target: " + target.FullName);
                            if (mSickSim.IsCat)
                            {
                                InteractionInstance action = new SocialInteractionA.Definition("Cat Hiss",
                                    new string[0], null, initialGreet: false).CreateInstance(target,
                                    mSickSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                    false, false);
                                mSickSim.InteractionQueue.AddNext(action);
                            } else
                            {
                                InteractionInstance action = new SocialInteractionA.Definition("Growl At",
                                    new string[0], null, initialGreet: false).CreateInstance(target,
                                    mSickSim, new InteractionPriority(InteractionPriorityLevel.UserDirected),
                                    false, false);
                                mSickSim.InteractionQueue.AddNext(action);
                            }
                        }
                        break;
                    case 2:
                        // Final stage, fly swarms (?), hygiene drops, random mean chase or fight 
                        // Drop hygiene to 0 with fly swarm
                        // Attack/chase random sim
                        symptomType = RandomUtil.GetInt(1, 3);
                        DebugNote("Petstilence stage 2 Symptom type " + " + " + symptomType + ": " + mSickSim.FullName);
                        mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                                .GetMotiveValue(CommodityKind.Energy) - 50);
                        if (symptomType == 1)
                        {
                            BuffExhausted.PassOut action = BuffExhausted.PassOut.Singleton.CreateInstance(mSickSim,
                                mSickSim, new InteractionPriority(InteractionPriorityLevel.High),
                                false, false) as BuffExhausted.PassOut;
                            mSickSim.InteractionQueue.AddNext(action);
                        } else if (symptomType == 2)
                        {
                            List<Sim> list = new List<Sim>(mSickSim.LotCurrent.GetSims());
                            list.Remove(mSickSim);

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
                                FightPet action = FightPet.Singleton.CreateInstance(target, mSickSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as FightPet;
                                mSickSim.InteractionQueue.AddNext(action);
                            } else
                            {
                                ChaseMean action = ChaseMean.Singleton.CreateInstance(target, mSickSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as ChaseMean;
                                mSickSim.InteractionQueue.AddNext(action);
                            }
                        } else
                        {
                            DebugNote("Petstilence run in terror ");
                            ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData("balloon_flames");
                            balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
                            balloonData.Duration = ThoughtBalloonDuration.Medium;
                            balloonData.mPriority = ThoughtBalloonPriority.High;
                            mSickSim.ThoughtBalloonManager.ShowBalloon(balloonData);
                            Fire.PetRunAwayFromFire action = Fire.PetRunAwayFromFire.Singleton.CreateInstance(mSickSim,
                                mSickSim, new InteractionPriority(InteractionPriorityLevel.Fire), false, false) as
                                Fire.PetRunAwayFromFire;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        break;
                    default:
                        break;
                }

                if (mSickSim.BuffManager.HasElement(buffName))
                {
                    mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenSymptoms,
                    kMaxTimeBetweenSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetstilence: Time until next symptom",
                    AlarmType.DeleteOnReset);
                }
            }

            public void AdvanceDisease()
            {
                mStage++;
                DebugNote("Petstilence advance to level " + mStage + ": " + mSickSim.FullName);
                if (mStage < 3)
                {
                    mAdvanceDiseaseAlarm = mSickSim.AddAlarm(mStageLength, TimeUnit.Minutes,
                        AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                        AlarmType.DeleteOnReset);
                    if (mStage == 1)
                    {
                        // Start drooling
                        StartDroolingFx(mSickSim);
                    } else if (mStage == 2)
                    {
                        // Start haze/fly swarm/whatever indicator of serious illness
                        StartHazeFx();
                    }
                    DoSymptom();
                } else if (mStage >= 3)
                {
                    // This disease is lethal if not cured
                    EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(mSickSim,
                        mSickSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath), false, false)
                        as EWPetSuccumbToDisease;
                    mSickSim.InteractionQueue.AddNext(die);
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
                DebugNote("Act wacky symptom.");
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

        public class Shiver : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, Shiver>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return "Localize - Shiver";
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Petstilence shiver symptom: " + Actor.FullName);
                StandardEntry();
                Actor.PlaySoloAnimation("a_react_stand_scaredShiver_x", yield: true, ProductVersion.EP5);
                StandardExit();

                return true;
            }

        }

        public class BeSkittish : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, BeSkittish>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return "Localize - Jump at Shadows";
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Petstilence skittish symptom: " + Actor.FullName);
                StandardEntry();
                Actor.PlaySoloAnimation("a_trait_skittish_x", yield: true, ProductVersion.EP5);
                StandardExit();

                return true;
            }

        }

        public class Yowl : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, Yowl>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return "Localize - Complain of Discomfort";
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Petstilence yowl/howl symptom: " + Actor.FullName);
                StandardEntry();
                if (Actor.IsCat)
                {
                    Actor.PlaySoloAnimation("a_idle_sit_yowl_x", yield: true, ProductVersion.EP5);
                } else
                {
                    Actor.PlaySoloAnimation("a_react_stand_howl_x", yield: true, ProductVersion.EP5);
                }
                StandardExit();

                return true;
            }

        }

        public class Whine : Interaction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Sim, Whine>
            {

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return "Localize - Feel Uncomfortable";
                }

                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Petstilence whine symptom: " + Actor.FullName);
                StandardEntry();
                Actor.PlaySoloAnimation("a_react_layDown_whine_start_x", yield: true, ProductVersion.EP5);
                Actor.PlaySoloAnimation("a_react_layDown_whine_loop2_x", yield: true, ProductVersion.EP5);
                Actor.PlaySoloAnimation("a_react_layDown_whine_stop_x", yield: true, ProductVersion.EP5);

                StandardExit();

                return true;
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
                    sickSim.BuffManager.AddElement(buffName, Origin.None);
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
            return new BuffInstanceEWPetstilence(this, BuffGuid, EffectValue, TimeoutSimMinutes);
        }

        public override void OnAddition(BuffManager bm, BuffInstance bi, bool travelReaddition)
        {
            BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            buffInstance.mSickSim = bm.Actor;
            buffInstance.TimeoutCount = RandomUtil.GetFloat(kMinDuration, kMaxDuration);
            buffInstance.mStageLength = bi.TimeoutCount / 3;
            buffInstance.mAdvanceDiseaseAlarm = bm.Actor.AddAlarm(buffInstance.mStageLength,
                TimeUnit.Minutes, buffInstance.AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                AlarmType.DeleteOnReset);
            //TODO: This should go on final stage
            buffInstance.DoSymptom();
            base.OnAddition(bm, bi, travelReaddition);
        }

        public override void OnRemoval(BuffManager bm, BuffInstance bi)
        {
            BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            buffInstance.StopFx();
            if (buffInstance.mSymptomAlarm != AlarmHandle.kInvalidHandle)
            {
                bm.Actor.RemoveAlarm(buffInstance.mSymptomAlarm);
                bm.Actor.RemoveAlarm(buffInstance.mAdvanceDiseaseAlarm);
                buffInstance.mSymptomAlarm = AlarmHandle.kInvalidHandle;
                buffInstance.mAdvanceDiseaseAlarm = AlarmHandle.kInvalidHandle;
            }
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
            Actor.PlaySoloAnimation("a_react_sit_whine_loop_x");
            return true;
        }
    }
}