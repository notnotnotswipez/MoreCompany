namespace MoreCompany.Cosmetics.BuiltIn
{
    public class FancyPantsCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/mustacheandmonocle.prefab";
        public override string cosmeticId => "builtin.fancypants";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/gunholstericon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}