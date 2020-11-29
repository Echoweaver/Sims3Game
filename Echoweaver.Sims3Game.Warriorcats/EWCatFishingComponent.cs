//using Sims3.Gameplay;
//using Sims3.Gameplay.Abstracts;
//using Sims3.Gameplay.Actors;
//using Sims3.Gameplay.ActorSystems;
//using Sims3.Gameplay.Autonomy;
//using Sims3.Gameplay.CAS;
//using Sims3.Gameplay.Core;
//using Sims3.Gameplay.EventSystem;
//using Sims3.Gameplay.Interactions;
//using Sims3.Gameplay.InteractionsShared;
//using Sims3.Gameplay.Interfaces;
//using Sims3.Gameplay.MapTags;
//using Sims3.Gameplay.ObjectComponents;
//using Sims3.Gameplay.Objects.Fishing;
//using Sims3.Gameplay.Objects.Gardening;
//using Sims3.Gameplay.Objects.Insect;
//using Sims3.Gameplay.PetObjects;
//using Sims3.Gameplay.Pools;
//using Sims3.Gameplay.Skills;
//using Sims3.Gameplay.Socializing;
//using Sims3.Gameplay.ThoughtBalloons;
//using Sims3.Gameplay.TuningValues;
//using Sims3.Gameplay.Utilities;
//using Sims3.SimIFace;
//using Sims3.SimIFace.CAS;
//using Sims3.SimIFace.Enums;
//using Sims3.UI;
//using Sims3.UI.Controller;
//using System;
//using System.Collections.Generic;
//using static Sims3.SimIFace.Route;
//using static Sims3.UI.ObjectPicker;
//using static Sims3.UI.StyledNotification;

//namespace Echoweaver.Sims3Game
//{
//	public class EWCatFishingComponent : StaticObjectComponent, IViewableBase
//	{
//        public class LocateFish : InteractionGameObjectHit<Sim, GameObject>
//        {
//            public class Definition : InteractionDefinition<Sim, GameObject, LocateFish>
//            {
//                public Definition()
//                {
//                }

//                public override bool Test(Sim a, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//                {
//                    Sim sim = target as Sim;
//                    if (sim != null && a != sim)
//                    {
//                        return false;
//                    }
//                    EWCatFishingSkill skill = a.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
//                    if (skill != null)
//                    {
//                        if (skill.CanCatchPreyFish())
//                        {
//                            return PetManager.PetSkillFatigueTest(a, PetManager.PetSkillFatigeGreyedOutTooltipType.CatHunting, ref greyedOutTooltipCallback);
//                        }
//                        greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(LocalizeString(a.IsFemale, "NoPreyAtSkillLevelTooltip"));
//                    }
//                    return false;
//                }

//                public override InteractionTestResult Test(ref InteractionInstanceParameters parameters, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//                {
//                    InteractionTestResult result = base.Test(ref parameters, ref greyedOutTooltipCallback);
//                    if (!IUtil.IsPass(result))
//                    {
//                        return result;
//                    }
//                    if (!parameters.Autonomous)
//                    {
//                        GameObjectHitType mType = parameters.Hit.mType;
//                        GameObjectHitType val = mType;
//                        switch ((int)val - 2)  // Added cast here because of Visual Studio complaint
//                        {
//                            case 0:
//                            case 1:
//                            case 4:
//                            case 6:
//                            case 7:
//                            case 8:
//                            case 9:
//                                return InteractionTestResult.Gen_BadTerrainType;
//                        }
//                        Route val2 = parameters.Actor.CreateRoute();
//                        if (!val2.IsPointRoutable(parameters.Hit.mPoint))
//                        {
//                            return InteractionTestResult.Gen_BadTerrainType;
//                        }
//                        LotLocation invalid = LotLocation.Invalid;
//                        ulong lotLocationAtFloor = World.GetLotLocationAtFloor(parameters.Hit.mPoint, ref invalid);
//                        if (World.HasPool(lotLocationAtFloor, invalid))
//                        {
//                            return InteractionTestResult.Gen_BadTerrainType;
//                        }
//                    }
//                    return InteractionTestResult.Pass;
//                }

//                public override void AddInteractions(InteractionObjectPair iop, Sim actor, GameObject target, List<InteractionObjectPair> results)
//                {
//                    results.Add(new InteractionObjectPair(new Definition(), target));
//                }

//                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
//                {
//                    return LocalizeString(actor.IsFemale, "PreyType" + CatHuntingSkill.PreyType.Fish);
//                }

//                public override string[] GetPath(bool isFemale)
//                {
//                    string text = LocalizeString(isFemale, "HuntingPieMenu");
//                    string text2 = LocalizeString(isFemale, "LocatePrey");
//                    return new string[2] {
//                    text,
//                    text2
//                };
//                }
//            }

//            public static InteractionDefinition Singleton = new Definition();

//            public override ThumbnailKey GetIconKey()
//            {
//                return new ThumbnailKey(ResourceKey.CreatePNGKey("w_hunting_cat_skill", ResourceUtils.ProductVersionToGroupId((ProductVersion)512)), (ThumbnailSize)1);
//            }

//            public override string GetInteractionName()
//            {
//                Definition definition = base.InteractionDefinition as Definition;
//                string text = LocalizeString(Actor.IsFemale, "PreyType" + CatHuntingSkill.PreyType.Fish);
//                return LocalizeString(Actor.IsFemale, "LocatePreyInteractionName", text);
//            }

//            public override bool Run()
//            {
//                if ((int)Hit.mType == 0 || (int)Hit.mType == 1 || !World.IsPositionOutside(Hit.mPoint))
//                {
//                    if (!Actor.IsOutside)
//                    {
//                        Door door = null;
//                        if (!Actor.LotCurrent.RouteToFrontDoorOrMailbox(Actor, Sim.MinDistanceFromDoorWhenGoingIntoYard, Sim.MaxDistanceFromDoorWhenGoingIntoYard, ref door, wantToBeOutside: true))
//                        {
//                            return false;
//                        }
//                    }
//                }
//                else if (!Actor.RouteToPoint(Hit.mPoint))
//                {
//                    return false;
//                }
//                StandardEntry();
//                BeginCommodityUpdates();
//                Actor.PlaySoloAnimation("ac_hunting_locatePrey_sniffAir_x", (ProductVersion)512);
//                Actor.MapTagManager.RemoveMapTagsOfType((MapTagType)46);
//                bool flag = false;
//                FishingSpot[] objects = Sims3.Gameplay.Queries.GetObjects<FishingSpot>();
//				if (objects.Length > 0)
//				{
//					flag = true;
//					FishingSpot[] array = objects;
//					foreach (FishingSpot target in array)
//					{
//						CatHuntingMapTag tag = new CatHuntingMapTag(target, Actor, CatHuntingSkill.PreyType.Fish);
//						Actor.MapTagManager.AddTag(tag);
//					}
//				}
//                if (flag)
//                {
//                    EWCatFishingSkill skill = Actor.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
//                    skill.StartMapTagAlarm();
//                    if (!CameraController.IsMapViewModeEnabled() && Actor.IsActiveSim)
//                    {
//                        Camera.ToggleMapView();
//                    }
//                }
//                else
//                {
//                    Actor.ShowTNSIfSelectable(Localization.LocalizeString(Actor.IsFemale, "Gameplay/Core/Terrain:NoPreyFound", Actor),
//						(NotificationStyle)2, ObjectGuid.InvalidObjectGuid);
//                }
//                EndCommodityUpdates(succeeded: true);
//                StandardExit();
//                return true;
//            }
//        }

//        public class GoCatchFish : SocialInteractionA
//		{
//			public class GoCatchFishDefinition : Definition
//			{
//				public List<Pair<Lot, GameObjectHit>> FishingPoints = new List<Pair<Lot, GameObjectHit>>();

//				public GoCatchFishDefinition()
//					: base(kCatchSpecificSocKey, null, null, initialGreet: false)
//				{
//					PopulateFishingPoints();
//				}

//				public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
//				{
//					GoCatchFish goCatchFish = new GoCatchFish();
//					goCatchFish.Init(ref parameters);
//					return goCatchFish;
//				}

//				public override void AddInteractions(InteractionObjectPair iop, Sim actor, Sim target, List<InteractionObjectPair> results)
//				{
//						results.Add(new InteractionObjectPair(new GoCatchFishDefinition(), target));
//				}

//				public override string GetInteractionName(Sim s, Sim target, InteractionObjectPair interaction)
//				{
//					return EWCatFishingComponent.LocalizeString(s.IsFemale, "PreyType" + CatHuntingSkill.PreyType.Fish);
//				}

