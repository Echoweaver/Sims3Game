using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI.Hud;
using System;
using System.Collections.Generic;

namespace Echoweaver.Sims3Game.PetIllness
{
	// Types of diseases tracked
	//  - sims affected: Pets, Humans, maybe specific occult, maybe ages (childhood diseases?)
	//  - Generation
	//    - Ambient
	//    - Romantic
	//    - Dirty encounter
	//    - Eat
	//    - Hunt
	//  - type of transmission: proximity, romantic (STD), fight, none (e.g. food poisioning)
	//  - gestation period
	//  - immunity traits
	//  - resistance traits
	//  - vulnerable traits

	// <EWDiseaseList>
	// <EWDisease>
	//   <DiseaseName>Sniffles</DiseaseName>
	//   <BuffGUID></BuffGUID>
	//   <CASAGSAvailabilityFlags>AC,EC,AD,ED,AL,EL</CASAGSAvailabilityFlags>
	//   <Generator>Ambient</Generator>
	//   <Transmission>Proximity, Romantic, Fight</Transmission>
	//   <Immunity Traits></Immunity Traits>
	//   <Resistance Traits></Resistance Traits>
	//   <Vunerable Traits></Vunerable Traits>
	// </EWDisease>
	//
	// <EWDisease>
	//   <DiseaseName>Food Poisoning</DiseaseName>
	//   <BuffGUID></BuffGUID>
	//   <CASAGSAvailabilityFlags>AC,EC,AD,ED,AL,EL</CASAGSAvailabilityFlags>
	//   <Generator>Eat,Dirt</Generator>
	//   <Transmission>None</Transmission>
	//   <Immunity Traits>Piggy</Immunity Traits>
	//   <Resistance Traits></Resistance Traits>
	//   <Vunerable Traits></Vunerable Traits>
	// </EWDisease>
	//
	// <EWDisease>
	//   <DiseaseName>Stomach Flu</DiseaseName>
	//   <BuffGUID></BuffGUID>
	//   <CASAGSAvailabilityFlags>AC,EC,AD,ED,AL,EL</CASAGSAvailabilityFlags>
	//   <Generator>Eat,Hunt</Generator>
	//   <Transmission>Proximity</Transmission>
	//   <Immunity Traits></Immunity Traits>
	//   <Resistance Traits>Piggy</Resistance Traits>
	//   <Vunerable Traits></Vunerable Traits>
	// </EWDisease>
	//
	// <EWDisease>
	//   <DiseaseName>Petstilence</DiseaseName>
	//   <BuffGUID></BuffGUID>
	//   <CASAGSAvailabilityFlags>AC,EC,AD,ED,AL,EL</CASAGSAvailabilityFlags>
	//   <Generator>Ambient</Generator>
	//   <Transmission>Proximity,Romantic,Fight</Transmission>
	//   <Immunity Traits></Immunity Traits>
	//   <Resistance Traits></Resistance Traits>
	//   <Vunerable Traits></Vunerable Traits>
	// </EWDisease>
	//
	// <EWDisease>
	//   <DiseaseName>Feverish</DiseaseName>
	//   <BuffGUID></BuffGUID>
	//   <CASAGSAvailabilityFlags>AC,EC,AD,ED,AL,EL</CASAGSAvailabilityFlags>
	//   <Generator>Ambient</Generator>
	//   <Transmission>Proximity,Romantic,Fight</Transmission>
	//   <Immunity Traits></Immunity Traits>
	//   <Resistance Traits></Resistance Traits>
	//   <Vunerable Traits></Vunerable Traits>
	// </EWDisease>
	// </EWDiseaseList>


	[Persistable]
    public class EWDisease : IExportableContent
    {
        public static Dictionary<SimDescription, SimIllness> SimIllnesses = new Dictionary<SimDescription, SimIllness>();

		public static SimIllness Manager(SimDescription param_simID)
        {
			if (SimIllnesses.ContainsKey(param_simID))
            {
				return SimIllnesses[param_simID];
            } else
            {
				SimIllness initialVal = new SimIllness(param_simID);
				SimIllnesses[param_simID] = initialVal;
				return initialVal;
            }
        }

