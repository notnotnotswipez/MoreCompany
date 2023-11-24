namespace MoreCompany.Cosmetics.BuiltIn
{
    public class FancyGlassesCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/fancyglassescosmetic.prefab";
        public override string cosmeticId => "builtin.fancyglasses";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/fancyglassesicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}