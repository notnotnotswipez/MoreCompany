namespace MoreCompany.Cosmetics.BuiltIn
{
    public class PartyGlassesCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/glassescosmetic.prefab";
        public override string cosmeticId => "builtin.partyglasses";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/glassesicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}