using System.Text;
using System.Text.RegularExpressions;

namespace ShellNotesApp;

internal static class CrProductsExport
{
    private const string FileNamePrefix = "CRProducts_SSC_BDS";
    private const string MarketCode = "XK";
    private const string EntityCode = "999999";

    private static readonly Regex NumericDigitsOrDecimalRegex = new(@"^\d+(\.\d+)?$", RegexOptions.Compiled);

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
        const int productSellingTypeIndex = 9;
        const int taxPercentIndex = 15;

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

            var columns = SplitSemicolonCsvLine(line);
            if (columns.Count != requiredColumns)
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka {columns.Count} kolona, kërkohen {requiredColumns}.");
            }

            if (i == 0)
            {
                continue;
            }

            if (line.Contains("\"NULL\"", StringComparison.OrdinalIgnoreCase) ||
                line.Contains(";NULL;", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} përmban tekstin e palejuar 'NULL'.");
            }

            var productSellingType = Unquote(columns[productSellingTypeIndex]);
            if (!string.IsNullOrEmpty(productSellingType) && !string.Equals(productSellingType, "g", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka PRODUCTSELLINGTYPE të palejuar. Lejohen vetëm bosh ose 'g'.");
            }

            var taxPercent = Unquote(columns[taxPercentIndex]);
            if (taxPercent.Contains(','))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka decimal me presje. Përdorni pikë si ndarës decimal.");
            }

            if (!NumericDigitsOrDecimalRegex.IsMatch(taxPercent))
            {
                throw new InvalidOperationException($"Rreshti {i + 1} ka vlerë numerike të pavlefshme te TAXPERCENT.");
            }
        }
    }

    private static List<string> SplitSemicolonCsvLine(string line)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }

                continue;
            }

            if (c == ';' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        parts.Add(current.ToString());
        return parts;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1].Replace("\"\"", "\"");
        }

        return value;
    }
}
