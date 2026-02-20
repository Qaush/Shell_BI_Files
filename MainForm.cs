using System.Data;
using Microsoft.Data.SqlClient;

namespace ShellNotesApp;

public sealed class MainForm : Form
{
    private const string ConnectionString = "Data Source=192.168.0.250,20343;Initial Catalog=SHELL;User Id=Kubit;Password=@KIKi34345#$@;";

    private readonly DataGridView _notesGrid;
    private readonly Button _loadButton;
    private readonly TextBox _queryTextBox;
    private readonly Label _statusLabel;

    public MainForm()
    {
        Text = "Shell - Shenime nga Databaza";
        Width = 1000;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;

        _queryTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 10f),
            Text = "SELECT TOP (100) * FROM Shenime ORDER BY Id DESC"
        };

        _loadButton = new Button
        {
            Text = "Ngarko Shenimet",
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
            Text = "Gati. Kliko \"Ngarko Shenimet\" për të lexuar të dhënat nga SQL Server."
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
        Controls.Add(_queryTextBox);
    }

    private async Task LoadNotesAsync()
    {
        _loadButton.Enabled = false;
        _statusLabel.Text = "Duke ngarkuar shënimet...";

        try
        {
            var query = string.IsNullOrWhiteSpace(_queryTextBox.Text)
                ? "SELECT TOP (100) * FROM Shenime"
                : _queryTextBox.Text;

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(query, connection);
            using var adapter = new SqlDataAdapter(command);

            var table = new DataTable();

            await connection.OpenAsync();
            adapter.Fill(table);

            _notesGrid.DataSource = table;
            _statusLabel.Text = $"U ngarkuan {table.Rows.Count} rreshta.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Dështoi ngarkimi i të dhënave.";
            MessageBox.Show(
                $"Ndodhi një gabim gjatë leximit të databazës:\n{ex.Message}",
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
