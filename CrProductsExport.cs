using System.Text;
using System.Text.RegularExpressions;

namespace ShellNotesApp;

internal static class CrProductsExport
{
    private const string FileNamePrefix = "CRProducts_SSC_BDS";
    private const string MarketCode = "XK";
    private const string EntityCode = "999999";
    private static readonly Regex DecimalWithCommaRegex = new(@"\d,\d", RegexOptions.Compiled);

    public static string BuildFileName(DateTime timestamp)
        => $"{FileNamePrefix}_{MarketCode}_{EntityCode}_{timestamp:yyyyMMddTHHmmss}.csv";

    public static void ExportLines(IEnumerable<string> lines, string path)
    {
        var normalizedLines = lines
            .Select(static line => line?.TrimEnd('\r', '\n') ?? string.Empty)
            .ToList();

        Validate(normalizedLines);

        File.WriteAllLines(path, normalizedLines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void Validate(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Nuk ka të dhëna për eksport.");
        }

        const int requiredColumns = 23;
        var expectedHeader = "BUID;BUCODE;INVENTORYITEMID;EXTERNALID;ITEMNAME;ITEMSTATUS;ORGUNITOWNERID;ORGUNITOWNERNAME;PRODUCTOWNERSHIP;PRODUCTSELLINGTYPE;LOCALSUBCATEGORYID;LOCALSUBCATEGORYCODE;LOCALSUBCATEGORYNAME;TAXID;TAXNAME;TAXPERCENT;MANUFACTURERID;MANUFACTURERCODE;MANUFACTURERNAME;BUSINESSUNITGRPID;BUSINESSUNITGRPNAME;BRANDCODE;BRANDNAME";

        if (!string.Equals(lines[0], expectedHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Header-i i CRProducts nuk përputhet me specifikimin me 23 kolona.");
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.EndsWith(';'))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka delimiter shtesë në fund.");
            }

            var columns = line.Split(';');
            if (columns.Length != requiredColumns)
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka {columns.Length} kolona, kërkohen {requiredColumns}.");
            }

            if (line.Contains("\"NULL\"", StringComparison.OrdinalIgnoreCase) ||
                line.Contains(";NULL;", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} përmban tekstin e palejuar 'NULL'.");
            }

            if (DecimalWithCommaRegex.IsMatch(line))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} përmban presje si ndarës decimal.");
            }
        }
    }
}
