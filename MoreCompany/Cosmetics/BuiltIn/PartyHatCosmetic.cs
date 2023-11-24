namespace MoreCompany.Cosmetics.BuiltIn
{
    public class PartyHatCosmetic : CosmeticGeneric
    {
        public override string gameObjectPath => "assets/morecompanyassets/cosmetics/partyhatcosmetic.prefab";
        public override string cosmeticId => "builtin.partyhat";
        public override string textureIconPath => "assets/morecompanyassets/cosmetics/party_hat_render.png";
        
        public override CosmeticType cosmeticType => CosmeticType.HAT;
    }
}