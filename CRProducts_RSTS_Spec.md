# CRProducts RSTS Mapping (Kosovo)

Ky dokument ruhet për referencë të ardhshme për gjenerimin e file-it **CRProducts** sipas renditjes dhe rregullave të kërkuara.

## Kolonat (renditja e detyrueshme)

1. `BUID` (INTEGER, **Y**) → `1000101`
2. `BUCODE` (NVARCHAR(255), **Y**) → `XK - TMLA`
3. `INVENTORYITEMID` (NVARCHAR(40), **Y**) → Product technical key
4. `EXTERNALID` (NVARCHAR(255), N) → Product/SKU business ID
5. `ITEMNAME` (NVARCHAR(250), **Y**) → Product name
6. `ITEMSTATUS` (NCHAR(1), **Y**) → `y`/`n`
7. `ORGUNITOWNERID` (INTEGER, **Y**) → `1000101`
8. `ORGUNITOWNERNAME` (NVARCHAR(255), N) → `XK - TMLA`
9. `PRODUCTOWNERSHIP` (NVARCHAR(50), N) → `Central`
10. `PRODUCTSELLINGTYPE` (NCHAR(1), N) → blank ose `g` (CR)
11. `LOCALSUBCATEGORYID` (INTEGER, **Y**) → technical key, fallback `-2`
12. `LOCALSUBCATEGORYCODE` (NVARCHAR(100), **Y**)
13. `LOCALSUBCATEGORYNAME` (NVARCHAR(255), **Y**)
14. `TAXID` (NVARCHAR(255), N)
15. `TAXNAME` (NVARCHAR(100), N)
16. `TAXPERCENT` (NUMBER(12,4), N)
17. `MANUFACTURERID` (INTEGER, N)
18. `MANUFACTURERCODE` (NVARCHAR(255), N)
19. `MANUFACTURERNAME` (NVARCHAR(255), N)
20. `BUSINESSUNITGRPID` (INTEGER, N)
21. `BUSINESSUNITGRPNAME` (NVARCHAR(255), N)
22. `BRANDCODE` (INTEGER, N)
23. `BRANDNAME` (NVARCHAR(255), N)

## Rregulla file-i

- Emri: `CRProducts_SSC_BDS_XK_999999_YYYYMMDDThhmmss.csv`
- Encoding: UTF-8
- Delimiter: `;`
- Header + data duhet të kenë saktë 23 kolona
- Nuk lejohet delimiter në fund të rreshtit
- Decimalet me pikë `.`
- Nuk lejohet literal `NULL`
