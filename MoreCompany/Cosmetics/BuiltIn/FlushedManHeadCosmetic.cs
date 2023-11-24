namespace MoreCompany.Cosmetics.BuiltIn
{
    public class FlushedManHeadCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/flushedmanheadcosmetic.prefab";
        public override string cosmeticId => "builtin.flushedmanhead";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/flushedmancosmeticicon.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}