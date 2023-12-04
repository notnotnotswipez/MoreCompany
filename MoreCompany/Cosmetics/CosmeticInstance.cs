using MoreCompany.Utils;
using UnityEngine;

namespace MoreCompany.Cosmetics
{
    public class CosmeticInstance : MonoBehaviour
    {
        public CosmeticType cosmeticType;
        public string cosmeticId;
        public Texture2D icon;
    }

    public class CosmeticGeneric
    {
        public virtual string gameObjectPath { get; }
        public virtual string cosmeticId { get;}
        public virtual string textureIconPath { get; }
        public virtual CosmeticType cosmeticType { get;  }

        public void LoadFromBundle(AssetBundle bundle)
        {
            GameObject cosmeticInstance = bundle.LoadPersistentAsset<GameObject>(gameObjectPath);
            Texture2D icon = bundle.LoadPersistentAsset<Texture2D>(textureIconPath);

            CosmeticInstance cosmeticInstanceBehavior = cosmeticInstance.AddComponent<CosmeticInstance>();
            cosmeticInstanceBehavior.cosmeticId = cosmeticId;
            cosmeticInstanceBehavior.icon = icon;
            cosmeticInstanceBehavior.cosmeticType = cosmeticType;
            
            MainClass.StaticLogger.LogInfo("Loaded cosmetic: " + cosmeticId + " from bundle: " + bundle.name);
            CosmeticRegistry.cosmeticInstances.Add(cosmeticId, cosmeticInstanceBehavior);
        }
    }

    public enum CosmeticType
    {
        HAT,
        WRIST,
        CHEST,
        R_LOWER_ARM,
        HIP,
        L_SHIN,
        R_SHIN
    }
}