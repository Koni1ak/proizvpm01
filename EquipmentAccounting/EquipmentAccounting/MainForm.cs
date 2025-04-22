using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EquipmentAccounting.Models;
using EquipmentAccounting.Services;
using System.Linq;
using System.Threading.Tasks;
using Supabase.Gotrue;

namespace EquipmentAccounting
{
    public partial class MainForm : Form
    {
        private readonly Supabase.Gotrue.User _currentUser;
        public MainForm(Supabase.Gotrue.User user)
        {
            InitializeComponent();
            _currentUser = user ?? throw new ArgumentNullException(nameof(user)); 
            ConfigureDataGridView();
            DisplayUserInfo(); 


        }
        private void DisplayUserInfo()
        {
            if (_currentUser != null)
            {
                
                this.Text = $"Учет оборудования - Пользователь: {_currentUser.Email}"; 
                toolStripStatusLabelUser.Text = $"Пользователь: {_currentUser.Email}"; 
            }
        }

        private void ConfigureDataGridView()
        {
            dgvEquipment.AutoGenerateColumns = false; 
            dgvEquipment.Columns.Clear();

            
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "EquipmentId", Width = 10, });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Название", DataPropertyName = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "InventoryNumber", HeaderText = "Инв. номер", DataPropertyName = "InventoryNumber", Width = 100 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "Сер. номер", DataPropertyName = "SerialNumber", Width = 120 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Категория", DataPropertyName = "CategoryName", Width = 100 }); // Используем свойство для отображения
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Статус", DataPropertyName = "StatusName", Width = 100 });     // Используем свойство для отображения
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Место", DataPropertyName = "LocationName", Width = 120 });   // Используем свойство для отображения
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedUser", HeaderText = "Пользователь", DataPropertyName = "AssignedUserFullName", Width = 150 }); // Используем свойство для отображения

           
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullEquipmentObject", Visible = false });
        }


        private async void MainForm_Load(object sender, EventArgs e)
        {
            await LoadEquipmentDataAsync();
        }

        private async Task LoadEquipmentDataAsync()
        {
            try
            {
                toolStripStatusLabelMessage.Text = "Загрузка данных..."; 
                btnRefresh.Enabled = false;
                btnAdd.Enabled = false;
                btnEdit.Enabled = false;
                btnDelete.Enabled = false;

                List<Equipment> equipmentList = await SupabaseService.GetEquipmentListAsync();

                
                var displayList = equipmentList.Select(eq => new
                {
                    eq.EquipmentId,
                    eq.Name,
                    eq.InventoryNumber,
                    eq.SerialNumber,
                    CategoryName = eq.Category?.CategoryName ?? "N/A", 
                    StatusName = eq.Status?.StatusName ?? "N/A",
                    LocationName = eq.Location?.LocationName ?? "N/A",
                    AssignedUserFullName = eq.AssignedUser != null ? $"{eq.AssignedUser.LastName} {eq.AssignedUser.FirstName}" : "Не назначен",
                    FullEquipmentObject = eq 
                }).ToList();


                dgvEquipment.DataSource = displayList;

                dgvEquipment.Columns["Id"].DataPropertyName = "EquipmentId";
                dgvEquipment.Columns["Name"].DataPropertyName = "Name";
                dgvEquipment.Columns["InventoryNumber"].DataPropertyName = "InventoryNumber";
                dgvEquipment.Columns["SerialNumber"].DataPropertyName = "SerialNumber";
                dgvEquipment.Columns["Category"].DataPropertyName = "CategoryName";
                dgvEquipment.Columns["Status"].DataPropertyName = "StatusName";
                dgvEquipment.Columns["Location"].DataPropertyName = "LocationName";
                dgvEquipment.Columns["AssignedUser"].DataPropertyName = "AssignedUserFullName";
                dgvEquipment.Columns["FullEquipmentObject"].DataPropertyName = "FullEquipmentObject";


                toolStripStatusLabelMessage.Text = $"Загружено {equipmentList.Count} записей.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabelMessage.Text = "Ошибка загрузки.";
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnAdd.Enabled = true;
                UpdateEditDeleteButtonState(); 
            }
        }


        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadEquipmentDataAsync();
        }

        private void dgvEquipment_SelectionChanged(object sender, EventArgs e)
        {
            UpdateEditDeleteButtonState();
        }

        private void UpdateEditDeleteButtonState()
        {
            
            bool isSingleRowSelected = dgvEquipment.SelectedRows.Count == 1;
            btnEdit.Enabled = isSingleRowSelected;
            btnDelete.Enabled = isSingleRowSelected;
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            string currentUserId = _currentUser?.Id; 
            if (string.IsNullOrEmpty(currentUserId))
            {
                MessageBox.Show("Не удалось определить ID текущего пользователя.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            
            using (var form = new EquipmentForm(null, currentUserId)) 
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    await LoadEquipmentDataAsync();
                    toolStripStatusLabelMessage.Text = "Оборудование успешно добавлено.";
                }
            }
        }


        private async void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEquipment.SelectedRows.Count != 1) return;
            var selectedRowObject = dgvEquipment.SelectedRows[0].Cells["FullEquipmentObject"].Value as Equipment;
            if (selectedRowObject == null) return;

            string currentUserId = _currentUser?.Id; 
            if (string.IsNullOrEmpty(currentUserId)) {  return; }

            
            using (var form = new EquipmentForm(selectedRowObject, currentUserId)) 
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    await LoadEquipmentDataAsync();
                    toolStripStatusLabelMessage.Text = "Оборудование успешно обновлено.";
                }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvEquipment.SelectedRows.Count != 1) return;
            var selectedRowObject = dgvEquipment.SelectedRows[0].Cells["FullEquipmentObject"].Value as Equipment;
            if (selectedRowObject == null) return;

            var confirmResult = MessageBox.Show("Нужно что-то написать!" );

            if (confirmResult == DialogResult.Yes)
            {
                string currentUserId = _currentUser?.Id; 
                if (string.IsNullOrEmpty(currentUserId)) {  return; }

                try
                {
                    toolStripStatusLabelMessage.Text = "Удаление...";
                    
                    await SupabaseService.DeleteEquipmentAsync(selectedRowObject.EquipmentId, currentUserId, "Deleted via application");
                    await LoadEquipmentDataAsync();
                    toolStripStatusLabelMessage.Text = "Оборудование успешно удалено.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripStatusLabelMessage.Text = "Ошибка удаления.";
                }
            }
        }

        
        private async void dgvEquipment_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            
            if (e.RowIndex >= 0 && btnEdit.Enabled)
            {
                btnEdit_Click(sender, EventArgs.Empty); 
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void dgvEquipment_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}