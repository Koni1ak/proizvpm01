using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EquipmentAccounting.Models; 
using EquipmentAccounting.Services; 
using System.Diagnostics;
using Guna.UI2.WinForms;

namespace EquipmentAccounting 
{
    public partial class EquipmentForm : Form
    {
        private Equipment _currentEquipment; 
        private Equipment _originalEquipmentData; 
        private List<Category> _categories;
        private List<Status> _statuses;
        private List<Location> _locations;
        private List<User> _users;

        
        public EquipmentForm() : this(null) { }

        
        public EquipmentForm(Equipment equipmentToEdit)
        {
            InitializeComponent();
            _currentEquipment = equipmentToEdit;

            
            if (_currentEquipment != null)
            {
                
                _originalEquipmentData = new Equipment
                {
                    
                    EquipmentId = _currentEquipment.EquipmentId,
                    Name = _currentEquipment.Name,
                    SerialNumber = _currentEquipment.SerialNumber,
                    InventoryNumber = _currentEquipment.InventoryNumber,
                    Description = _currentEquipment.Description,
                    PurchaseDate = _currentEquipment.PurchaseDate,
                    WarrantyExpiryDate = _currentEquipment.WarrantyExpiryDate,
                    CategoryId = _currentEquipment.CategoryId,
                    StatusId = _currentEquipment.StatusId,
                    LocationId = _currentEquipment.LocationId,
                    AssignedUserId = _currentEquipment.AssignedUserId,
                    CreatedAt = _currentEquipment.CreatedAt, 
                    UpdatedAt = _currentEquipment.UpdatedAt 
                                                           
                };
                this.Text = $"Редактировать: {_currentEquipment.Name}";
            }
            else
            {
                this.Text = "Добавить новое оборудование";
            }
        }

        private async void EquipmentForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("EquipmentForm: Loading...");
            this.Cursor = Cursors.WaitCursor;
            try
            {
                await LoadComboBoxDataAsync();
                PopulateFields();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! EquipmentForm: Error loading ComboBox data: {ex}");
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.BeginInvoke(new Action(() => this.Close())); 
            }
            finally
            {
                this.Cursor = Cursors.Default;
                Debug.WriteLine("EquipmentForm: Load complete.");
            }
        }

        private async Task LoadComboBoxDataAsync()
        {
            Debug.WriteLine("EquipmentForm: Loading ComboBox data...");
            // Загружаем данные параллельно
            var categoryTask = SupabaseService.GetCategoriesAsync();
            var statusTask = SupabaseService.GetStatusesAsync();
            var locationTask = SupabaseService.GetLocationsAsync();
            var userTask = SupabaseService.GetUsersAsync(); // Загружаем пользователей

            await Task.WhenAll(categoryTask, statusTask, locationTask, userTask);

            _categories = await categoryTask;
            _statuses = await statusTask;
            _locations = await locationTask;
            _users = await userTask;
            Debug.WriteLine($"EquipmentForm: Loaded {_categories.Count} categories, {_statuses.Count} statuses, {_locations.Count} locations, {_users.Count} users.");


            // Настройка ComboBox'ов
            SetupComboBox(cmbCategory, _categories, "CategoryName", "CategoryId");
            SetupComboBox(cmbStatus, _statuses, "StatusName", "StatusId");
            SetupComboBox(cmbLocation, _locations, "LocationName", "LocationId");

            // Пользователи + опция "Не назначен"
            var userListWithNone = new List<User> { new User { UserId = 0, FirstName = "Не", LastName = "назначен" } }; // ID 0 - признак null
            // Добавляем только активных пользователей и сортируем
            userListWithNone.AddRange(_users.Where(u => u.IsActive).OrderBy(u => u.LastName).ThenBy(u => u.FirstName));
            SetupComboBox(cmbAssignedUser, userListWithNone, "FullName", "UserId"); // Используем свойство FullName
            Debug.WriteLine("EquipmentForm: ComboBoxes setup complete.");
        }

        // Вспомогательный метод для настройки ComboBox
        private void SetupComboBox(ComboBox comboBox, object dataSource, string displayMember, string valueMember)
        {
            comboBox.DataSource = dataSource;
            comboBox.DisplayMember = displayMember;
            comboBox.ValueMember = valueMember;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList; // Запретить ручной ввод
            comboBox.SelectedIndex = -1; // Сбросить выбор по умолчанию
        }


        private void PopulateFields()
        {
            Debug.WriteLine("EquipmentForm: Populating fields...");
            if (_currentEquipment != null) // Режим редактирования
            {
                Name.Text = _currentEquipment.Name;
                SerialNumber.Text = _currentEquipment.SerialNumber;
                InventoryNumber.Text = _currentEquipment.InventoryNumber;
                Description.Text = _currentEquipment.Description;

                SetDateTimePickerValue(dtpPurchaseDate, _currentEquipment.PurchaseDate);
                SetDateTimePickerValue(dtpWarrantyExpiryDate, _currentEquipment.WarrantyExpiryDate);

                cmbCategory.SelectedValue = _currentEquipment.CategoryId;
                cmbStatus.SelectedValue = _currentEquipment.StatusId;
                cmbLocation.SelectedValue = _currentEquipment.LocationId;
                cmbAssignedUser.SelectedValue = _currentEquipment.AssignedUserId ?? 0; // 0 для "Не назначен"

                // Поле заметок для редактирования должно быть пустым
                Notes.Text = string.Empty;
                lblNotes.Visible = true; // Показываем поле заметок
                Notes.Visible = true;
                Debug.WriteLine("EquipmentForm: Fields populated for Edit mode.");
            }
            else // Режим добавления
            {
                // Сбросить поля или установить значения по умолчанию
                Name.Text = string.Empty;
                SerialNumber.Text = string.Empty;
                InventoryNumber.Text = string.Empty;
                Description.Text = string.Empty;
                dtpPurchaseDate.Checked = false; // Сбросить дату
                dtpWarrantyExpiryDate.Checked = false; // Сбросить дату
                cmbCategory.SelectedIndex = -1;
                cmbStatus.SelectedIndex = -1;
                cmbLocation.SelectedIndex = -1;
                cmbAssignedUser.SelectedValue = 0; // "Не назначен" по умолчанию

                // Скрываем поле заметок при добавлении (или оставляем для начальной заметки)
                lblNotes.Visible = false;
                Notes.Visible = false;
                Notes.Text = "Equipment created via application"; // Заметка по умолчанию для создания
                Debug.WriteLine("EquipmentForm: Fields reset for Add mode.");
            }
        }

