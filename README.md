# Shell Notes App (Windows Forms, .NET 10)

Aplikacion Windows Forms në C# për ekzekutimin e raportit **CR Product file** dhe shfaqjen e rezultateve në `DataGridView`.

## Raporti

- **Emri i raportit:** `CR Product file`
- Query është vendosur fikse sipas kërkesës dhe respekton renditjen e 23 kolonave të detyrueshme.
- Në rezultat kthehet kolona `Line` (header + data rows në format CSV me `;`).
- Ka buton eksporti që ruan file UTF-8 me emër automatik sipas formatit `CRProducts_SSC_BDS_XK_999999_YYYYMMDDThhmmss.csv`.
- Specifikimi i plotë i kolonave CRProducts ruhet në `CRProducts_RSTS_Spec.md`.

## Connection string

```txt
Data Source=192.168.0.250,20343;Initial Catalog=SHELL;User Id=Kubit;Password=@KIKi34345#$@;
```

## Kërkesat

- .NET 10 SDK
- Windows OS (Windows Forms)

## Nisja

```bash
dotnet restore
dotnet run
```

## Çfarë bën forma kryesore

- Shfaq emrin e raportit dhe instruksionin e fushave.
- Shfaq query-n SQL (read-only).
- Butoni **Ngarko raportin CR Product file** e ekzekuton query-n dhe i shfaq rezultatet në grid.
- Butoni **Eksporto CRProducts CSV** verifikon rregullat (23 kolona, pa delimiter në fund, pa `NULL`, decimal me `.`) dhe ruan file sipas specifikimit.
