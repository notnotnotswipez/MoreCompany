namespace MoreCompany.Cosmetics.BuiltIn
{
    public class DenimHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/denimhatcosmetic.prefab";
        public override string cosmeticId => "builtin.denimhat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/denimcapicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}