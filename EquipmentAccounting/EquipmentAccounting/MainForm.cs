using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EquipmentAccounting.Models; // Проверьте namespace
using EquipmentAccounting.Services; // Проверьте namespace
using System.Diagnostics; // Для Debug

namespace EquipmentAccounting // Проверьте namespace
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ConfigureDataGridView();
        }

        private void ConfigureDataGridView()
        {
            dgvEquipment.AutoGenerateColumns = false;
            dgvEquipment.Columns.Clear();
            dgvEquipment.ReadOnly = true;
            dgvEquipment.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEquipment.AllowUserToAddRows = false;
            dgvEquipment.AllowUserToDeleteRows = false;
            dgvEquipment.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // Или другая настройка

            // Добавляем столбцы (настройте DataPropertyName и HeaderText)
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdCol", HeaderText = "ID", DataPropertyName = "EquipmentId", Width = 50 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "NameCol", HeaderText = "Название", DataPropertyName = "Name", FillWeight = 200 }); // Пример FillWeight
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvNumCol", HeaderText = "Инв. номер", DataPropertyName = "InventoryNumber", Width = 100 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumCol", HeaderText = "Сер. номер", DataPropertyName = "SerialNumber", Width = 120 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryCol", HeaderText = "Категория", DataPropertyName = "CategoryName", Width = 100 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusCol", HeaderText = "Статус", DataPropertyName = "StatusName", Width = 100 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "LocationCol", HeaderText = "Место", DataPropertyName = "LocationName", Width = 120 });
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserCol", HeaderText = "Пользователь", DataPropertyName = "AssignedUserFullName", Width = 150 });

            // Скрытый столбец для хранения полного объекта Equipment
            dgvEquipment.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullEquipmentObjectCol", Visible = false, DataPropertyName = "FullEquipmentObject" });
        }


        private async void MainForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("MainForm: Loading...");
            await LoadEquipmentDataAsync();
        }

        private async Task LoadEquipmentDataAsync()
        {
            Debug.WriteLine("MainForm: LoadEquipmentDataAsync started...");
            try
            {
                 SetLoadingState(true); // Блокируем UI
                 List<Equipment> equipmentList = await SupabaseService.GetEquipmentListAsync();
                 Debug.WriteLine($"MainForm: Received {equipmentList.Count} items from service.");

                // Создаем проекцию для удобного отображения связанных данных
                var displayList = equipmentList.Select(eq => new
                {
                    eq.EquipmentId,
                    eq.Name,
                    eq.InventoryNumber,
                    eq.SerialNumber,
                    CategoryName = eq.Category?.CategoryName ?? "N/A",
                    StatusName = eq.Status?.StatusName ?? "N/A",
                    LocationName = eq.Location?.LocationName ?? "N/A",
                    // Формируем полное имя пользователя
                    AssignedUserFullName = eq.AssignedUser != null
                                           ? $"{eq.AssignedUser.LastName} {eq.AssignedUser.FirstName}".Trim()
                                           : "Не назначен",
                    FullEquipmentObject = eq // Сохраняем весь объект
                }).ToList();


                
                 dgvEquipment.DataSource = displayList; // Прямая привязка тоже работает

                 if (dgvEquipment.Rows.Count > 0)
                 {
                     dgvEquipment.ClearSelection();
                 }

                 UpdateStatusStrip($"Загружено {equipmentList.Count} записей.");
                 Debug.WriteLine("MainForm: DataGridView updated.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! MainForm: Error loading data: {ex}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 UpdateStatusStrip("Ошибка загрузки данных.");
            }
            finally
            {
                 SetLoadingState(false); // Разблокируем UI
                 Debug.WriteLine("MainForm: LoadEquipmentDataAsync finished.");
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            this.Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
            btnAdd.Enabled = !isLoading;
            btnRefresh.Enabled = !isLoading;
            dgvEquipment.Enabled = !isLoading;

            // Правильно устанавливаем Edit/Delete: они зависят и от isLoading, и от SelectionChanged
            bool isSingleRowSelected = dgvEquipment.SelectedRows.Count == 1;
            btnEdit.Enabled = !isLoading && isSingleRowSelected;
            btnDelete.Enabled = !isLoading && isSingleRowSelected;

            // Обновляем StatusStrip (не влияет на кнопки, но полезно)
            // UpdateStatusStrip(isLoading ? "Загрузка данных..." : ""); // Обновляется в LoadEquipmentDataAsync
        }

        private void UpdateStatusStrip(string message)
        {
             
        }


        private async void btnRefresh_Click(object sender, EventArgs e)
        {
             Debug.WriteLine("MainForm: Refresh button clicked.");
             await LoadEquipmentDataAsync();
        }

        private void dgvEquipment_SelectionChanged(object sender, EventArgs e)
        {
             // Обновляем доступность кнопок Edit/Delete при изменении выбора
             bool isSingleRowSelected = dgvEquipment.SelectedRows.Count == 1;
             btnEdit.Enabled = isSingleRowSelected;
             btnDelete.Enabled = isSingleRowSelected;
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
             Debug.WriteLine("MainForm: Add button clicked.");
             // Открываем форму добавления
            using (var form = new EquipmentForm()) // null - режим добавления
            {
                 Debug.WriteLine("MainForm: Opening EquipmentForm for Add.");
                 if (form.ShowDialog(this) == DialogResult.OK) // Указываем владельца
                 {
                      Debug.WriteLine("MainForm: EquipmentForm returned OK (Add). Reloading data...");
                     // Если сохранили, обновляем список
                     await LoadEquipmentDataAsync();
                     UpdateStatusStrip("Оборудование успешно добавлено.");
                 } else {
                      Debug.WriteLine("MainForm: EquipmentForm returned Cancel (Add).");
                 }
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("MainForm: Edit button clicked.");
            if (dgvEquipment.SelectedRows.Count != 1)
            {
                Debug.WriteLine("MainForm: Edit button clicked, but no single row selected.");
                return;
            }

            DataGridViewRow selectedRow = dgvEquipment.SelectedRows[0];
            Equipment selectedEquipment = null;

            
            if (selectedRow.DataBoundItem != null)
            {
                
                var dataItem = selectedRow.DataBoundItem;
                try
                {
                    
                    dynamic dynamicItem = dataItem; 
                    selectedEquipment = dynamicItem.FullEquipmentObject as Equipment;
                    
                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"!!! MainForm: Error accessing FullEquipmentObject from DataBoundItem: {ex.Message}");
                    // Ошибка при доступе к свойству анонимного типа
                }
            }
            // --- КОНЕЦ НАДЕЖНОГО СПОСОБА ---

            if (selectedEquipment == null)
            {
                Debug.WriteLine("!!! MainForm: Failed to retrieve Equipment object for editing.");
                MessageBox.Show("Не удалось получить данные выбранного оборудования для редактирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Важно выйти, если объект не получен
            }

            Debug.WriteLine($"MainForm: Editing Equipment ID {selectedEquipment.EquipmentId}, Name: {selectedEquipment.Name}");

            // Открываем форму редактирования, передавая ПОЛУЧЕННЫЙ объект
            using (var form = new EquipmentForm(selectedEquipment))
            {
                Debug.WriteLine("MainForm: Opening EquipmentForm for Edit.");
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    Debug.WriteLine("MainForm: EquipmentForm returned OK (Edit). Reloading data...");
                    await LoadEquipmentDataAsync();
                    UpdateStatusStrip("Оборудование успешно обновлено.");
                }
                else
                {
                    Debug.WriteLine("MainForm: EquipmentForm returned Cancel (Edit).");
                }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
             Debug.WriteLine("MainForm: Delete button clicked.");
             if (dgvEquipment.SelectedRows.Count != 1)
             {
                 Debug.WriteLine("MainForm: Delete button clicked, but no single row selected.");
                 return;
             }

             // Получаем объект так же, как при редактировании
             DataGridViewRow selectedRow = dgvEquipment.SelectedRows[0];
             var selectedRowObject = selectedRow.Cells["FullEquipmentObjectCol"]?.Value as Equipment;
              if (selectedRowObject == null)
             {
                 if (dgvEquipment.DataSource is IList<dynamic> dataSource && selectedRow.Index < dataSource.Count) {
                     var dynamicObject = dataSource[selectedRow.Index];
                     try {
                         selectedRowObject = dynamicObject.GetType().GetProperty("FullEquipmentObject")?.GetValue(dynamicObject, null) as Equipment;
                     } catch {}
                 }
                 if (selectedRowObject == null) {
                      Debug.WriteLine("!!! MainForm: Failed to retrieve Equipment object for deletion.");
                      MessageBox.Show("Не удалось получить данные для удаления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                      return;
                 }
             }
             Debug.WriteLine($"MainForm: Attempting to delete Equipment ID {selectedRowObject.EquipmentId}, Name: {selectedRowObject.Name}");


            // Подтверждение удаления
            var confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить '{selectedRowObject.Name}' (Инв. №: {selectedRowObject.InventoryNumber ?? "N/A"})?",
                                               "Подтверждение удаления",
                                               MessageBoxButtons.YesNo,
                                               MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                Debug.WriteLine("MainForm: Deletion confirmed by user.");
                try
                {
                    SetLoadingState(true); // Блокируем UI на время удаления
                    // Используем ID пользователя-заглушки
                    long currentUserId = SupabaseService.GetCurrentUserIdPlaceholder();
                    await SupabaseService.DeleteEquipmentAsync(selectedRowObject.EquipmentId, currentUserId, "Deleted via application");
                    Debug.WriteLine($"MainForm: Delete operation for ID {selectedRowObject.EquipmentId} completed in service.");

                    // Обновляем список ПОСЛЕ успешного удаления
                    await LoadEquipmentDataAsync();
                    UpdateStatusStrip("Оборудование успешно удалено.");
                }
                catch (Exception ex)
                {
                     Debug.WriteLine($"!!! MainForm: Error during deletion: {ex}");
                     MessageBox.Show($"Ошибка удаления: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     UpdateStatusStrip("Ошибка удаления.");
                }
                finally
                {
                    SetLoadingState(false); // Разблокируем UI в любом случае
                }
            } else {
                 Debug.WriteLine("MainForm: Deletion cancelled by user.");
            }
        }

        // Обработка двойного клика для редактирования
        private void dgvEquipment_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
             // Проверяем, что клик был по строке (не по заголовку) и кнопка Edit доступна
             if (e.RowIndex >= 0 && btnEdit.Enabled)
             {
                  Debug.WriteLine($"MainForm: Double-click detected on row {e.RowIndex}. Initiating Edit.");
                  btnEdit_Click(sender, EventArgs.Empty); // Вызываем обработчик кнопки Edit
             }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}