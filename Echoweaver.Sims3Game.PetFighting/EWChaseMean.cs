using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWChaseMean : ChaseMean
    {

        public class EWChaseMeanDefinition : ChaseMeanDefinition
        {
            public EWChaseMeanDefinition() : base()
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                ChaseBaseClass chaseBaseClass = new EWChaseMean();
                chaseBaseClass.Init(ref parameters);
                chaseBaseClass.IsMeanChase = true;
                return chaseBaseClass;
            }
        }

        public static new InteractionDefinition Singleton = new EWChaseMeanDefinition();

        public override bool Run()
        {
            EWPetFightingSkill skillActor = Actor.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
            if (skillActor == null)
            {
                skillActor = Actor.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                if (skillActor == null)
                {
                    return false;
                }
            }
            skillActor.StartSkillGain(skillActor.getSkillGainRate());

            EWPetFightingSkill skillTarget = new EWPetFightingSkill(EWPetFightingSkill.skillNameID);
            if (Target.IsCat || Target.IsADogSpecies)
            {
                skillTarget = Target.SkillManager.GetSkill<EWPetFightingSkill>(EWPetFightingSkill.skillNameID);
                if (skillTarget == null)
                {
                    skillTarget = Target.SkillManager.AddElement(EWPetFightingSkill.skillNameID) as EWPetFightingSkill;
                    if (skillTarget == null)
                    {
                        return false;
                    }
                }
                skillTarget.StartSkillGain(skillTarget.getSkillGainRate());
            }
            bool returnVal = base.Run();
            skillActor.StopSkillGain();
            if (Target.IsCat || Target.IsADogSpecies)
            {
                skillTarget.StopSkillGain();
            }
            return returnVal;
        }
    }
}
