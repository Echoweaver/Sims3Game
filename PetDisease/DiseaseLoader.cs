using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

//Template Created by Battery

namespace Echoweaver.Sims3Game.PetDisease
{
	public class Loader
	{
		[Tunable] static bool init = false;

        [Tunable]
        public static bool kAllowPetDeath = true;

        // Word on the street is that ghost shaders don't require the associated EP.
        [Tunable]
        public static SimDescription.DeathType diseaseDeathType = SimDescription.DeathType.Shark;

        public static List<ulong> BuffGuids = new List<ulong>() { Buffs.BuffEWPetGermy.mGuid,
			Buffs.BuffEWTummyTrouble.mGuid,
			Buffs.BuffEWPetstilence.mGuid,
			Buffs.BuffEWBabyDistress.mGuid,
			Buffs.BuffEWPetPneumonia.mGuid
		};

		static Loader()
		{
			LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoaded;
		}

		public static void Initialize()
		{
			for (int i = 0; i < BuffGuids.Count; i++)
			{
				Sim.ActiveActor.BuffManager.AddElement(BuffGuids[i], Origin.None);
			}
		}

		static void OnPreload()
		{
			LoadBuffXMLandParse(null);
		}
		static void OnWorldLoaded(object sender, EventArgs e)
		{
			Initialize();
		}

		public static void LoadBuffXMLandParse(ResourceKey[] resourceKeys)
		{

			ResourceKey key = new ResourceKey(5522594682370665020ul, 53690476u, 0u);
			XmlDbData xmlDbData = XmlDbData.ReadData(key, false);
			if (xmlDbData != null)
			{
				BuffManager.ParseBuffData(xmlDbData, true);
			}
			UIManager.NewHotInstallStoreBuffData += LoadBuffXMLandParse;
		}

	}
}