//				public override string[] GetPath(bool isFemale)
//				{
//					string text = EWCatFishingComponent.LocalizeString(isFemale, "HuntingPieMenu");
//					string text2 = EWCatFishingComponent.LocalizeString(isFemale, "GoCatchA");
//					return new string[2] {
//					text,
//					text2
//				};
//				}

//				public override bool Test(Sim a, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					EWCatFishingSkill skill = target.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
//					if (skill != null && skill.SkillLevel >= kCatchSpecificRequiredHuntingSkill)
//					{
//						if (skill.CanCatchPreyFish())
//						{
//							if (!AreAnyFishingPointsValid(target))
//							{
//								greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(EWCatFishingComponent.LocalizeString(a.IsFemale, "NoFishingSpotsTooltip"));
//								return false;
//							}
//							return PetManager.PetSkillFatigueTest(target, PetManager.PetSkillFatigeGreyedOutTooltipType.CatHunting, ref greyedOutTooltipCallback);
//						}
//						greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(EWCatFishingComponent.LocalizeString(a.IsFemale, "NoPreyAtSkillLevelTooltip"));
//					}
//					return false;
//				}

//				public void PopulateFishingPoints()
//				{
//					foreach (Lot allLotsWithoutCommonException in LotManager.AllLotsWithoutCommonExceptions)
//					{
//						Vector3[] array = default(Vector3[]);
//						if (World.FindPondRepresentativePositions(allLotsWithoutCommonException.LotId, out array) && array.Length != 0)
//						{
//							Vector3[] array2 = array;
//							foreach (Vector3 position in array2)
//							{
//								GameObjectHit b = InteractionInstance.CreateFakeGameObjectHit(position);
//								if ((int)b.mType == 9 || ((int)b.mType == 8 && PondManager.ArePondsLiquid()))
//								{
//									FishingPoints.Add(new Pair<Lot, GameObjectHit>(allLotsWithoutCommonException, b));
//								}
//							}
//						}
//					}
//				}

//				public bool AreAnyFishingPointsValid(Sim fishingCat)
//				{
//					foreach (Pair<Lot, GameObjectHit> fishingPoint in FishingPoints)
//					{
//						if (fishingPoint.First.IsCommunityLot || fishingPoint.First == fishingCat.LotHome)
//						{
//							return true;
//						}
//					}
//					return false;
//				}
//			}


//			[TunableComment("Range 0-100, Min/Max chance of cat understanding what Sim asked them to go hunt for linearly interpolated based on cat's hunting skill level.")]
//			[Tunable]
//			public static int[] kMinMaxChanceUnderstand = new int[2] {
//			5,
//			80
//			};

//			[TunableComment("Range 0-100.  This value will be added to the ouctome of kMinMaxChanceUnderstand if the pet has the Genius trait.")]
//			[Tunable]
//			public static int kChanceUnderstandModGenius = 10;

//			[Tunable]
//			[TunableComment("Range 0-100.  This value will be added to the ouctome of kMinMaxChanceUnderstand if the pet has the Clueless trait.")]
//			public static int kChanceUnderstandModClueless = -10;

//			[TunableComment("Range 0-10.  The cat's hunting skill must be >= than this for catch anything version of the interaction to be available.")]
//			[Tunable]
//			public static int kCatchAnythingRequiredHuntingSkill = 4;

//			[TunableComment("Range 0-10.  The cat's hunting skill must be >= than this for catch specific prey version of the interaction to be available.")]
//			[Tunable]
//			public static int kCatchSpecificRequiredHuntingSkill = 5;

//			public static string kCatchSpecificSocKey = "Go Catch Specific Prey";

//			public static string kCatchAnySocKey = "Go Catch Any Prey";

//			public static InteractionDefinition Singleton = new GoCatchFishDefinition();

//			public bool mCatUnderstands;

//			public override ThoughtBalloonManager.BalloonData CreateSpeechBalloon(string actionKey, Sim actor, Sim target, ActiveTopic topic)
//			{
//				return null;
//			}

//			public override ThoughtBalloonManager.BalloonData CreateListenerSpeechBalloon(string actionKey, Sim actor, Sim target, ActiveTopic topic)
//			{
//				return null;
//			}

//			public void OnGoCatchSpecificScriptEvent(StateMachineClient sender, IEvent evt)
//			{
//				if (evt.EventId == 101)
//				{
//					ShowThoughtBalloonFishing(Actor);
//					mCatUnderstands = RollDiceForCatUnderstands();
//				}
//				else
//				{
//					if (evt.EventId != 102)
//					{
//						return;
//					}
//					if (mCatUnderstands)
//					{
//						ShowThoughtBalloonFishing(Target);
//						PushStalkForFish(forceFailureObj: false);
//						return;
//					}
//					ShowFailureObjectBalloon(Target);
//				}
//			}

//			public bool RollDiceForCatUnderstands()
//			{
//				EWCatFishingSkill skill = Target.SkillManager.GetSkill<EWCatFishingSkill>(EWCatFishingSkill.SkillNameID);
//				if (skill != null)
//				{
//					float[] traitAdjustedChance = GetTraitAdjustedChance(Actor, kMinMaxChanceUnderstand);
//					float num = MathHelpers.LinearInterpolate(0f, skill.MaxSkillLevel, traitAdjustedChance[0], traitAdjustedChance[1], skill.SkillLevel);
//					if (Target.HasTrait(TraitNames.GeniusPet))
//					{
//						num += (float)kChanceUnderstandModGenius;
//					}
//					else if (Target.HasTrait(TraitNames.CluelessPet))
//					{
//						num += (float)kChanceUnderstandModClueless;
//					}
//					return RandomUtil.RandomChance(num);
//				}
//				return false;
//			}

//			public void PushStalkForFish(bool forceFailureObj)
//			{
//				if (!forceFailureObj)
//				{
//					GoCatchFishDefinition goCatchPreyDefinition = base.InteractionDefinition as GoCatchFishDefinition;
//					List<GameObjectHit> list = new List<GameObjectHit>();
//					foreach (Pair<Lot, GameObjectHit> fishingPoint in goCatchPreyDefinition.FishingPoints)
//					{
//						if (fishingPoint.First.IsCommunityLot || fishingPoint.First == Target.LotHome)
//						{
//							list.Add(fishingPoint.Second);
//						}
//					}
//					if (list.Count != 0)
//					{
//						GameObjectHit randomObjectFromList = RandomUtil.GetRandomObjectFromList(list);
//						Terrain.CatFishHere catFishHere = Terrain.CatFishHere.Singleton.CreateInstance(Terrain.Singleton, Target, Target.InheritedPriority(), base.Autonomous, cancellableByPlayer: true) as Terrain.CatFishHere;
//						if (catFishHere != null)
//						{
//							catFishHere.Hit = randomObjectFromList;
//							Target.InteractionQueue.TryPushAsContinuation(Target.CurrentInteraction, catFishHere);
//						}
//					}
//				}
//			}

//			public void ShowFailureObjectBalloon(Sim sim)
//			{
//				ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData("balloon_question");
//				balloonData.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
//				balloonData.mPriority = ThoughtBalloonPriority.High;
//				balloonData.mCoolDown = ThoughtBalloonCooldown.None;
//				sim.ThoughtBalloonManager.ShowBalloon(balloonData);
//			}

//			public void ShowThoughtBalloonFishing(Sim sim)
//			{
//				string icon;
//				icon = "balloon_fish";
//				ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData(icon);
//				balloonData.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
//				balloonData.mPriority = ThoughtBalloonPriority.High;
//				balloonData.mCoolDown = ThoughtBalloonCooldown.None;
//				sim.ThoughtBalloonManager.ShowBalloon(balloonData);
//			}

//			public void ShowHuntingTopicBalloon(Sim sim)
//			{
//				ThoughtBalloonManager.BalloonData balloonData = new ThoughtBalloonManager.BalloonData("skillobjecthuntingcat");
//				balloonData.BalloonType = ThoughtBalloonTypes.kSpeechBalloon;
//				balloonData.mPriority = ThoughtBalloonPriority.High;
//				balloonData.mCoolDown = ThoughtBalloonCooldown.None;
//				sim.ThoughtBalloonManager.ShowBalloon(balloonData);
//			}

//		}

//		public class FishData : HuntingObjectData
//		{
//			public MinorPetRarity mRarity;

//			public MinorPetRarity Rarity => mRarity;

//			public void ParseFishRow(XmlDbRow dr)
//			{
//				ParserFunctions.TryParseEnum(dr["Rarity"], out MinorPetRarity value, MinorPetRarity.Common);
//				ParseRow(dr, value);
//			}

