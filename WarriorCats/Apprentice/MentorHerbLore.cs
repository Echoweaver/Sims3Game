using System.Collections.Generic;
using Echoweaver.Sims3Game.WarriorCats.HerbLore;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.Pets;
using Sims3.Gameplay.ThoughtBalloons;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats.Apprentice
{
    public class MentorHerbLore : EWAbstractMentor
    {
        public class Definition : InteractionDefinition<Sim, Sim, MentorHerbLore>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!HasApprentice(a, target))
                {
                    return false;
                }
                if (!a.SkillManager.HasElement(EWHerbLoreSkill.SkillNameID))
                {
                    return false;
                }
                if (target.SkillManager.HasElement(EWHerbLoreSkill.SkillNameID))
                {
                    if ((target.SkillManager.GetElement(EWHerbLoreSkill.SkillNameID).SkillLevel + 1) >=
                        a.SkillManager.GetElement(EWHerbLoreSkill.SkillNameID).SkillLevel)
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
                return "Mentor Herb Lore";
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

        public override bool Run()
        {
            skillName = EWHerbLoreSkill.SkillNameID;
            remainingRepeats = 5;
            speechBallons = new string[] { "balloon_moodlet_one", "balloon_trait_unlucky",
                "balloon_question", "balloon_trait_lucky", "balloon_tree", "balloon_veggies",
                "balloon_trait_vegetarian", "balloon_ladybug", "balloon_rain"
            };
            // balloon_moodlet_min 0x26F4420B337D0179 _IMG Group=0
            // balloon_moodlet_one 0x5E436B52F74E6B0F

            DemonstrateDefinition = new DemonstrateHerbLore.Definition();

            Plant p = FindNearestPlant(Actor);
            if (p == null)
            {
                // TODO: Localize!
                Actor.ShowTNSIfSelectable("There are no plants nearby. Try somewhere else.",
                    NotificationStyle.kGameMessageNegative);
                return false;
            }

            return base.Run();
        }

        public static List<Plant> demoedPlants = new List<Plant>();

        public static Plant FindNearestPlant(Sim actor)
        {
            Plant[] objects = actor.LotCurrent.GetObjects<Plant>();
            float bestVal = float.MaxValue;
            Plant result = null;
            Plant[] possiblePlants = objects;
            foreach (Plant p in possiblePlants)
            {
                if (!demoedPlants.Contains(p))
                {
                    GameObject gameObject = p as GameObject;
                    float distanceToObject = gameObject.GetDistanceToObject(actor);
                    if (distanceToObject < bestVal)
                    {
                        bestVal = distanceToObject;
                        result = p;
                    }
                }
            }
            return result;
        }

        public class DemonstrateHerbLore : DemonstrateSkill
        {
            public class Definition : InteractionDefinition<Sim, Sim, DemonstrateHerbLore>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    // Localize!
                    return "Demonstrate Herb Lore";
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool DemonstrateAnim()
            {
                Plant plant = FindNearestPlant(Actor);
                if (plant == null)
                {
                    // If there are no plants you haven't looked at, pick a random one you have
                    int i = RandomUtil.GetInt(0, demoedPlants.Count - 1);
                    plant = demoedPlants[i];
                }
                if (plant == null)
                {
                    DebugNote("Could not find a plant, though there should be one on the lot");
                    return false;
                }
                plant.RouteSimToMeAndCheckInUse(Actor);

                ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager
                    .BalloonData(plant.GetThumbnailKey());
                balloonData.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
                balloonData.Duration = ThoughtBalloonDuration.Medium;
                Actor.ThoughtBalloonManager.ShowBalloon(balloonData);

                int choice = RandomUtil.GetInt(1, 3);

                if (choice == 1)
                {
                    AcquireStateMachine("catdoginvestigate");
                    EnterStateMachine("catdoginvestigate", "Enter", "x");
                    AnimateSim("Investigate");
                    AnimateSim("Exit");
                }
                else if (choice == 2)
                {
                    AcquireStateMachine("eatharvestablepet");
                    mCurrentStateMachine.SetActor("x", Actor);
                    mCurrentStateMachine.EnterState("x", "Enter");
                    SetParameter("IsEatingOnGround", paramValue: true);
                    AnimateSim("EatHarvestable");
                    AnimateSim("Exit");
                }
                else
                {
                    EnterStateMachine("ScratchingPost", "Enter", "x");
                    SetActor("post", plant);
                    AnimateSim("Loop");
                    bool flag = base.DoTimedLoop(ScratchingPost.kScratchTime, ExitReason.Default);
                    AnimateSim("Exit");
                }
                if (!demoedPlants.Contains(plant))
                {
                    demoedPlants.Add(plant);
                }
                return true;
            }
        }
    }
}

