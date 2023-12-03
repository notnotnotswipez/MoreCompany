namespace MoreCompany.Cosmetics.BuiltIn
{
    public class HardHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/hardhatcosmetic.prefab";
        public override string cosmeticId => "builtin.hardhat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/hardhaticon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}