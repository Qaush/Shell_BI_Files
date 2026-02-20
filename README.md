# Shell Notes App (Windows Forms, .NET 10)

Aplikacion Windows Forms në C# për ekzekutimin e raporteve **CR Product file** dhe **CR Retail Pack** dhe shfaqjen e rezultateve në `DataGridView`.

## Raporti

- **Emrat e raporteve:** `CR Product file`, `CR Retail Pack`
- Query-t janë vendosur fikse sipas kërkesës.
- `CR Product file` përdor eksport me linja CSV sipas formatit `CRProducts_SSC_BDS_XK_999999_YYYYMMDDThhmmss.csv`.
- `CR Retail Pack` kthen 28 kolona sipas renditjes së detyrueshme dhe eksportohet si CSV me format `CRRetailPacks_QBS_XK_999999_YYYY-MM-DD-hh-mm-ss.csv`.
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
- Butonat e raporteve e ekzekutojnë query-n përkatës dhe i shfaqin rezultatet në grid.
- Butoni i eksportit përshtatet sipas raportit aktiv dhe ruan file CSV sipas specifikimit përkatës.
