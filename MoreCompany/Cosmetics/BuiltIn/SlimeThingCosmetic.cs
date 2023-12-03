namespace MoreCompany.Cosmetics.BuiltIn
{
    public class SlimeThingCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/slimething.prefab";
        public override string cosmeticId => "builtin.slimething";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/slimethingicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}