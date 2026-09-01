namespace MyDefense.Lobby
{
    public static class LobbyMaterialCurrencyFormatter
    {
        public const string UniversalPieceDisplayName = "왹져 DNA";
        public const string GrowthCellDisplayName = "성장 세포";
        public const string MutationCatalystDisplayName = "변이 촉매";

        public static string FormatUniversalPiece(int amount) => UniversalPieceDisplayName + "  " + amount.ToString("N0");
        public static string FormatGrowthCell(int amount) => GrowthCellDisplayName + "  " + amount.ToString("N0");
        public static string FormatMutationCatalyst(int amount) => MutationCatalystDisplayName + "  " + amount.ToString("N0");
    }
}