		public static void ParseDiseaseData(XmlDbData data)
		{
			XmlDbTable value = null;
			data.Tables.TryGetValue("DiseaseList", out value);
			if (value == null)
			{
				value = data.Tables["Disease"];
			}
			foreach (XmlDbRow row in value.Rows)
			{
				bool flag = false;
				string text = row["DiseaseName"];
				string mDescription = row["DiseaseDescription"];
				BuffNames guid = (BuffNames)BuffManager.GetGuid(row["Hex"], false);
				//double mVersion = 0.0;
				if (guid == BuffNames.Undefined)
				{
					flag = true;
				}
				Buff buff = null;
				if (flag)
				{
					continue;
				}
				string immuneTraits = row["ImmuneTraits"];
				string vulnerableTraits = row["VulnerableTraits"];
				string resitantTraits = row["ResistantTraits"];
				string text2 = row["NeededTraitForRobot"];
				ParserFunctions.TryParseCommaSeparatedList(immuneTraits, out List<TraitNames> list, TraitNames.Unknown);
				ParserFunctions.TryParseCommaSeparatedList(vulnerableTraits, out List<TraitNames> vulnerableList, TraitNames.Unknown);
				ParserFunctions.TryParseCommaSeparatedList(resitantTraits, out List<TraitNames> resistantList, TraitNames.Unknown);
				string mTopic = row["Topic"];
				if (ParserFunctions.TryParseEnum(row["AxisEffected"], out MoodAxis value4, MoodAxis.None, ignoreCase: true))
				{
					ParserFunctions.TryParseEnum(row["PolarityOverride"], out Polarity value5, Polarity.NoOverride, ignoreCase: true);
					Buff.BuffData buffData = new Buff.BuffData();
					ParserFunctions.TryParseEnum<ProductVersion>(row["SKU"], out buffData.mProductVersion, (ProductVersion)1);
					ResourceKey val = ResourceKey.kInvalidResourceKey;
					string text3 = null;
					if (AppDomain.CurrentDomain.GetData("UIManager") != null)
					{
						text3 = row["ThumbFilename"];
						val = ResourceKey.CreatePNGKey(text3, ResourceUtils.ProductVersionToGroupId(buffData.mProductVersion));
						if (!World.ResourceExists(val))
						{
							val = ResourceKey.CreatePNGKey(text3, 0u);
						}
					}
					//buffData.mBuffGuid = guid;
					//buffData.mBuffName = text;
					//buffData.mDescription = mDescription;
					//buffData.mHelpText = mHelpText;
					//buffData.mBuffCategory = value2;
					//buffData.mVersion = mVersion;
					//buffData.SetFlags(Buff.BuffData.FlagField.PermaMoodlet, ParserFunctions.ParseBool(row["PermaMoodlet"]));
					//string @string = row.GetString("PermaMoodletColor");
					//ParserFunctions.TryParseEnum<MoodColor>(@string, out buffData.mMoodletColor, (MoodColor)(-1));
					//buffData.mAxisEffected = value4;
					//buffData.mPolarityOverride = value5;
					//buffData.mEffectValue = ParserFunctions.ParseInt(row["EffectValue"], 0);
					//buffData.mDelayTimer = ParserFunctions.ParseInt(row["DelayTimer"], 0);
					//buffData.mTimeoutSimMinutes = ParserFunctions.ParseFloat(row["TimeoutLength"], -1f);
					//buffData.mSolveCommodity = value3;
					//buffData.mSolveTime = ParserFunctions.ParseFloat(row["SolveTime"], float.MinValue);
					//buffData.SetFlags(Buff.BuffData.FlagField.AttemptAutoSolve, ParserFunctions.ParseBool(row["AttemptAutoSolve"]));
					//ParserFunctions.ParseCommaSeparatedString(row["FacialIdle"], out buffData.mFacialIdles);
					//buffData.SetFlags(Buff.BuffData.FlagField.IsExtreme, ParserFunctions.ParseBool(row["IsExtreme"]));
					//buffData.mNeededTraitList = list;
					//buffData.mIncreasedEffectivenessList = vulnerableList;
					//buffData.mReducedEffectivenessList = resistantList;
					//buffData.mThumbKey = val;
					//buffData.mThumbString = text3;
					//buffData.mTopic = mTopic;
					buffData.SetFlags(Buff.BuffData.FlagField.ShowBalloon, row.GetBool("ShowBallon"));
					buffData.SetFlags(Buff.BuffData.FlagField.Travel, row.GetBool("Travel"));
					ParserFunctions.TryParseCommaSeparatedList<OccultTypes>(row["DisallowedOccults"], out buffData.mDisallowedOccults, (OccultTypes)0);
					if (buffData.mDisallowedOccults.Count == 0)
					{
						buffData.mDisallowedOccults = null;
					}
					string text4 = row.GetString("JazzStateSuffix");
					if (string.IsNullOrEmpty(text4))
					{
						text4 = text;
					}
					buffData.mJazzStateSuffix = text4;
					string string2 = row.GetString("SpeciesAvailability");
					if (string.IsNullOrEmpty(string2))
					{
						buffData.mAvailabilityFlags = (CASAGSAvailabilityFlags)127;
					}
					else
					{
						buffData.mAvailabilityFlags = ParserFunctions.ParseAllowableAgeSpecies(string2);
					}
					string text5 = row["CustomClassName"];
					if (text5.Length > 0)
					{
						text5 = text5.Replace(" ", "");
						int num = text5.IndexOf(',');
						if (num < 0)
						{
							flag = true;
						}
						else
						{
							string str = text5.Substring(0, num);
							text5.Substring(num + 1);
							Type type = null;
							//if (bStore)
							//{
							//	type = Type.GetType(str + ",Sims3StoreObjects");
							//}
							if ((object)type == null)
							{
								type = Type.GetType(text5);
							}
							if ((object)type == null)
							{
								flag = true;
							}
							else
							{
								Type[] types = new Type[1] {
							typeof(Buff.BuffData)
						};
								//ConstructorInfo constructor = type.GetConstructor(types);
								// object obj; // = constructor.Invoke(new object[1] {
						//	buffData
						//});
								//buff = (Buff)obj;
							}
						}
					}
					else
					{
						buff = new Buff(buffData);
					}
				}
				if (flag || buff == null)
				{
					continue;
				}
				BuffInstance value6 = buff.CreateBuffInstance();
				if (GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary.ContainsKey((ulong)guid))
				{
					if (GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary[(ulong)guid].mBuff.BuffVersion < buff.BuffVersion)
					{
						GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary[(ulong)guid] = value6;
					}
				}
				else
				{
					GenericManager<BuffNames, BuffInstance, BuffInstance>.sDictionary.Add((ulong)guid, value6);
					//sBuffEnumValues.AddNewEnumValue(text, guid);
				}
			}
		}

