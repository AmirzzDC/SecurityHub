using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNet.SignalR.Client;

namespace SecureNetAdminPanel
{
    public partial class MainForm : Form
    {
        // SignalR Hub connection and proxy
        private HubConnection hubConnection;
        private IHubProxy hubProxy;

        // Data structures
        private List<PCInfo> pcList = new List<PCInfo>();
        private List<string> blacklist = new List<string>();

        // UI Controls
        private DataGridView dgvPCList;
        private ListBox lbBlacklist;
        private TextBox txtNewBlacklist;
        private Button btnAddBlacklist;
        private Button btnRemoveBlacklist;

        private Button btnShutdown, btnRestart, btnLock, btnDisableInput, btnSendMessage;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            InitializeSignalR();
        }

        private void InitializeUI()
        {
            this.Text = "SecureNet Admin Panel";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            var tabControl = new TabControl() { Dock = DockStyle.Fill };

            // PC List Tab
            var tabPCList = new TabPage("PC List");
            dgvPCList = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                ReadOnly = true
            };
            dgvPCList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "PC Name", DataPropertyName = "PCName", Width = 200 });
            dgvPCList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "IP Address", DataPropertyName = "IP", Width = 150 });
            dgvPCList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Admin Code", DataPropertyName = "AdminCode", Width = 150 });
            dgvPCList.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Connected", DataPropertyName = "IsConnected", Width = 100 });

            tabPCList.Controls.Add(dgvPCList);
            tabControl.TabPages.Add(tabPCList);

            // Blacklist Tab
            var tabBlacklist = new TabPage("Blacklist Words/URLs");
            lbBlacklist = new ListBox() { Dock = DockStyle.Top, Height = 400 };
            txtNewBlacklist = new TextBox() { Dock = DockStyle.Top, PlaceholderText = "Add new blacklisted word or URL..." };
            btnAddBlacklist = new Button() { Text = "Add", Dock = DockStyle.Top };
            btnRemoveBlacklist = new Button() { Text = "Remove Selected", Dock = DockStyle.Top };

            btnAddBlacklist.Click += BtnAddBlacklist_Click;
            btnRemoveBlacklist.Click += BtnRemoveBlacklist_Click;

            tabBlacklist.Controls.Add(btnRemoveBlacklist);
            tabBlacklist.Controls.Add(btnAddBlacklist);
            tabBlacklist.Controls.Add(txtNewBlacklist);
            tabBlacklist.Controls.Add(lbBlacklist);
            tabControl.TabPages.Add(tabBlacklist);

            // PC Actions Tab
            var tabPCActions = new TabPage("PC Actions");

            btnShutdown = new Button() { Text = "Shutdown", Dock = DockStyle.Top, Height = 40, Enabled = false };
            btnRestart = new Button() { Text = "Restart", Dock = DockStyle.Top, Height = 40, Enabled = false };
            btnLock = new Button() { Text = "Lock", Dock = DockStyle.Top, Height = 40, Enabled = false };
            btnDisableInput = new Button() { Text = "Disable Keyboard & Mouse", Dock = DockStyle.Top, Height = 40, Enabled = false };
            btnSendMessage = new Button() { Text = "Send Message", Dock = DockStyle.Top, Height = 40, Enabled = false };

            btnShutdown.Click += BtnShutdown_Click;
            btnRestart.Click += BtnRestart_Click;
            btnLock.Click += BtnLock_Click;
            btnDisableInput.Click += BtnDisableInput_Click;
            btnSendMessage.Click += BtnSendMessage_Click;

            tabPCActions.Controls.Add(btnSendMessage);
            tabPCActions.Controls.Add(btnDisableInput);
            tabPCActions.Controls.Add(btnLock);
            tabPCActions.Controls.Add(btnRestart);
            tabPCActions.Controls.Add(btnShutdown);

            tabControl.TabPages.Add(tabPCActions);

            this.Controls.Add(tabControl);

            // Enable actions when a PC is selected
            dgvPCList.SelectionChanged += (s, e) =>
            {
                bool hasSelection = dgvPCList.SelectedRows.Count > 0;
                btnShutdown.Enabled = hasSelection;
                btnRestart.Enabled = hasSelection;
                btnLock.Enabled = hasSelection;
                btnDisableInput.Enabled = hasSelection;
                btnSendMessage.Enabled = hasSelection;
            };
        }

        private async void InitializeSignalR()
        {
            try
            {
                // TODO: Replace URL with your backend server SignalR endpoint
                string serverUrl = "http://localhost:5000/signalr";

                hubConnection = new HubConnection(serverUrl);
                hubProxy = hubConnection.CreateHubProxy("AdminHub");

                // Receive PC list updates
                hubProxy.On<List<PCInfo>>("UpdatePCList", (pcs) =>
                {
                    pcList = pcs;
                    Invoke(new Action(() =>
                    {
                        dgvPCList.DataSource = null;
                        dgvPCList.DataSource = pcList;
                    }));
                });

                // Receive blacklist updates
                hubProxy.On<List<string>>("UpdateBlacklist", (list) =>
                {
                    blacklist = list;
                    Invoke(new Action(() =>
                    {
                        lbBlacklist.DataSource = null;
                        lbBlacklist.DataSource = blacklist;
                    }));
                });

                await hubConnection.Start();
                // Request initial data after connection established
                await hubProxy.Invoke("RequestInitialData");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to server: " + ex.Message);
            }
        }

        // Blacklist buttons
        private async void BtnAddBlacklist_Click(object sender, EventArgs e)
        {
            string newWord = txtNewBlacklist.Text.Trim();
            if (!string.IsNullOrEmpty(newWord))
            {
                await hubProxy.Invoke("AddToBlacklist", newWord);
                txtNewBlacklist.Clear();
            }
        }

        private async void BtnRemoveBlacklist_Click(object sender, EventArgs e)
        {
            if (lbBlacklist.SelectedItem != null)
            {
                string selected = lbBlacklist.SelectedItem.ToString();
                await hubProxy.Invoke("RemoveFromBlacklist", selected);
            }
        }

        // PC Action buttons
        private async void BtnShutdown_Click(object sender, EventArgs e)
        {
            var pc = GetSelectedPC();
            if (pc != null)
                await SendCommandToPC(pc.PCName, "Shutdown");
        }
        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            var pc = GetSelectedPC();
            if (pc != null)
                await SendCommandToPC(pc.PCName, "Restart");
        }
        private async void BtnLock_Click(object sender, EventArgs e)
        {
            var pc = GetSelectedPC();
            if (pc != null)
                await SendCommandToPC(pc.PCName, "Lock");
        }
        private async void BtnDisableInput_Click(object sender, EventArgs e)
        {
            var pc = GetSelectedPC();
            if (pc != null)
                await SendCommandToPC(pc.PCName, "ToggleInput");
        }
        private async void BtnSendMessage_Click(object sender, EventArgs e)
        {
            var pc = GetSelectedPC();
            if (pc != null)
            {
                string message = Prompt.ShowDialog("Enter message to send:", "Send Message");
                if (!string.IsNullOrEmpty(message))
                    await SendCommandToPC(pc.PCName, "Message", message);
            }
        }

        private PCInfo GetSelectedPC()
        {
            if (dgvPCList.SelectedRows.Count == 0) return null;
            return dgvPCList.SelectedRows[0].DataBoundItem as PCInfo;
        }

        private async Task SendCommandToPC(string pcName, string command, string param = null)
        {
            try
            {
                await hubProxy.Invoke("SendCommandToPC", pcName, command, param);
                MessageBox.Show($"Command '{command}' sent to {pcName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send command: {ex.Message}");
            }
        }
    }

    // PC info model - matches backend model
    public class PCInfo
    {
        public string PCName { get; set; }
        public string IP { get; set; }
        public string AdminCode { get; set; }
        public bool IsConnected { get; set; }
    }

    // Helper prompt dialog for input
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "Ok", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : null;
        }
    }
}
