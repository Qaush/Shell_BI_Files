namespace ShellNotesApp;

internal enum ExportKind
{
    CsvLines,
    DataTableSemicolon
}

internal sealed record ReportDefinition(
    string ReportName,
    string Instructions,
    string Query,
    Func<DateTime, string> BuildFileName,
    ExportKind ExportKind);

internal static class ReportCatalog
{
    public const string ConnectionString = "Data Source=192.168.0.250,20343;Initial Catalog=SHELL;User Id=Kubit;Password=@KIKi34345#$@;TrustServerCertificate=True;";

    public static readonly IReadOnlyList<ReportDefinition> Reports =
    [
        new(
            "CR Product file",
            "Raporti duhet të dorëzohet sipas RSTS: emri i file-it CRProducts_SSC_BDS_XK_999999_YYYYMMDDThhmmss.csv, UTF-8, delimiter ';', pa kolona bosh dhe me 23 kolona të detyrueshme: " +
            "BUID, BUCODE, INVENTORYITEMID, EXTERNALID, ITEMNAME, ITEMSTATUS, ORGUNITOWNERID, ORGUNITOWNERNAME, " +
            "PRODUCTOWNERSHIP, PRODUCTSELLINGTYPE, LOCALSUBCATEGORYID, LOCALSUBCATEGORYCODE, LOCALSUBCATEGORYNAME, " +
            "TAXID, TAXNAME, TAXPERCENT, MANUFACTURERID, MANUFACTURERCODE, MANUFACTURERNAME, BUSINESSUNITGRPID, " +
            "BUSINESSUNITGRPNAME, BRANDCODE, BRANDNAME.",
            """
            /* =========================================================
               CR PRODUCT FILE – RSTS Kosovo
               Version pa Prodhuesit (Manufacturer default)
               ========================================================= */

            DECLARE @BUID INT = 1000101;
            DECLARE @BUCODE NVARCHAR(50) = N'XK - TMLA';
            DECLARE @ORGUNITOWNERID INT = 1000101;
            DECLARE @ORGUNITOWNERNAME NVARCHAR(50) = N'XK - TMLA';

            ;WITH P AS
            (
                SELECT
                    a.Id AS INVENTORYITEMID,
                    a.Shifra AS EXTERNALID,
                    a.Pershkrimi AS ITEMNAME,

                    CASE
                        WHEN a.Statusi = 1 THEN 'y'
                        ELSE 'n'
                    END AS ITEMSTATUS,

                    'Central' AS PRODUCTOWNERSHIP,
                    'g' AS PRODUCTSELLINGTYPE,

                    ISNULL(gs.Id, -2) AS LOCALSUBCATEGORYID,
                    ISNULL(gs.Shifra, '-2') AS LOCALSUBCATEGORYCODE,
                    ISNULL(gs.Pershkrimi, 'Unknown') AS LOCALSUBCATEGORYNAME,

                    t.Id AS TAXID,
                    ISNULL(t.Pershkrimi, 'UNKNOWN') AS TAXNAME,
                    CAST(t.Vlera AS DECIMAL(12,4)) AS TAXPERCENT,

                    0 AS MANUFACTURERID,
                    'NA' AS MANUFACTURERCODE,
                    'N/A' AS MANUFACTURERNAME,

                    0 AS BUSINESSUNITGRPID,
                    'KOSOVO TMLA' AS BUSINESSUNITGRPNAME,

                    ISNULL(b.Id, 0) AS BRANDCODE,
                    ISNULL(b.Pershkrimi, '') AS BRANDNAME
                FROM dbo.Artikujt a
                LEFT JOIN dbo.Tatimet t ON t.Id = a.TatimetID
                LEFT JOIN dbo.GrupetEMallrave gs ON gs.Id = a.GrupiMallitID
                LEFT JOIN dbo.Brendet b ON b.Id = a.BrendId
            )

            SELECT Line
            FROM
            (
                SELECT
                    'BUID;BUCODE;INVENTORYITEMID;EXTERNALID;ITEMNAME;ITEMSTATUS;ORGUNITOWNERID;ORGUNITOWNERNAME;PRODUCTOWNERSHIP;PRODUCTSELLINGTYPE;LOCALSUBCATEGORYID;LOCALSUBCATEGORYCODE;LOCALSUBCATEGORYNAME;TAXID;TAXNAME;TAXPERCENT;MANUFACTURERID;MANUFACTURERCODE;MANUFACTURERNAME;BUSINESSUNITGRPID;BUSINESSUNITGRPNAME;BRANDCODE;BRANDNAME' AS Line

                UNION ALL

                SELECT
                    CONCAT(
                        @BUID, ';',
                        '"', @BUCODE, '";',
                        INVENTORYITEMID, ';',
                        '"', ISNULL(EXTERNALID, ''), '";',
                        '"', REPLACE(ISNULL(ITEMNAME, ''), '"', '""'), '";',
                        ITEMSTATUS, ';',
                        @ORGUNITOWNERID, ';',
                        '"', @ORGUNITOWNERNAME, '";',
                        '"', PRODUCTOWNERSHIP, '";',
                        '"', PRODUCTSELLINGTYPE, '";',
                        LOCALSUBCATEGORYID, ';',
                        '"', LOCALSUBCATEGORYCODE, '";',
                        '"', REPLACE(LOCALSUBCATEGORYNAME, '"', '""'), '";',
                        ISNULL(TAXID, 0), ';',
                        '"', ISNULL(TAXNAME, ''), '";',
                        REPLACE(CONVERT(VARCHAR(30), ISNULL(TAXPERCENT, 0)), ',', '.'), ';',
                        MANUFACTURERID, ';',
                        '"', MANUFACTURERCODE, '";',
                        '"', MANUFACTURERNAME, '";',
                        BUSINESSUNITGRPID, ';',
                        '"', BUSINESSUNITGRPNAME, '";',
                        BRANDCODE, ';',
                        '"', BRANDNAME, '"'
                    )
                FROM P
            ) X
            ORDER BY CASE WHEN Line LIKE 'BUID;BUCODE%' THEN 0 ELSE 1 END;
            """,
            CrProductsExport.BuildFileName,
            ExportKind.CsvLines),
        new(
            "CR Retail Pack",
            "Raporti duhet të dorëzohet sipas renditjes së detyrueshme me 28 kolona. Emri i file-it duhet të jetë CRRetailPacks_QBS_XK_999999_YYYY-MM-DD-hh-mm-ss.csv, BUSINESS_UNIT_CODE duhet të jetë XK - TMLA, dhe RETAIL_PACK_NAME duhet të jetë i njëjtë me PRODUCT_RECEIPT_TEXT.",
            """
            /* =========================================================
               CR RETAIL PACK FILE – RSTS Kosovo
               28 Columns – Exact Mandatory Sequence
               ========================================================= */

            DECLARE @BUID INT = 1000101;
            DECLARE @BUCODE NVARCHAR(50) = N'XK - TMLA';
            DECLARE @Today DATE = CAST(GETDATE() AS DATE);

            ;WITH P AS
            (
                SELECT
                    a.Id AS INVENTORYITEMID,
                    ISNULL(a.Shifra,'') AS EXTERNALID,
                    ISNULL(a.Pershkrimi,'') AS ITEMNAME,
                    ISNULL(a.Pershkrimi,'') AS PRODUCT_RECEIPT_TEXT,

                    n.Njesia AS BASE_UNIT_OF_MEASURE,

                    a.Id AS RMI_ID,
                    a.Shifra AS RMI_EXTERNAL_ID,
                    a.Pershkrimi AS RMI_NAME,

                    a.Id AS RETAIL_PACK_ID,
                    ISNULL(a.Pershkrimi,'') AS RETAIL_PACK_NAME,
                    '' AS RETAIL_PACK_SIZE,

                    0 AS CONTAINER_FEE_RETAIL,
                    '' AS CONTAINER_FEE_NAME,

                    -- Barcode
                    b.Barkodi AS RETAIL_ITEM_BARCODE,

                    CASE
                        WHEN LEN(b.Barkodi) = 13 THEN 'EAN13'
                        WHEN LEN(b.Barkodi) = 12 THEN 'EAN12'
                        WHEN LEN(b.Barkodi) = 8 THEN 'EAN8'
                        ELSE 'Primary'
                    END AS RETAIL_TYPE,

                    c.QmimiIShitjes AS RECOMMENDED_RETAIL_PRICE,
                    c.QmimiIShitjes AS MAXIMUM_RETAIL_PRICE,

                    t.Id AS TAX_CODE,
                    t.Pershkrimi AS TAX_NAME,
                    CAST(t.Vlera AS DECIMAL(6,2)) AS TAX_PERCENT

                FROM dbo.Artikujt a
                INNER JOIN dbo.Njesit n ON n.Id = a.NjesiaID
                INNER JOIN dbo.Cmimorja c ON c.ArtikulliId = a.Id AND c.OrganizataId = 188
                LEFT JOIN dbo.Tatimet t ON t.Id = a.TatimetID
                OUTER APPLY
                (
                    SELECT TOP 1 Barkodi
                    FROM dbo.Barkodat
                    WHERE ArtikulliId = a.Id
                ) b
            )

            SELECT
                @BUID AS BUSINESS_UNIT_ID,
                @BUCODE AS BUSINESS_UNIT_CODE,
                RMI_ID,
                RMI_EXTERNAL_ID,
                RMI_NAME,
                PRODUCT_RECEIPT_TEXT,
                BASE_UNIT_OF_MEASURE,
                INVENTORYITEMID AS INVENTORY_ITEM_ID,
                '1' AS RETAIL_LEVEL_ID,
                'Default' AS RETAIL_LEVEL_NAME,
                RETAIL_PACK_ID,
                RETAIL_PACK_NAME,
                RETAIL_PACK_SIZE,
                CONTAINER_FEE_NAME,
                CAST(CONTAINER_FEE_RETAIL AS DECIMAL(12,2)) AS CONTAINER_FEE_RETAIL,
                0 AS BARCODE_ID,
                RETAIL_ITEM_BARCODE,
                RETAIL_TYPE,
                @Today AS PRICE_FROM_DATE,
                '9999-12-31' AS PRICE_TO_DATE,
                CAST(RECOMMENDED_RETAIL_PRICE AS DECIMAL(12,2)) AS RECOMMENDED_RETAIL_PRICE,
                CAST(MAXIMUM_RETAIL_PRICE AS DECIMAL(12,2)) AS MAXIMUM_RETAIL_PRICE,
                ISNULL(TAX_CODE,0) AS TAX_CODE,
                ISNULL(TAX_NAME,'') AS TAX_NAME,
                ISNULL(TAX_PERCENT,0) AS TAX_PERCENT,
                CONVERT(INT, FORMAT(@Today,'yyyyMMdd')) AS PRICE_KEY,
                0 AS VERSION,
                'C' AS OWNERSHIP_IND
            FROM P;
            """,
            CrRetailPacksExport.BuildFileName,
            ExportKind.DataTableSemicolon)
    ];
}