//			public void ParseRow(XmlDbRow dr, MinorPetRarity rarity)
//			{
//				ParseRow(dr);
//				mRarity = rarity;
//			}
//		}

//		public enum EWCatFishingModelState : byte
//		{
//			Carried,
//			InInventory,
//			InWorld
//		}

//		public class PresentTo : Interaction<Sim, ICatPrey>
//		{
//			public class Definition : InteractionDefinition<Sim, ICatPrey, PresentTo>
//			{
//				public override string GetInteractionName(Sim actor, ICatPrey target, InteractionObjectPair iop)
//				{
//					return LocalizeString(actor.IsFemale, "PresentTo") + Localization.Ellipsis;
//				}

//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					CatHuntingComponent fishingComponent = target.CatHuntingComponent;
//					if (fishingComponent == null)
//					{
//						return false;
//					}
//					if (!fishingComponent.HasBeenCaught)
//					{
//						return false;
//					}
//					if (!SharedPresentTest(a, target, isAutonomous))
//					{
//						return false;
//					}
//					if (GetValidSims(a, target.InInventory ? a.LotCurrent : target.LotCurrent) == null)
//					{
//						greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(LocalizeString(a.IsFemale, "NoSimsNearby"));
//						return false;
//					}
//					if (fishingComponent.mHasBeenPresented)
//					{
//						greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(LocalizeString(a.IsFemale, "AlreadyPresented"));
//						return false;
//					}
//					if (fishingComponent.mCatcherId != a.SimDescription.SimDescriptionId)
//					{
//						greyedOutTooltipCallback = InteractionInstance.CreateTooltipCallback(LocalizeString(a.IsFemale, "NotCaughtBySim"));
//						return false;
//					}
//					return true;
//				}

//				public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters, out List<TabInfo> listObjs, out List<HeaderInfo> headers, out int NumSelectableRows)
//				{
//					Sim sim = parameters.Actor as Sim;
//					ICatPrey catPrey = parameters.Target as ICatPrey;
//					NumSelectableRows = 1;
//					PopulateSimPicker(ref parameters, out listObjs, out headers, GetValidSims(sim, catPrey.InInventory ? sim.LotCurrent : catPrey.LotCurrent), includeActor: false);
//				}
//			}

//			[TunableComment("The distance the cat should get to the Sim before dropping the object on the ground.")]
//			[Tunable]
//			public static float kDistanceFromSimToPresent = 1f;

//			[TunableComment("The maximum distance from the prey the target needs to be in order to react. Should be greater than kDistanceFromSimToPresent")]
//			[Tunable]
//			public static float kMaxDistanceForSimToReact = 10f;

//			[Tunable]
//			[TunableComment("The chance of autonomy choosing the highest LTR Sim to present to. 0.0 - 1.0")]
//			public static float kChanceOfAutonomyChooseHighestLTR = 0.7f;

//			[Tunable]
//			[TunableComment("The LTR gain for a positive reaction from a cat.")]
//			public static float kLtrGainForPositiveReactionFromCat = 20f;

//			[Tunable]
//			[TunableComment("The LTR loss for a negative reaction from a cat.")]
//			public static float kLtrLossForNegativeReactionFromCat = 10f;

//			[TunableComment("The LTR gain for a positive reaction from a human.")]
//			[Tunable]
//			public static float kLtrGainForPositiveReactionFromHuman = 20f;

//			[TunableComment("The LTR loss for a negative reaction from a human.")]
//			[Tunable]
//			public static float kLtrLossForNegativeReactionFromHuman = 10f;

//			[TunableComment("The radius around the cat to look for valid sims.")]
//			[Tunable]
//			public static float kRadiusForValidSims = 10f;

//			[TunableComment("The base chance for a human sim to pet the cat after being presented something.")]
//			[Tunable]
//			public static float kHumanBaseChanceToPetCat = 0.5f;

//			[Tunable]
//			[TunableComment("The human traits that will get a modifier on the base chance to pet a cat")]
//			public static TraitNames[] kHumanTraits = new TraitNames[3] {
//			TraitNames.Slob,
//			TraitNames.Snob,
//			TraitNames.Neat
//		};

//			[TunableComment("The modifiers that correspond to the human traits in the kHumanTraits tunable.  Must be in the same order.  -1.0 - 1.0 for each trait")]
//			[Tunable]
//			public static float[] kHumanTraitModifiers = new float[3] {
//			0.2f,
//			-0.2f,
//			-0.3f
//		};

//			[Tunable]
//			[TunableComment("The required relationship for a sim to qualify for the high relationship modifier. -100.0 - 100.0")]
//			public static float kHumanRequiredRelationshipToQualifyForModifier = 30f;

//			[TunableComment("The modifier applied to the base chance, if the relationship is higher than the kHumanRequiredRelationshipToQualifyForModifier")]
//			[Tunable]
//			public static float kHumanRelationshipModifier = 0.3f;

//			[TunableComment("The base chance for a cat sim to nuzzle the cat after being presented something.")]
//			[Tunable]
//			public static float kCatBaseChanceToNuzzleCat = 0.5f;

//			[TunableComment("The cat traits that will get a modifier on the base chance to nuzzle a cat")]
//			[Tunable]
//			public static TraitNames[] kCatTraits = new TraitNames[2] {
//			TraitNames.FriendlyPet,
//			TraitNames.AggressivePet
//		};

//			[Tunable]
//			[TunableComment("The modifiers that correspond to the pet traits in the kCatTraits tunable. Must be in the same order. -1.0 - 1.0 for each trait")]
//			public static float[] kCatTraitModifiers = new float[2] {
//			0.2f,
//			-0.2f
//		};

//			[TunableComment("The required relationship for a cat to qualify for the high relationship modifier. -100.0 - 100.0")]
//			[Tunable]
//			public static float kCatRequiredRelationshipToQualifyForModifier = 30f;

//			[TunableComment("The modifier applied to the base chance, if the relationship is higher than the kCatRequiredRelationshipToQualifyForModifier")]
//			[Tunable]
//			public static float kCatRelationshipModifier = 0.3f;

//			public Sim mSimToPresent;

//			public static InteractionDefinition Singleton = new Definition();

//			public static List<Sim> GetValidSims(Sim actor, Lot lot)
//			{
//				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
//				List<Sim> list = null;
//				if (!lot.IsWorldLot)
//				{
//					foreach (Sim allActor in lot.GetAllActors())
//					{
//						if (allActor != actor && Relationship.AreAcquaintedOrBetter(actor, allActor) && allActor.SimDescription.ToddlerOrAbove)
//						{
//							Lazy.Add<List<Sim>, Sim>(ref list, allActor);
//						}
//					}
//				}
//				Sim[] objects = Sims3.Gameplay.Queries.GetObjects<Sim>(actor.Position, kRadiusForValidSims);
//				foreach (Sim sim in objects)
//				{
//					if (Relationship.AreAcquaintedOrBetter(actor, sim) && sim.SimDescription.ToddlerOrAbove && !Lazy.Contains<List<Sim>, Sim>(list, sim))
//					{
//						Lazy.Add<List<Sim>, Sim>(ref list, sim);
//					}
//				}
//				return list;
//			}

//			public bool ShouldSimDoPositiveReaction(Sim simToPresentTo)
//			{
//				float num;
//				TraitNames[] array;
//				float[] array2;
//				float num2;
//				float num3;
//				if (simToPresentTo.IsHuman)
//				{
//					num = kHumanBaseChanceToPetCat;
//					array = kHumanTraits;
//					array2 = kHumanTraitModifiers;
//					num2 = kHumanRequiredRelationshipToQualifyForModifier;
//					num3 = kHumanRelationshipModifier;
//				}
//				else
//				{
//					num = kCatBaseChanceToNuzzleCat;
//					array = kCatTraits;
//					array2 = kCatTraitModifiers;
//					num2 = kCatRequiredRelationshipToQualifyForModifier;
//					num3 = kCatRelationshipModifier;
//				}
//				for (int i = 0; i < array.Length; i++)
//				{
//					if (simToPresentTo.HasTrait(array[i]))
//					{
//						num += array2[i];
//					}
//				}
//				Relationship relationship = Actor.GetRelationship(simToPresentTo, bCreateIfNotPresent: false);
//				if (relationship != null && relationship.CurrentLTRLiking >= num2)
//				{
//					num += num3;
//				}
//				MathUtils.Clamp(num, 0f, 1f);
//				return RandomUtil.RandomChance01(num);
//			}

