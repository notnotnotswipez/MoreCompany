namespace MoreCompany.Cosmetics.BuiltIn
{
    public class RudolphCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/rudolphcosmetic.prefab";
        public override string cosmeticId => "builtin.rudolph";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/rudolphhatcosmetic.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}