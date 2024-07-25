using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CelebritySystem;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Miscellaneous;
using Sims3.Gameplay.Objects.Seating;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using static Sims3.Gameplay.Actors.Sim.FightPet;
using static Sims3.Gameplay.Objects.Miscellaneous.BubbleBar;
using static Sims3.SimIFace.VisualEffect;

namespace Echoweaver.Sims3Game.MoreGroupTalk
{
    public class EWBlowBubbles : BlowBubbles
    {

        public new class Definition : InteractionDefinition<Sim, BubbleBar, EWBlowBubbles>
        {
            public BubbleFlavor CurrentBubbleFlavor;

            public Definition()
            {
            }

            public Definition(BubbleFlavor bubFlav)
            {
                CurrentBubbleFlavor = bubFlav;
            }

            public override string GetInteractionName(Sim actor, BubbleBar target, InteractionObjectPair iop)
            {
                if (target.LotCurrent.IsResidentialLot)
                {
                    return LocalizeString(actor, Enum.GetName(typeof(BubbleFlavor), CurrentBubbleFlavor) + "NoCost");
                }
                return LocalizeString(actor, Enum.GetName(typeof(BubbleFlavor), CurrentBubbleFlavor), kCostToUse);
            }

            public override bool Test(Sim a, BubbleBar target, bool isAutonomous,
                ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!target.HasChairs())
                {
                    greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(LocalizeString
                        (a, "NeedsStools"));
                    return false;
                }
                if (!target.LotCurrent.IsResidentialLot)
                {
                    if (a.FamilyFunds < kCostToUse)
                    {
                        greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback
                            (LocalizeString(a, "CannotAfford"));
                        return false;
                    }
                }
                else if (target.NumUsesRemaining <= 0)
                {
                    greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback
                        (LocalizeString(a, "OutOfBubbles"));
                    return false;
                }
                return target.HasUnoccupiedChair(a);
            }

            public override string[] GetPath(bool isFemale)
            {
                return new string[1] { LocalizeString(isFemale, "BlowBubbles") };
            }

            public override void AddInteractions(InteractionObjectPair iop, Sim actor, BubbleBar target,
                List<InteractionObjectPair> results)
            {
                foreach (BubbleFlavor value in Enum.GetValues(typeof(BubbleFlavor)))
                {
                    results.Add(new InteractionObjectPair(new Definition(value), iop.Target));
                }
            }
        }
        public static new InteractionDefinition Singleton = new Definition();

        public override void ConfigureInteraction()
        {
            Definition definition = InteractionDefinition as Definition;
            CurrentBubbleFlavor = definition.CurrentBubbleFlavor;
        }

        public override bool Run()
        {

            ChairBarStool chairBarStool = Actor.Posture.Container as ChairBarStool;
            if (chairBarStool == null)
            {
                return false;
            }
            if (!Target.LotCurrent.IsResidentialLot && !CelebrityManager
                .TryModifyFundsWithCelebrityDiscount(Actor, Target, kCostToUse, true))
            {
                return false;
            }
            StandardEntry();
            Slot chosenSlot = GetChosenSlot(chairBarStool);
            EnterStateMachine("BubbleBar", "Enter", "x");
            PipeUsed = GlobalFunctions.CreateObjectOutOfWorld("bubbleBarPipeProp", ProductVersion.EP3);
            PipeUsed.SetHiddenFlags(unchecked((HiddenFlags)(-1)));
            if (PipeUsed == null)
            {
                StandardExit();
                return false;
            }
            PipeUsed.ParentToSlot(Target, GetPipeSlot(chosenSlot));
            SetActor("Pipe", PipeUsed);
            SetActor("barstool", chairBarStool);
            AddPersistentScriptEventHandler(101u, new SacsEventHandler(PlayBubbleEffect));
            AddOneShotScriptEventHandler(110u, new SacsEventHandler(HidePipe));
            mNumMinsForCurrentInteraction = RandomUtil.GetFloat(kMinSessionLength, kMaxSessionLength);
            Target.StartInternalBubbleEffect();
            Target.StartTopBubbleEffect();
            AnimateSim("BlowingLoop");
            Target.StartBroadcaster();
            Actor.SkillManager.AddElement(SkillNames.Bubbles);
            BeginCommodityUpdates();
            Actor.RegisterGroupTalk();
            DoLoop(ExitReason.Default, new InsideLoopFunction(EWLoopCallback), mCurrentStateMachine);
            Actor.UnregisterGroupTalk();
            EndCommodityUpdates(true);
            AnimateSim("Exit");
            if (mBuffName != 0)
            {
                Actor.BuffManager.UnpauseBuff(mBuffName);
            }
            if (Target.LotCurrent.IsResidentialLot)
            {
                Target.NumUsesRemaining--;
                if (Target.NumUsesRemaining <= 0)
                {
                    Target.StartTopBubbleEffect();
                }
            }
            StandardExit();
            if (Target.ActorsUsingMe.Count == 0)
            {
                Target.StopBroadcaster();
            }
            return true;
        }

        public void EWLoopCallback(StateMachineClient smc, LoopData loopData)
        {
            mTimePassed += loopData.mDeltaTime;
            if (mBuffName == 0 && mTimePassed >= kMinsUntilBuff)
            {
                mBuffName = AddFlavorBuff();
            }
            Actor.TryGroupTalk();
            if (mTimePassed >= mNumMinsForCurrentInteraction)
            {
                Actor.AddExitReason(ExitReason.Finished);
            }
        }
    }

}

