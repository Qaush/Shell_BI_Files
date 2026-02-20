using System.Data;
using Microsoft.Data.SqlClient;

namespace ShellNotesApp;

public sealed class MainForm : Form
{
    private readonly DataGridView _notesGrid;
    private readonly Label _statusLabel;
    private readonly Label _reportTitleLabel;
    private readonly Label _instructionLabel;

    private ReportDefinition? _selectedReport;
    private Button? _activeReportButton;

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
        container.Controls.Add(reportsButtonsPanel);
        container.Controls.Add(reportsLabel);

        return container;
    }

    private void SelectReport(ReportDefinition report, Button? button)
    {
        _selectedReport = report;
        _reportTitleLabel.Text = $"Raporti aktiv: {report.ReportName}";
        _instructionLabel.Text = report.Instructions;

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
                CommandTimeout = 0
            };
            using var adapter = new SqlDataAdapter(command);

            var table = new DataTable();

            await connection.OpenAsync();
            adapter.Fill(table);

            _notesGrid.DataSource = table;
            _statusLabel.Text = $"Raporti u ngarkua me sukses. Rreshta: {table.Rows.Count}.";
        }
        catch (Exception ex)
        {
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

    private void SetReportButtonsEnabled(bool enabled)
    {
        foreach (Control control in Controls)
        {
            ToggleButtons(control, enabled);
        }
    }

    private static void ToggleButtons(Control parent, bool enabled)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Button button)
            {
                button.Enabled = enabled;
            }

            if (control.HasChildren)
            {
                ToggleButtons(control, enabled);
            }
        }
    }
}
