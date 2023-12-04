namespace MoreCompany.Cosmetics.BuiltIn
{
    public class MimeMaskCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/mimemaskcosmetic.prefab";
        public override string cosmeticId => "builtin.mimemask";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/mimemaskicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}