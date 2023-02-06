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
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.ThoughtBalloons;
using static Echoweaver.Sims3Game.PetDisease.Loader;
using static Echoweaver.Sims3Game.PetDisease.PetDiseaseManager;
using static Echoweaver.Sims3Game.PetDisease.Buffs.BuffEWPetPneumonia;

//Template Created by Battery

// Petstilence (Carrionplace Disease)
//   -- Generated from fights or hunting (maybe just hunting rodents), getting fleas
//   -- Bloodborne, transmitted by fighting, woohoo
//   -- Symptoms: passing out and vomiting, moments of psychosis, drooling
//   -- Frequently lethal.


namespace Echoweaver.Sims3Game.PetDisease.Buffs
{
    //XMLBuffInstanceID = 5522594682370665020ul
    public class BuffEWPetstilence : Buff
	{
		public const ulong mGuid = 0x7768716F913C2054ul;
        public const BuffNames buffName = (BuffNames)mGuid;

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
            public VisualEffect mFliesEffect;
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

            public void StartFliesEffect()
            {
                DebugNote("Start Petstilence fly swarm: " + mSickSim.FullName);

                //mHazeEffect = VisualEffect.Create("ep11buffhealthyglowlrg_main");
                //mHazeEffect = VisualEffect.Create("ep5unicornblessblacksparklessmpet");
                mFliesEffect = VisualEffect.Create("ep7BuffSickandTired_main");
                //mHazeEffect2 = VisualEffect.Create("ep11buffhealthyglowlrg_main");
                mFliesEffect.ParentTo(mSickSim, Sim.FXJoints.Spine2);
                //mHazeEffect2.ParentTo(mSickSim, Sim.FXJoints.Crotch);
                //Vector3 fxColor = new Vector3(0.7f, 8f, 0f);
                //Vector3 fxColor = new Vector3(0.2f, 0.2f, 0.2f);
                //mHazeEffect.SetEffectColorScale(fxColor);
                //mHazeEffect2.SetEffectColorScale(fxColor);
                mFliesEffect.Start();
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
                if (mFliesEffect != null)
                {
                    mFliesEffect.Stop(VisualEffect.TransitionType.HardTransition);
                    mFliesEffect.Dispose();
                    mFliesEffect = null;
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
                        mSickSim.Motives.SetValue(CommodityKind.Energy, mSickSim.Motives
                                .GetMotiveValue(CommodityKind.Energy) - 5);
                        DebugNote("Petstilence stage 0 Symptom type " + symptomType + ": " + mSickSim.FullName);
                        if (symptomType == 1)
                        {
                            ShowBalloon(mSickSim, "moodlet_feelingAnxious");
                            mSickSim.BuffManager.AddElement(BuffNames.FeelingAnxious, Origin.FromUnknown);
                            BeSkittish action = BeSkittish.Singleton.CreateInstance(mSickSim,
                                mSickSim, priority, false, false) as BeSkittish;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else if (symptomType == 2)
                        {
                            ShowBalloon(mSickSim, "moodlet_spooked");
                            mSickSim.BuffManager.AddElement(BuffNames.Spooked, Origin.FromUnknown);
                            Shiver action = Shiver.Singleton.CreateInstance(mSickSim, mSickSim,
                                priority, false, false) as Shiver;
                            mSickSim.InteractionQueue.AddNext(action);
                        } else
                        {
                            mSickSim.BuffManager.AddElement(BuffNames.Scolded, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "moodlet_embarrassed");
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
                            // This moodlet does not show up.
                            //mSickSim.BuffManager.AddElement(BuffNames.SqueakSqueak, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "moodlet_excited");
                            ActWacky action = ActWacky.Singleton.CreateInstance(mSickSim, mSickSim,
                                priority, false, false) as ActWacky;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else if (symptomType == 2)
                        {
                            mSickSim.BuffManager.AddElement(BuffNames.Embarrassed, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "moodlet_embarrassed");
                            Yowl action = Yowl.Singleton.CreateInstance(mSickSim,
                                mSickSim, priority, false, false) as Yowl;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        else
                        {
                            // Hiss/growl at random sim
                            mSickSim.BuffManager.AddElement(BuffNames.OutOfSorts, 240f, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "moodlet_offended");
                            List<Sim> list = new List<Sim>(mSickSim.LotCurrent.GetSims());
                            list.AddRange(mSickSim.LotCurrent.GetAnimals());
                            list.Remove(mSickSim);
                            foreach (Sim s in list)
                            {
                                if (s.SimDescription.ToddlerOrBelow)
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
                            mSickSim.BuffManager.AddElement(BuffNames.Betrayed, 240f, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "moodlet_enemy");
                            List<Sim> list = new List<Sim>(mSickSim.LotCurrent.GetAllActors());
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
                            DebugNote(list.Count + "Fight/Chase target optons.");
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
                            if (target.IsPet)
                            {
                                DebugNote("Fight/Chase target is pet.");
                                Sim.FightPet action = Sim.FightPet.Singleton.CreateInstance(target, mSickSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as Sim.FightPet;
                                mSickSim.InteractionQueue.AddNext(action);
                            } else
                            {
                                DebugNote("Fight/Chase target is NOT pet.");
                                Sim.ChaseMean action = Sim.ChaseMean.Singleton.CreateInstance(target, mSickSim,
                                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false, false) as Sim.ChaseMean;
                                mSickSim.InteractionQueue.AddNext(action);
                            }
                        } else
                        {
                            DebugNote("Petstilence run in terror ");
                            mSickSim.BuffManager.AddElement(BuffNames.Panicked, Origin.FromUnknown);
                            ShowBalloon(mSickSim, "balloon_flames");
                            Fire.PetRunAwayFromFire action = Fire.PetRunAwayFromFire.Singleton.CreateInstance(mSickSim,
                                mSickSim, new InteractionPriority(InteractionPriorityLevel.Fire), false, false) as
                                Fire.PetRunAwayFromFire;
                            mSickSim.InteractionQueue.AddNext(action);
                        }
                        break;
                    default:
                        break;
                }

                // Make sure sim still has the moodlet so we don't get an infinite repeating timer.
                if (mSickSim.BuffManager.HasElement(buffName))
                {
                    mSymptomAlarm = mSickSim.AddAlarm(RandomUtil.GetFloat(kMinTimeBetweenPetstilenceSymptoms,
                    kMaxTimeBetweenPetstilenceSymptoms), TimeUnit.Minutes, DoSymptom, "BuffEWPetstilence: Time until next symptom",
                    AlarmType.DeleteOnReset);
                } else
                {
                    DebugNote("Attempted petstilence symptom, but the moodlet has been removed: " + mSickSim.FullName);
                }
            }

            protected void ShowBalloon(Sim s, string thought_name)
            {
                ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(thought_name);
                balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
                balloonData.Duration = ThoughtBalloonDuration.Medium;
                balloonData.mPriority = ThoughtBalloonPriority.High;
                s.ThoughtBalloonManager.ShowBalloon(balloonData);
            }

            public void AdvanceDisease()
            {
                mStage++;
                DebugNote("Petstilence advance to level " + mStage + ": " + mSickSim.FullName);
                if (mStage <= 2)
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
                        StartFliesEffect();
                    }
                    DoSymptom();
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

        private static bool CheckContagionEligibility(Sim s)
        {
            if (s.BuffManager.HasElement(buffName))
            {
                DebugNote("Sim already has Petstilence: " + s.Name);
                return false;
            }
            if (PetDiseaseManager.CheckForVaccination(s))
            {
                DebugNote("Sim is vaccinated: " + s.Name);
                return false;
            }
            return true;
        }

        public static void CheckAmbientContagion(Sim s)
        {
            if (!CheckContagionEligibility(s))
                return;
            if (RandomUtil.RandomChance01(kAmbientPetstilenceOdds))
            // This ambient check is 5%. It's rare to catch.
            {
                DebugNote("Sim Caught Petstilence: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            } else
            {
                DebugNote("Sim did NOT catch Petstilence: " + s.Name);
            }
        }

        public static void CheckContactContagion(Sim s)
        {
            // Contact with subject that always has a chance of transmitting Petstilence
            // (Probably raccoon)
            if (!CheckContagionEligibility(s))
                return;
            if (RandomUtil.RandomChance01(HealthManager.kInteractSicknessOdds))
            {
                DebugNote("Sim Caught Petstilence: " + s.Name);
                // Get Sick
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
            else
            {
                DebugNote("Sim did NOT catch Petstilence: " + s.Name);
            }
        }

        public static void CheckBloodborneContagion(Sim s)
        {
            if (!CheckContagionEligibility(s))
                return;
            if (RandomUtil.RandomChance01(kBloodbornePetstilenceOdds))
            {
                // Get Sick
                DebugNote("Sim Caught Petstilence: " + s.Name);
                s.AddAlarm(RandomUtil.GetFloat(HealthManager.kMaxIncubationTime -
                    HealthManager.kMinIncubationTime) + HealthManager.kMinIncubationTime,
                    TimeUnit.Hours, new GetSick(s).Execute, "petstilence incubation alarm",
                    AlarmType.AlwaysPersisted);
            }
            else
            {
                DebugNote("Sim did not catch pestilence: " + s.Name);
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
            buffInstance.TimeoutCount = RandomUtil.GetFloat(kMinPetstilenceDuration, kMaxPetstilenceDuration);
            buffInstance.mStageLength = bi.TimeoutCount / 3;
            buffInstance.mAdvanceDiseaseAlarm = bm.Actor.AddAlarm(buffInstance.mStageLength,
                TimeUnit.Minutes, buffInstance.AdvanceDisease, "BuffEWPetstilence: Time until disease gets worse",
                AlarmType.DeleteOnReset);
            buffInstance.mSymptomAlarm = buffInstance.mSickSim.AddAlarm(2f, TimeUnit.Minutes,
                buffInstance.DoSymptom, "BuffEWPetstilence: Time until next symptom", AlarmType.DeleteOnReset);
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
            base.OnRemoval(bm, bi);        }

        public override void OnTimeout(BuffManager bm, BuffInstance bi, OnTimeoutReasons reason)
        {
            BuffInstanceEWPetstilence buffInstance = bi as BuffInstanceEWPetstilence;
            DebugNote("Moodlet complete - Petstilence is always lethal.");
            EWPetSuccumbToDisease die = EWPetSuccumbToDisease.Singleton.CreateInstance(buffInstance.mSickSim,
                    buffInstance.mSickSim, new InteractionPriority(InteractionPriorityLevel.MaxDeath),
                    false, false) as EWPetSuccumbToDisease;
            buffInstance.mSickSim.InteractionQueue.AddNext(die);
            base.OnTimeout(bm, bi, reason);
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