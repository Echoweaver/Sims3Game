using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.DreamsAndPromises;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Objects.Island;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Opportunities;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.Gameplay.Utilities.NotificationSystem;
using static Sims3.SimIFace.Route;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game
{

    public class EWCatFishAWhile : TerrainInteraction, IPondInteraction
    {
        public class Definition : InteractionDefinition<Sim, Terrain, EWCatFishAWhile>
        {

            //public override string GetInteractionName(Sim a, Terrain target, InteractionObjectPair interaction)
            //{
            //    return Localization.LocalizeString("Echoweaver/Interactions:EWFishAWhile");
            //}

            public override bool Test(Sim a, Terrain target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (a.IsCat)
                {
                    EWCatFishingSkill skill = a.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
                    if (skill != null && skill.CanCatchPreyFish())
                    {
                        return PetManager.PetSkillFatigueTest(a, ref greyedOutTooltipCallback);
                    }
                }
                return false;
            }

            public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (FishTestTerrain(ref parameters))
                {
                    return base.Test(ref parameters, ref greyedOutTooltipCallback);
                }
                return InteractionTestResult.Gen_BadTerrainType;
            }
        }

        public class CountedFishStage : SimpleStage
        {
            public int mGoalCount;

            public static CountedFishStage MakeCountedStage(int count)
            {
                return new CountedFishStage(Localization.LocalizeString("Gameplay/Objects/Fishing:FishCountedStage", count), count);
            }

            public CountedFishStage()
            {
            }

            public CountedFishStage(string name, int count)
                : base(name, count * kEstimatedTimeToCatchOneFish, null, showCompletionTime: false, selectable: true, visibleProgress: true)
            {
                mGoalCount = count;
                mCompletionTest = CountedComplete;
            }

            public float CountedComplete(InteractionInstance inst)
            {
                float val = (float)(inst as FishHere).mNumberFishCaught / (float)mGoalCount;
                return Math.Min(val, 1f);
            }

            public override ResourceKey GetIconKey()
            {
                return ResourceKey.CreatePNGKey("hud_interactions_stage_x5", 0u);
            }
        }

        public const float kSlotPosOffset = 0.5f;

        public const float kRayCastSpread = 0.15f;

        [Tunable]
        [TunableComment("Description:  The min and max chances for success, will lerp between these values based on your hunting skill")]
        public static float[] kMinMaxSuccesChance = new float[2] {
        30f,
        75f
        };


        [Tunable]
        public static int kEstimatedTimeToCatchOneFish = 50;

        [Tunable]
        [TunableComment("Range:  Positive intergers  Description:  The amount of fish to catch in the medium stage")]
        public static int kNumberOfFishToCatchInMediumStage = 5;

        [TunableComment("Time in Sim Minutes before we show a TNS about come back to fish here later.")]
        [Tunable]
        public static float kTimeToShowFishingTns = 60f;

        [TunableComment("Chance your Sim will play the reel in animation and catch nothing.")]
        [Tunable]
        public static float kChanceOfPlayingLostFishAnim = 0.5f;

        [TunableComment("Design intent wwas to make this more often than for normal sims.  Please tune relative.")]
        [Tunable]
        public static float kChanceOfPlayingLostFishAnimIfClumsy = 0.7f;

        [Tunable]
        [TunableComment("Time after the No Fish TNS shows before it is possible a fish can be caught.")]
        public static float kTimeToCatchPostTns = 30f;

        [Tunable]
        public static float kAnglerFunPerHour = 10f;

        [Tunable]
        [TunableComment("Threshold for distance checking, (Note: It's square distance.)")]
        public static float kDistanceThreshold = 160000f;

        public static InteractionDefinition Singleton = new Definition();

        EWCatFishingSkill skill;

        public int mNumberFishCaught;

        public Stage mIndefiniteStage;

        public bool mHasCatFisherTrait;

        public FishingData mFishingData;

        public VisualEffect mFishingLineVfx;

        public VisualEffect mBobberVfx;

        public float mLoopLengthForNextFish;

        public bool mIsHouseboat;

        public SittingInBoat mSittingInBoatPosture;

        public static ulong kIconNameHash = ResourceUtils.HashString64("hud_interactions_fish");

        public bool TerrainIsWaterPond => (int)Hit.mType == 8;

        public static bool FishTestTerrain(ref InteractionInstanceParameters parameters)
        {
            Sim sim = parameters.Actor as Sim;
            GameObjectHit hit = parameters.Hit;
            SwimmingInPool swimmingInPool = sim.Posture as SwimmingInPool;
            GameObjectHitType mType = hit.mType;
            if ((int)mType != 1)
            {
                switch ((int)mType - 8)
                {
                    case 1:
                        return true;
                    case 0:
                        if (PondManager.ArePondsLiquid())
                        {
                            return true;
                        }
                        return false;
                    case 2:
                        return false;  // Cats cannot fish in swimming pools. So much human code about swimming pools.
                }
            }
            else
            {
                FishingSpot fishingSpot = GameObject.GetObject(new ObjectGuid(hit.mId)) as FishingSpot;
                if (fishingSpot != null)
                {
                    if (PondManager.ArePondsLiquid())
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public override void ConfigureInteraction()
        {
            List<Stage> list = new List<Stage>();
            mIndefiniteStage = new IndefiniteStage(Localization.LocalizeString("Gameplay/Objects/Fishing:FishIndefiniteStage"),
                selectable: true);
            list.Add(mIndefiniteStage);
            list.Add(CountedFishStage.MakeCountedStage(kNumberOfFishToCatchInMediumStage));
            list.Add(new SkillStage(Localization.LocalizeString("Gameplay/Objects/Fishing:FishSkillStage"),
                EWCatFishingSkill.SkillNameID, Actor, EWCatFishingSkill.kEWFishingSkillGainRateNormal,
                showCompletionTime: false, selectable: true));
            SetStages(list);
        }

        public float IndefiniteCompletion(InteractionInstance inst)
        {
            return 0f;
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        public override bool ShouldReplace(InteractionInstance interaction)
        {
            EWCatFishAWhile fishHere = interaction as EWCatFishAWhile;
            if (fishHere == null)
            {
                return false;
            }
            if (fishHere.ActiveStage == fishHere.mIndefiniteStage)
            {
                return true;
            }
            return false;
        }

        public override string GetInteractionName()
        {
            // TODO: Add name
            return base.GetInteractionName();
        }

        public override ThumbnailKey GetIconKey()
        {
            // TODO: Add thumbail
            return new ThumbnailKey(new ResourceKey(kIconNameHash, 796721156u, 0u), (ThumbnailSize)1);
        }

        //public override void AddExcludedDreams(ICollection<DreamNames> excludedDreams)
        //{
        //	base.AddExcludedDreams(excludedDreams);
        //	excludedDreams.Add(DreamNames.go_fishing);
        //}

        public override bool Run()
        {
            Vector3 trySpot = Hit.mPoint;
            Vector3 newTargetPos = trySpot;
            if (!DrinkFromPondHelper.RouteToDrinkLocation(Hit.mPoint, Actor, Hit.mType, Hit.mId))
            {
                return false;
            }
            skill = Actor.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
            skill.StartSkillGain(EWCatFishingSkill.kEWFishingSkillGainRateNormal);
            mFishingData = FishingSpot.GetFishingData(trySpot, Hit.mType);
            EnterStateMachine("CatHuntInPond", "Enter", "x");
            StandardEntry();
            BeginCommodityUpdates();
            StartStages();
            //MotiveDelta delta = null;
            //if (Actor.TraitManager.HasElement(TraitNames.Angler))
            //{
            //	mHasCatFisherTrait = true;
            //	delta = AddMotiveDelta(CommodityKind.Fun, kAnglerFunPerHour);
            //}
            mHasCatFisherTrait = false;
            AnimateSim("PrePounceLoop");
            bool flag = DoLoop(ExitReason.Default, LoopFunc, mCurrentStateMachine);
            //if (mHasCatFisherTrait)
            //{
            //    RemoveMotiveDelta(delta);
            //}
            SnapOnExit(null, null);
            skill.StopSkillGain();
            EndCommodityUpdates(flag);
            StandardExit();
            return flag;
        }

        public void SnapOnExit(StateMachineClient sender, IEvent evt)
        {
            Vector3 forwardVector = Actor.ForwardVector;
            Quaternion val = Quaternion.MakeFromEulerAngles(0f, 3.14159274f, 0f);
            Matrix44 val2 = val.ToMatrix();
            forwardVector = val2.TransformVector(forwardVector);
            Actor.SetPosition(Actor.Position + 0.283f * forwardVector);
            Vector3 position = Actor.Position;
            position.y = World.GetTerrainHeight(position.x, position.z);
            Actor.SetPosition(position);
            Actor.SetForward(forwardVector);
        }

        public void LoopFunc(StateMachineClient smc, LoopData ld)
        {
            // Saving this method in case I want to use it. Humans seem to wait longer for a fish action but
            // fail less often.
            //mLoopLengthForNextFish += mFishingData.GetNextFishTimeLength(mHasCatFisherTrait, skill.OppFishercatCompleted);
            float mLoopLengthForNextFish = RandomUtil.GetFloat(EWCatFishHere.kMinMaxPrePounceTime[0], EWCatFishHere.kMinMaxPrePounceTime[1]);
            bool loopFlag = DoTimedLoop(mLoopLengthForNextFish);
            if (loopFlag)
            {
                EventTracker.SendEvent(EventTypeId.kGoFishingCat, Actor);
                AnimateSim("FishLoop");
                bool successFlag = RandomUtil.InterpolatedChance(0f, skill.MaxSkillLevel, EWCatFishHere.kMinMaxSuccesChance[0],
                    kMinMaxSuccesChance[1], skill.SkillLevel);
                FishType fishType = FishType.None;
                if (successFlag)
                {
                    fishType = EWCatFishHere.GetCaughtFishType(Actor, Hit);
                }
                switch (fishType)
                {
                    case FishType.None:
                        {
                            AnimateSim("ExitFailure");
                            break;
                        }
                    default:
                        {
                            Fish fish = Fish.CreateFishOfRandomWeight(fishType, Actor.SimDescription);
                            string message = skill.RegisterCaughtPrey(fish, TerrainIsWaterPond);
                            if (fish.CatHuntingComponent != null)
                            {
                                fish.CatHuntingComponent.SetCatcher(Actor);
                            }
                            fish.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
                            mNumberFishCaught++;
                            SetActor("fish", (IHasScriptProxy)(object)fish);
                            if (Actor.Motives.GetValue(CommodityKind.Hunger) <= EWCatFishHere.kEatFishHungerThreshold)
                            {
                                // Notify if cat eats caught fish
                                message += Localization.LocalizeString("Gameplay/Abstracts/ScriptObject/CatFishHere:EatFishTns",
                                    Actor, fish.GetLocalizedName(), fish.Weight);
                                Actor.ShowTNSIfSelectable(message, NotificationStyle.kGameMessagePositive);
                                AnimateSim("ExitEat");
                                fish.Destroy();
                                Actor.Motives.ChangeValue(CommodityKind.Hunger, EWCatFishHere.kHungerGainFromEating);
                            }
                            else
                            {
                                if (message != "")
                                {
                                    // Notify if the fish is interesting (new type or weight record)
                                    message += Localization.LocalizeString("Gameplay/Abstracts/ScriptObject/CatFishHere:PutFishInInventoryTns",
                                        Actor, fish.GetLocalizedName(), fish.Weight);
                                    Actor.ShowTNSIfSelectable(message, NotificationStyle.kGameMessagePositive);
                                }
                                AnimateSim("ExitInventory");
                                fish.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
                                if (!Actor.Inventory.TryToAdd(fish))
                                {
                                    fish.Destroy();
                                }
                            }
                            break;
                        }
                }
            }
            if (Actor.HasExitReason(ExitReason.UserCanceled) || (Actor.HasExitReason(ExitReason.MoodFailure))
                || (Actor.HasExitReason(ExitReason.Canceled)))
            {
                Actor.AddExitReason(ExitReason.UserCanceled);
                Actor.AddExitReason(ExitReason.MoodFailure);
            }
            if (!ActiveStage.IsComplete(this))
            {
                DrinkFromPondHelper.RouteToDrinkLocation(Hit.mPoint, Actor, Hit.mType, Hit.mId);
                AnimateSim("PrePounceLoop");
            }
        }
    }
}
