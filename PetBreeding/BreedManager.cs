using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Socializing;

namespace Echoweaver.Sims3Game.PetBreeding
{
    public static class BreedManager
    {
        public static void setOffspringBreed(Sim pet)
        {
            if (pet.SimDescription.PetManager.BreedName == "")
            {
                string breed1 = "";
                string breed2 = "";
                if (pet.Genealogy.Parents.Count >= 2)
                {
                    breed1 = pet.Genealogy.Parents[0].SimDescription.PetManager.BreedName;
                    breed2 = pet.Genealogy.Parents[1].SimDescription.PetManager.BreedName;
                    if (breed1 == breed2)
                    {
                        pet.SimDescription.PetManager.BreedName = breed1;
                    }
                }
            }
        }
    }
}
