# Shell Notes App (Windows Forms, .NET 10)

Aplikacion i thjeshtë Windows Forms në C# për të lexuar shënime nga databaza `SHELL` dhe për t'i shfaqur në `DataGridView`.

## Connection string

Aplikacioni përdor këtë connection string:

```txt
Data Source=192.168.0.250,20343;Initial Catalog=SHELL;User Id=Kubit;Password=@KIKi34345#$@;
```

## Kërkesat

- .NET 10 SDK (preview ose version final kur të dalë)
- Windows OS (për Windows Forms)

## Nisja

```bash
dotnet restore
dotnet run
```

Në formën kryesore:
- Mund të ndryshosh SQL query në textbox-in sipër.
- Kliko **Ngarko Shenimet** për të lexuar të dhënat dhe për t’i shfaqur në grid.
