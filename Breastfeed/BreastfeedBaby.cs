using System;
using Sims3.Gameplay;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.Breastfeed
{
    public class BreastfeedBaby : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, BreastfeedBaby>
        {
            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if ((a.IsFemale || Loader.kAllowMaleNurse)
                    && Genealogy.IsParent(a.Genealogy, target.Genealogy) && target.SimDescription.ToddlerOrBelow)
                {
                    if (!Loader.kAllowAdoptiveNursing)
                    {
                        return !target.SimDescription.WasAdopted;
                    }
                    return true;
                }
                return false;
            }

            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return Localization.LocalizeString(s.IsFemale, "NonaMena/BreastFeedBaby/BreastFeed:InteractionName", new object[0]);
            }
        }

        [Tunable]
        [TunableComment("LTR increase from nursing.")]
        public static float kLTRIncreaseFromNursing = 15f;

        public static InteractionDefinition Singleton = new Definition();

        private bool mCensorEnabled;

        public override bool Run()
        {
            // Switch out the state machine used by CarryingChildPosture to my custom one
            StateMachineClient feedMachine = AcquireBreastfeedStateMachine(Actor, Target);
            ChildUtils.StartCarry(Actor, Target, feedMachine, false);

            SocialInteractionB interactionB = new CarriedChildInteractionB.Definition("BeGivenBottle")
                .CreateInstance(Actor, Target, GetPriority(), EffectivelyAutonomous, CancellableByPlayer) as SocialInteractionB;

            if (!ChildUtils.StartInteractionWithCarriedChild(this, interactionB))
            {
                return false;
            }

            BeginCommodityUpdates();

            if (Loader.kEnableBreastFeedCensor)
            {
                mCensorEnabled = true;
                Actor.EnableCensor(CensorType.FullBody);
            }

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Nurse");

            Actor.CarryingChildPosture.AnimateInteractionWithCarriedChild("Exit");

            Target.Motives.SetMax(CommodityKind.Hunger);
            Actor.GetRelationship(Target, true).LTR.UpdateLiking(kLTRIncreaseFromNursing);
            EndCommodityUpdates(true);
            ApplyBuffs();
            DoPostFeed();

            FinishLinkedInteraction(); 
            WaitForSyncComplete(3);  // Not sure why the ChildUtils finish method did not work here.

            // Restore the EA's child carry state machine (JAZZ script "CarryToddler")
            StateMachineClient carryMachine = ChildUtils.AcquireCarryStateMachine(Actor, Target);
            ChildUtils.StartCarry(Actor, Target, carryMachine, false);

            Cleanup();
            return true;
        }

        public static StateMachineClient AcquireBreastfeedStateMachine(Sim parent, Sim child)
        {
            StateMachineClient newMachine = StateMachineClient.Acquire(parent, "breastfeed_carry");
            newMachine.SetActor("x", parent);
            newMachine.SetActor("y", child);
            newMachine.EnterState("x", "Enter");
            newMachine.EnterState("y", "Enter");
            return newMachine;
        }

        public void ApplyBuffs()
        {
            Target.BuffManager.AddElement(BuffPeacefulBaby.StaticGuid,
                (Origin)ResourceUtils.HashString64("FromBabyNursing"));
            if (Actor.IsFemale)
            {
                Actor.BuffManager.AddElement(BuffPeacefulMama.StaticGuid,
                    (Origin)ResourceUtils.HashString64("FromNursingChild"));
            }
            else
            {
                Actor.BuffManager.AddElement(BuffPeacefulPapa.StaticGuid,
                    (Origin)ResourceUtils.HashString64("FromNursingChild"));
            }
        }

        public void DoPostFeed()
        {
            if (Actor.SimDescription.IsVampire)
            {
                Actor.Motives.ChangeValue(CommodityKind.VampireThirst, Loader.kHungerDrainFromNursing);
            }
            else if (Actor.SimDescription.IsPlantSim)
            {
                Actor.Motives.ChangeValue(CommodityKind.Hygiene, Loader.kHungerDrainFromNursing);
            }
            else
            {
                Actor.Motives.ChangeValue(CommodityKind.Hunger, Loader.kHungerDrainFromNursing);
            }
        }

        public override void Cleanup()
        {
            if (mCensorEnabled)
            {
                Actor.AutoEnableCensor();
            }
            base.Cleanup();
        }
    }
}