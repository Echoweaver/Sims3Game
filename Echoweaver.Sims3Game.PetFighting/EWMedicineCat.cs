using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;

namespace Echoweaver.Sims3Game
{

    public class EWMedicineCatSkill : Skill
    {
        public const SkillNames SkillNameID = (SkillNames)0x277ECF3A;
        public EWMedicineCatSkill(SkillNames guid) : base(guid)
        {
        }

        private EWMedicineCatSkill()
        {
        }

        public override void CreateSkillJournalInfo()
        {
        }

        public override bool ExportContent(IPropertyStreamWriter writer)
        {
            base.ExportContent(writer);
            return true;
        }

        public override bool ImportContent(IPropertyStreamReader reader)
        {
            base.ImportContent(reader);
            return true;
        }
    }

    public class ExampleUsesClass : GameObject
    {
        public const SkillNames EWMedicineCatSkill = (SkillNames)0x277ECF3A;  // Hash32 of EWMedicineCatSkill

        public ExampleUsesClass()
        {
        }

        public void ExampleUses(Sim s)
        {
            if (!s.SkillManager.HasElement(EWMedicineCatSkill))
            {
                s.SkillManager.AddElement(EWMedicineCatSkill);
            }
            s.SkillManager.AddSkillPoints(EWMedicineCatSkill, 3.0f);
            Skill sk = s.SkillManager.GetElement(EWMedicineCatSkill);
            float sl = sk.SkillPoints;

        }

    }
}
