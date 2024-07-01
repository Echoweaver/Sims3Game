using System;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Sims3.SimIFace.RouteDestinations;
using static Echoweaver.Sims3Game.WarriorCats.Config;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using static Sims3.Gameplay.Objects.Pets.BoxStall;
using static Sims3.SimIFace.Route;

namespace Echoweaver.Sims3Game.WarriorCats.Apprentice
{
    public class MentorCatHunting : EWAbstractMentor
    {
        public class Definition : InteractionDefinition<Sim, Sim, MentorCatHunting>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!HasApprentice(a, target))
                {
                    return false;
                }
                if (!a.SkillManager.HasElement(SkillNames.CatHunting))
                {
                    return false;
                }
                if (target.SkillManager.HasElement(SkillNames.CatHunting))
                {
                    if ((target.SkillManager.GetElement(SkillNames.CatHunting).SkillLevel + 1) >=
                        a.SkillManager.GetElement(SkillNames.CatHunting).SkillLevel)
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
                return "Mentor Hunting";
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
            skillName = SkillNames.CatHunting;
            speechBallons = new string[] { "ep5_balloon_turtle", "ep5_balloon_snake", "ep5_balloon_rodent",
                "ep5_balloon_lizard", "ep5_balloon_leaf", "ep5_balloon_nuthatc", "balloon_question",
                "ep5_balloon_bear", "ep5_balloon_bird"
            };
            remainingRepeats = 5;
            DemonstrateDefinition = new DemonstrateHunting.Definition();

            return base.Run();
        }

        public class DemonstrateHunting: DemonstrateSkill
        {
            public class Definition : InteractionDefinition<Sim, Sim, DemonstrateHunting>
            {
                public override bool Test(Sim a, Sim target, bool isAutonomous,
                    ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }

            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool DemonstrateAnim()
            {
                Actor.PlaySoloAnimation("ac_hunting_locatePrey_sniffAir_x", ProductVersion.EP5);
                PouncePosture pouncePosture = PouncePosture.Create(Actor);
                pouncePosture.EnterPounce();
                Actor.Posture = pouncePosture;
                RequestWalkStyle(Sim.WalkStyle.CatStalk);
                Actor.Wander(StalkForPrey.kMinMaxWanderDist[0], StalkForPrey.kMinMaxWanderDist[1],
                    limitOutdoors: false, RouteDistancePreference.NoPreference, doRoutFail: false,
                    StalkForPrey.kRouteOptions);
                UnrequestWalkStyle(Sim.WalkStyle.CatStalk);

                EnterStateMachine("CatHunt", "Enter", "x");
                AnimateSim("Loop");
                DoTimedLoop(CatchPrey.kFightLength, ExitReason.Default);
                AnimateSim("Exit Eat");

                return true;
            }
        }
    }
}

