using MyDefense.Lobby;
using NUnit.Framework;

public class LobbyMaterialCurrencyFormatterTests
{
    [Test]
    public void VisibleCurrencyNamesMatchConfirmedPolicy()
    {
        Assert.AreEqual("왹져 DNA  1,234", LobbyMaterialCurrencyFormatter.FormatUniversalPiece(1234));
        Assert.AreEqual("성장 세포  56", LobbyMaterialCurrencyFormatter.FormatGrowthCell(56));
        Assert.AreEqual("변이 촉매  7", LobbyMaterialCurrencyFormatter.FormatMutationCatalyst(7));
    }
}
