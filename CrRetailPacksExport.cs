namespace ShellNotesApp;

internal static class CrRetailPacksExport
{
    private const string FileNamePrefix = "CRRetailPacks_QBS";
    private const string MarketCode = "XK";
    private const string EntityCode = "999999";

    public static string BuildFileName(DateTime timestamp)
        => $"{FileNamePrefix}_{MarketCode}_{EntityCode}_{timestamp:yyyy-MM-dd-HH-mm-ss}.csv";
}
