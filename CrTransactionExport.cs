using System.Text;

namespace ShellNotesApp;

internal static class CrTransactionExport
{
    private const string MarketCode = "XK";

    // ─── File naming ────────────────────────────────────────────────────────────

    /// <summary>Preview filename (shfaqet në SaveDialog kur eksportohet raporti preview).</summary>
    public static string BuildFileName(DateTime timestamp)
        => $"POSSales_{MarketCode}_PREVIEW_{timestamp:yyyyMMddTHHmmss}_{timestamp.AddDays(-1):yyyyMMdd}.txt";

    /// <summary>Filename per-site: POSSales_XK_{orgId}_{generatedTs}_{businessDate}.txt</summary>
    public static string BuildSiteFileName(int orgId, string siteCode, DateTime businessDate)
    {
        var ts = DateTime.Now;
        return $"POSSales_{MarketCode}_{orgId}_{ts:yyyyMMddTHHmmss}_{businessDate:yyyyMMdd}.txt";
    }

    // ─── Org list query ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kthen listën e organizatave që kanë shitje dje.
    /// Kolonat: Id (int), Code (nvarchar).
    /// </summary>
    public static readonly string OrgListQuery = """
        DECLARE @DateFrom DATE = CAST(GETDATE()-1 AS DATE);
        DECLARE @DateTo   DATE = CAST(GETDATE()-1 AS DATE);

        SELECT DISTINCT
            dm.OrganizataId            AS Id,
            CAST(dm.OrganizataId AS NVARCHAR(50)) AS Code
        FROM dbo.DaljaMallit dm
        WHERE dm.Data >= @DateFrom
          AND dm.Data <  DATEADD(DAY, 1, @DateTo)
        ORDER BY dm.OrganizataId;
        """;

    // ─── Main query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Query template me @OrganizataId = 1882 dhe datë = dje.
    /// Për export multi-org përdor BuildQuery(orgId).
    /// </summary>
    public static readonly string Query = BuildQuery(1882);

