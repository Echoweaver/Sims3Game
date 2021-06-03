using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Echoweaver.Sims3.WarriorCats
{
    public class Loader : GameObject
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            // gets the OnPreload method to run before the whole savegame is loaded so your sim doesn't find
            // the skill missing if they need to access its data
            LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
        }

        static void OnPreload()
        {
            try
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
            catch (Exception ex)
            {
                return;
            }
        }
    }

    public class ExampleUsesClass : GameObject
    {
        private const SkillNames ExampleCustomSkillGuid = (SkillNames)0x277ECF3A;

        public ExampleUsesClass()
        {
        }

        public void ExampleUses(Sim s)
        {
            if (!s.SkillManager.HasElement(ExampleCustomSkillGuid))
            {
                s.SkillManager.AddElement(ExampleCustomSkillGuid);
            }
            s.SkillManager.AddSkillPoints(ExampleCustomSkillGuid, 3.0f);
            Skill sk = s.SkillManager.GetElement(ExampleCustomSkillGuid);
            float sl = sk.SkillPoints;

        }

    }
}