//			public void DoLtrAdjustment(bool goodReaction)
//			{
//				float num = 0f;
//				bool isHuman = mSimToPresent.IsHuman;
//				num = ((!goodReaction) ? (0f - (isHuman ? kLtrLossForNegativeReactionFromHuman : kLtrLossForNegativeReactionFromCat)) : (isHuman ? kLtrGainForPositiveReactionFromHuman : kLtrGainForPositiveReactionFromCat));
//				Relationship relationship = Relationship.Get(Actor, mSimToPresent, createIfNone: true);
//				LongTermRelationshipTypes currentLTR = relationship.CurrentLTR;
//				float currentLTRLiking = relationship.CurrentLTRLiking;
//				relationship.LTR.UpdateLiking(num);
//				LongTermRelationshipTypes currentLTR2 = relationship.CurrentLTR;
//				float currentLTRLiking2 = relationship.CurrentLTRLiking;
//				bool isPositive = currentLTRLiking2 >= currentLTRLiking;
//				SocialComponent.SetSocialFeedbackForActorAndTarget(CommodityTypes.Friendly, Actor, mSimToPresent, isPositive, 0, currentLTR, currentLTR2);
//			}

//			public override bool Run()
//			{
//				if (mSimToPresent == null)
//				{
//					mSimToPresent = (GetSelectedObject() as Sim);
//					if (mSimToPresent == null)
//					{
//						return false;
//					}
//					if (!PetCarrySystem.PickUp(Actor, Target))
//					{
//						return false;
//					}
//					Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
//				}
//				if (mSimToPresent != null && !mSimToPresent.HasBeenDestroyed)
//				{
//					Route val = Actor.CreateRoute();
//					val.DoRouteFail = true;
//					val.SetOption((RouteOption)512, true);
//					val.PlanToPointRadialRange((IHasScriptProxy)(object)mSimToPresent, mSimToPresent.Position, kDistanceFromSimToPresent, kDistanceFromSimToPresent, Vector3.UnitZ, 360f, (RouteDistancePreference)1, (RouteOrientationPreference)1, mSimToPresent.LotCurrent.LotId, new int[1] {
//					mSimToPresent.RoomId
//				});
//					if (Actor.DoRoute(val))
//					{
//						val.SetOption((RouteOption)512, false);
//						val.PlanToPointRadialRange((IHasScriptProxy)(object)mSimToPresent, mSimToPresent.Position, kDistanceFromSimToPresent, kDistanceFromSimToPresent, Vector3.UnitZ, 360f, (RouteDistancePreference)1, (RouteOrientationPreference)1, mSimToPresent.LotCurrent.LotId, new int[1] {
//						mSimToPresent.RoomId
//					});
//						Actor.DoRoute(val);
//					}
//				}
//				PetCarrySystem.PutDownOnFloor(Actor);
//				Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InWorld);
//				if (Actor.HasExitReason())
//				{
//					return false;
//				}
//				BeginCommodityUpdates();
//				Target.CatHuntingComponent.mHasBeenPresented = true;
//				if (Actor.GetDistanceToObjectSquared(mSimToPresent) > kMaxDistanceForSimToReact * kMaxDistanceForSimToReact)
//				{
//					EndCommodityUpdates(succeeded: true);
//					return true;
//				}
//				EventTracker.SendEvent(EventTypeId.kCatPresentGift, Actor, mSimToPresent);
//				if (ShouldSimDoPositiveReaction(mSimToPresent))
//				{
//					bool flag = false;
//					if (mSimToPresent.IsHuman)
//					{
//						SocialInteractionA.Definition definition = new SocialInteractionA.Definition("Pet On Floor Auto Accept", new string[0], null, initialGreet: false);
//						InteractionInstance instance = definition.CreateInstance(Actor, mSimToPresent, new InteractionPriority(InteractionPriorityLevel.UserDirected), base.Autonomous, base.CancellableByPlayer);
//						flag = mSimToPresent.InteractionQueue.AddNextIfPossible(instance);
//					}
//					else
//					{
//						SocialInteractionA.Definition definition2 = new SocialInteractionA.Definition("Nuzzle Auto Accept", new string[0], null, initialGreet: false);
//						InteractionInstance instance2 = definition2.CreateInstance(mSimToPresent, Actor, new InteractionPriority(InteractionPriorityLevel.UserDirected), base.Autonomous, base.CancellableByPlayer);
//						flag = Actor.InteractionQueue.AddNextIfPossible(instance2);
//					}
//					if (flag)
//					{
//						DoLtrAdjustment(goodReaction: true);
//					}
//				}
//				else
//				{
//					InteractionInstance instance3 = Sim.ReactToPresentedObject.Singleton.CreateInstance(Target, mSimToPresent, new InteractionPriority(InteractionPriorityLevel.UserDirected), base.Autonomous, base.CancellableByPlayer);
//					if (mSimToPresent.InteractionQueue.AddNextIfPossible(instance3))
//					{
//						DoLtrAdjustment(goodReaction: false);
//					}
//				}
//				EndCommodityUpdates(succeeded: true);
//				return true;
//			}

//			public override bool RunFromInventory()
//			{
//				if (base.Autonomous && mSimToPresent == null)
//				{
//					float num = float.MinValue;
//					List<Sim> list = null;
//					foreach (Sim allActor in Actor.LotHome.GetAllActors())
//					{
//						if (allActor.IsHuman || allActor.IsCat)
//						{
//							Lazy.Allocate<List<Sim>>(ref list);
//							list.Add(allActor);
//							Relationship relationship = Actor.GetRelationship(allActor, bCreateIfNotPresent: false);
//							if (relationship != null)
//							{
//								float currentLTRLiking = relationship.CurrentLTRLiking;
//								if (mSimToPresent == null || relationship.CurrentLTRLiking > num)
//								{
//									mSimToPresent = allActor;
//									num = currentLTRLiking;
//								}
//							}
//						}
//					}
//					if (mSimToPresent == null)
//					{
//						return false;
//					}
//					if (list.Count > 1)
//					{
//						list.Remove(mSimToPresent);
//						if (!RandomUtil.RandomChance01(kChanceOfAutonomyChooseHighestLTR))
//						{
//							mSimToPresent = RandomUtil.GetRandomObjectFromList(list);
//						}
//					}
//				}
//				else if (mSimToPresent == null)
//				{
//					mSimToPresent = (GetSelectedObject() as Sim);
//					if (mSimToPresent == null)
//					{
//						return false;
//					}
//				}
//				Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
//				if (!PetCarrySystem.PickUpFromSimInventory(Actor, Target, removeFromInventory: true))
//				{
//					Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//					return false;
//				}
//				return Run();
//			}

//			public override void Cleanup()
//			{
//				if (Target.InInventory)
//				{
//					Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//				}
//				else
//				{
//					Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InWorld);
//				}
//				base.Cleanup();
//			}
//		}

//		public class DropSomewhere : Interaction<Sim, ICatPrey>
//		{
//			public class Definition : InteractionDefinition<Sim, ICatPrey, DropSomewhere>
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					if (!target.InInventory && target.Parent != a)
//					{
//						return false;
//					}
//					if (a.LotCurrent.IsWorldLot)
//					{
//						return false;
//					}
//					if (isAutonomous && a.LotCurrent != a.LotHome)
//					{
//						return false;
//					}
//					return SharedPresentTest(a, target, isAutonomous);
//				}
//			}

//			[TunableComment("The distance the cat should get to the object before dropping the prey on the ground.")]
//			[Tunable]
//			public static float kDistanceFromObjectToDrop = 1f;

//			[Tunable]
//			[TunableComment("The chance of a cat picking a bed to drop prey off. 0.0 - 1.0")]
//			public static float kChanceToDropAtBed = 0.4f;

//			[TunableComment("The chance of a cat picking a plant to drop prey off. 0.0 - 1.0")]
//			[Tunable]
//			public static float kChanceToDropAtPlant = 0.4f;

//			public static InteractionDefinition Singleton = new Definition();

