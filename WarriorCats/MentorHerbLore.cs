﻿using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using static Echoweaver.Sims3Game.WarriorCats.Config;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class MentorHerbLore : SocialInteraction
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
            return true;
        }

    }
}

