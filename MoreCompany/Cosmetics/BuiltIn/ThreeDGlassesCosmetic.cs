namespace MoreCompany.Cosmetics.BuiltIn
{
    public class ThreeDGlassesCosmetic : CosmeticGeneric {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/3dglassescosmetic.prefab";
        public override string cosmeticId => "builtin.3dglasses";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/3dglassesicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}