//			public override bool RunFromInventory()
//			{
//				Vector3 point = Actor.Position;
//				float radius = 0f;
//				Lot lotCurrent = Actor.LotCurrent;
//				bool flag = false;
//				if (lotCurrent == Actor.LotHome && Actor.Bed != null && RandomUtil.RandomChance01(kChanceToDropAtBed))
//				{
//					point = Actor.Bed.Position;
//					radius = kDistanceFromObjectToDrop;
//					flag = true;
//				}
//				if (!flag && RandomUtil.RandomChance01(kChanceToDropAtPlant))
//				{
//					Plant[] objects = lotCurrent.GetObjects<Plant>();
//					if (objects.Length > 0)
//					{
//						Plant randomObjectFromList = RandomUtil.GetRandomObjectFromList(objects);
//						point = randomObjectFromList.Position;
//						radius = kDistanceFromObjectToDrop;
//						flag = true;
//					}
//				}
//				if (!flag)
//				{
//					int num = 10;
//					do
//					{
//						point = lotCurrent.GetRandomPosition(insideValid: true, outsideValid: false);
//						if (World.IsSimRoutable(point.x, point.z))
//						{
//							flag = true;
//							break;
//						}
//					} while (--num != 0);
//				}
//				if (!flag)
//				{
//					int num2 = 10;
//					do
//					{
//						point = lotCurrent.GetRandomPosition(insideValid: false, outsideValid: true);
//						if (World.IsSimRoutable(point.x, point.z))
//						{
//							flag = true;
//							break;
//						}
//					} while (--num2 != 0);
//				}
//				if (!flag)
//				{
//					return false;
//				}
//				if (Target.Parent != Actor)
//				{
//					Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
//					PetCarrySystem.PickUpFromSimInventory(Actor, Target, removeFromInventory: true);
//				}
//				if (!Actor.RouteToPointRadius(point, radius) && !Actor.HasExitReason(ExitReason.UserCanceled) && (Actor.LotCurrent != Actor.LotHome || Actor.IsOutside))
//				{
//					PetCarrySystem.AnimateIntoSimInventory(Actor);
//					if (Actor.Inventory.TryToAdd(Target))
//					{
//						Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//					}
//					return false;
//				}
//				PetCarrySystem.PutDownOnFloor(Actor);
//				Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InWorld);
//				return true;
//			}
//		}

//		public class CatchPrey : Interaction<Sim, ICatPrey>
//		{
//			public enum FailureTypes
//			{
//				Scared,
//				PreyGone,
//				FailureObject
//			}

//			public class Definition : InteractionDefinition<Sim, ICatPrey, CatchPrey>
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					if (target.CatHuntingComponent == null || target.CatHuntingComponent.mPreyData == null)
//					{
//						return false;
//					}
//					if (target.CatHuntingComponent.HasBeenCaught)
//					{
//						return false;
//					}
//					return PetManager.PetSkillFatigueTest(a, PetManager.PetSkillFatigeGreyedOutTooltipType.CatHunting, ref greyedOutTooltipCallback);
//				}
//			}

//			public class FailurePreyDefinition : Definition
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					return true;
//				}
//			}

//			[Tunable]
//			[TunableComment("Distance cat should route to prey before cat hunkers down and starts stalking prey.")]
//			public static float kInitialRouteDistance = 8f;

//			[Tunable]
//			[TunableComment("Num sim minutes fight loop lasts.")]
//			public static float kFightLength = 5f;

//			[TunableComment("If cats hunger is below this value and they successfully cat the prey they will eat it.")]
//			[Tunable]
//			public static int kHungerThreshold = -50;

//			[Tunable]
//			[TunableComment("Range 0-100, Min/Max chance of cat catching prey. The actual chance is linearly interpolated between these 2 values based on sim's skill level and coresponding skill level tuning of the prey")]
//			public static int[] kMinMaxChanceCatchPrey = new int[2] {
//			20,
//			80
//		};

//			[Tunable]
//			[TunableComment("Range 0-100, [Rare,Extra Rare] additional chances of catching rare and extra rare beetles if pet has Skill Lifetime opp Creeping and Crawling")]
//			public static int[] kIncreasedCatchChanceCreepyCrawling = new int[2] {
//			10,
//			20
//		};

//			[TunableComment("Base Weighting for failure outcomes [Scared, PreyGone, FailureObject], any of these weights can be overriden on a trait basis by the coresponding tunables.")]
//			[Tunable]
//			public static float[] kFailureOutcomeWeights = new float[4] {
//			1f,
//			1f,
//			1f,
//			1f
//		};

//			[Tunable]
//			[TunableComment("Trait overrides for Scared outcome.")]
//			public static TraitNames[] kScaredOutcomeOverrideTraits = new TraitNames[2] {
//			TraitNames.AdventurousPet,
//			TraitNames.SkittishPet
//		};

//			[Tunable]
//			[TunableComment("Weighting override for Scared outcome for coresponding tuned traits.")]
//			public static float[] kScaredOutcomeOverrideWeights = new float[2] {
//			0f,
//			3f
//		};

//			[TunableComment("Trait overrides for PreyGone outcome.")]
//			[Tunable]
//			public static TraitNames[] kPreyGoneOutcomeOverrideTraits = new TraitNames[1] {
//			TraitNames.CluelessPet
//		};

//			[Tunable]
//			[TunableComment("Weighting override for PreyGone outcome for coresponding tuned traits.")]
//			public static float[] kPreyGoneOutcomeOverrideWeights = new float[1] {
//			5f
//		};

//			[Tunable]
//			[TunableComment("Trait overrides for catch Failure Object outcome.")]
//			public static TraitNames[] kFailureObjectOutcomeOverrideTraits = new TraitNames[1] {
//			TraitNames.AggressivePet
//		};

//			[Tunable]
//			[TunableComment("Weighting override for catch Failure Object outcome for coresponding tuned traits.")]
//			public static float[] kFailureObjectOutcomeOverrideWeights = new float[1] {
//			5f
//		};

//			[TunableComment("Hunger Gain from Eating prey")]
//			[Tunable]
//			public static int kEatPreyHungerGain = 25;

//			public CatHuntingJig mJig;

//			public EWCatFishingSkill mFishingSkill;

//			public bool DestroyPrey;

//			public bool ForceCatchPrey;

//			public bool FromEatPreyInteraction;

//			public ICatPrey mTempPreyObj;

//			public ulong SimToPresentToID;

//			public static InteractionDefinition Singleton = new Definition();

//			public static InteractionDefinition FailureObjectSingleton = new FailurePreyDefinition();

//			public override bool Run()
//			{
//				//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
//				//IL_00cf: Expected O, but got Unknown
//				if (!FromEatPreyInteraction && !Actor.RouteToObjectRadius(Target, kInitialRouteDistance))
//				{
//					return false;
//				}
//				if (Target.InUse)
//				{
//					return false;
//				}
//				StandardEntry();
//				mFishingSkill = (Actor.SkillManager.AddElement(EWCatFishingSkill.SkillNameID) as EWCatFishingSkill);
//				EnterStateMachine("CatHunt", "Enter", "x");
//				SetActor("prey", (IHasScriptProxy)Target);
//				AddOneShotScriptEventHandler(200u, (SacsEventHandler)(object)new SacsEventHandler(OnAnimationEvent));
//				AnimateSim("Loop");
//				Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
//				if (Actor.HasTrait(TraitNames.IndependentPet))
//				{
//					BeginCommodityUpdate(CommodityKind.Fun, TraitTuning.IndependentPetTraitFunMultiplier);
//				}
//				BeginCommodityUpdates();
//				if (SimToPresentToID != 0)
//				{
//					SimDescription simDescription = SimDescription.Find(SimToPresentToID);
//					if (simDescription.TraitManager.HasElement(TraitNames.CatPerson))
//					{
//						ModifyCommodityUpdate(CommodityKind.SkillCatHunting, TraitTuning.kPreferredPetSkillGainBoost);
//					}
//				}
//				DoTimedLoop(kFightLength, ExitReason.Default);
//				EndCommodityUpdates(succeeded: true);
//				if (Actor.Posture is PouncePosture)
//				{
//					Actor.PopPosture();
//				}
//				EventTracker.SendEvent(EventTypeId.kGoHuntingCat, Actor);
//				if (base.InteractionDefinition is FailurePreyDefinition)
//				{
//					DoFailureObjectOutcome();
//				}
//				else if (FromEatPreyInteraction || ForceCatchPrey || RollDiceForCatchingPrey())
//				{
//					mFishingSkill.RegisterCaughtPrey(Target);
//					if (FromEatPreyInteraction || (SimToPresentToID == 0 && Actor.Motives.GetValue(CommodityKind.Hunger) < (float)kHungerThreshold))
//					{
//						DoEatPreyOutcome();
//					}
//					else
//					{
//						DoExitWithPreyOutcome();
//					}
//				}
//				else
//				{
//					RunFailureBehavior();
//				}
//				StandardExit(Target.IsActorUsingMe(Actor));
//				return true;
//			}

//			public override void Cleanup()
//			{
//				if (mJig != null)
//				{
//					mJig.Destroy();
//					mJig = null;
//				}
//				if (mTempPreyObj != null)
//				{
//					mTempPreyObj.Destroy();
//					mTempPreyObj = null;
//				}
//				if (DestroyPrey)
//				{
//					Target.Destroy();
//				}
//				base.Cleanup();
//			}

