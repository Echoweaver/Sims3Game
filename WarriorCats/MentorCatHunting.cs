using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class MentorCatHunting : SocialInteraction
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
            return true;
        }

    }
}

