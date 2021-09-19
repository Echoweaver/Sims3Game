using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using static Sims3.Gameplay.Actors.Sim;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class EWChasePlay : ChasePlay
    {
        public class EWChasePlayDefinition : ChasePlayDefinition
        {
            public EWChasePlayDefinition() : base()
            {
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                ChaseBaseClass chaseBaseClass = new EWChasePlay();
                chaseBaseClass.Init(ref parameters);
                chaseBaseClass.IsMeanChase = true;
                return chaseBaseClass;
            }

        }

        public static new InteractionDefinition Singleton = new EWChasePlayDefinition();

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
            skillActor.StartSkillGain(EWPetFightingSkill.kSkillGainRateNormal);
            bool returnVal = base.Run();
            skillActor.StopSkillGain();
            return returnVal;
        }
    }
}
