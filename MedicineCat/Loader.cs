using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.MedicineCat
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        [Tunable]
        public static bool kAllowPetDeath = true;
        public static SimDescription.DeathType diseaseDeathType = SimDescription.DeathType.Starve;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64("EW_MedicineCat_Skill"),
                0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);

        }
        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            if (GameUtils.IsInstalled(ProductVersion.EP9))
            {
                diseaseDeathType = SimDescription.DeathType.Ranting;
            }
        }

    }
}
