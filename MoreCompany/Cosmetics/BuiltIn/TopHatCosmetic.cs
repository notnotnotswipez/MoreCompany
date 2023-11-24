namespace MoreCompany.Cosmetics.BuiltIn
{
    public class TopHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/tophatcosmetic.prefab";
        public override string cosmeticId => "builtin.tophat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/tophatcosmeticicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}