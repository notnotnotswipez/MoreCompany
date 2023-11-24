namespace MoreCompany.Cosmetics.BuiltIn
{
    public class GunHolsterCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/gunholstercosmetic.prefab";
        public override string cosmeticId => "builtin.gunholster";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/gunholstericon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HIP;
    }
}