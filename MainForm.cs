using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace ShellNotesApp;

public sealed class MainForm : Form
{
    private readonly DataGridView _notesGrid;
    private Label _statusLabel = null!;
    private Label _reportTitleLabel = null!;
    private Label _instructionLabel = null!;

    private ReportDefinition? _selectedReport;
    private Button? _activeReportButton;
    private Button _exportButton = null!;
    private bool _canExport;

    public MainForm()
    {
        Text = "Shell Reports";
        Width = 1300;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var leftPanel = BuildLeftPanel();

        _notesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        root.Controls.Add(leftPanel, 0, 0);
        root.Controls.Add(_notesGrid, 1, 0);

        Controls.Add(root);

        if (ReportCatalog.Reports.Count > 0)
        {
            SelectReport(ReportCatalog.Reports[0], null);
        }
    }

    private Control BuildLeftPanel()
    {
        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        var reportsLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = "Raportet",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var reportsButtonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 240,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 4)
        };

        foreach (var report in ReportCatalog.Reports)
        {
            var reportButton = new Button
            {
                Width = 300,
                Height = 40,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Text = report.ReportName,
                Tag = report
            };

            reportButton.Click += async (_, _) =>
            {
                SelectReport(report, reportButton);
                await LoadReportAsync();
            };

            reportsButtonsPanel.Controls.Add(reportButton);
        }

        _exportButton = new Button
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Text = "Eksporto CSV",
            Enabled = false
        };

        _exportButton.Click += async (_, _) => await ExportCurrentReportAsync();

        _reportTitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 4, 0, 0)
        };

        _instructionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 140,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.TopLeft
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            Text = "Zgjidh një raport nga lista dhe kliko butonin përkatës."
        };

        container.Controls.Add(_statusLabel);
        container.Controls.Add(_instructionLabel);
        container.Controls.Add(_reportTitleLabel);
        container.Controls.Add(_exportButton);
        container.Controls.Add(reportsButtonsPanel);
        container.Controls.Add(reportsLabel);

        return container;
    }

    private void SelectReport(ReportDefinition report, Button? button)
    {
        _selectedReport = report;
        _reportTitleLabel.Text = $"Raporti aktiv: {report.ReportName}";
        _instructionLabel.Text = report.Instructions;
        _exportButton.Text = $"Eksporto {report.ReportName} CSV";
        _canExport = false;
        _exportButton.Enabled = false;

        if (_activeReportButton is not null)
        {
            _activeReportButton.BackColor = SystemColors.Control;
            _activeReportButton.ForeColor = SystemColors.ControlText;
        }

        if (button is not null)
        {
            _activeReportButton = button;
            _activeReportButton.BackColor = Color.FromArgb(29, 78, 216);
            _activeReportButton.ForeColor = Color.White;
        }

        // Ndryshon tekstin e butonit sipas llojit të eksportit
        _exportButton.Text = report.BuildSiteFileName is not null
            ? $"Gjenero skedarët e {report.ReportName}"
            : $"Eksporto {report.ReportName} CSV";
    }

    private async Task LoadReportAsync()
    {
        if (_selectedReport is null)
        {
            _statusLabel.Text = "Nuk është zgjedhur asnjë raport.";
            return;
        }

        SetReportButtonsEnabled(false);
        _statusLabel.Text = $"Duke ngarkuar: {_selectedReport.ReportName}...";

        try
        {
            using var connection = new SqlConnection(ReportCatalog.ConnectionString);
            using var command = new SqlCommand(_selectedReport.Query, connection)
            {
                CommandTimeout = 0,
                CommandType = CommandType.Text
            };
            using var adapter = new SqlDataAdapter(command);

            var table = new DataTable();

            await connection.OpenAsync();
            adapter.Fill(table);

            _notesGrid.DataSource = table;
            _canExport = table.Rows.Count > 0;
            _exportButton.Enabled = _canExport;
            _statusLabel.Text = $"Raporti u ngarkua me sukses. Rreshta: {table.Rows.Count}.";
        }
        catch (Exception ex)
        {
            _canExport = false;
            _exportButton.Enabled = false;
            _statusLabel.Text = "Dështoi ekzekutimi i raportit.";
            MessageBox.Show(
                $"Ndodhi një gabim gjatë leximit nga databaza:\n{ex.Message}",
                "Gabim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetReportButtonsEnabled(true);
        }
    }


    private async Task ExportCurrentReportAsync()
    {
        if (_selectedReport is null)
        {
            MessageBox.Show("Zgjidh fillimisht raportin.", "Informacion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_notesGrid.DataSource is not DataTable table || table.Columns.Count == 0)
        {
            MessageBox.Show("Ngarko fillimisht raportin para eksportit.", "Informacion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // ── Multi-org export (p.sh. CR Transaction) ──────────────────────────
        if (_selectedReport.BuildSiteFileName is not null
            && _selectedReport.OrgListQuery is not null)
        {
            await ExportPerSiteAsync(_selectedReport);
            return;
        }

        // ── Single-file export ────────────────────────────────────────────────
        using var saveDialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = _selectedReport.BuildFileName(DateTime.Now),
            OverwritePrompt = true,
            AddExtension = true,
            DefaultExt = "csv",
            Title = $"Ruaj {_selectedReport.ReportName} sipas RSTS"
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (_selectedReport.ExportKind == ExportKind.CsvLines)
            {
                var lines = table.Rows
                    .Cast<DataRow>()
                    .Select(static row => Convert.ToString(row[0]) ?? string.Empty)
                    .ToList();

                CrProductsExport.ExportLines(lines, saveDialog.FileName);
            }
            else
            {
                ExportTableAsSemicolonCsv(table, saveDialog.FileName);
            }

            _statusLabel.Text = $"Eksporti u krye me sukses: {Path.GetFileName(saveDialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eksporti dështoi: {ex.Message}", "Gabim eksporti", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Gjeneron një skedar TXT për çdo organizatë që ka shitje dje.
    /// Kërkon FolderBrowserDialog dhe ekzekuton query-n për secilin site.
    /// </summary>
    private async Task ExportPerSiteAsync(ReportDefinition report)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = $"Zgjidh dosjen ku do të ruhen skedarët e {report.ReportName}",
            UseDescriptionForTitle = true
        };

        if (folderDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var folder = folderDialog.SelectedPath;
        var businessDate = DateTime.Today.AddDays(-1);

        SetReportButtonsEnabled(false);
        _statusLabel.Text = "Duke marrë listën e organizatave...";

        try
        {
            // 1. Merr listën e organizatave me shitje dje
            var orgs = await GetOrganizationListAsync(report.OrgListQuery!);

            if (orgs.Count == 0)
            {
                MessageBox.Show(
                    $"Nuk u gjet asnjë organizatë me shitje për datën {businessDate:yyyy-MM-dd}.",
                    "Asnjë të dhënë",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _statusLabel.Text = $"Duke gjeneruar {orgs.Count} skedarë...";

            int success = 0, empty = 0;
            var errors = new List<string>(); // (orgId → mesazhi i gabimit)

            // 2. Per-site: ndërtojmë query-n, ekzekutojmë, ruajmë skedarin
            foreach (var (orgId, siteCode) in orgs)
            {
                try
                {
                    var query = CrTransactionExport.BuildQuery(orgId);
                    var lines = await RunQueryAsLinesAsync(query);

                    // Kontrollojmë: nëse ka vetëm 000 + 999 (2 rreshta), nuk ka shitje
                    if (lines.Count <= 2)
                    {
                        empty++;
                        continue;
                    }

                    var fileName = report.BuildSiteFileName!(orgId, siteCode, businessDate);
                    var path = Path.Combine(folder, fileName);

                    CrTransactionExport.ExportLines(lines, path);
                    success++;

                    _statusLabel.Text = $"Gjeneruar {success}/{orgs.Count - empty}... ({orgId})";
                }
                catch (Exception ex)
                {
                    errors.Add($"Org {orgId}: {ex.Message}");
                }
            }

            var summary = $"Gjenerimi u krye: {success} skedarë. " +
                          (empty        > 0 ? $"Pa shitje: {empty}. "      : "") +
                          (errors.Count > 0 ? $"Gabime: {errors.Count}."   : "");

            _statusLabel.Text = summary;

            if (errors.Count > 0)
            {
                // Tregojmë detajet e gabimeve (max 10 rreshta për të mos mbushur dritaren)
                var errorDetails = string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                    errorDetails += $"\n... dhe {errors.Count - 10} gabime të tjera.";

                MessageBox.Show(
                    $"{summary}\n\nDetajet e gabimeve:\n{errorDetails}" +
                    (success > 0 ? $"\n\nSkedarët e suksesshëm u ruajtën te:\n{folder}" : ""),
                    "Gjenerim i kryer me gabime",
                    MessageBoxButtons.OK,
                    errors.Count == orgs.Count - empty ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            else if (success > 0)
            {
                MessageBox.Show(
                    $"{summary}\n\nDosja: {folder}",
                    "Gjenerim i kryer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gjenerimi dështoi:\n{ex.Message}",
                "Gabim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetReportButtonsEnabled(true);
        }
    }

    /// <summary>Ekzekuton OrgListQuery dhe kthen listë (orgId, siteCode).</summary>
    private static async Task<List<(int Id, string Code)>> GetOrganizationListAsync(string query)
    {
        var result = new List<(int, string)>();

        using var connection = new SqlConnection(ReportCatalog.ConnectionString);
        using var command    = new SqlCommand(query, connection) { CommandTimeout = 60 };

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id   = reader.GetInt32(0);
            var code = reader.GetString(1);
            result.Add((id, code));
        }

        return result;
    }

    /// <summary>
    /// Ekzekuton query-n dhe kthen listën e rreshtave (kolona e parë, si string).
    /// Përdoret për query-t e tipit CsvLines/PipeLines.
    /// </summary>
    private static async Task<List<string>> RunQueryAsLinesAsync(string query)
    {
        var lines = new List<string>();

        using var connection = new SqlConnection(ReportCatalog.ConnectionString);
        using var command    = new SqlCommand(query, connection) { CommandTimeout = 0 };
        using var adapter    = new SqlDataAdapter(command);

        var table = new DataTable();
        await connection.OpenAsync();
        adapter.Fill(table);

        foreach (DataRow row in table.Rows)
        {
            lines.Add(Convert.ToString(row[0], CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return lines;
    }

    private static void ExportTableAsSemicolonCsv(DataTable table, string path)
    {
        var lines = new List<string>(table.Rows.Count + 1)
        {
            string.Join(';', table.Columns.Cast<DataColumn>().Select(static c => c.ColumnName))
        };

        foreach (DataRow row in table.Rows)
        {
            var fields = row.ItemArray
                .Select(static value => EscapeCsvField(Convert.ToString(value) ?? string.Empty));

            lines.Add(string.Join(';', fields));
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeCsvField(string value)
    {
        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    private void SetReportButtonsEnabled(bool enabled)
    {
        foreach (Control control in Controls)
        {
            ToggleButtons(control, enabled);
        }

        _exportButton.Enabled = enabled && _canExport;
    }

    private void ToggleButtons(Control parent, bool enabled)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Button button)
            {
                if (!ReferenceEquals(button, _exportButton))
                {
                    button.Enabled = enabled;
                }
            }

            if (control.HasChildren)
            {
                ToggleButtons(control, enabled);
            }
        }
    }
}