//			public bool TryPlaceJigOnPrey()
//			{
//				mJig = (CatHuntingJig)GlobalFunctions.CreateObjectOutOfWorld("catHuntJig", (ProductVersion)512);
//				if (mJig != null)
//				{
//					mJig.mPrey = Target;
//					mJig.mSim = Actor;
//					Vector3 position = Target.Position;
//					Vector3 forward = Target.ForwardVector;
//					FindGoodLocationBooleans fglBools = (FindGoodLocationBooleans)16;
//					if (GlobalFunctions.FindGoodLocationNearby(mJig, ref position, ref forward, 0f, GlobalFunctions.FindGoodLocationStrategies.SamePosition, fglBools))
//					{
//						mJig.SetPosition(position);
//						mJig.SetForward(forward);
//						mJig.SetOpacity(0f, 0f);
//						mJig.AddToWorld();
//						return true;
//					}
//				}
//				return false;
//			}

//			public bool RollDiceForCatchingPrey()
//			{
//                CatHuntingComponent.PreyData mPreyData = Target.CatHuntingComponent.mPreyData;
//				float[] traitAdjustedChance = GetTraitAdjustedChance(Actor, kMinMaxChanceCatchPrey);
//				float num = MathHelpers.LinearInterpolate(mPreyData.MinSkillLevel, mPreyData.MaxSkillLevel, traitAdjustedChance[0], traitAdjustedChance[1], mFishingSkill.SkillLevel);
//				return RandomUtil.RandomChance(num);
//			}

//			public void DoEatPreyOutcome()
//			{
//				Target.SetOpacity(1f, 0f);
//				AnimateSim("Exit Eat");
//				Actor.ShowTNSIfSelectable(LocalizeString(Actor.IsFemale, "CatchPreyEat", Actor, Target.GetLocalizedName()), (NotificationStyle)3, ObjectGuid.InvalidObjectGuid, ObjectGuid.InvalidObjectGuid);
//				DestroyPrey = true;
//				Actor.Motives.SetValue(CommodityKind.Hunger, Actor.Motives.GetValue(CommodityKind.Hunger) + (float)kEatPreyHungerGain);
//				CatHuntingComponent.PreyData mPreyData = Target.CatHuntingComponent.mPreyData;
//				int kBuffThreshold;
//				float kBuffDuration;
//				switch (mPreyData.Rarity)
//				{
//					case MinorPetRarity.Rare:
//						kBuffThreshold = Cooking.GreatMealBuffTuning.kBuffThreshold;
//						kBuffDuration = Cooking.GreatMealBuffTuning.kBuffDuration;
//						break;
//					case MinorPetRarity.ExtraordinarilyUnusual:
//						kBuffThreshold = Cooking.AmazingMealBuffTuning.kBuffThreshold;
//						kBuffDuration = Cooking.AmazingMealBuffTuning.kBuffDuration;
//						break;
//					default:
//						kBuffThreshold = Cooking.GoodMealBuffTuning.kBuffThreshold;
//						kBuffDuration = Cooking.GoodMealBuffTuning.kBuffDuration;
//						break;
//				}
//				BuffManager buffManager = Actor.BuffManager;
//				if (FromEatPreyInteraction)
//				{
//					buffManager.AddElement(BuffNames.Tasty, Origin.FromEatingFish);
//				}
//				else
//				{
//					buffManager.AddElement(BuffNames.Meal, kBuffThreshold, kBuffDuration, Origin.FromEatingPrey);
//				}
//				Sim.CatGroomSelf.Definition definition = new Sim.CatGroomSelf.Definition(forceShortGroom: true);
//				InteractionInstance continuation = definition.CreateInstance(Actor, Actor, new InteractionPriority(InteractionPriorityLevel.High), isAutonomous: true, cancellableByPlayer: true);
//				Actor.InteractionQueue.TryPushAsContinuation(this, continuation);
//			}

//			public void DoExitWithPreyOutcome()
//			{
//				ICatPrey catPrey = Target;
//				DestroyPrey = false;
//				if (catPrey != null)
//				{
//					catPrey.SetOpacity(1f, 0f);
//					AnimateSim("Exit With Prey");
//					if (catPrey.CatHuntingComponent != null)
//					{
//						catPrey.CatHuntingComponent.SetCatcher(Actor);
//					}
//					if (Actor.Inventory.TryToAdd(catPrey))
//					{
//						catPrey.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//						Actor.ShowTNSIfSelectable(LocalizeString(Actor.IsFemale, "CatchPreySuccess", Actor, Target.GetLocalizedName()), (NotificationStyle)3, ObjectGuid.InvalidObjectGuid, ObjectGuid.InvalidObjectGuid);
//						foreach (Sim sim in Actor.Household.Sims)
//						{
//							EventTracker.SendEvent(new Event(EventTypeId.kArkBuilder, sim, Target));
//						}
//						TryPushPresentTo(catPrey);
//					}
//					else
//					{
//						DestroyPrey = true;
//					}
//				}
//				else
//				{
//					DoPreyGoneOutcome();
//				}
//			}

//			public void DoScaredOutcome()
//			{
//				AnimateSim("Exit Fail Jump");
//				DestroyPrey = true;
//				Actor.ShowTNSIfSelectable(LocalizeString(Actor.IsFemale, "CatchPreyScared", Actor, Target.GetLocalizedName()), (NotificationStyle)4, ObjectGuid.InvalidObjectGuid, ObjectGuid.InvalidObjectGuid);
//			}

//			public void DoPreyGoneOutcome()
//			{
//				AddOneShotScriptEventHandler(101u, (SacsEventHandler)(object)new SacsEventHandler(OnAnimationEvent));
//				AnimateSim("Exit Fail No Prey");
//				DestroyPrey = true;
//				Actor.ShowTNSIfSelectable(LocalizeString(Actor.IsFemale, "CatchPreyGone", Actor, Target.GetLocalizedName()), (NotificationStyle)4, ObjectGuid.InvalidObjectGuid, ObjectGuid.InvalidObjectGuid);
//			}

//			public void DoFailureObjectOutcome()
//			{
//				string randomStringFromList = RandomUtil.GetRandomStringFromList(kFailureObjects);
//				ICatPrey catPrey = GlobalFunctions.CreateObject(randomStringFromList, (ProductVersion)512, Vector3.OutOfWorld, 0, Vector3.UnitZ, null, null) as ICatPrey;
//				if (catPrey != null)
//				{
//					if (catPrey.CatHuntingComponent != null)
//					{
//						catPrey.CatHuntingComponent.SetCatcher(Actor);
//					}
//					catPrey.SetPosition(mJig.Position);
//					catPrey.SetForward(mJig.ForwardVector);
//					SetActor("prey", (IHasScriptProxy)catPrey);
//					AnimateSim("Exit With Prey");
//					if (Actor.Inventory.TryToAdd(catPrey))
//					{
//						catPrey.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//						Actor.ShowTNSIfSelectable(LocalizeString(Actor.IsFemale, "CatchPreyFailure", Actor, catPrey.GetLocalizedName()), (NotificationStyle)4, ObjectGuid.InvalidObjectGuid, ObjectGuid.InvalidObjectGuid);
//					}
//					else
//					{
//						catPrey.Destroy();
//					}
//					DestroyPrey = true;
//				}
//				else
//				{
//					DoPreyGoneOutcome();
//				}
//			}

//			public void TryPushPresentTo(ICatPrey prey)
//			{
//				if (SimToPresentToID == 0)
//				{
//					return;
//				}
//				SimDescription simDescription = SimDescription.Find(SimToPresentToID);
//				if (simDescription == null)
//				{
//					return;
//				}
//				Sim createdSim = simDescription.CreatedSim;
//				if (createdSim != null)
//				{
//					PresentTo presentTo = PresentTo.Singleton.CreateInstance(prey, Actor, Actor.InheritedPriority(), base.Autonomous, cancellableByPlayer: true) as PresentTo;
//					if (presentTo != null)
//					{
//						presentTo.mSimToPresent = createdSim;
//						Actor.InteractionQueue.TryPushAsContinuation(this, presentTo);
//					}
//				}
//			}

//			public void RunFailureBehavior()
//			{
//				switch (RollDiceForFailureType())
//				{
//					case FailureTypes.Scared:
//						DoScaredOutcome();
//						break;
//					case FailureTypes.PreyGone:
//						DoPreyGoneOutcome();
//						break;
//					case FailureTypes.FailureObject:
//						DoFailureObjectOutcome();
//						break;
//				}
//			}

