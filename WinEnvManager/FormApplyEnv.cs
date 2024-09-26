using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinEnvManager
{
    public partial class FormApplyEnv : Form
    {
        private string envFolder = EnvManager.BASE_FOLDER; // Changez ici le chemin du dossier
        private Dictionary<string, BindingList<EnvVariable>> envFiles = new Dictionary<string, BindingList<EnvVariable>>();
        public FormApplyEnv()
        {
            InitializeComponent();
            AddButtons();
        }

        private void AddButtons()
        {
            var files = Directory.GetFiles(envFolder);

            foreach (var file in files)
            {
                // Charger les variables à partir du fichier JSON
                var variables = EnvManager.LoadVariablesFromJson(file);
                var profileName = Path.GetFileNameWithoutExtension(file);
                envFiles[profileName] = new BindingList<EnvVariable>(variables);

                var btn = new Button();
                btn.Text = profileName;
                btn.Dock = DockStyle.Top;
                btn.Click += delegate 
                {
                    EnvManager.ApplyEnv(variables);
                    Application.Exit();
                };
                panel1.Controls.Add(btn);
            }
        }
    }
}
