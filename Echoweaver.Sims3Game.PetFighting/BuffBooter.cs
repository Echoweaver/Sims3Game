using Sims3.Gameplay.ActorSystems;
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
            StyledNotification.Show(new StyledNotification.Format("BuffBooter",
                StyledNotification.NotificationStyle.kDebugAlert));
            AddBuffs(null);
            UIManager.NewHotInstallStoreBuffData += new Sims3.UI.UIManager.NewHotInstallStoreBuffCallback(AddBuffs);
        }

        public void AddBuffs(ResourceKey[] resourceKeys)
        {
            StyledNotification.Show(new StyledNotification.Format("Loading Buffs",
                StyledNotification.NotificationStyle.kDebugAlert));
            ResourceKey key = new ResourceKey(ResourceUtils.HashString64("EWPetFighting_Buffs"), 0x0333406C, 0x0);
            XmlDbData data = XmlDbData.ReadData(key, false);
            if (data != null)
            {
                BuffManager.ParseBuffData(data, true);
            }
        }
    }
}
