using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.Store.Objects;

namespace Echoweaver.Sims3Game.WarriorCats
{
	public static class Config
	{
        // Stores the relationship apprentice->master
        // Masters can have multiple apprentices. An apprentice can only have one master
        [Persistable]
        public static Dictionary<ulong, ulong> Apprentices = new Dictionary<ulong, ulong>();

        [Persistable]
        public static Dictionary<ulong, bool> Unapprenticed = new Dictionary<ulong, bool>();

		// Custom skills from other mods
		public const SkillNames FightingSkillName = (SkillNames)0x20F47569;
		public const CommodityKind FightingCommodityKind = (CommodityKind)0x262891D3;

		public const SkillNames FishingSkillName = (SkillNames)0xDE46D7FA;
		public const CommodityKind FishingCommodityKindID = unchecked((CommodityKind)0xFD000E72);


		public static bool CanTakeApprentice(Sim s)
		{
			if (s.SimDescription.ChildOrBelow)
				return false;
			// Can't take an apprentice if you ARE an apprentice
			if (Apprentices.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			// Can't take an apprentice if you are looking to become an apprentice
			if (Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			return true;
		}

		public static bool HasApprentice(Sim master, Sim apprentice)
		{
			if (!Apprentices.ContainsKey(apprentice.SimDescription.SimDescriptionId))
			{
				return false;
			}
			if (Apprentices[apprentice.SimDescription.SimDescriptionId] == master.SimDescription.SimDescriptionId)
			{
				return true;
			}
			return false;
		}

		public static bool CanBeApprenticed(Sim s)
		{
			// TODO: Properly add children to the Unapprenticed dict
			return Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId)
				|| (s.SimDescription.ChildOrBelow && !Apprentices.ContainsKey(s.SimDescription.SimDescriptionId));
		}

		public static string LocalizeStr(string name, params object[] parameters)
		{
			return Localization.LocalizeString("Echoweaver/Warriorcats:" + name, parameters);
		}

		public static void AddApprentice(Sim master, Sim apprentice)
		{
			Unapprenticed.Remove(apprentice.SimDescription.SimDescriptionId);
			Apprentices.Add(apprentice.SimDescription.SimDescriptionId, master.SimDescription.SimDescriptionId);
		}

		public static void RemoveApprentice(Sim apprentice)
		{
			Apprentices.Remove(apprentice.SimDescription.SimDescriptionId);
			Unapprenticed.Add(apprentice.SimDescription.SimDescriptionId, true);
		}

		// Tunings will go here

		[Tunable]
		public static bool kPetWarriorDebug = false;
	}
}

