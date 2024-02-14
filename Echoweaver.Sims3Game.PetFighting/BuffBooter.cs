using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.PetFighting
{
    public class BuffBooter
    {

        public BuffBooter()
        {
        }

        public void LoadBuffData()
        {
            this.AddBuffs(null);
            Sims3.UI.UIManager.NewHotInstallStoreBuffData += new Sims3.UI.UIManager.NewHotInstallStoreBuffCallback(this.AddBuffs);
        }

        public void AddBuffs(ResourceKey[] resourceKeys)
        {
            ResourceKey key = new ResourceKey(ResourceUtils.HashString64("EWPetFighting_Buffs"), 0x0333406C, 0x0);
            XmlDbData data = XmlDbData.ReadData(key, false);
            if (data != null)
            {
                BuffManager.ParseBuffData(data, true);
            }
        }

        public static void addCommodityMultiplier(Sim s, CommodityKind commodity, float multiplier)
        {
            BuffCommodityDecayModifier.BuffInstanceCommodityDecayModifier buffModifier
                = s.BuffManager.GetElement(BuffNames.CommodityDecayModifier)
                as BuffCommodityDecayModifier.BuffInstanceCommodityDecayModifier;

            if (buffModifier == null)
            {
                BuffManager.BuffDictionary.TryGetValue((ulong)BuffNames.CommodityDecayModifier,
                    out BuffInstance value);
                if (value == null)
                {
                    Loader.DebugNote("ERROR Add Commodity Multiplier buff failed for: " + s.Name);
                    return;
                }

                buffModifier = (BuffCommodityDecayModifier.BuffInstanceCommodityDecayModifier)value;
                buffModifier.mTimeoutPaused = true;
                buffModifier.SetCustomBuffInstanceName("EWDecayModifierBuff");
                buffModifier.SetCustomBuffInstanceDescription("EWDecayModifierBuff_BuffDescription");
                buffModifier.SetThumbnail("moodlet_whackedout", 0x48000000, s);
                s.BuffManager.AddBuff(buffModifier);
            }
            buffModifier.AddCommodityMultiplier(commodity, multiplier);
        }

        public static void removeCommodityMultiplier(Sim s, CommodityKind commodity, float multiplier)
        {
            BuffCommodityDecayModifier.BuffInstanceCommodityDecayModifier buffModifier
                = s.BuffManager.GetElement(BuffNames.CommodityDecayModifier)
                as BuffCommodityDecayModifier.BuffInstanceCommodityDecayModifier;
            if (buffModifier == null)
            {
                Loader.DebugNote("ERROR Remove " + commodity + " multiplier failed for: " + s.Name);
                return;
            }

            buffModifier.AddCommodityMultiplier(commodity, 1 / multiplier);
            if (buffModifier.GetCommodityMultiplier(commodity) == 1f)
            {
                buffModifier.mCommodityDecayMultipliers.Remove(commodity);
            }

            if (buffModifier.mCommodityDecayMultipliers.Count == 0)
            {
                s.BuffManager.RemoveElement(buffModifier.Guid);
            }
        }
    }
}