        // Вспомогательный метод для установки значения DateTimePicker (с поддержкой null)
        private void SetDateTimePickerValue(Guna2DateTimePicker dtp, DateTime? value)
        {
            if (value.HasValue)
            {
                // Убедимся, что значение в допустимом диапазоне DateTimePicker
                // Свойства MinDate/MaxDate/Value/Checked обычно совпадают у Guna
                if (value.Value >= dtp.MinDate && value.Value <= dtp.MaxDate)
                {
                    dtp.Value = value.Value;
                    dtp.Checked = true; // Guna2DateTimePicker также имеет свойство Checked
                }
                else
                {
                    // Значение вне диапазона - сбрасываем
                    dtp.Checked = false;
                    Debug.WriteLine($"!!! Warning: Date {value.Value} is out of range for {dtp.Name}.");
                }
            }
            else
            {
                dtp.Checked = false; // Если null, снимаем галочку
            }
        }


        private async void btnSave_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("EquipmentForm: Save button clicked.");
            // --- Валидация ---
            if (!ValidateInput())
            {
                Debug.WriteLine("EquipmentForm: Validation failed.");
                return;
            }
            Debug.WriteLine("EquipmentForm: Validation passed.");

            // --- Сбор данных ---
            // Создаем новый объект или получаем ссылку на редактируемый
            var equipmentData = _currentEquipment ?? new Equipment();

            equipmentData.Name = Name.Text.Trim();
            equipmentData.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber.Text) ? null : SerialNumber.Text.Trim();
            equipmentData.InventoryNumber = string.IsNullOrWhiteSpace(InventoryNumber.Text) ? null : InventoryNumber.Text.Trim();
            equipmentData.Description = string.IsNullOrWhiteSpace(Description.Text) ? null : Description.Text.Trim();
            equipmentData.PurchaseDate = dtpPurchaseDate.Checked ? dtpPurchaseDate.Value.Date : (DateTime?)null;
            equipmentData.WarrantyExpiryDate = dtpWarrantyExpiryDate.Checked ? dtpWarrantyExpiryDate.Value.Date : (DateTime?)null;
            equipmentData.CategoryId = (long)cmbCategory.SelectedValue;
            equipmentData.StatusId = (long)cmbStatus.SelectedValue;
            equipmentData.LocationId = (long)cmbLocation.SelectedValue;
            long selectedUserId = (long)cmbAssignedUser.SelectedValue;
            equipmentData.AssignedUserId = selectedUserId == 0 ? (long?)null : selectedUserId;

            // Получаем ID пользователя-заглушки
            long currentUserId = SupabaseService.GetCurrentUserIdPlaceholder();
            string notes = string.IsNullOrWhiteSpace(Notes.Text) ? null : Notes.Text.Trim();


            this.Cursor = Cursors.WaitCursor;
            btnSave.Enabled = false; // Блокируем кнопку на время сохранения

            try
            {
                if (_currentEquipment == null) // --- Добавление ---
                {
                    Debug.WriteLine("EquipmentForm: Calling AddEquipmentAsync...");
                    if (string.IsNullOrWhiteSpace(notes)) notes = "Created via application";
                    await SupabaseService.AddEquipmentAsync(equipmentData, currentUserId, notes);
                    Debug.WriteLine("EquipmentForm: AddEquipmentAsync completed.");
                }
                else // --- Редактирование ---
                {
                    Debug.WriteLine("EquipmentForm: Calling UpdateEquipmentAsync...");
                    if (string.IsNullOrWhiteSpace(notes)) notes = "Updated via application";
                    // Передаем обновленные данные и ОРИГИНАЛЬНЫЕ данные для истории
                    await SupabaseService.UpdateEquipmentAsync(equipmentData, _originalEquipmentData, currentUserId, notes);
                    Debug.WriteLine("EquipmentForm: UpdateEquipmentAsync completed.");
                }

                this.DialogResult = DialogResult.OK; // Сигнализируем об успехе
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! EquipmentForm: Error saving data: {ex}");
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Остаемся на форме
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnSave.Enabled = true; // Разблокируем кнопку
            }
        }

        private bool ValidateInput()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show("Поле 'Название' не может быть пустым.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Name.Focus();
                return false;
            }
            if (cmbCategory.SelectedValue == null || (long)cmbCategory.SelectedValue <= 0)
            {
                MessageBox.Show("Выберите категорию.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }
            if (cmbStatus.SelectedValue == null || (long)cmbStatus.SelectedValue <= 0)
            {
                MessageBox.Show("Выберите статус.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return false;
            }
            if (cmbLocation.SelectedValue == null || (long)cmbLocation.SelectedValue <= 0)
            {
                MessageBox.Show("Выберите местоположение.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbLocation.Focus();
                return false;
            }

            // Дополнительные проверки (например, уникальность номеров - лучше на стороне БД)
            // ...

            return true; // Все проверки пройдены
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("EquipmentForm: Cancel button clicked.");
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}