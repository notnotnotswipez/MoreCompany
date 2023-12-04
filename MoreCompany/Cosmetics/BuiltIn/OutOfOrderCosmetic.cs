namespace MoreCompany.Cosmetics.BuiltIn
{
    public class OutOfOrderCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/outofordercosmetic.prefab";
        public override string cosmeticId => "builtin.outoforder";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/outofordericon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.CHEST;
    }
}