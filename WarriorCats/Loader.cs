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

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class LoadThis : GameObject
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        [Tunable]
        public static bool kAllowPetDeath = true;
        public static SimDescription.DeathType sickDeathType = SimDescription.DeathType.Starve;


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
                }
            }

            if (GameUtils.IsInstalled(ProductVersion.EP9))
            {
                sickDeathType = SimDescription.DeathType.Ranting;
            }
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));
        }

        public static ListenerAction OnObjectChanged(Event e)
        {
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

            if (e.TargetObject.InInventory && !(e.TargetObject.Plantable == null))
            {
                bool has_plant = false;
                foreach (InteractionObjectPair pair in e.TargetObject.GetAllInventoryInteractionsForActor(e.Actor))
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
                    o.AddInteraction(EWPetPickUpPlantable.Singleton, true);
                }
            }
            return ListenerAction.Keep;
        }
    }
}