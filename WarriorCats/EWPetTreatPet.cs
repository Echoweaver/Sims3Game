using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.Controller;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using static Sims3.UI.StyledNotification;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class EWPetTreatPet : SocialInteraction
    {
        public class Definition : InteractionDefinition<Sim, Sim, EWPetTreatPet>
        {
            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                SocialInteraction socialInteraction = new EWPetTreatPet();
                socialInteraction.Init(ref parameters);
                return socialInteraction;
            }

            public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (isAutonomous)
                {
                    return false;
                }
                return true;
            }


            public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
            {
                return "EWPetTreatPet";
            }
        }


        public bool mSuccess;
        public BuffNames mBuffID;
        public IGameObject mHerb;
        public bool mDestroyPrey;

        public ReactionBroadcaster mPetFightNoiseBroadcast;

        public static InteractionDefinition Singleton = new Definition();


        [Tunable]
        [TunableComment("The LTR gain for a positive reaction from successful treatment.")]
        public static float kLtrGainForSuccess = 20f;

        [Tunable]
        [TunableComment("The LTR loss for a negative reaction from failed treatment.")]
        public static float kLtrLossForFail = 10f;

        public void SetParams(bool pSuccess, BuffNames pBuffID, IGameObject pHerb)
        {
            mSuccess = pSuccess;
            mBuffID = pBuffID;
            mHerb = pHerb;
            mDestroyPrey = false;
        }


        public override bool Run()
        {
            Actor.SynchronizationLevel = Sim.SyncLevel.NotStarted;
            Target.SynchronizationLevel = Sim.SyncLevel.NotStarted;
            Target.InteractionQueue.CancelAllInteractions();
            Target.DisableInteractions();

            if (mHerb.CatHuntingComponent != null)
            {
                ICatPrey prey = mHerb as ICatPrey;
                if (!PetCarrySystem.PickUpFromSimInventory(Actor, prey, removeFromInventory: true))
                {
                    prey.UpdateVisualState(CatHuntingModelState.InInventory);
                    return false;
                }
                ((ICatPrey)mHerb).UpdateVisualState(CatHuntingModelState.Carried);
            }
            else
            {
                PickUpFromSimInventory(Actor, mHerb as IPetCarryable, true);
            }

            //Actor.RouteTurnToFace(Target.Position);
            //Target.RouteTurnToFace(Actor.Position);

            Actor.RouteToObjectRadius(Target, PetEatPrey.kCatEatingDistance);
            Target.EnableInteractions();
            if (!BeginSocialInteraction(new SocialInteractionB.Definition(), false, 0.75f, true))
            {
                return false;
            }
            if (!SafeToSync())
            {
                return false;
            }

            StandardEntry();
            BeginCommodityUpdates();
            StartSocialContext();

            // Animation Start
            //Actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
            //CarryUtils.Request(Actor, "PutDown");

            if (mHerb.CatHuntingComponent != null)
            {
                PetCarrySystem.PutDownOnFloor(Actor);
                //Actor.CarryStateMachine.SetParameter("Height", SurfaceHeight.Floor);
                //CarryUtils.Request(Actor, "PutDown");
                ((ICatPrey)mHerb).UpdateVisualState(CatHuntingModelState.InWorld);
                StyledNotification.Show(new StyledNotification.Format("mHerb location: " + mHerb.PositionOnFloor,
                    StyledNotification.NotificationStyle.kDebugAlert));

            }


            if (!Target.RouteTurnToFace(mHerb.PositionOnFloor))
            {
                StyledNotification.Show(new StyledNotification.Format("Routing failed",
                    StyledNotification.NotificationStyle.kDebugAlert));
                //return false;
            }
            EnterStateMachine("eatofffloor", "Enter", "x");
            SetActor("x", Target);
            AnimateSim("EatOffFloorLoop");
            bool flag = DoTimedLoop(3f, ExitReason.Default);
            mDestroyPrey = true;
            AnimateSim("Exit");

            //AcquireStateMachine("eatharvestablepet");
            //mCurrentStateMachine.SetActor("x", Target);
            //mCurrentStateMachine.EnterState("x", "Enter");
            //SetParameter("IsEatingOnGround", paramValue: true);
            //AnimateSim("EatHarvestable");
            //AnimateSim("Exit");

            //AcquireStateMachine("eatofffloor");
            //SetParameter("x", Target);
            //EnterState("x", "Enter");
            //AnimateSim("EatOffFloorLoop");
            //bool flag = DoTimedLoop(3f, ExitReason.Default);
            //AnimateSim("Exit");

            // Animation End

            if (mSuccess)
            {
                Actor.ShowTNSIfSelectable("EWLocalize - success", NotificationStyle.kGameMessagePositive);
                Target.BuffManager.RemoveElement(mBuffID);
                    // Say thank you
                    //SocialInteractionA.Definition definition2 = new SocialInteractionA.Definition("Nuzzle Auto Accept",
                    //    new string[0], null, initialGreet: false);
                    //InteractionInstance nuzzleInteraction = definition2.CreateInstance(Target, Actor,
                    //    new InteractionPriority(InteractionPriorityLevel.UserDirected), Autonomous,
                    //    CancellableByPlayer);
                    //Target.InteractionQueue.AddNext(nuzzleInteraction);
                DoLtrAdjustment(goodReaction: true);
            }
            else
            {
                Actor.ShowTNSIfSelectable("EWLocalize - failed", NotificationStyle.kGameMessageNegative);
                DoLtrAdjustment(goodReaction: false);
            }

            FinishLinkedInteraction(true);
            WaitForSyncComplete();
            StandardExit();
            EndCommodityUpdates(true);
            return true;
        }

        public void DoLtrAdjustment(bool goodReaction)
        {
            float num = !goodReaction ? (0f - kLtrLossForFail) : kLtrGainForSuccess;
            Relationship relationship = Relationship.Get(Actor, Target,
                createIfNone: true);
            LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
            float currentLTRLiking = relationship.CurrentLTRLiking;
            relationship.LTR.UpdateLiking(num);
            LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
            float currentLTRLiking2 = relationship.CurrentLTRLiking;
            bool isPositive = currentLTRLiking2 >= currentLTRLiking;
            SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly,
                Actor, Target, isPositive, 0, currentLTR, currentLTR2);
        }


        public static bool PickUpFromSimInventory(Sim a, IBaseCarryable o, bool removeFromInventory)
        {
            //IL_004c: Unknown result type (might be due to invalid IL or missing references)
            if (removeFromInventory && !a.Inventory.TryToRemove(o))
            {
                return false;
            }
            o.AddToWorld();
            CarryUtils.Acquire(a, o);
            EnterCarry(a, o);
            a.CarryStateMachine.SetParameter("Height", SurfaceHeight.SimInventory);
            o.AddToUseList(a);
            Objects.EnableDropShadow(o.ObjectId, false);
            CarryUtils.Request(a, "PickUp");
            o.SetHiddenFlags(HiddenFlags.Nothing);
            CarryUtils.Request(a, "Carry");
            CarryUtils.VerifyAnimationParent(o, a);
            return true;
        }

        public static void EnterCarry(Sim a, IBaseCarryable o)
        {
            a.CarryStateMachine.SetActor("x", a);
            a.CarryStateMachine.SetActor("object", o);
            a.CarryStateMachine.SetParameter("model", "prey", ProductVersion.EP5);
            a.CarryStateMachine.SetParameter("NamespaceMap0From", o.CarryModelName);
            a.CarryStateMachine.SetParameter("NamespaceMap0To", "object");
            a.CarryStateMachine.EnterState("x", "Enter");
        }


        public override void Cleanup()
        {
            if (mDestroyPrey)
            {
                DestroyObject(mHerb);
            }
            base.Cleanup();
        }

    }
}
