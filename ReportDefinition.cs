namespace ShellNotesApp;

internal sealed record ReportDefinition(string ReportName, string Instructions, string Query);

internal static class ReportCatalog
{
    public const string ConnectionString = "Data Source=192.168.0.250,20343;Initial Catalog=SHELL;User Id=Kubit;Password=@KIKi34345#$@;TrustServerCertificate=True;";

    public static readonly IReadOnlyList<ReportDefinition> Reports =
    [
        new(
            "CR Product file",
            "Raporti duhet të ndjekë renditjen e detyrueshme me 23 kolona: " +
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
                    '' AS PRODUCTSELLINGTYPE,

                    ISNULL(gs.Id, -2) AS LOCALSUBCATEGORYID,
                    ISNULL(gs.Shifra, '-2') AS LOCALSUBCATEGORYCODE,
                    ISNULL(gs.Pershkrimi, 'Unknown') AS LOCALSUBCATEGORYNAME,

                    t.Id AS TAXID,
                    t.Pershkrimi AS TAXNAME,
                    CAST(t.Vlera AS DECIMAL(12,4)) AS TAXPERCENT,

                    0 AS MANUFACTURERID,
                    '' AS MANUFACTURERCODE,
                    '' AS MANUFACTURERNAME,

                    0 AS BUSINESSUNITGRPID,
                    '' AS BUSINESSUNITGRPNAME,

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
            """)
    ];
}
