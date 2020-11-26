using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;



namespace Echoweaver.Sims3Game
{
    public class DiscussKelpRecipes
    {
        [Tunable]
        protected static bool kInstantiator = false;
        [Tunable]
        protected static int kChanceOfLearningKelpRecipe = 4;

        static DiscussKelpRecipes()
        {
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            EventTracker.AddListener(EventTypeId.kSocialInteraction, new ProcessEventDelegate(OnSocialInteraction));
        }

        public static ListenerAction OnSocialInteraction(Event e)
        {
            // Turns out a social interaction like "Chat" triggers 4 events of EventTypeId kSocialInteraction.
            // Two cast to SocialEvent, one for the recipient and one for the initiator. I have no idea what
            // the other two are, but we don't want them.
            if (e is SocialEvent)
            {
                SocialEvent cevent = (SocialEvent)e;
                // There are two social interactions for discussing kelp recipes -- on land and in water.
                // I don't know that Discussing Kelp Recipes can be rejected, but obviously you shouldn't learn
                // anything if it was.
                if (cevent != null && cevent.SocialName.Contains("Discuss Kelp")
                    && cevent.WasAccepted)
                {
                    Sim speaker = (Sim)cevent.Actor;
                    // Anyone can learn a kelp recipe from initiating a conversation with a Mermaid, even other Mermaids.
                    // If you're not a Mermaid, you can also learn one if a Mermaid initiates the conversation with you.
                    if ((!cevent.WasRecipient && cevent.TargetSimDescription.IsMermaid)
                        || cevent.WasRecipient && !speaker.OccultManager.HasOccultType(Sims3.UI.Hud.OccultTypes.Mermaid))
                    {
                        if (speaker.SkillManager.HasElement(SkillNames.Cooking)  // Must know Cooking to learn
                            && RandomUtil.GetInt(1, kChanceOfLearningKelpRecipe) == 1)  // 1 in [default 4] chance of learning recipe
                        {
                            LearnKelpRecipe(speaker);
                        }
                    }
                }
            }
            return ListenerAction.Keep;
        }

        public static bool LearnKelpRecipe(Sim actor)
        {
            // Learn the lowest level recipe the sim doesn't already know,
            // provided Cooking skill is high enough

            Cooking simCooking = actor.SkillManager.GetElement(SkillNames.Cooking) as Cooking;
            Recipe kelpRecipe = new Recipe();
            // Echoweaver/Localization/DiscussKelpRecipes:LearnNotification
            string notification = Localization.LocalizeString("Echoweaver/Localization/DiscussKelpRecipes:LearnNotification");

            if ((kelpRecipe = Recipe.NameToRecipeHash["EWSeaweedSalad"]) != null
                && simCooking.SkillLevel >= kelpRecipe.CookingSkillLevelRequired
                && !simCooking.KnownRecipes.Contains(kelpRecipe.Key))
            {
                StyledNotification.Show(new StyledNotification.Format(actor.Name + notification +
                    kelpRecipe.GenericName + "!", StyledNotification.NotificationStyle.kGameMessagePositive));
                simCooking.AddRecipe(kelpRecipe);
                return true;
            }
            else if ((kelpRecipe = Recipe.NameToRecipeHash["MisoSoup"]) != null
                && simCooking.SkillLevel >= kelpRecipe.CookingSkillLevelRequired
                && !simCooking.KnownRecipes.Contains(kelpRecipe.Key))
            {
                StyledNotification.Show(new StyledNotification.Format(actor.Name + notification +
                    kelpRecipe.GenericName + "!", StyledNotification.NotificationStyle.kGameMessagePositive));
                simCooking.AddRecipe(kelpRecipe);
                return true;
            }
            else if ((kelpRecipe = Recipe.NameToRecipeHash["EWFishSandwich"]) != null
                && simCooking.SkillLevel >= kelpRecipe.CookingSkillLevelRequired
                && !simCooking.KnownRecipes.Contains(kelpRecipe.Key))
            {
                StyledNotification.Show(new StyledNotification.Format(actor.Name + notification +
                    kelpRecipe.GenericName + "!", StyledNotification.NotificationStyle.kGameMessagePositive));
                simCooking.AddRecipe(kelpRecipe);
                return true;
            }
            else if ((kelpRecipe = Recipe.NameToRecipeHash["EWSpicyTuna"]) != null
                && simCooking.SkillLevel >= kelpRecipe.CookingSkillLevelRequired
                && !simCooking.KnownRecipes.Contains(kelpRecipe.Key))
            {
                StyledNotification.Show(new StyledNotification.Format(actor.Name + notification +
                    kelpRecipe.GenericName + "!", StyledNotification.NotificationStyle.kGameMessagePositive));
                simCooking.AddRecipe(kelpRecipe);
                return true;
            }
            // Guess you know everything you can learn at your level
            return true;
        }
    }
}
