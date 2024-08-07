﻿using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Pools;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.Core.Terrain;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats.Apprentice
{
    public class MentorFishing : EWAbstractMentor
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
        public static Vector3 waterLoc;

        public override bool Run()
        {
            IPond nearestWater = HerbLore.EWPetWaterPlant.GetNearestWater(Actor.Position, float.MaxValue);
            if (nearestWater == null)
            {
                // TODO: Localize!
                Actor.ShowTNSIfSelectable("There is no nearby pond to mentor fishing",
                    NotificationStyle.kGameMessageNegative);
                return false;
            }
            waterLoc = nearestWater.RepresentativePondPosition();


            Vector3[] trySpotArray = default(Vector3[]);
            Quaternion[] array2 = default(Quaternion[]);
            if (!World.FindPlacesOnShoreline(Actor.Proxy, waterLoc, 6u, false, out trySpotArray,
                out array2))
            {
                // TODO: Localize!
                Actor.ShowTNSIfSelectable("It's too hard to reach the nearest pond. We should try someplace else.",
                    NotificationStyle.kGameMessageNegative);
                return false;
            }

            skillName = FishingSkillName;
            remainingRepeats = 5;
            DemonstrateDefinition = new DemonstrateFishing.Definition();
            speechBallons = new string[] { "balloon_pond", "balloon_jellyfish", "ep10_balloon_fish",
                "balloon_question", "balloon_fish", "ep5_balloon_giantsquid", "ep5_balloon_shark",
                "ep3_balloon_snail", "ep3_balloon_waterfall"};

            return base.Run();
        }

        public class DemonstrateFishing : DemonstrateSkill
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

            public override bool DemonstrateAnim()
            {
                int waitMax = 2;
                while (waitMax-- > 0 && Actor.GetDistanceToObjectSquared(Target)
                    > WatchSkill.kRouteDistance)
                {
                    if (Simulator.CheckYieldingContext(false))
                    {
                        // TODO: Not sure how many ticks is good to wait or if this is the best way
                        // to wait.
                        Simulator.Sleep(5u);
                    }
                }
                ulong notUsed = 10u; // Not used by the method. I don't know what it was supposed to be.
                if (!DrinkFromPondHelper.RouteToDrinkLocation(waterLoc, Actor, GameObjectHitType.WaterPond,
                    notUsed))
                {
                    DebugNote("DemonstrateFishing: Route to drink location failed");
                    if (!Actor.RouteTurnToFace(waterLoc))
                    {
                        DebugNote("DemonstrateFishing: Turn to face water failed");
                        return false;
                    }
                }
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
    }
}

