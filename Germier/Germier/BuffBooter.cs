using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Echoweaver.Sims3Game.Germier
{
    public class BuffBooter
    {

        public BuffBooter()
        {
        }

        public void LoadBuffData()
        {
            this.AddBuffs(null);
            Sims3.UI.UIManager.NewHotInstallStoreBuffData += new Sims3.UI.UIManager
                .NewHotInstallStoreBuffCallback(this.AddBuffs);
        }

        public void AddBuffs(ResourceKey[] resourceKeys)
        {
            ResourceKey key = new ResourceKey(ResourceUtils.HashString64("EWGermySymptoms_Buffs"),
                0x0333406C, 0x0);
            XmlDbData data = XmlDbData.ReadData(key, false);
            if (data != null)
            {
                BuffManager.ParseBuffData(data, true);
            }
        }

    }
}
