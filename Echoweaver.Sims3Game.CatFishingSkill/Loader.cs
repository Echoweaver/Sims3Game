using System;
using System.Collections.Generic;
using System.Xml;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.DreamsAndPromises;
//using Sims3.Gameplay.DreamsAndPromises;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game.CatFishing
{
    public class Loader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static Loader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
            //World.sOnStartupAppEventHandler += new EventHandler(OnStartupAppHandler);
        }

        public static void OnPreLoad()
        {
            if (HasBeenLoaded) return; // you only want to run it once per gameplay session
            HasBeenLoaded = true;

            // fill this in with the resourcekey of your SKIL xml
            XmlDbData data = XmlDbData.ReadData(new ResourceKey(0x67503AA43670DA12, 0xA8D58BE5, 0x00000000), false);

            if (data == null)
            {
                return;
            }
            SkillManager.ParseSkillData(data, true);
        }

        public static void OnStartupAppHandler(object sender, System.EventArgs e)
        {
            // Dream trees do not work as of now.
            AddDreamEnums();
            ParseEWCatFishingPrimitives();
        }

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            // Add custom fishing interaction that uses custom fishing skill
            if (Terrain.Singleton != null)
            {
                Terrain.Singleton.RemoveInteractionByType(Terrain.CatFishHere.Singleton);
                Terrain.Singleton.AddInteraction(EWCatFishHere.Singleton);
                Terrain.Singleton.AddInteraction(EWCatInspectWater.Singleton);
                Terrain.Singleton.AddInteraction(EWCatPlayInWater.Singleton);
                Terrain.Singleton.AddInteraction(EWCatFishAWhile.Singleton);
            }

            try
            {
                // If there's no existing tuning for EWCatEatFish, copy over the Hunger output from PetEatPrey
                InteractionTuning eatTuning = AutonomyTuning.GetTuning(EWCatEatFish.Singleton.GetType().FullName,
                    "Sims3.Gameplay.Interfaces.ICatPrey");
                if (eatTuning == null)
                {
                    InteractionTuning oldTuning = AutonomyTuning.GetTuning(PetEatPrey.Singleton.GetType().FullName,
                        "Sims3.Gameplay.Interfaces.ICatPrey");
                    AutonomyTuning.AddTuning(EWCatEatFish.Singleton.GetType().FullName,
                    "Sims3.Gameplay.Interfaces.ICatPrey", oldTuning);
                }
            }
            catch (Exception ex)
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR loading EWCatEatFish tuning: " + ex.Message,
                    StyledNotification.NotificationStyle.kDebugAlert));
            }

            MinorPet[] objects = Queries.GetObjects<MinorPet>();
            foreach (MinorPet val in objects)
            {
                if (val.CatHuntingComponent != null)
                {
                    val.AddInventoryInteraction(EWCatDropHere.Singleton);

                    if (val.CatHuntingComponent.mPreyData.PreyType == CatHuntingSkill.PreyType.Fish)
                    {
                        // Separate out eating fish from land prey.
                        val.RemoveInteractionByType(PetEatPrey.Singleton);
                        val.AddInteraction(EWCatEatFish.Singleton);
                    }
                }
            }
            Fish[] fish = Queries.GetObjects<Fish>();
            foreach (Fish f in fish)
            {
                if (f.CatHuntingComponent != null)
                {
                    // Separate out eating fish from land prey.
                    f.RemoveInteractionByType(PetEatPrey.Singleton);
                    f.AddInteraction(EWCatEatFish.Singleton);
                }
            }
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));
        }

        public static ListenerAction OnObjectChanged(Event e)
        {
            try
            {
                if (e.TargetObject.CatHuntingComponent != null)
                {
                    if (e.TargetObject.InInventory)
                    {
                        bool hasDrop = false;
                        GameObject g = e.TargetObject as GameObject;
                        foreach (InteractionObjectPair pair in g.GetAllInventoryInteractionsForActor(e.Actor))
                        {
                            if (pair.InteractionDefinition.GetType() == EWCatDropHere.Singleton.GetType())
                            {
                                hasDrop = true;
                                break;
                            }
                        }
                        if (!hasDrop)
                        {
                            g.AddInventoryInteraction(EWCatDropHere.Singleton);
                        }
                    }
                    Fish newFish = e.TargetObject as Fish;
                    if (newFish != null)
                    {
                        bool hasEat = false;
                        foreach (InteractionObjectPair pair in newFish.Interactions)
                        {
                            if (pair.InteractionDefinition.GetType() == EWCatEatFish.Singleton.GetType())
                            {
                                hasEat = true;
                                break;
                            }
                        }
                        if (!hasEat)
                        {
                            newFish.RemoveInteractionByType(PetEatPrey.Singleton);
                            newFish.AddInteraction(EWCatEatFish.Singleton, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR assigning EWCatEatFish interaction: " + ex.Message,
                    StyledNotification.NotificationStyle.kDebugAlert));
            }
            return ListenerAction.Keep;
        }

        public static void AddDreamEnums()
        {
            EnumParser parser;
            Dictionary<Type, EnumParser> dictionary_ic = ParserFunctions.sCaseInsensitiveEnumParsers;
            Dictionary<Type, EnumParser> dictionary_c = ParserFunctions.sCaseSensitiveEnumParsers;
            string[] new_enum_names = { "play_in_water_echoweaver" };
            object[] new_enum_values = { 0x0E0E0DB1 };
            if (!dictionary_ic.TryGetValue(typeof(DreamNames), out parser))
            {
                parser = new EnumParser(typeof(DreamNames), true);
                dictionary_ic.Add(typeof(DreamNames), parser);
            }

            for (int i = 0; i < new_enum_names.Length; i++)
                parser.mLookup.Add(new_enum_names[i].ToLowerInvariant(), new_enum_values[i]);

            if (!dictionary_c.TryGetValue(typeof(DreamNames), out parser))
            {
                parser = new EnumParser(typeof(DreamNames), true);
                dictionary_c.Add(typeof(DreamNames), parser);
            }
            for (int i = 0; i < new_enum_names.Length; i++)
                parser.mLookup.Add(new_enum_names[i], new_enum_values[i]);
        }

        public static void ParseEWCatFishingPrimitives()
        {
            //sNodePrimitves = new Dictionary<uint, DreamNodePrimitive>();

            List<DreamNodePrimitive> cachePrimitives = new List<DreamNodePrimitive>();
            XmlDbData xmlDbData = XmlDbData.ReadData(new ResourceKey(0xC54CBCBB14C4EC27, 0x0333406C, 0x00000000), false);
            if (xmlDbData != null)
            {
                DreamsAndPromisesManager.ParseNodePrimitivesFromXmlDbData(xmlDbData, ref cachePrimitives, isStore: false);
            }

        }

        public static void ParseEWCatFishingDreamTrees()
        {
            Dictionary<string, XmlElement> instanceDefults = DreamsAndPromisesManager.ParseDefaults();
            List<DreamTree> cacheTrees = new List<DreamTree>();
            uint[] array = new uint[2];
            uint[] array2 = array;
            ResourceKey item = new ResourceKey(0xA43129F3D1D0E08C, 0x0604ABDA, 0x0);
            array2[0]++;
            if (DreamsAndPromisesManager.ParseDreamTreeByKey(item, instanceDefults, ref cacheTrees))
            {
                array2[1]++;
            }
        }

    }
}
