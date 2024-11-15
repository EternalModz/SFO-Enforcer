using DarkModeForms;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SFO_Enforcer
{
    public partial class mainForm : Form
    {
        #region Fields
        private sfoWorker paramSFO;
        private sfoEntry titleEntry;
        string programVer = "1.1.0 (Build: #1100AB)";
        #endregion

        #region Constructor
        public mainForm()
        {
            InitializeComponent();
            Text = $"SFO Enforcer" + programVer;
            _ = new DarkModeCS(this);
        }
        #endregion

        #region Key Processing
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case (Keys.Control | Keys.O):
                    openToolStripMenuItem_Click(null, null);
                    return true;
                case (Keys.Control | Keys.S):
                    saveToolStripMenuItem_Click(null, null);
                    return true;
                case (Keys.Control | Keys.R):
                    reloadToolStripMenuItem_Click(null, null);
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        #endregion

        #region Event Handlers
        private void titleBox_TextChanged(object sender, EventArgs e)
        {
            saveToolStripMenuItem.Enabled = File.Exists(filenameBox.Text);
        }

        private void filenameBox_TextChanged(object sender, EventArgs e)
        {
            saveToolStripMenuItem.Enabled = false;
            paramSFO = null;
            titleEntry = null;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileDrop = e.Data.GetData(DataFormats.FileDrop) as string[];
                filenameBox.Text = fileDrop?[0];
                reloadToolStripMenuItem_Click(sender, e);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filenameBox.Text = openFileDialog1.FileName;
                reloadToolStripMenuItem_Click(sender, e);
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem.Enabled = false;
            titleBox.TextChanged -= titleBox_TextChanged;

            if (string.IsNullOrEmpty(filenameBox.Text))
                return;

            if (!File.Exists(filenameBox.Text))
            {
                MessageBox.Show("Please check the path to param.sfo", "File not found",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var stream = File.Open(filenameBox.Text, FileMode.Open,
                                          FileAccess.Read, FileShare.Read)) paramSFO =
                sfoWorker.ReadFrom(stream);
            titleEntry = paramSFO.Items.FirstOrDefault(i => i.Key == "TITLE");
            if (titleEntry == null)
            {
                paramSFO = null;
                MessageBox.Show("Title entry not found", "Unsupported param.sfo file",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            titleBox.Text = titleEntry.StringValue;
            titleBox.Focus();

            titleBox.TextChanged += titleBox_TextChanged;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!saveToolStripMenuItem.Enabled)
                return;

            var filename = filenameBox.Text;
            titleEntry.StringValue = titleBox.Text;

            var bakFilename = filename + ".bak";
            if (!File.Exists(bakFilename))
                File.Copy(filename, bakFilename);

            using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write,
                                          FileShare.Read)) paramSFO.WriteTo(stream);

            saveToolStripMenuItem.Enabled = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SFO Enforcer\nVersion: 1.1.0 (Build: #1100AB)\nDeveloped by: EternalModz",
                            "About SFO Enforcer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
