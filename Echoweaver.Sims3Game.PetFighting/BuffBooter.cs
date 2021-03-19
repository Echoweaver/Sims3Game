using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

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
            ResourceKey key = new ResourceKey(ResourceUtils.HashString64("EW_Warriorcats_Buffs"), 0x0333406C, 0x0);
            XmlDbData data = XmlDbData.ReadData(key, false);
            if (data != null)
            {
                BuffManager.ParseBuffData(data, true);
            }
        }
    }
}
