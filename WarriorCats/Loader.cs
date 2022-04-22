using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Objects.Fishing;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class LoadThis : GameObject
    {
        static bool HasBeenLoaded = false;

        // Wound buffs from Pet Fighting mod
        public static BuffNames buffNameGraveWound = (BuffNames)0x384B537AE0B8F97A;
        public static BuffNames buffNameSeriousWound = (BuffNames)0xAE4D28F1BCEC603D;
        public static BuffNames buffNameMinorWound = (BuffNames)0x3BE0F368D4653A9E;
        public static BuffNames[] woundBuffList = new BuffNames[] { buffNameGraveWound,
            buffNameMinorWound, buffNameSeriousWound };

        public static BuffNames buffNamePetstilence = (BuffNames)0xD79EDE5CB789F85D;
        public static BuffNames buffNameFeverish = (BuffNames)0x0A6994F5F35A8CD8;
        public static BuffNames buffNameSniffles = (BuffNames)0x2AFC0D6468CD9CD7;

        public static BuffNames buffNameFoodPoisingPet = (BuffNames)0x41BFC2124133973F;
        public static BuffNames buffNameStomachFluPet = (BuffNames)0xB6F4522A924504ED;
        public static BuffNames[] nauseaBuffList = new BuffNames[] { BuffNames.NauseousPet,
            buffNameFoodPoisingPet, buffNameStomachFluPet };

        [Tunable]
        protected static bool kInstantiator = false;
        public static bool kAllowPetDeath = true;


        static LoadThis()
        {
            // gets the OnPreload method to run before the whole savegame is loaded so your sim doesn't find
            // the skill missing if they need to access its data
            LoadSaveManager.ObjectGroupsPreLoad += OnPreload;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
        }

        static void OnPreload()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x37302B56D63A81A2, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            foreach (Plant p in Queries.GetObjects<Plant>())
            {
                //p.AddInteraction(EWPetMarkPlant.Singleton);
                p.AddInteraction(EWPetWatchPlant.Singleton);
                p.AddInteraction(EWPetHarvest.Singleton);
                p.AddInteraction(EWPetWeedPlant.Singleton);
                p.AddInteraction(EWPetWaterPlant.Singleton);
                p.AddInteraction(EWPetDisposePlant.Singleton);
            }

            foreach (GameObject p in Queries.GetObjects<GameObject>())
            {
                if (!(p.Plantable == null))
                {
                    p.AddInteraction(EWPetPickUpPlantable.Singleton);
                    p.AddInventoryInteraction(EWPetPlantSeed.Singleton);
                    p.AddInventoryInteraction(EWPetTreatNausea.Singleton);
                }
                if (p.CatHuntingComponent != null)
                {
                    if (p.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Rodent)
                    {
                        p.AddInventoryInteraction(EWPetTreatFleas.Singleton);
                    }
                }

                Fish fish = p as Fish;
                if (fish != null)
                {
                    fish.AddInventoryInteraction(EWCarryFish.Singleton);
                }
            }

            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));
        }

        public static ListenerAction OnObjectChanged(Event e)
        {
            Sim a = e.Actor as Sim;
            Plant p = e.TargetObject as Plant;
            if (p != null)
            {
                //p.AddInteraction(EWPetMarkPlant.Singleton, true);
                p.AddInteraction(EWPetWatchPlant.Singleton, true);
                p.AddInteraction(EWPetHarvest.Singleton, true);
                p.AddInteraction(EWPetWeedPlant.Singleton, true);
                p.AddInteraction(EWPetWaterPlant.Singleton, true);
                p.AddInteraction(EWPetDisposePlant.Singleton, true);
            }

            // Turning this off until I can test it
            if (a.IsPet && e.TargetObject.InInventory && !(e.TargetObject.Plantable == null) && false)
            {
                bool has_plant = false;
                foreach (InteractionObjectPair pair in e.TargetObject.GetAllInventoryInteractionsForActor(a))
                {
                    if (pair.InteractionDefinition.GetType() == EWPetPlantSeed.Singleton.GetType())
                    {
                        has_plant = true;
                        break;
                    }
                }
                if (!has_plant)
                {
                    GameObject o = e.TargetObject as GameObject;
                    o.AddInventoryInteraction(EWPetPlantSeed.Singleton);
                    o.AddInventoryInteraction(EWPetTreatNausea.Singleton);
                    o.AddInteraction(EWPetPickUpPlantable.Singleton, true);
                }
            }
            else if (e.TargetObject.CatHuntingComponent != null)
            {
                if (e.TargetObject.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Rodent)
                {
                    bool has_treatFleas = false;
                    foreach (InteractionObjectPair pair in e.TargetObject.GetAllInventoryInteractionsForActor(e.Actor))
                    {
                        if (pair.InteractionDefinition.GetType() == EWPetTreatFleas.Singleton.GetType())
                        {
                            has_treatFleas = true;
                            break;
                        }
                    }
                    if (!has_treatFleas)
                    {
                        GameObject o = e.TargetObject as GameObject;
                        o.AddInventoryInteraction(EWPetTreatFleas.Singleton);
                    }
                }
            }
            return ListenerAction.Keep;
        }
    }
}