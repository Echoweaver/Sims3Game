using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.Store.Objects;
using Sims3.UI;
using Sims3.UI.CAS;
using Sims3.UI.Hud;
using static Sims3.UI.StringInputDialog;

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

        [Persistable]
        public static Dictionary<ulong, bool> Graduated = new Dictionary<ulong, bool>();

        // Custom skills from other mods
        public const SkillNames FightingSkillName = (SkillNames)0x20F47569;
		public const CommodityKind FightingCommodityKind = (CommodityKind)0x262891D3;

		public const SkillNames FishingSkillName = (SkillNames)0xDE46D7FA;
		public const CommodityKind FishingCommodityKindID = unchecked((CommodityKind)0xFD000E72);

		public static bool CanTakeApprentice(Sim s)
		{
			if (!(s.IsCat || s.IsADogSpecies))
				return false;
			if (s.SimDescription.ChildOrBelow)
				return false;
			// If you are a former apprentice who graduted, you may take an apprentice
			if (Graduated.ContainsKey(s.SimDescription.SimDescriptionId))
				return true;
			// Can't take an apprentice if you ARE an apprentice
			if (Apprentices.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			// Can't take an apprentice if you are in the pool of pets needing apprentices
			if (Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			// If you are a new pet or an apprentice who has graduated, you won't be in
			// either the Apprentice or Unapprenticed pool
            if (!Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId)
                && !Apprentices.ContainsKey(s.SimDescription.SimDescriptionId) &&
				s.SimDescription.AdultOrAbove)
                return true;
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
            if (!(s.IsCat || s.IsADogSpecies))
                return false;
			// Can't be an apprentice if you are already an apprentce
            if (Apprentices.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			// Can't be an apprentice again if you graduated from apprenticeship
			if (Graduated.ContainsKey(s.SimDescription.SimDescriptionId))
				return false;
			if (s.SimDescription.Elder)  // Elder sims can't be apprentices
                return false;
            if (Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId))
				return true;
			if (s.SimDescription.ChildOrBelow && !Apprentices.ContainsKey(s.SimDescription.SimDescriptionId))
				return true;
			// Default state of a cat newly added to the household
			if (!Apprentices.ContainsKey(s.SimDescription.SimDescriptionId)
				&& !Unapprenticed.ContainsKey(s.SimDescription.SimDescriptionId))
				return true;
			return false;
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

		public static void RejectApprentice(Sim apprentice)
		{
			Apprentices.Remove(apprentice.SimDescription.SimDescriptionId);
			Unapprenticed.Add(apprentice.SimDescription.SimDescriptionId, true);
		}

		public static void GraduateApprentice(Sim apprentice)
		{
			Apprentices.Remove(apprentice.SimDescription.SimDescriptionId);
			Unapprenticed.Remove(apprentice.SimDescription.SimDescriptionId);
			Graduated.Add(apprentice.SimDescription.SimDescriptionId, true);
		}

		public static void ClearApprenticeState(Sim s)
		{
			// Debug only option to fix data that might be messed up
			Apprentices.Remove(s.SimDescription.SimDescriptionId);
			Unapprenticed.Remove(s.SimDescription.SimDescriptionId);
            Graduated.Remove(s.SimDescription.SimDescriptionId);
        }

        public static bool RenameSim(Sim s, string promptText)
		{
            if (!UIUtils.IsOkayToStartModalDialog())
            {
                return false;
            }
            string changeFnameText = Localization.LocalizeString(s.IsFemale,
				"Gameplay/Objects/RabbitHoles/CityHall:ChangeNameFirstName", new object[1] { s.SimDescription });
            string changeLnameText = Localization.LocalizeString(s.IsFemale,
				"Gameplay/Objects/RabbitHoles/CityHall:ChangeNameLastName", new object[1] { s.SimDescription });
            do
            {
                s.SimDescription.FirstName = StringInputDialog.Show(promptText, changeFnameText,
					s.FirstName, CASBasics.GetMaxNameLength(), Validation.SimNameText);
            } while (string.IsNullOrEmpty(s.FirstName));
            do
            {
                s.SimDescription.LastName = StringInputDialog.Show(promptText, changeLnameText,
					s.LastName, CASBasics.GetMaxNameLength(), Validation.SimNameText);
            } while (string.IsNullOrEmpty(s.LastName));
            Household.AddDirtyNameSimID(s.SimDescription.SimDescriptionId);
            IHudModel hudModel = Sims3.Gameplay.UI.Responder.Instance.HudModel;
            HudModel val = (HudModel)(hudModel is HudModel ? hudModel : null);
            if (s.SimDescription.CreatedSim != null)
            {
                val.NotifyNameChanged(s.SimDescription.CreatedSim.ObjectId);
            }
            return true;
        }

		// Tunings will go here

		[Tunable]
		public static bool kPetWarriorDebug = false;

        public static void DebugNote(string str)
        {
            if (kPetWarriorDebug)
            {
                StyledNotification.Show(new StyledNotification.Format(str, StyledNotification
					.NotificationStyle.kDebugAlert));
            }
        }

        [Tunable]
        public static float kApprenticeSkillGainRate = 20f;

        [Tunable]
        [TunableComment("The max distance a pet must be from a water source to try to route to it.")]
        public static float kMaxWaterDistance = 40f;
        [Tunable]

        [TunableComment("Description:  Max amount of time (in minutes) to play in the water")]
        public static float kMaxPlayInWaterTime = 30f;

    }
}

