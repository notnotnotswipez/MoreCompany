namespace MoreCompany.Cosmetics.BuiltIn
{
    public class PlagueMaskCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/plaguemaskcosmetic.prefab";
        public override string cosmeticId => "builtin.plaguemask";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/plaguemaskicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}