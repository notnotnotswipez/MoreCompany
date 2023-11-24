namespace MoreCompany.Cosmetics.BuiltIn
{
    public class WatchCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/watchcosmetic.prefab";
        public override string cosmeticId => "builtin.watch";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/watchicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.R_LOWER_ARM;
    }
}