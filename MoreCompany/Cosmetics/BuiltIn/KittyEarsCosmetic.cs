namespace MoreCompany.Cosmetics.BuiltIn
{
    public class KittyEarsCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/kittyearscosmetic.prefab";
        public override string cosmeticId => "builtin.kittyears";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/kittyearsicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}