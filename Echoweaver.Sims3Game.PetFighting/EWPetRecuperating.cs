
//using Sims3.Gameplay;
//using Sims3.Gameplay.Actors;
//using Sims3.Gameplay.ActorSystems;
//using Sims3.Gameplay.Autonomy;
//using Sims3.Gameplay.CAS;
//using Sims3.Gameplay.Core;
//using Sims3.Gameplay.EventSystem;
//using Sims3.Gameplay.Interactions;
//using Sims3.Gameplay.Interfaces;
//using Sims3.Gameplay.Utilities;
//using Sims3.SimIFace;
//using Sims3.SimIFace.CAS;

//namespace Echoweaver.Sims3Game.PetFighting
//{
//	public class EWPetWoundPassOut : Interaction<Sim, Sim>
//	{
//		public class Definition : InteractionDefinition<Sim, Sim, EWPetWoundPassOut>
//		{
//			public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
//			{
//				return LocalizeString(a.IsFemale, "InteractionName");
//			}

//			public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//			{
//				// Really we should drop queue?
//				if ((a.IsCat || a.IsADogSpecies) && !a.IsSleeping && !a.InteractionQueue.HasInteractionOfType(Singleton))
//				{
//					return true;
//				}
//				return false;
//			}
//		}

//		//public class SleepOnFloorDefinition : InteractionDefinition<Sim, Sim, EWPetWoundPassOut>
//		//{
//		//	public override string GetInteractionName(Sim a, Sim target, InteractionObjectPair interaction)
//		//	{
//		//		return LocalizeString(a.IsFemale, "SleepOnFloor");
//		//	}

//		//	public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//		//	{
//		//		WildParty situationOfType = a.GetSituationOfType<WildParty>();
//		//		if (situationOfType != null && situationOfType.Started && a == target)
//		//		{
//		//			if (!a.IsSleeping && !a.InteractionQueue.HasInteractionOfType(SleepOnFloorSingleton))
//		//			{
//		//				return !FirefighterSituation.IsSimOnFire(a);
//		//			}
//		//			return false;
//		//		}
//		//		return false;
//		//	}
//		//}

//		public class Jig : SocialJigOnePerson
//		{
//		}

//		public const string sLocalizationKey = "Gameplay/ActorSystems/BuffExhausted/PassOut";

//		[TunableComment("Range:  Sim minutes.  Description:  Number of minutes to pass out when motive fails because of wound.")]
//		[Tunable]
//		public static float kPetRecuperateMinutes = 60f;

//		[Tunable]
//		[TunableComment("Range:  Distance.  Description:  Distance at which Sims react to another Sim passing out.")]
//		public static float kDistanceToReact = 10f;

//		[TunableComment("Energy hit for when a sim fails to be knocked out.")]
//		[Tunable]
//		public static float kEnergyHitOnFailToKnockOut = 20f;

//		public static InteractionDefinition Singleton = new Definition();

////		public static InteractionDefinition SleepOnFloorSingleton = new SleepOnFloorDefinition();

//		public bool IgnoreExhaustedBuff;

//		public bool IsKnockedOut;

//		public bool SkipCollapseJig;

//		public VisualEffect mZzz;

//		public float PassOutMinutesOverride = -1f;

//		public bool BeingDramatic;

//		public SocialJigOnePerson mJig;

//		public static ulong kIconNameHash = ResourceUtils.HashString64("hud_interactions_passedout");

//		public static string LocalizeString(bool isFemale, string name, params object[] parameters)
//		{
//			// TODO: Change name to recuperate
//			return Localization.LocalizeString(isFemale, "Gameplay/ActorSystems/BuffExhausted/PassOut:" + name, parameters);
//		}

//		public override bool Test()
//		{
//			if (Actor.IsSleeping || (Actor.CurrentInteraction != null && Actor.CurrentInteraction is ISleeping))
//			{
//				return false;
//			}
//			if(Actor.BuffManager.HasAnyElement(BuffNames.ExhaustedPet, BuffNames.StarvingPet)
//				&& Actor.BuffManager.HasElement(BuffEWGraveWound.StaticGuid))
//			{
//				return true;
//			}
//			return false;
//		}

