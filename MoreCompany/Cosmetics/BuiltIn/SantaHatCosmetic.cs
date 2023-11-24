namespace MoreCompany.Cosmetics.BuiltIn
{
    public class SantaHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/santahatcosmetic.prefab";
        public override string cosmeticId => "builtin.santahat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/santahatcosmeticicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}