﻿using System;
using System.Collections.Generic;
using System.Xml;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
//using Sims3.Gameplay.DreamsAndPromises;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using static Sims3.Gameplay.ObjectComponents.CatHuntingComponent;
using Queries = Sims3.Gameplay.Queries;

namespace Echoweaver.Sims3Game
{
    public class EWCatFishingSkillLoader
    {
        static bool HasBeenLoaded = false;

        [Tunable]
        protected static bool kInstantiator = false;

        static EWCatFishingSkillLoader()
        {
            LoadSaveManager.ObjectGroupsPreLoad += OnPreLoad;
            World.sOnWorldLoadFinishedEventHandler += new EventHandler(OnWorldLoadFinishedHandler);
//            World.sOnStartupAppEventHandler += new EventHandler(OnStartupAppHandler);
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

        //public static void OnStartupAppHandler(object sender, System.EventArgs e)
        //{
        //    AddDreamEnums();
        //    ParseEWCatFishingPrimitives();
        //}

        public static void OnWorldLoadFinishedHandler(object sender, System.EventArgs e)
        {
            // Add custom fishing interaction that uses custom fishing skill
            // TODO: Remove old interaction
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
                InteractionTuning eatTuning = AutonomyTuning.GetTuning("Echoweaver.Sims3Game+EWCatEatFish+Definition",
                    "Sims3.Gameplay.Interfaces.ICatPrey");
                if (eatTuning == null)
                {
                    InteractionTuning newTuning = new InteractionTuning();
                    InteractionTuning oldTuning = AutonomyTuning.GetTuning("Sims3.Gameplay.ObjectComponents.CatHuntingComponent+PetEatPrey+Definition",
                        "Sims3.Gameplay.Interfaces.ICatPrey");
                    foreach (CommodityChange mOldOutput in oldTuning.mTradeoff.mOutputs)
                    {
                        if (mOldOutput.Commodity == CommodityKind.Hunger)
                        {
                            newTuning.mTradeoff.mOutputs.Add(mOldOutput);
                        }
                    }
                    AutonomyTuning.AddTuning("Echoweaver.Sims3Game+EWCatEatFish+Definition",
                    "Sims3.Gameplay.Interfaces.ICatPrey", newTuning);
                }
            } catch (Exception ex)
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR loading EWCatEatFish tuning: " + ex.Message,
                    StyledNotification.NotificationStyle.kDebugAlert));
            }


            Fish[] objects = Queries.GetObjects<Fish>();
            foreach (Fish val in objects)
            {
                if (val.CatHuntingComponent != null)
                {
                    // Separate out eating fish from land prey.
                    // I don't think dogs eat fish, or anyway it's fine if they can't.
                    val.RemoveInteractionByType(PetEatPrey.Singleton);
                    val.AddInteraction(EWCatEatFish.Singleton);
                }
            }
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));
        }

        public static ListenerAction OnObjectChanged(Event e)
        {
            try
            {
                Fish newFish = e.TargetObject as Fish;
                if (newFish != null)
                {
                    foreach (InteractionObjectPair pair in newFish.Interactions)
                    {
                        if (pair.InteractionDefinition.GetType() == EWCatEatFish.Singleton.GetType())
                        {
                            return ListenerAction.Keep;
                        }
                    }
                    newFish.RemoveInteractionByType(PetEatPrey.Singleton);
                    newFish.AddInteraction(EWCatEatFish.Singleton);

                }
            } catch (Exception ex)
            {
                StyledNotification.Show(new StyledNotification.Format("ERROR assigning EWCatEatFish interaction: " + ex.Message,
                    StyledNotification.NotificationStyle.kDebugAlert));
            }
            return ListenerAction.Keep;
        }

        //public static void AddDreamEnums()
        //{
        //    EnumParser parser;
        //    Dictionary<Type, EnumParser> dictionary_ic = ParserFunctions.sCaseInsensitiveEnumParsers;
        //    Dictionary<Type, EnumParser> dictionary_c = ParserFunctions.sCaseSensitiveEnumParsers;
        //    string[] new_enum_names = { "play_in_water_echoweaver" };
        //    object[] new_enum_values = { 0x0E0E0DB1 };
        //    if (!dictionary_ic.TryGetValue(typeof(DreamNames), out parser))
        //    {
        //        parser = new EnumParser(typeof(DreamNames), true);
        //        dictionary_ic.Add(typeof(DreamNames), parser);
        //    }

        //    for (int i = 0; i < new_enum_names.Length; i++)
        //        parser.mLookup.Add(new_enum_names[i].ToLowerInvariant(), new_enum_values[i]);

        //    if (!dictionary_c.TryGetValue(typeof(DreamNames), out parser))
        //    {
        //        parser = new EnumParser(typeof(DreamNames), true);
        //        dictionary_c.Add(typeof(DreamNames), parser);
        //    }
        //    for (int i = 0; i < new_enum_names.Length; i++)
        //        parser.mLookup.Add(new_enum_names[i], new_enum_values[i]);
        //}

        //public static void ParseEWCatFishingPrimitives()
        //{
        //    DreamsAndPromisesManager.sNodePrimitves = new Dictionary<uint, DreamNodePrimitive>();
        //    List<DreamNodePrimitive> cachePrimitives = new List<DreamNodePrimitive>();
        //    XmlDbData xmlDbData = XmlDbData.ReadData("Echoweaver_cat_fishing_dreams");
        //    if (xmlDbData != null)
        //    {
        //        DreamsAndPromisesManager.ParseNodePrimitivesFromXmlDbData(xmlDbData, ref cachePrimitives, isStore: false);
        //    }
        //    //uint num = 16777216u;
        //    //ResourceKey key = default(ResourceKey);
        //    //((ResourceKey)(ref key))._002Ector(ResourceUtils.HashString64("DreamsAndPromisesNodes_store"), 3162301119u, num);
        //    //XmlDbData xmlDbData2 = XmlDbData.ReadData(key, bSuppressLogs: false);
        //    //if (xmlDbData2 != null)
        //    //{
        //    //    ParseNodePrimitivesFromXmlDbData(xmlDbData2, ref cachePrimitives, isStore: true);
        //    //}
        //    //if (CacheManager.get_IsCachingEnabled())
        //    //{
        //    //    CacheManager.SaveTuningData("DreamsAndPromisesPrimitives", (object)cachePrimitives);
        //    //}
        //}

        //public static void ParseEWCatFishingDreamTrees()
        //{
        //    Dictionary<string, XmlElement> instanceDefults = DreamsAndPromisesManager.ParseDefaults();
        //    //SetupDefaultNodeInstance(instanceDefults);
        //    //if (CacheManager.get_IsCachingEnabled() && LoadCachedTrees())
        //    //{
        //    //    return;
        //    //}
        //    //sDreamTrees = new Dictionary<ulong, DreamTree>();
        //    List<DreamTree> cacheTrees = new List<DreamTree>();
        //    ResourceKey key = new ResourceKey(0xA43129F3D1D0E08C, 0x333406C, 0x0);
        //    DreamsAndPromisesManager.ParseDreamTreeByKey(key, instanceDefults, ref cacheTrees);
        //}

    }
}
