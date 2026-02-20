using System.Data;
using Microsoft.Data.SqlClient;

namespace ShellNotesApp;

public sealed class MainForm : Form
{
    private readonly DataGridView _notesGrid;
    private readonly Button _loadButton;
    private readonly Label _statusLabel;

    public MainForm()
    {
        Text = $"Shell - {ReportDefinition.ReportName}";
        Width = 1200;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;

        var reportNameLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Text = $"Raporti: {ReportDefinition.ReportName}",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        var instructionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 56,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            Text = ReportDefinition.Instructions,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        var queryBox = new RichTextBox
        {
            Dock = DockStyle.Top,
            Height = 220,
            Font = new Font("Consolas", 9f),
            ReadOnly = true,
            WordWrap = false,
            Text = ReportDefinition.Query
        };

        _loadButton = new Button
        {
            Text = "Ngarko raportin CR Product file",
            Dock = DockStyle.Top,
            Height = 42,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _loadButton.Click += async (_, _) => await LoadNotesAsync();

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            Text = "Gati. Kliko butonin për të ekzekutuar query-n e raportit."
        };

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

        Controls.Add(_notesGrid);
        Controls.Add(_statusLabel);
        Controls.Add(_loadButton);
        Controls.Add(queryBox);
        Controls.Add(instructionLabel);
        Controls.Add(reportNameLabel);
    }

    private async Task LoadNotesAsync()
    {
        _loadButton.Enabled = false;
        _statusLabel.Text = "Duke ngarkuar të dhënat e raportit...";

        try
        {
            using var connection = new SqlConnection(ReportDefinition.ConnectionString);
            using var command = new SqlCommand(ReportDefinition.Query, connection)
            {
                CommandTimeout = 180
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
            _loadButton.Enabled = true;
        }
    }
}