//			public FailureTypes RollDiceForFailureType()
//			{
//				float[] array = new float[3];
//				for (int i = 0; i < 3; i++)
//				{
//					array[i] = kFailureOutcomeWeights[i];
//				}
//				for (int j = 0; j < kScaredOutcomeOverrideTraits.Length; j++)
//				{
//					if (Actor.HasTrait(kScaredOutcomeOverrideTraits[j]))
//					{
//						array[0] = kScaredOutcomeOverrideWeights[j];
//					}
//				}
//				for (int k = 0; k < kPreyGoneOutcomeOverrideTraits.Length; k++)
//				{
//					if (Actor.HasTrait(kPreyGoneOutcomeOverrideTraits[k]))
//					{
//						array[1] = kPreyGoneOutcomeOverrideWeights[k];
//					}
//				}
//				for (int l = 0; l < kFailureObjectOutcomeOverrideTraits.Length; l++)
//				{
//					if (Actor.HasTrait(kFailureObjectOutcomeOverrideTraits[l]))
//					{
//						array[2] = kFailureObjectOutcomeOverrideWeights[l];
//					}
//				}
//				return (FailureTypes)RandomUtil.GetWeightedIndex(array);
//			}

//			public void OnAnimationEvent(StateMachineClient sender, IEvent evt)
//			{
//				if (evt.EventId == 101)   // TODO: Check. This is a change from the original I don't understand
//				{
//					Vector3 position = mJig.Position;
//					Actor.SetPosition(position);
//					Actor.SetForward(mJig.ForwardVector);
//				}
//				else if (evt.EventId == 200)
//				{
//					Target.SetOpacity(0f, 0.1f);
//				}
//			}
//		}

//		public class PetEatPrey : Interaction<Sim, ICatPrey>
//		{
//			public class Definition : InteractionDefinition<Sim, ICatPrey, PetEatPrey>
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					if (target.CatHuntingComponent == null)
//					{
//						return false;
//					}
//					if (!target.IsUnconscious)
//					{
//						return false;
//					}
//					if (target.Parent != null)
//					{
//						return false;
//					}
//					return true;
//				}
//			}

//			[TunableComment("The amount of time it should take a cat to finish a Prey, in minutes. 0.0+")]
//			[Tunable]
//			public static float kSimMinutesForCatToEatPrey = 10f;

//			[TunableComment("The distance for a cat to get to a Prey for eating. 0.0+")]
//			[Tunable]
//			public static float kCatEatingDistance = 1f;

//			[Tunable]
//			[TunableComment("The distance for a kitten to get to a Prey for eating. 0.0+")]
//			public static float kKittenEatingDistance = 1f;

//			[Tunable]
//			[TunableComment("The distance between the Prey and the cat for the cat to run the hunting behavior. 0.0+")]
//			public static float kDistanceForEWCatFishingBehavior = 16f;

//			[Tunable]
//			[TunableComment("The distance away from the Prey the cat will run before starting the hunting behavior. Should be less than kDistanceForEWCatFishingBehavior. 0.0+")]
//			public static float kDistanceFromPreyForCatToHunting = 8f;

//			[TunableComment("The amount of time it should take a dog to finish a Prey, in minutes. 0.0+")]
//			[Tunable]
//			public static float kSimMinutesForDogToEatPrey = 5f;

//			[TunableComment("The distance for a dog to get to a Prey for eating. 0.0+")]
//			[Tunable]
//			public static float kDogEatingDistance = 1f;

//			[Tunable]
//			[TunableComment("The distance for a puppy to get to a Prey for eating. 0.0+")]
//			public static float kPuppyEatingDistance = 1f;

//			[Tunable]
//			[TunableComment("The distance for a little dog to get to a Prey for eating. 0.0+")]
//			public static float kLittleDogEatingDistance = 0.2f;

//			[TunableComment("The distance for a little puppy to get to a Prey for eating. 0.0+")]
//			[Tunable]
//			public static float kLittlePuppyEatingDistance = 0.2f;

//			[TunableComment("The distance between the Prey and the dog for the dog to run the sniffing behavior. 0.0+")]
//			[Tunable]
//			public static float kDistanceForDogSniffingBehavior = 15f;

//			[Tunable]
//			[TunableComment("The distance from the Prey the dog will run if it's going to sniff the air. Should be less than kDistanceForDogSniffingBehavior. 0.0+")]
//			public static float kDistanceFromPreyForDogToSniffAir = 7f;

//			public bool mDestroyPrey;

//			public static InteractionDefinition Singleton = new Definition();

//			public override bool Run()
//			{
//				bool flag = false;
//				float distanceToObjectSquared = Actor.GetDistanceToObjectSquared(Target);
//				StandardEntry();
//				Target.DisableInteractions();
//				CASAgeGenderFlags species = Actor.SimDescription.Species;
//				if ((int)species <= 1024)
//				{
//					if ((int)species != 768)
//					{
//						if ((int)species == 1024)
//						{
//							goto IL_006c;
//						}
//						goto IL_0089;
//					}
//					flag = CatBehavior(distanceToObjectSquared);
//				}
//				else
//				{
//					if ((int)species == 1280)
//					{
//						goto IL_006c;
//					}
//					if ((int)species != 1792)
//					{
//						goto IL_0089;
//					}
//					flag = SharedNearDistanceBehavior(kCatEatingDistance, kSimMinutesForCatToEatPrey);
//				}
//				goto IL_008b;
//			IL_006c:
//				flag = DogBehavior(distanceToObjectSquared);
//				goto IL_008b;
//			IL_0089:
//				flag = false;
//				goto IL_008b;
//			IL_008b:
//				StandardExit();
//				return flag;
//			}

//			public override void Cleanup()
//			{
//				if (mDestroyPrey)
//				{
//					DestroyObject(Target);
//				}
//				base.Cleanup();
//			}

//			public bool SharedFarDistanceBehavior(float routingDistance)
//			{
//				RequestWalkStyle(Sim.WalkStyle.PetRun);
//				bool result = Actor.RouteToObjectRadius(Target, routingDistance);
//				UnrequestWalkStyle(Sim.WalkStyle.PetRun);
//				return result;
//			}

//			public bool SharedNearDistanceBehavior(float routingDistance, float loopTime)
//			{
//				if (!Actor.RouteToObjectRadius(Target, routingDistance))
//				{
//					return false;
//				}
//				EnterStateMachine("eatofffloor", "Enter", "x");
//				SetParameter("isFish", true);
//				BeginCommodityUpdates();
//				AnimateSim("EatOffFloorLoop");
//				bool flag = DoTimedLoop(loopTime, ExitReason.Default);
//				EndCommodityUpdates(flag);
//				mDestroyPrey = true;
//				AnimateSim("Exit");
//				if (Actor.IsCat)
//				{
//					Actor.BuffManager.AddElement(BuffNames.Tasty, Origin.FromEatingFish);
//				}
//				EventTracker.SendEvent(EventTypeId.kAteFish, Actor, Target);
//				return flag;
//			}

//			public bool DogBehavior(float distanceSquared)
//			{
//				bool flag = (int)Actor.SimDescription.Species == 1280;
//				if (distanceSquared > kDistanceForDogSniffingBehavior * kDistanceForDogSniffingBehavior)
//				{
//					if (!SharedFarDistanceBehavior(kDistanceFromPreyForDogToSniffAir))
//					{
//						return false;
//					}
//					Actor.PlayReaction(ReactionTypes.Sniff, ReactionSpeed.ImmediateWithoutOverlay);
//				}
//				float num = 0f;
//				num = ((!flag) ? (Actor.IsPuppy ? kPuppyEatingDistance : kDogEatingDistance) : (Actor.IsPuppy ? kLittlePuppyEatingDistance : kLittleDogEatingDistance));
//				return SharedNearDistanceBehavior(num, kSimMinutesForDogToEatPrey);
//			}

//			public bool CatBehavior(float distanceSquared)
//			{
//				if (distanceSquared > kDistanceForEWCatFishingBehavior * kDistanceForEWCatFishingBehavior)
//				{
//					if (!SharedFarDistanceBehavior(kDistanceFromPreyForCatToHunting))
//					{
//						return false;
//					}
//					CatchPrey catchPrey = CatchPrey.Singleton.CreateInstance(Target, Actor, GetPriority(), base.Autonomous, base.CancellableByPlayer) as CatchPrey;
//					catchPrey.FromEatPreyInteraction = true;
//					if (Actor.InteractionQueue.TryPushAsContinuation(this, catchPrey))
//					{
//						return true;
//					}
//				}
//				return SharedNearDistanceBehavior(Actor.IsKitten ? kKittenEatingDistance : kCatEatingDistance, kSimMinutesForCatToEatPrey);
//			}
//		}

