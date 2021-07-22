using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Socializing;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetBreedfix
{
    public static class BreedManager
    {
        public static Dictionary<ulong, string> breedDict =
            new System.Collections.Generic.Dictionary<ulong, string>();

        public static string retrievePetBreed(ulong simID)
        {
            string breed;
            if (!breedDict.TryGetValue(simID, out breed))
            {
                breed = "";
            }
            return breed;
        }

        public static void storePetBreed(SimDescription sim)
        {
            breedDict[sim.SimDescriptionId] = sim.PetManager.BreedName;
        }

        public static void setOffspringBreed(Sim pet)
        {
            if (pet.SimDescription.PetManager.BreedName == "")
            {
                if (pet.Genealogy.Parents.Count >= 2)
                {
                    string breed1 = pet.Genealogy.Parents[0].SimDescription.PetManager.BreedName;
                    string breed2 = pet.Genealogy.Parents[1].SimDescription.PetManager.BreedName;
                    if (breed1 == breed2)
                    {
                        pet.SimDescription.PetManager.BreedName = breed1;
                        storePetBreed(pet.SimDescription);
                    }
                }
            }
        }
    }
}
