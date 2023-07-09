using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.ObjectComponents;
using Sims3.UI;
using Sims3.Gameplay.Socializing;
using System.Collections.Generic;
using System.Xml;

namespace Echoweaver.Sims3Game.WarriorCats
{
    public class Loader 
    {
        static bool HasBeenLoaded = false;

        // Wound buffs from Pet Fighting mod
        public static BuffNames buffNameGraveWound = (BuffNames)0x384B537AE0B8F97A;
        public static BuffNames buffNameSeriousWound = (BuffNames)0xAE4D28F1BCEC603D;
        public static BuffNames buffNameMinorWound = (BuffNames)0x3BE0F368D4653A9E;
        public static BuffNames[] woundBuffList = new BuffNames[] { buffNameGraveWound,
            buffNameMinorWound, buffNameSeriousWound };

        public static BuffNames buffNamePetstilence = (BuffNames)0x7768716F913C2054;
        public static BuffNames buffNamePneumoniaPet = (BuffNames)0x904F100B14974699;
        public static BuffNames buffNameGermyPet = (BuffNames)0x9086F0050AC3673Dul;
        public static BuffNames buffNameTummyTrouble = (BuffNames)0xDFF72BA95943E99;


        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
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

            //LoadSocialData("EWWarriors_SocialData");

            foreach (Sim pet in Queries.GetObjects<Sim>())
            {
                if (pet.IsCat || pet.IsADogSpecies)
                {
                    AddPetInteractions(pet);
                }
            }

            foreach (Plant p in Queries.GetObjects<Plant>())
            {
                AddPlantInteractions(p);
            }

            foreach (Ingredient i in Queries.GetObjects<Ingredient>())
            {
                PlantableComponent plantable = i.Plantable;
                if (plantable != null)
                {
                            i.AddInteraction(EWPetPickUpPlantable.Singleton, true);
                            AddPlantableInventoryInteractions(i);
                }
            }

            foreach(MinorPet p in Queries.GetObjects<MinorPet>())
            if (p.CatHuntingComponent != null)
            {
                if (p.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Rodent)
                {
                    p.AddInventoryInteraction(EWPetTreatFleas.Singleton);
                }
            }

            //Fish fish = p as Fish;
            //if (fish != null)
            //{
            //    fish.AddInventoryInteraction(EWCarryFish.Singleton);
            //}

            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));

            if (Config.kPetWarriorDebug)
            {
                AlarmManager.Global.AddAlarm(10f, TimeUnit.Seconds, NotifyDebugState, "Notify that debug is on",
                    AlarmType.NeverPersisted, null);
            }
        }

        public static void LoadSocialData(string spreadsheet)
        {
            XmlDocument root = Simulator.LoadXML(spreadsheet);
            bool isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
            if (spreadsheet != null)
            {
                XmlElementLookup lookup = new XmlElementLookup(root);
                List<XmlElement> list = lookup["Action"];
                foreach (XmlElement element in list)
                {
                    CommodityTypes types;
                    XmlElementLookup table = new XmlElementLookup(element);
                    ParserFunctions.TryParseEnum<CommodityTypes>(element.GetAttribute("com"),
                        out types, CommodityTypes.Undefined);
                    ActionData data = new ActionData(element.GetAttribute("key"),
                        types, ProductVersion.BaseGame, table, isEp5Installed);
                    ActionData.Add(data);
                }
            }
        }

        public static void AddPetInteractions(Sim p)
        {
            p.AddInteraction(EWEnlistApprentice.Singleton, true);
            p.AddInteraction(EWDismissApprentice.Singleton, true);
            p.AddInteraction(MentorFishing.Singleton, true);
            p.AddInteraction(MentorFighting.Singleton, true);
            p.AddInteraction(MentorHerbLore.Singleton, true);
            p.AddInteraction(MentorMedicine.Singleton, true);
        }

        public static void AddPlantableInventoryInteractions(Ingredient i)
        {
            i.AddInventoryInteraction(EWPetPlantSeed.Singleton);
            i.AddInventoryInteraction(EWPetTreatNausea.Singleton);
            i.AddInventoryInteraction(EWPetTreatWound.Singleton);
            i.AddInventoryInteraction(EWPetTreatPneumonia.Singleton);
            i.AddInventoryInteraction(EWPetTreatPetstilence.Singleton);
        }

        public static void AddPlantInteractions(Plant p)
        {
            p.AddInteraction(EWPetWatchPlant.Singleton, true);
            p.AddInteraction(EWPetHarvest.Singleton, true);
            p.AddInteraction(EWPetWeedPlant.Singleton, true);
            p.AddInteraction(EWPetWaterPlant.Singleton, true);
            p.AddInteraction(EWPetDisposePlant.Singleton, true);
        }

        public static ListenerAction OnObjectChanged(Event e)
        {
            Sim sim = e.Actor as Sim;
            Plant p = e.TargetObject as Plant;
            if (p != null)
            {
                AddPlantInteractions(p);
            }

            Ingredient i = e.TargetObject as Ingredient;

            if (i != null)
            {
                PlantableComponent pc = i.Plantable;
                if (pc != null)
                {
                    i.AddInteraction(EWPetPickUpPlantable.Singleton, true);

                    if (sim.IsPet && i.InInventory)
                    {
                        bool has_plantable_interactions = false;
                        foreach (InteractionObjectPair pair in e.TargetObject.GetAllInventoryInteractionsForActor(sim))
                        {
                            if (pair.InteractionDefinition.GetType() == EWPetPlantSeed.Singleton.GetType())
                            {
                                has_plantable_interactions = true;
                                break;
                            }
                        }
                        if (!has_plantable_interactions)
                        {
                            AddPlantableInventoryInteractions(i);
                        }
                    }
                }
            }
            else if (e.TargetObject.CatHuntingComponent != null && sim.IsPet && e.TargetObject.InInventory)
            {
                if (e.TargetObject.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Rodent)
                {
                    bool has_treatFleas = false;
                    foreach (InteractionObjectPair pair in e.TargetObject.GetAllInventoryInteractionsForActor(sim))
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

        public static void NotifyDebugState()
        {
            Config.DebugNote("Pet Warrior Debug Mode ON");
        }

    }
}