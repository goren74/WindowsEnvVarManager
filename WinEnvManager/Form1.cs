using System.ComponentModel;
using System.Text.Json;
using System.Windows.Forms;

namespace WinEnvManager
{
    public partial class Form1 : Form
    {
        private string envFolder = EnvManager.BASE_FOLDER; // Changez ici le chemin du dossier

        private Dictionary<string, BindingList<EnvVariable>> envFiles = new Dictionary<string, BindingList<EnvVariable>>();
        private DataGridView currentDataGrid;
        private int currentTab = 0;
        public Form1()
        {
            InitializeComponent();
            InitializeMenu();
            InitializeContextMenu();
            LoadEnvFiles();
        }

        #region Menu
        private void InitializeMenu()
        {
            // Create a MenuStrip
            MenuStrip menuStrip = new MenuStrip();

            // Add "Profiles" dropdown menu
            ToolStripMenuItem profilesMenuItem = new ToolStripMenuItem("Profiles");

            // Add "Add Profile" item to the "Profiles" menu
            ToolStripMenuItem addProfileMenuItem = new ToolStripMenuItem("Add Profile");
            addProfileMenuItem.Click += delegate
            {
                string profileName = PromptForProfileName();
                if (!string.IsNullOrWhiteSpace(profileName))
                {
                    AddNewProfileTab(profileName);
                }
            }; // Hook up the click event
            profilesMenuItem.DropDownItems.Add(addProfileMenuItem);

            // Add the "Profiles" menu to the MenuStrip
            MainMenuStrip.Items.Add(profilesMenuItem);

            // Add the MenuStrip to the form
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }
        private void AddNewProfileTab(string profileName)
        {
            // Create a new tab page with the profile name
            TabPage newTabPage = new TabPage(profileName);

            // Add the new tab to the tab control
            var variables = new List<EnvVariable>();
            if (envFiles.Count > 0)
                variables = envFiles.First().Value.ToList();
            envFiles[profileName] = new BindingList<EnvVariable>(variables);
            var dataGridView = CreateDataGridView(envFiles[profileName]);
            newTabPage.Controls.Add(dataGridView);
            tabControl1.TabPages.Add(newTabPage);

            // Set the new tab as the selected tab
            tabControl1.SelectedTab = newTabPage;
        }
        private string PromptForProfileName()
        {
            // Show an input dialog to get the profile name
            using (Form prompt = new Form())
            {
                prompt.Width = 300;
                prompt.Height = 150;
                prompt.Text = "Enter Profile Name";

                Label textLabel = new Label() { Left = 50, Top = 20, Text = "Profile Name:" };
                TextBox inputBox = new TextBox() { Left = 50, Top = 50, Width = 200 };
                Button confirmation = new Button() { Text = "Ok", Left = 150, Width = 100, Top = 80 };
                confirmation.DialogResult = DialogResult.OK;

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : string.Empty;
            }
        }
        private void InitializeContextMenu()
        {
            // Create a new ContextMenuStrip
            contextMenu = new ContextMenuStrip();

            // Add "Remove" option to the context menu
            var removeMenuItem = new ToolStripMenuItem("Remove");
            removeMenuItem.Click += RemoveMenuItem_Click;
            contextMenu.Items.Add(removeMenuItem);
        }
        private void RemoveMenuItem_Click(object sender, EventArgs e)
        {
            // Check if any row is selected
            if (currentDataGrid.SelectedRows.Count > 0)
            {
                // Get the selected row
                var selectedRow = currentDataGrid.SelectedRows[0].DataBoundItem as EnvVariable;
                foreach (var env in envFiles)
                {
                    var correspondingVar = env.Value.FirstOrDefault(v => v.Id == selectedRow.Id);
                    env.Value.Remove(correspondingVar);
                }
                //currentDataGrid.Rows.Remove(selectedRow);
            }
        }
        #endregion

