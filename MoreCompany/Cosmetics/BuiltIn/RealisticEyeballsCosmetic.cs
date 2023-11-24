namespace MoreCompany.Cosmetics.BuiltIn
{
    public class RealisticEyeballsCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/realisticeyeballscosmetic.prefab";
        public override string cosmeticId => "builtin.eyeballs";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/eyecosmeticicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}