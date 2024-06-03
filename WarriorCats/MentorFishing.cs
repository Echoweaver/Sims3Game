using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.Controller;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using static Sims3.Gameplay.Objects.Toys.ToyBoxToy.PlayWith;
using static Sims3.SimIFace.Route;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class MentorFishing : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, MentorFishing>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                //if (!HasApprentice(a, target))
                //{
                //    return false;
                //}
                if (!a.SkillManager.HasElement(FishingSkillName))
                {
                    return false;
                }

                if (target.SkillManager.HasElement(FishingSkillName))
                {
                    if ((target.SkillManager.GetElement(FishingSkillName).SkillLevel + 1) >=
                        a.SkillManager.GetElement(FishingSkillName).SkillLevel)
                    {
                        // TODO: Localize!
                        greyedOutTooltipCallback = CreateTooltipCallback("This apprentice has learned everything you can teach right now");
                        return false;
                    }
                }
                return true;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                //return LocalizeStr("?");
                // TODO: Localize!
                return "Mentor Fishing";
            }

            public override string[] GetPath(bool isFemale)
            {
                // TODO: Localize!!
                return new string[1] {
                    "Apprentice..."
                };
            }
        }

        public static InteractionDefinition Singleton = new Definition();
        public static bool actorIsMentor = true;
        public static int remainingRepeats = 5;
        public static Vector3 waterLoc;
        public static Skill skillMentor;
        public static Skill skillStudent;

        //public static string SocialName = "Mentor Fishing";

        public override bool Run()
        {
            IPond nearestWater = EWPetWaterPlant.GetNearestWater(Actor.Position, float.MaxValue);
            if (nearestWater == null)
            {
                // TODO: Localize!
                Actor.ShowTNSIfSelectable("There is no nearby pond to mentor fishing",
                    NotificationStyle.kGameMessageNegative);
                return false;
            }
            waterLoc = nearestWater.RepresentativePondPosition();
            skillMentor = Actor.SkillManager.GetElement(FishingSkillName);
            skillStudent = Target.SkillManager.GetElement(FishingSkillName);

            if (skillMentor == null)
            {
                DebugNote("Mentor Fishing: Mentor's skill is unaccountably NULL.");
                return false;
            }
            if (skillStudent == null)
            {
                skillStudent = Target.SkillManager.AddElement(FishingSkillName);
                if (skillStudent == null)
                {
                    DebugNote("Mentor Fishing: Student's skill is unaccountably NULL.");
                    return false;
                }
            }

            if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, "Learn Fishing",
                allowCarryChild: false), pairedSocial: true, doCallOver: true))
            {
                DebugNote("Discuss Fishing: BeginSocialInteraction failed");
                return false;
            }
            skillStudent.StartSkillGain(kApprenticeSkillGainRate);
            EnterStateMachine("Socialize", "Enter", "x", "y");
            ThoughtBalloonManager.BalloonData balloonData1 = new ThoughtBalloonManager.BalloonData("balloon_fish");
            balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
            balloonData1.Duration = ThoughtBalloonDuration.Short;
            Actor.ThoughtBalloonManager.ShowBalloon(balloonData1);
            mCurrentStateMachine.RequestState(false, "x", "Socialize");
            mCurrentStateMachine.RequestState(true, "y", "Socialize");
            ThoughtBalloonManager.BalloonData balloonData2 = new ThoughtBalloonManager.BalloonData("balloon_fish");
            balloonData2.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
            balloonData2.Duration = ThoughtBalloonDuration.Short;
            Target.ThoughtBalloonManager.ShowBalloon(balloonData2);
            mCurrentStateMachine.RequestState(false, "x", "Exit");
            mCurrentStateMachine.RequestState(true, "y", "Exit");
            FinishLinkedInteraction();

            if (!RouteToShoreline(waterLoc, Actor, Target))
            {
                // TODO: Localize!
                Actor.ShowTNSIfSelectable("It's too hard to reach the nearest pond. We should try someplace else.",
                    NotificationStyle.kGameMessageNegative);
                skillStudent.StopSkillGain();
                return false;
            }

            DiscussFishing.Definition definition2 = new DiscussFishing.Definition();
            InteractionInstance discuss = definition2.CreateInstance(Target, Actor,
                new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                true);
            //Actor.InteractionQueue.TryPushAsContinuation(this, discuss);
            Actor.InteractionQueue.AddNext(discuss);

            skillStudent.StopSkillGain();
            return true;
        }

        public bool RouteToShoreline(Vector3 trySpot, Sim leader, Sim follower)
        {
            DebugNote("Route to shoreline: " + leader.Name);
            Vector3[] trySpotArray = default(Vector3[]);
            Quaternion[] array2 = default(Quaternion[]);
            if (!World.FindPlacesOnShoreline(leader.Proxy, trySpot, 6u, false, out trySpotArray, out array2))
            {
                return false;
            }
            Route route = leader.CreateRoute();
            route.SetOption((RouteOption)274877906944L, true);
            route.SetOption((RouteOption)18014398509481984L, true);
            route.SetOption((RouteOption)35184372088832L, false);
            LotLocation invalid = LotLocation.Invalid;
            ulong lotLocation = World.GetLotLocation(trySpot, ref invalid);
            bool flag = true;
            invalid.mLevel = 0;
            bool onWorldLot = lotLocation == 0;
            List<Sim> followlist = new List<Sim>();
            followlist.Add(follower);
            if (!onWorldLot && World.IsSimNearShoreline(leader.Proxy, trySpot))
            {
                return true;
            }
            else
            {
                Route.CreateRouteToNaturalWaterBody(onWorldLot, route, leader.Position, trySpotArray,
                    lotLocation, invalid, null);
            }
            if (flag)
            {
                RoutePlanResult result = route.Plan();
                bool success = leader.DoRouteWithFollowers(route, followlist);
            }
            return true;
        }

        public class DiscussFishing : SocialInteraction
        {
            public class Definition : InteractionDefinition<Sim, Sim, DiscussFishing>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
                {
                    //return LocalizeStr("?");
                    // TODO: Localize!
                    return "Discuss Fishing";
                }
            }
            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                DebugNote("Begin discuss fishing");
                if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, "Discuss Fishing",
                    allowCarryChild: false), pairedSocial: true, doCallOver: true))
                {
                    DebugNote("Discuss Fishing: BeginSocialInteraction failed");
                    return false;
                }
                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                string[] kSpeechBallons = new string[] { "balloon_pond", "balloon_jellyfish",
                    "balloon_question", "balloon_fish" };
                //string[] kSpeechBallons = new string[] { "balloon_pond", "balloon_jellyfish",
                //        "balloon_question", "balloon_fish", "ep5_balloon_giantsquid", "ep10_balloon_fish",
                //        "ep10_balloon_shark"};
                //if (!BeginSocialInteraction(new SocialInteractionB.Definition(null, "Learn Fishing",
                //    allowCarryChild: false), pairedSocial: true, doCallOver: true))
                //{
                //    return;
                //} 
                EnterStateMachine("Socialize", "Enter", "x", "y");
                string randomBalloon1 = RandomUtil.GetRandomStringFromList(kSpeechBallons);
                ThoughtBalloonManager.BalloonData balloonData1 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
                balloonData1.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData1.Duration = ThoughtBalloonDuration.Short;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData1);
                mCurrentStateMachine.RequestState(false, "x", "Socialize");
                mCurrentStateMachine.RequestState(true, "y", "Socialize");
                randomBalloon1 = RandomUtil.GetRandomStringFromList(kSpeechBallons);
                ThoughtBalloonManager.BalloonData balloonData2 = new ThoughtBalloonManager.BalloonData(randomBalloon1);
                balloonData2.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData2.Duration = ThoughtBalloonDuration.Short;
                Target.ThoughtBalloonManager.ShowBalloon(balloonData2);
                mCurrentStateMachine.RequestState(false, "x", "Exit");
                mCurrentStateMachine.RequestState(true, "y", "Exit");
                FinishLinkedInteraction();

                if (skillMentor.SkillLevel <= (skillStudent.SkillLevel + 1))
                {
                    Actor.ShowTNSIfSelectable("This apprentice has learned everything the mentor can teach.",
                        NotificationStyle.kGameMessageNegative);
                } else
                {
                    DebugNote("Mentor Fishing remaining repeats = " + remainingRepeats);
                    if (remainingRepeats > 0 && skillMentor.SkillLevel > skillStudent.SkillLevel)
                    {
                        DemonstrateFishing.Definition demoDef = new DemonstrateFishing.Definition();
                        DemonstrateFishing demo = demoDef.CreateInstance(Target, Actor,
                            new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                            true) as DemonstrateFishing;
                        WatchFishing.Definition watchDef = new WatchFishing.Definition();
                        WatchFishing watch = watchDef.CreateInstance(Actor, Target,
                            new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                            true) as WatchFishing;
                        demo.waitInstance = watch;

                        Actor.InteractionQueue.TryPushAsContinuation(this, demo);
                        Target.InteractionQueue.TryPushAsContinuation(this, watch);
                    }
                }
                skillStudent.StopSkillGain();
                return true;
            }
        }

        public class DemonstrateFishing : Interaction<Sim, Sim>
        {
            public class Definition : InteractionDefinition<Sim, Sim, DemonstrateFishing>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    // Localize!
                    return "Demonstrate Fishing";
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public WatchFishing waitInstance;

            // TODO: Localize!

            public override bool Run()
            {
                DebugNote("Begin Demonstrate Fishing " + Actor.Name);
                DebugNote("Demonstrate fishing targeet = " + Target.Name);
                ulong notUsed = 10u; // Not used by the method. I don't know what it was supposed to be.
                if (!DrinkFromPondHelper.RouteToDrinkLocation(waterLoc, Actor, GameObjectHitType.WaterPond,
                    notUsed))
                {
                    DebugNote("DemonstrateFishing: Route to drink location failed");
                    if (!Actor.RouteTurnToFace(waterLoc))
                    {
                        DebugNote("DemonstrateFishing: Turn to face water failed");
                        waitInstance.fishingComplete = true;
                        return false;
                    }
                }

                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                if (Actor.SimDescription.AdultOrAbove)
                {
                    mCurrentStateMachine = StateMachineClient.Acquire(Actor, "CatHuntInPond", AnimationPriority.kAPDefault);
                    AddOneShotScriptEventHandler(101u, new SacsEventHandler(SnapOnExit));
                    mCurrentStateMachine.SetActor("x", Actor);
                    mCurrentStateMachine.EnterState("x", "Enter");
                    AnimateSim("PrePounceLoop");
                    AnimateSim("FishLoop");
                    AnimateSim("ExitFailure");
                }
                else
                {
                    mCurrentStateMachine = StateMachineClient.Acquire(Actor, "Puddle", AnimationPriority.kAPDefault);
                    mCurrentStateMachine.SetActor("x", Actor);
                    mCurrentStateMachine.EnterState("x", "Enter");
                    AnimateSim("Loop Play");
                    AnimateSim("Loop Play");
                    AnimateSim("Exit");
                }

                waitInstance.fishingComplete = true;
                skillStudent.StopSkillGain();
                return true;
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
        }

        public class WatchFishing : Interaction<Sim, Sim>
        {
            public class Definition : InteractionDefinition<Sim, Sim, WatchFishing>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    // Localize!
                    return "Watch fisher";
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            [Tunable]
            [TunableComment("Approximate distance to sim being watched")]
            public static float kRouteDistance = 2f;

            [Tunable]
            [TunableComment("Distance beyond kRouteDistance a watching sim can be before they try to re-route")]
            public static float kMaxViewingRing = 1f;

            [TunableComment("Minutes between watching sim reacting")]
            [Tunable]
            public static float kTimeBetweenReactions = 5f;

            public bool fishingComplete = false;

            public override bool Run()
            {
                DebugNote("Begin Watch Fishing " + Actor.Name);
                skillStudent.StartSkillGain(kApprenticeSkillGainRate);
                if (!Actor.RoutingComponent.RouteToDynamicObjectRadius(Target, 0, kRouteDistance, null))
                {
                    DebugNote("Watch fishing: RouteToDynamicObjectRadius failed");
                    return false;
                }
                StandardEntry();
                BeginCommodityUpdates();
                bool flag = DoLoop(ExitReason.Default, LoopFunc, null, kTimeBetweenReactions);
                EndCommodityUpdates(flag);
                StandardExit();
                skillStudent.StopSkillGain();
                return flag;
            }

            public void LoopFunc(StateMachineClient smc, LoopData ld)
            {
                ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(Target.GetThumbnailKey());
                balloonData.BalloonType = ThoughtBalloonTypes.kThoughtBalloon;
                balloonData.mPriority = ThoughtBalloonPriority.Low;
                balloonData.mFlags = ThoughtBalloonFlags.ShowIfSleeping;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData);
                if (fishingComplete)
                {
                    DebugNote("Watch fishing is complete");
                    Actor.AddExitReason(ExitReason.Finished);
                    Repeat();
                    return;
                }
                if (Actor.GetDistanceToObject(Target) > kRouteDistance + kMaxViewingRing
                    && !Actor.RoutingComponent.RouteToDynamicObjectRadius(Target, kRouteDistance,
                    null, null))
                {
                    DebugNote("Watch fishing routing failed.");
                    Actor.AddExitReason(ExitReason.Finished);
                    Repeat();
                    return;
                }
            }

            public void Repeat()
            {
                actorIsMentor = !actorIsMentor;
                --remainingRepeats;
                DiscussFishing.Definition definition2 = new DiscussFishing.Definition();
                InteractionInstance discuss = definition2.CreateInstance(Target, Actor,
                    new InteractionPriority(InteractionPriorityLevel.UserDirected), false,
                    true);
                Actor.InteractionQueue.TryPushAsContinuation(this, discuss);
            }
        }
    }
}

