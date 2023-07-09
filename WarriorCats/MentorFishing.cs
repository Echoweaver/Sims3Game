using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class MentorFishing : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, MentorFishing>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!HasApprentice(a, target))
                {
                    return false;
                }
                if (a.SkillManager.HasElement(FishingSkillName))
                {
                    return true;
                }
                return false;
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
                    "Apprentice"
                };
            }
        }


        public static InteractionDefinition Singleton = new Definition();

        // balloon_pond (fishing)
        // balloon_jellyfish (fishing)
        // balloon_question
        // balloon_fish
        // balloon_rain
        // ep5_balloon_giantsquid
        // ep10_balloon_fish
        // ep10_balloon_shark

        public override bool Run()
        {
            return true;
        }

    }
}