//		public class PetPutInInventory : Interaction<Sim, ICatPrey>
//		{
//			public class Definition : InteractionDefinition<Sim, ICatPrey, PetPutInInventory>
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					if (target.CatHuntingComponent == null)
//					{
//						return false;
//					}
//					if (a.IsPet && target.Parent != null)
//					{
//						return false;
//					}
//					return target.IsUnconscious;
//				}

//				public override string GetInteractionName(Sim actor, ICatPrey target, InteractionObjectPair iop)
//				{
//					if (actor.IsHuman)
//					{
//						return Localization.LocalizeString(actor.IsFemale, "Gameplay/Abstracts/ScriptObject/PutInInventory:InteractionName");
//					}
//					return base.GetInteractionName(actor, target, iop);
//				}
//			}

//			public static InteractionDefinition Singleton = new Definition();

//			public override bool Run()
//			{
//				if (Actor.IsPet)
//				{
//					if (!PetCarrySystem.PickUp(Actor, Target, 0.2f, (SacsEventHandler)(object)new SacsEventHandler(OnPickup), 101u, doNotRoute: false, ignoreInUse: false, null))
//					{
//						return false;
//					}
//					if (CarryUtils.PutInSimInventory(Actor))
//					{
//						Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.InInventory);
//						return true;
//					}
//					Target.Destroy();
//					return false;
//				}
//				if (!Actor.RouteToObjectRadius(Target, 0.5f))
//				{
//					return false;
//				}
//				Actor.RouteTurnToFace(Target.Position);
//				if (Target.InUse)
//				{
//					return false;
//				}
//				Actor.PlaySoloAnimation("a2o_object_genericSwipe_x", yield: true);
//				Target.FadeOut(yield: true);
//				if (!Actor.Inventory.TryToAdd(Target))
//				{
//					Target.Destroy();
//					return false;
//				}
//				return true;
//			}

//			public void OnPickup(StateMachineClient sender, IEvent evt)
//			{
//				Target.UpdateVisualState(CatHuntingComponent.CatHuntingModelState.Carried);
//			}
//		}

//		public class DEBUG_SetAsNotPresented : ImmediateInteraction<Sim, ICatPrey>
//		{
//			[DoesntRequireTuning]
//			public class Definition : ActorlessInteractionDefinition<Sim, ICatPrey, DEBUG_SetAsNotPresented>
//			{
//				public override bool Test(Sim a, ICatPrey target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
//				{
//					return target.CatHuntingComponent?.mHasBeenPresented ?? false;
//				}

//				public override string GetInteractionName(Sim actor, ICatPrey target, InteractionObjectPair iop)
//				{
//					return LocalizeString(actor.IsFemale, "SetAsNotPresented");
//				}
//			}

//			public static InteractionDefinition Singleton = new Definition();

//			public override bool Run()
//			{
//				Target.CatHuntingComponent.mHasBeenPresented = false;
//				return true;
//			}
//		}

//		public const string sLocalizationKey = "Gameplay/ObjectComponents/EWCatFishingComponent:";

//		[TunableComment("Min/Max Distance at which the Sim will view the hunted prey. Positive float.")]
//		[Tunable]
//		public static float[] kMinMaxDistanceToView = new float[2] {
//		2f,
//		3f
//	};

//		[TunableComment("Number of sim minutes prey maptags stay visible after cat does Locate Prey Interaction.")]
//		[Tunable]
//		public static int kMinsUntilMaptagsFade = 60;

//		public ulong mCatcherId;

//		public string mCatcherName = string.Empty;

//		public bool mHasBeenPresented;

//		public FishData mPreyData;

//		public bool IsUnconscious;

//		public static string[] kFailureObjects = new string[4] {
//		"catHuntColorfulFeather",
//		"catHuntSnakeSkinPiece",
//		"catHuntMapleLeaf",
//		"catHuntWorthlessChipBag"
//	};

//		public bool HasBeenCaught;

//		public override Type BaseType => typeof(EWCatFishingComponent);

//		public float MinDistanceFromObjectToView => kMinMaxDistanceToView[0];

//		public float MaxDistanceFromObjectToView => kMinMaxDistanceToView[1];

//		public static string LocalizeString(bool isFemale, string name, params object[] parameters)
//		{
//			return Localization.LocalizeString(isFemale, "Gameplay/ObjectComponents/EWCatFishingComponent:" + name, parameters);
//		}

//		public EWCatFishingComponent()
//		{
//		}

//		public EWCatFishingComponent(GameObject o)
//			: base(o)
//		{
//			mHasBeenPresented = false;
//		}

//		public EWCatFishingComponent(GameObject o, FishData preyData)
//			: base(o)
//		{
//			mPreyData = preyData;
//			mHasBeenPresented = false;
//		}

//		public override void OnStartup()
//		{
//			base.OnStartup();
//			base.Owner.AddInteraction(PresentTo.Singleton);
//			base.Owner.AddInteraction(CatchPrey.Singleton);
//			base.Owner.AddInteraction(ViewObjects.Singleton);
//			base.Owner.AddInteraction(PetPutInInventory.Singleton);
//			if (!(base.Owner is CatHuntFailureObject))
//			{
//				base.Owner.AddInteraction(PetEatPrey.Singleton);
//			}
//			if (base.Owner.ItemComp != null)
//			{
//				base.Owner.AddInventoryInteraction(PresentTo.Singleton);
//				base.Owner.AddInventoryInteraction(DropSomewhere.Singleton);
//			}
//		}

//		public override void Dispose()
//		{
//			base.Owner.RemoveInteractionByType(PresentTo.Singleton);
//			base.Owner.RemoveInteractionByType(DropSomewhere.Singleton);
//			base.Owner.RemoveInteractionByType(CatchPrey.Singleton);
//			base.Owner.RemoveInteractionByType(ViewObjects.Singleton);
//			if (base.Owner.ItemComp != null)
//			{
//				base.Owner.RemoveInteractionByType(PetPutInInventory.Singleton);
//			}
//			if (!(base.Owner is CatHuntFailureObject))
//			{
//				base.Owner.RemoveInteractionByType(PetEatPrey.Singleton);
//			}
//			base.Dispose();
//		}

//		public override void AddDebugInteractions(List<InteractionDefinition> debugInteractions)
//		{
//			base.AddDebugInteractions(debugInteractions);
//			debugInteractions.Add(DEBUG_SetAsNotPresented.Singleton);
//		}

//		public bool ArtisticObjectSpecificEffects(Sim actor, ref string jazzState)
//		{
//			return false;
//		}

//		public string GetViewInteractionName(Sim actor)
//		{
//			return ViewObjects.DefaultInteractionName;
//		}

//		public GameObject GetViewRouteTarget()
//		{
//			return mScriptObject;
//		}

//		public bool IsViewable(Sim viewingSim)
//		{
//			return mCatcherId != 0;
//		}

//		public void OnView(Sim viewingSim)
//		{
//			viewingSim.ShowTNSIfSelectable(TNSNames.ViewCaughtPrey, null, mScriptObject, mCatcherName);
//		}

//		public static bool SharedPresentTest(Sim actor, ICatPrey target, bool isAutonomous)
//		{
//			CatHuntingComponent fishingComponent = target.CatHuntingComponent;
//			if (fishingComponent == null)
//			{
//				return false;
//			}
//			if (isAutonomous && !target.InInventory)
//			{
//				return false;
//			}
//			if (target.Parent != null)
//			{
//				return false;
//			}
//			return target.IsUnconscious;
//		}

//		public void SetCatcher(Sim s)
//		{
//			SimDescription simDescription = s.SimDescription;
//			mCatcherId = simDescription.SimDescriptionId;
//			mCatcherName = simDescription.FirstName;
//			HasBeenCaught = true;
//			MinorPet minorPet = base.Owner as MinorPet;
//			if (minorPet != null)
//			{
//				minorPet.Captured = true;
//			}
//		}

//		public static float[] GetTraitAdjustedChance(Sim actor, int[] chanceInput)
//		{
//			float[] array = new float[chanceInput.Length];
//			float num = 1f;
//			if (actor.TraitManager.HasElement(TraitNames.AlphaPet))
//			{
//				num = TraitTuning.AlphaPetTraitHuntingSkillMultiplier;
//			}
//			for (int i = 0; i < chanceInput.Length; i++)
//			{
//				array[i] = (float)chanceInput[i] * num;
//			}
//			return array;
//		}
//	}
//}