    /// <summary>Ndërton query-n e plotë për një organizatë të caktuar.</summary>
    public static string BuildQuery(int orgId) => $"""
        DECLARE @OrganizataId INT = {orgId};
        DECLARE @DateFrom     DATE = CAST(GETDATE()-1 AS DATE);
        DECLARE @DateTo       DATE = CAST(GETDATE()-1 AS DATE);
        DECLARE @CountryCode  NVARCHAR(3)  = N'356';
        DECLARE @SiteId       NVARCHAR(50) = CAST(@OrganizataId AS NVARCHAR(50));
        DECLARE @FileType     NVARCHAR(5)  = N'SALES';
        DECLARE @IncrementalCounter INT = 1;

        ;WITH
        -- 🔴 Gjej çiftet e korrektimeve (origjinal + stornim)
        CTE_Storno AS
        (
            SELECT DISTINCT
                   s.DaljaMallitId AS DaljaIdOrigjinale,
                   f.DaljaMallitId AS DaljaIdStorno
            FROM dbo.PMPTransaksionet s
            INNER JOIN dbo.PMPTransaksionet f
                ON f.KorrektimiNrManual = s.KorrektimiNrManual
            WHERE s.OrganizataId = @OrganizataId
              AND CONVERT(DATE,s.DataERegjistrimit) BETWEEN @DateFrom AND @DateTo
              AND s.KorrektimiNrManual IS NOT NULL
        ),
        -- 🔴 Lista e të gjitha faturave që duhet përjashtuar
        CTE_Exclude AS
        (
            SELECT DaljaIdOrigjinale AS DaljaId FROM CTE_Storno
            UNION
            SELECT DaljaIdStorno FROM CTE_Storno
        ),
        -- 🔴 Transaksionet valide
        CTE_Tx AS
        (
            SELECT dm.Id, dm.Data, dm.DataERegjistrimit,
                   dm.NumriArkes, dm.RegjistruarNga,
                   dm.KthimiMallitArsyejaId
            FROM dbo.DaljaMallit dm
            WHERE dm.OrganizataId = @OrganizataId
              AND dm.Data >= @DateFrom
              AND dm.Data < DATEADD(DAY,1,@DateTo)
              AND dm.Id NOT IN (SELECT DaljaId FROM CTE_Exclude)
        ),
        -- 🔴 Rreshtat
        CTE_Lines AS
        (
            SELECT
                dmd.Id LineDbId,
                dmd.DaljaMallitID TxId,
                dmd.NR LineItemId,
                dmd.ArtikulliId,
                a.Shifra ProductHostCode,
                a.Pershkrimi ProductDescription,
                a.GrupiMallitID CategoryId,
                CASE WHEN ISNUMERIC(dmd.Barkodi)=1 THEN dmd.Barkodi ELSE '' END ProductEAN,
                dmd.Sasia,
                ABS(dmd.Sasia) QtyAbs,
                CAST(dmd.QmimiShitjes AS DECIMAL(19,4)) UnitSalePrice,
                CAST(dmd.Tvsh AS DECIMAL(19,4)) VatPercent,
                CAST(ABS(dmd.Sasia)*dmd.QmimiShitjes AS DECIMAL(19,4)) OriginalSalesAmount,
                CAST(
                    (ABS(dmd.Sasia)*dmd.QmimiShitjes)
                    * (1-ISNULL(dmd.Rabati,0)/100.0)
                    * (1-ISNULL(dmd.EkstraRabati,0)/100.0)
                AS DECIMAL(19,4)) ExtendedSalesPrice,
                CAST(
                    (
                      (ABS(dmd.Sasia)*dmd.QmimiShitjes)
                      * (1-ISNULL(dmd.Rabati,0)/100.0)
                      * (1-ISNULL(dmd.EkstraRabati,0)/100.0)
                    )*(ISNULL(dmd.Tvsh,0)/100.0)
                AS DECIMAL(19,4)) ExtendedVAT,
                ROUND(
                    (dmd.QmimiShitjes
                     * (1-ISNULL(dmd.Rabati,0)/100.0)
                     * (1-ISNULL(dmd.EkstraRabati,0)/100.0)
                    )*(ISNULL(dmd.Tvsh,0)/100.0)
                ,3) UnitVAT,
                CASE WHEN ISNULL(dmd.Rabati,0)>0
                       OR ISNULL(dmd.EkstraRabati,0)>0
                     THEN 'd' ELSE 'o' END MarkDown
            FROM dbo.DaljaMallitDetale dmd
            INNER JOIN CTE_Tx tx ON tx.Id=dmd.DaljaMallitID
            LEFT JOIN dbo.Artikujt a ON a.Id=dmd.ArtikulliId
        ),
        CTE_Totals AS
        (
            SELECT TxId,
                   SUM(
                     CASE WHEN Sasia<0
                          THEN -(ExtendedSalesPrice+ExtendedVAT)
                          ELSE (ExtendedSalesPrice+ExtendedVAT)
                     END
                   ) TicketTotal
            FROM CTE_Lines
            GROUP BY TxId
        ),
        CTE_Windows AS
        (
            SELECT ISNULL(MIN(DataERegjistrimit), GETDATE()) WinStart,
                   ISNULL(MAX(DataERegjistrimit), GETDATE()) WinEnd
            FROM CTE_Tx
        ),
        -- Totalet për record-in 999 (trailer)
        CTE_Summary AS
        (
            SELECT
                (SELECT COUNT(*) FROM CTE_Tx)    AS TxCount,
                (SELECT COUNT(*) FROM CTE_Lines) AS LineCount,
                ISNULL((SELECT SUM(OriginalSalesAmount) FROM CTE_Lines), 0) AS HashTotal1
        ),
        AllRows AS
        (
            -- 000 – File Header
            SELECT 0 Sort1, 0 Sort2, 0 Sort3,
            CONCAT('000|',@CountryCode,'|',@SiteId,'|',@IncrementalCounter,'|',@FileType,'|',
                   CONVERT(CHAR(8),@DateFrom,112),'|',
                   CONVERT(CHAR(8),@DateFrom,112),'|',
                   REPLACE(CONVERT(CHAR(8),(SELECT WinStart FROM CTE_Windows),108),':',''),'|',
                   CONVERT(CHAR(8),@DateTo,112),'|',
                   REPLACE(CONVERT(CHAR(8),(SELECT WinEnd FROM CTE_Windows),108),':','')
            ) AS LineText

            UNION ALL

            -- 500 – Sales Transaction Header
            SELECT 1, tx.Id, 0,
            CONCAT('500|',
                   CONVERT(CHAR(8),tx.Data,112),'|',
                   REPLACE(CONVERT(CHAR(8),tx.DataERegjistrimit,108),':',''),'|',
                   tx.Id,'|',
                   ISNULL(tx.RegjistruarNga,0),'|',
                   ISNULL(tx.NumriArkes,0),'|',
                   '0|',
                   REPLACE(CONVERT(varchar(50),CAST(ISNULL(t.TicketTotal,0) AS DECIMAL(19,2))),',','.'),'|',
                   '|EUR||||',
                   ISNULL(tx.NumriArkes,0),'|IN'
            )
            FROM CTE_Tx tx
            LEFT JOIN CTE_Totals t ON t.TxId=tx.Id

            UNION ALL

            -- 501 – Sales Transaction Item
            SELECT 2, l.TxId, l.LineItemId,
            CONCAT('501|',
                   l.LineItemId,'|',l.TxId,'|',ISNULL(l.CategoryId,0),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.ExtendedSalesPrice AS DECIMAL(19,2))),',','.'),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.UnitSalePrice AS DECIMAL(19,2))),',','.'),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.ExtendedVAT AS DECIMAL(19,2))),',','.'),'|',
                   l.MarkDown,'|0|',
                   l.ProductEAN,'|',
                   l.ArtikulliId,'|',
                   REPLACE(l.ProductDescription,'|',' '),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.QtyAbs AS DECIMAL(19,3))),',','.'),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.OriginalSalesAmount AS DECIMAL(19,2))),',','.'),'|',
                   REPLACE(CONVERT(varchar(50),CAST(l.UnitVAT AS DECIMAL(19,3))),',','.'),'|',
                   l.ProductHostCode,'|||'
            )
            FROM CTE_Lines l

            UNION ALL

            -- 999 – File Trailer
            SELECT 3, 0, 0,
            CONCAT('999|',
                   REPLACE(CONVERT(CHAR(8),(SELECT WinStart FROM CTE_Windows),108),':',''),'|',
                   REPLACE(CONVERT(CHAR(8),(SELECT WinEnd FROM CTE_Windows),108),':',''),'|',
                   (2 + (SELECT TxCount FROM CTE_Summary) + (SELECT LineCount FROM CTE_Summary)),'|',
                   REPLACE(CONVERT(varchar(50), CAST((SELECT HashTotal1 FROM CTE_Summary) AS DECIMAL(19,2))),',','.'),'|',
                   '0'
            ) AS LineText
        )
        SELECT LineText
        FROM AllRows
        ORDER BY Sort1, Sort2, Sort3;
        """;

    // ─── Export ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shkruan rreshtat pipe-delimited në file .txt (UTF-8 pa BOM).
    /// Nuk bën validim shtesë – SQL-i është përgjegjës për formatimin.
    /// </summary>
    public static void ExportLines(IEnumerable<string> lines, string path)
    {
        var normalizedLines = lines
            .Select(static line => line?.TrimEnd('\r', '\n') ?? string.Empty)
            .ToList();

        File.WriteAllLines(path, normalizedLines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