        // Charger les fichiers .env_
        private void LoadEnvFiles()
        {
            var files = Directory.GetFiles(envFolder);

            tabControl1.TabPages.Clear();
            tabControl1.SelectedIndexChanged += delegate
            {
                currentTab = tabControl1.SelectedIndex;
            };
            foreach (var file in files)
            {
                // Charger les variables à partir du fichier JSON
                var variables = EnvManager.LoadVariablesFromJson(file);
                var profileName = Path.GetFileNameWithoutExtension(file);
                envFiles[profileName] = new BindingList<EnvVariable>(variables);

                // Créer un onglet avec DataGridView
                var tabPage = new TabPage(profileName);
                var dataGridView = CreateDataGridView(envFiles[profileName]);
                tabPage.Controls.Add(dataGridView);
                tabControl1.TabPages.Add(tabPage);
            }
        }


        // Créer un DataGridView pour afficher et éditer les variables
        private DataGridView CreateDataGridView(BindingList<EnvVariable> variables)
        {
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DataSource = variables,
            };

            //dataGridView.Columns["Id"].Visible = false; // Hide the ID column
            //dataGridView.Columns["Name"].HeaderText = "Nom de la Variable";
            //dataGridView.Columns["Value"].HeaderText = "Valeur de la Variable";

            // Handle name change propagation across all tabs
            dataGridView.ScrollBars = ScrollBars.Vertical;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.UserDeletingRow += delegate (object sender, DataGridViewRowCancelEventArgs e)
            {
                var rowId = e.Row.Cells["Id"].Value;
                foreach (var env in envFiles)
                {
                    var correspondingVar = env.Value.FirstOrDefault(v => v.Id == rowId);
                    env.Value.Remove(correspondingVar);
                }
            };
            dataGridView.MouseClick += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    // Get the current mouse position relative to DataGridView
                    var hitTest = dataGridView.HitTest(e.X, e.Y);

                    // Check if a row was clicked
                    if (hitTest.RowIndex >= 0)
                    {
                        // Select the row
                        dataGridView.ClearSelection();
                        dataGridView.Rows[hitTest.RowIndex].Selected = true;
                        currentDataGrid = dataGridView;
                        // Show context menu at the mouse position
                        contextMenu.Show(dataGridView, e.Location);
                    }
                }
            };

            return dataGridView;
        }

        // Propagate name changes across all environments based on unique ID
        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var dataGridView = sender as DataGridView;
            if (dataGridView.Columns[e.ColumnIndex].Name == "Name")
            {
                var updatedVariable = dataGridView.Rows[e.RowIndex].DataBoundItem as EnvVariable;
                if (string.IsNullOrEmpty(updatedVariable.Id))
                    updatedVariable.Id = Guid.NewGuid().ToString(); // Generate a unique ID


                // Update the variable name in all environments using the same ID
                foreach (var env in envFiles)
                {
                    var correspondingVar = env.Value.FirstOrDefault(v => v.Id == updatedVariable.Id);
                    if (correspondingVar != null)
                    {
                        correspondingVar.Name = updatedVariable.Name;
                    }
                    else
                    {
                        env.Value.Add(new EnvVariable
                        {
                            Id = updatedVariable.Id,
                            Name = updatedVariable.Name,
                            Value = ""
                        });
                    }
                }
            }
        }



        // Sauvegarder les fichiers après modification
        private void SaveEnvFiles()
        {
            foreach (var envFile in envFiles)
            {
                string filePath = envFolder + Path.DirectorySeparatorChar + envFile.Key + ".json";
                var variables = envFile.Value;

                WriteVariablesToJson(filePath, variables.ToList());
            }

            MessageBox.Show("Configuration saved !");
        }

        // Écrire les variables dans un fichier JSON
        private void WriteVariablesToJson(string filePath, List<EnvVariable> variables)
        {
            string json = JsonSerializer.Serialize(variables, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // Événement du bouton "Sauvegarder"
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveEnvFiles();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveEnvFiles();
            EnvManager.ApplyEnv(tabControl1.TabPages[currentTab].Text);
        }
    }
}
