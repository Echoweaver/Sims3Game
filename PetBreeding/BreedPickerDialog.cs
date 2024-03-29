﻿using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System.Collections.Generic;
using static Sims3.UI.CAS.CASController;

namespace Echoweaver.Sims3Game.PetBreedfix
{
    public class BreedPickerDialog 
    {
        public List<BreedOutfit> mBreeds;
        public List<ObjectListPickerInfo> breedInfo;

        public BreedOutfit Show()
        {
            return ObjectListPickerDialog.Show(breedInfo) as BreedOutfit;
        }

        public BreedPickerDialog(CASAgeGenderFlags speciesFlag)
        {
            if (CASLogic.sBreedOutfitDict == null)
            {
                CASLogic.LoadPetBreedsXML();
            }
            mBreeds = CASLogic.GetBreedOutfitList(speciesFlag);
            breedInfo = new List<ObjectListPickerInfo>();

            string emptyBreed = "";
            if (speciesFlag == CASAgeGenderFlags.Dog || speciesFlag == CASAgeGenderFlags.LittleDog)
            {
                emptyBreed = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:DogNoBreed");
            } else
            {
                emptyBreed = StringTable.GetLocalizedString("Ui/Caption/HUD/PetAdoptionInfoTooltip:CatHorseNoBreed");
            }
            breedInfo.Add(new ObjectListPickerInfo(emptyBreed, new BreedOutfit("", "")));

            foreach (BreedOutfit b in mBreeds)
            {
                // Breed names ending in "Delta" display as, "Create a [Species]".
                // I think they are used in CAP.
                if (!b.BreedOutfitName.EndsWith("Delta"))
                {
                    ObjectListPickerInfo o = new ObjectListPickerInfo(StringTable.
                        GetLocalizedString(b.BreedLocKey), b);
                    breedInfo.Add(o);
                }
            }
        }
    }
}
