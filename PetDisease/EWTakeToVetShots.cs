using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Utilities;

namespace Echoweaver.Sims3Game.PetDisease
{
    public class EWTakeToVetShots
    {
        public EWTakeToVetShots()
        {
        }

        public void Vaccinate(Sim s)
        {
            DateAndTime mVaccinationDate = SimClock.CurrentTime();
            Loader.VaccineRecord[s.SimDescription.SimDescriptionId] = mVaccinationDate;
            if (s != null)
            {
                // Should raise this event for pets?
                // EventTracker.SendEvent(EventTypeId.kGotFluShot, s);
                // TODO: Should allow vaccination while sick? Not sure this makes
                // sense.
                s.BuffManager.RemoveElement(Buffs.BuffEWPetGermy.buffName);
                s.BuffManager.RemoveElement(Buffs.BuffEWPetPneumonia.buffName);
                s.BuffManager.RemoveElement(Buffs.BuffEWPetstilence.buffName);
                // TODO: See if this sting makes any sense for pets. We can make a new one.
                s.ShowTNSAndPlayStingIfSelectable("sting_get_immunized", TNSNames.FluShotTNS, s,
                    null, null, null, new bool[1] { s.IsFemale }, false, s);
            }
        }
    }
}

