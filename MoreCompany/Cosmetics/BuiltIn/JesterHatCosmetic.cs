namespace MoreCompany.Cosmetics.BuiltIn
{
    public class JesterHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/jesterhatcosmetic.prefab";
        public override string cosmeticId => "builtin.jesterhat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/jesterhaticon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}