using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Medley.CollectorUtil.Data;

namespace Medley.CollectorUtil
{
    public partial class FellowApiKeys : Form
    {
        private readonly ApiKeyService _apiKeyService;
        private readonly ConfigurationService _configurationService;

        public FellowApiKeys()
        {
            InitializeComponent();
            _apiKeyService = new ApiKeyService();
            _configurationService = new ConfigurationService();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                btnSave.Enabled = false;
                btnCancel.Enabled = false;
                
                // Validate workspace
                var workspace = txtWorkspace.Text?.Trim();
                if (string.IsNullOrWhiteSpace(workspace))
                {
                    MessageBox.Show("Please enter a workspace.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var apiKeys = new List<ApiKey>();
                
                foreach (DataGridViewRow row in dgvApiKeys.Rows)
                {
                    if (row.IsNewRow) continue;
                    
                    var id = row.Cells[0].Tag is int existingId ? existingId : 0;
                    var name = row.Cells[0].Value?.ToString()?.Trim();
                    var key = row.Cells[1].Value?.ToString()?.Trim();
                    var isEnabled = row.Cells[2].Value is bool enabled ? enabled : true;
                    
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(key))
                    {
                        apiKeys.Add(new ApiKey { Id = id, Name = name, Key = key, IsEnabled = isEnabled });
                    }
                }

                if (apiKeys.Count == 0)
                {
                    MessageBox.Show("Please enter at least one API key with a name.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Save workspace and API keys
                await _configurationService.SaveWorkspaceAsync(workspace);
                await _apiKeyService.SaveApiKeysAsync(apiKeys);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving API keys: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private async void FellowApiKeys_Load(object sender, EventArgs e)
        {
            try
            {
                // Load workspace
                var workspace = await _configurationService.GetWorkspaceAsync();
                txtWorkspace.Text = workspace;
                
                // Load API keys
                var keys = await _apiKeyService.GetAllApiKeysAsync();
                
                dgvApiKeys.Rows.Clear();
                
                foreach (var apiKey in keys)
                {
                    var rowIndex = dgvApiKeys.Rows.Add(apiKey.Name, apiKey.Key, apiKey.IsEnabled);
                    // Store the Id in the first cell's Tag property for later retrieval
                    dgvApiKeys.Rows[rowIndex].Cells[0].Tag = apiKey.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading API keys: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