//		public override bool Run()
//		{
//			if (!SkipCollapseJig && Actor.SimDescription.AdultOrAbove)
//			{
//				mJig = CreateSleepingJig();
//				if (mJig == null)
//				{
//					return false;
//				}
//				mJig.RegisterParticipants(Actor, null);
//				mJig.AddToWorld();
//				mJig.SetOpacity(0f, 0f);
//				int num = 3;
//				bool flag2 = false;
//				do
//				{
//					num--;
//					Vector3 position = Actor.Position;
//					Vector3 forward = Actor.ForwardVector;
//					if (GlobalFunctions.FindGoodLocationNearby(mJig, ref position, ref forward))
//					{
//						mJig.SetPosition(position);
//						mJig.SetForward(forward);
//						Route val = Actor.CreateRoute();
//						val.DoRouteFail = false;
//						val.AddObjectToIgnoreForRoute(mJig.ObjectId);
//						val.AddObjectToIgnoreForRoute(Actor.ObjectId);
//						val.PlanToSlot((IHasScriptProxy)(object)mJig, mJig.GetRoutingSlots());
//						flag2 = Actor.DoRoute(val);
//						if (Actor.HasExitReason(~(ExitReason.RouteFailed)))
//						{
//							break;
//						}
//						Actor.ClearExitReasons();
//					}
//					if (!flag2 && num == 1)
//					{
//						Actor.PlayReaction(ReactionTypes.Yawn, ReactionSpeed.ImmediateWithoutOverlay);
//						Route val = Actor.CreateRoute();
//						val.DoRouteFail = false;
//						val.PlanToPointRadialRange(Actor.Position, 2f, 10f);
//						Actor.DoRoute(val);
//						if (Actor.HasExitReason(~(ExitReason.RouteFailed | ExitReason.UserCanceled)))
//						{
//							break;
//						}
//						Actor.ClearExitReasons();
//					}
//				} while (!flag2 && num >= 0);
//				if (!flag2)
//				{
//					if (IgnoreExhaustedBuff && Actor.HasNoExitReason())
//					{
//						Actor.Motives.SetValue(CommodityKind.Energy, Actor.Motives.GetValue(CommodityKind.Energy) - kEnergyHitOnFailToKnockOut);
//					}
//					Actor.BuffManager.RemoveElement(BuffNames.KnockedOut);

//					return false;
//				}
//			}
//			StandardEntry();

//			BeginCommodityUpdates();
//			string text = Actor.OccultManager.GetSleepFXName();
//			Slot val2 = Sim.ContainmentSlots.Mouth;
//			mZzz = VisualEffect.Create(text);
//			mZzz.ParentTo((IHasScriptProxy)(object)Actor, val2);
//			mZzz.Start();
//			Actor.SetIsSleeping(value: true, !SkipCollapseJig);
//			// TODO: Should we send a passed out event? I don't want to try to make custom event
//			EventTracker.SendEvent(EventTypeId.kSimPassedOut, Actor);
//			EnterStateMachine("PetPassOut", "Enter", "x");
//			AnimateSim("PassOutLoop");

//			bool succeeded = false;
//			ExitReason acceptedExitReasons = ~(ExitReason.Finished | ExitReason.MidRoutePushRequested | ExitReason.Replan);
//			float initialMinutes = SimClock.ElapsedTime(TimeUnit.Minutes);
//			float cumulativeMinutes = initialMinutes;
//			while (!Actor.WaitForExitReason(1f, acceptedExitReasons))
//			{
//				// Snoring at interval 4f while passed out
//				float currentMinutes = SimClock.ElapsedTime(TimeUnit.Minutes);
//				if (currentMinutes - initialMinutes > kPetRecuperateMinutes)
//				{
//					succeeded = true;
//					break;
//				}
//				if (currentMinutes >= cumulativeMinutes)
//				{
//					Audio.StartObjectSound(Actor.ObjectId, "vo_snoreA", false);
//					cumulativeMinutes += 4f;
//				}
//				UpdateLoop();
//			}
//			AnimateSim("Exit");
//			EndCommodityUpdates(succeeded);
//			DestroyJig();
//			Actor.SetIsSleeping(value: false, !SkipCollapseJig);
//			mZzz.Stop();
//			mZzz.Dispose();
//			mZzz = null;
//			StandardExit();
//			return succeeded;
//		}

//		public override void Cleanup()
//		{
//			Actor.SetIsSleeping(value: false);
//			if (mZzz != null)
//			{
//				mZzz.Stop();
//				mZzz.Dispose();
//				mZzz = null;
//			}
//			DestroyJig();
//			base.Cleanup();
//		}

//		public void DestroyJig()
//		{
//			if (mJig != null)
//			{
//				mJig.Destroy();
//				mJig = null;
//			}
//		}

//		public override ThumbnailKey GetIconKey()
//		{
//			// TODO: Need recuperate icon
//			return new ThumbnailKey(new ResourceKey(kIconNameHash, 796721156u, 0u), (ThumbnailSize)1);
//		}

//		public SocialJigOnePerson CreateSleepingJig()
//		{
//			SimDescription simDescription = Actor.SimDescription;
//			CASAgeGenderFlags species = simDescription.Species;

//			if (species == CASAgeGenderFlags.Dog)
//			{
//				return GlobalFunctions.CreateObjectOutOfWorld("dogGenericIdleJig", (ProductVersion)512) as SocialJigOnePerson;
//			}
//			if (species == CASAgeGenderFlags.LittleDog || species == CASAgeGenderFlags.Cat)
//			{
//				return GlobalFunctions.CreateObjectOutOfWorld("smallPetGenericIdleJig", (ProductVersion)512) as SocialJigOnePerson;
//			}
//			return null;
//		}
//	}
//}