		public bool ExportContent(ResKeyTable resKeyTable, ObjectIdTable objIdTable, IPropertyStreamWriter writer)
		{
			throw new NotImplementedException();
		}

		public bool ImportContent(ResKeyTable resKeyTable, ObjectIdTable objIdTable, IPropertyStreamReader reader)
		{
			throw new NotImplementedException();
		}

		public EWDisease()  
        {
        }

		public class SimIllness
		{
			SimDescription mSimID;

			public SimIllness(SimDescription parm_simID)
			{
				mSimID = parm_simID;
			}

			public void UpdateInsideOutside()
			{
				Sim createdSim = mSimID.CreatedSim;
				if (createdSim == null)
				{
					return;
				}
				bool inRabbitHole = createdSim.SimInRabbitHolePosture;
				if (!inRabbitHole)
				{
					inRabbitHole = (createdSim.RoomId != 0);
					if (inRabbitHole && createdSim.IsSelectable)
					{
						Vector3 position = createdSim.Position;
						LotLocation invalid = LotLocation.Invalid;
						ulong lotLocationAtFloor = World.GetLotLocationAtFloor(position, ref invalid);
						if (World.HasPool(lotLocationAtFloor, invalid) && !World.HasAnyRoofAtPos(position))
						{
							inRabbitHole = false;
						}
					}
				}
				BuffGermy.BuffInstanceGermy buffInstanceGermy = createdSim.BuffManager.GetElement(BuffNames.Germy) as BuffGermy.BuffInstanceGermy;
				if (buffInstanceGermy != null)
				{
					buffInstanceGermy.IsIndoors = inRabbitHole;
				}
			}

