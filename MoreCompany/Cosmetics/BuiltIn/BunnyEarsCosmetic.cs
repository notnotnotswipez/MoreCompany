namespace MoreCompany.Cosmetics.BuiltIn
{
    public class BunnyEarsCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/bunnyearscosmetic.prefab";
        public override string cosmeticId => "builtin.bunnyears";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/bunnyearsicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}