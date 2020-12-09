using System;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
//using static Sims3.Gameplay.Core.Terrain;
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

            // Replace cat fishing interaction
            Terrain.CatFishHere.Singleton = new EWCatFishHere.Definition();
        }

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

            Fish[] objects = Queries.GetObjects<Fish>();
            foreach (Fish val in objects)
            {
                if (val.CatHuntingComponent != null)
                {
                    // Separate out eating fish from prey.
                    // I don't think dogs eat fish, or anyway it's fine if they can't.
                    val.RemoveInteractionByType(PetEatPrey.Singleton);
                    val.AddInteraction(EWCatEatFish.Singleton);
                }
            }
            EventTracker.AddListener(EventTypeId.kInventoryObjectAdded, new ProcessEventDelegate(OnObjectChanged));
            EventTracker.AddListener(EventTypeId.kObjectStateChanged, new ProcessEventDelegate(OnObjectChanged));
        }

        public static ListenerAction OnAteFish(Event e)
        {
            StyledNotification.Show(new StyledNotification.Format("AteFish Happened",
                StyledNotification.NotificationStyle.kGameMessagePositive));
            return ListenerAction.Keep;
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
                StyledNotification.Show(new StyledNotification.Format("ERROR in EWCatEatFish: " + ex.Message,
                    StyledNotification.NotificationStyle.kGameMessagePositive));
            }
            return ListenerAction.Keep;
        }


    }
}