			public void ActivateSickness()
			{
				//mSickIncubationAlarm = AlarmHandle.kInvalidHandle;
				if (GameUtils.IsInstalled(ProductVersion.EP8))
				{
					Sim createdSim = mSimID.CreatedSim;
					if (createdSim != null && createdSim.BuffManager.AddElement(BuffNames.Germy, Origin.None))
					{
						//mSickDate = SimClock.CurrentTime();
						createdSim.ShowTNSAndPlayStingIfSelectable("sting_get_sick", TNSNames.GotSickTNS,
							createdSim, null, null, null, new bool[1] { createdSim.IsFemale },
							false, createdSim);
					}
				}
			}
			public void TimedSickCheck()
			{
				//Sim createdSim = mOwnerDescription.CreatedSim;
				//if (createdSim != null && GameUtils.IsInstalled((ProductVersion)32768))
				//{
				//	createdSim.RemoveAlarm(mSickCheckAlarm);
				//	float num = HealthManager.kMaxSickCheckInterval;
				//	float num2 = HealthManager.kMinSickCheckInterval;
				//	if (createdSim.BuffManager.HasAnyElement(BuffNames.Frostbitten, BuffNames.TeethChattering))
				//	{
				//		num = HealthManager.kMaxSickCheckFreezingInterval;
				//		num2 = HealthManager.kMinSickCheckFreezingInterval;
				//	}
				//	else if (createdSim.BuffManager.HasElement(BuffNames.GettingChilly))
				//	{
				//		num = HealthManager.kMaxSickCheckChillyInterval;
				//		num2 = HealthManager.kMinSickCheckChillyInterval;
				//	}
				//	PossiblyGetSick(HealthManager.kAmbientSicknessOdds);
				//	mSickCheckAlarm = createdSim.AddAlarm(RandomUtil.GetFloat(num - num2) + num2, TimeUnit.Minutes, TimedSickCheck, "Periodic Sick Alarm", AlarmType.AlwaysPersisted);
				//}
			}

			public void PossiblyGetSick(float odds)
			{
				//Sim createdSim = mOwnerDescription.CreatedSim;
				//if (createdSim != null)
				//{
				//	TraitManager traitManager = createdSim.TraitManager;
				//	if (traitManager.HasElement(TraitNames.LovesTheHeat))
				//	{
				//		odds *= HealthManager.kWarmbloodedModifier;
				//	}
				//	else if (traitManager.HasElement(TraitNames.LovesTheCold))
				//	{
				//		odds *= HealthManager.kColdBloodedModifier;
				//	}
				//}
				//if (RandomUtil.RandomChance01(odds))
				//{
				//	GetSick();
				//}
			}

			public void GetSick()
			{
				//if (GameUtils.IsInstalled((ProductVersion)32768) && mOwnerDescription.ChildOrAbove && !mOwnerDescription.IsImmuneToAllergiesAndSickness())
				//{
				//	Sim createdSim = mOwnerDescription.CreatedSim;
				//	if (createdSim != null && mSickIncubationAlarm == AlarmHandle.kInvalidHandle && !createdSim.BuffManager.HasElement(BuffNames.Germy) && (mSickDate.Ticks == 0 || SimClock.ElapsedTime(TimeUnit.Hours, mSickDate) > kSicknessCooldown) && (mVaccinationDate.Ticks == 0 || SimClock.ElapsedTime(TimeUnit.Days, mVaccinationDate) > (float)SeasonsManager.GetYearLength()))
				//	{
				//		mSickIncubationAlarm = mOwnerDescription.CreatedSim.AddAlarm(RandomUtil.GetFloat(kMaxIncubationTime - kMinIncubationTime) + kMinIncubationTime, TimeUnit.Hours, ActivateSickness, "sickness incubation alarm", AlarmType.AlwaysPersisted);
				//	}
				//}
			}

			public void PossibleDirtyObjectContagion(object targetObject)
			{
				if (targetObject == null)
				{
					return;
				}
				Sim sim = targetObject as Sim;
				if (sim != null)
				{
					Posture posture = sim.Posture;
					if (posture != null)
					{
						targetObject = posture.Container;
					}
				}
				//foreach (Type sDirtyObjectType in sDirtyObjectTypes)
				//{
				//	if (sDirtyObjectType.IsAssignableFrom(targetObject.GetType()))
				//	{
				//		PossiblyGetSick(HealthManager.kInteractSicknessOdds);
				//		break;
				//	}
				//}
			}

			public void PossibleProximityContagion()
			{
				PossiblyGetSick(HealthManager.kProximitySicknessOdds);
			}

			public void PossibleProximityContagion(float odds)
			{
				PossiblyGetSick(odds);
			}

			public void PossibleRomanticContagion()
			{
				PossiblyGetSick(HealthManager.kRomanticSicknessOdds);
			}

			public void TemperatureChange(bool isWarm)
			{
				//BuffGermy.BuffInstanceGermy buffInstanceGermy = mOwnerDescription.CreatedSim.BuffManager.GetElement(BuffNames.Germy) as BuffGermy.BuffInstanceGermy;
				//if (buffInstanceGermy != null)
				//{
				//	if (isWarm)
				//	{
				//		buffInstanceGermy.ModifyDuration(HealthManager.kWarmInteractionDelta);
				//	}
				//	else
				//	{
				//		buffInstanceGermy.ModifyDuration(HealthManager.kColdInteractionDelta);
				//	}
				//}
			}

		}

	}
}
