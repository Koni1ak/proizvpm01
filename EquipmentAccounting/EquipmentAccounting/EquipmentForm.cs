using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; 
using System.Windows.Forms;
using System.Xml.Linq;
using EquipmentAccounting.Models; 
using EquipmentAccounting.Services; 

namespace EquipmentAccounting
{
    public partial class EquipmentForm : Form
    {
        
        private readonly Equipment _currentEquipment; 
        private readonly string _performingUserId;  
        private List<Category> _categories;
        private List<Status> _statuses;
        private List<Location> _locations;
        private List<User> _users; 

        
        public EquipmentForm(Equipment equipmentToEdit, string performingUserId)
        {
            InitializeComponent();

            _currentEquipment = equipmentToEdit; 

            
            if (string.IsNullOrWhiteSpace(performingUserId))
            {
               
                throw new ArgumentNullException(nameof(performingUserId), "Performing User ID cannot be null or empty.");
            }
            _performingUserId = performingUserId;

     
            this.Text = _currentEquipment == null ? "Добавить оборудование" : "Редактировать оборудование";
        }

        
        private async void EquipmentForm_Load(object sender, EventArgs e)
        {
          
            try
            {
                await LoadComboBoxDataAsync(); 
                PopulateFields();             
            }
            catch (Exception ex)
            {
                
                MessageBox.Show($"Критическая ошибка при загрузке справочников: {ex.Message}\nФорма не может быть использована.",
                                "Ошибка загрузки данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Abort; 
                this.Close();                           
            }
        }

      
        private async Task LoadComboBoxDataAsync()
        {
            
            var categoryTask = SupabaseService.GetCategoriesAsync();
            var statusTask = SupabaseService.GetStatusesAsync();
            var locationTask = SupabaseService.GetLocationsAsync();
            var userTask = SupabaseService.GetUsersAsync(); 

            
            await Task.WhenAll(categoryTask, statusTask, locationTask, userTask);

            
            _categories = await categoryTask;
            _statuses = await statusTask;
            _locations = await locationTask;
            _users = await userTask;

            
            Console.WriteLine($"Categories loaded for ComboBox: {_categories?.Count ?? 0}");
            Console.WriteLine($"Statuses loaded for ComboBox: {_statuses?.Count ?? 0}");
            Console.WriteLine($"Locations loaded for ComboBox: {_locations?.Count ?? 0}");
            Console.WriteLine($"Users (dictionary) loaded for ComboBox: {_users?.Count ?? 0}");
        

         
            if (_categories == null || _statuses == null || _locations == null || _users == null)
            {
                throw new InvalidOperationException("Один или несколько справочников не удалось загрузить.");
            }

          

          
            cmbCategory.DataSource = _categories;
            cmbCategory.DisplayMember = nameof(Category.CategoryName); 
            cmbCategory.ValueMember = nameof(Category.CategoryId);

          
            cmbStatus.DataSource = _statuses;
            cmbStatus.DisplayMember = nameof(Status.StatusName);
            cmbStatus.ValueMember = nameof(Status.StatusId);

            
            cmbLocation.DataSource = _locations;
            cmbLocation.DisplayMember = nameof(EquipmentAccounting.Models.Location.LocationName);
            cmbLocation.ValueMember = nameof(EquipmentAccounting.Models.Location.LocationId);

            
            var userListWithNone = new List<User>
            {
               
                new User { UserId = 0, FirstName = "Не", LastName = "назначен" }
            };
            
            userListWithNone.AddRange(_users.Where(u => u.IsActive).OrderBy(u => u.LastName).ThenBy(u => u.FirstName));

            cmbAssignedUser.DataSource = userListWithNone;
            cmbAssignedUser.DisplayMember = nameof(User.FullName); 
            cmbAssignedUser.ValueMember = nameof(User.UserId);
        }

        
        private void PopulateFields()
        {
            if (_currentEquipment != null) 
            {
                Name.Text = _currentEquipment.Name;
                SerialNumber.Text = _currentEquipment.SerialNumber;
                InventoryNumber.Text = _currentEquipment.InventoryNumber;
                Description.Text = _currentEquipment.Description;

                
                dtpPurchaseDate.Checked = _currentEquipment.PurchaseDate.HasValue;
                if (_currentEquipment.PurchaseDate.HasValue)
                {
                   
                    dtpPurchaseDate.Value = _currentEquipment.PurchaseDate.Value.Date;
                }

                dtpWarrantyExpiryDate.Checked = _currentEquipment.WarrantyExpiryDate.HasValue;
                if (_currentEquipment.WarrantyExpiryDate.HasValue)
                {
                    dtpWarrantyExpiryDate.Value = _currentEquipment.WarrantyExpiryDate.Value.Date;
                }

               
                cmbCategory.SelectedValue = _currentEquipment.CategoryId;
                cmbStatus.SelectedValue = _currentEquipment.StatusId;
                cmbLocation.SelectedValue = _currentEquipment.LocationId;
                
                cmbAssignedUser.SelectedValue = _currentEquipment.AssignedUserId ?? 0;

                
                lblNotes.Visible = true;
                Notes.Visible = true;
                Notes.Text = string.Empty; 
            }
            else 
            {
               
                lblNotes.Visible = false;
                Notes.Visible = false;
                Notes.Text = "Запись создана через приложение"; 
            }
        }

        
        private async void btnSave_Click(object sender, EventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show("Поле 'Название' не может быть пустым.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Name.Focus();
                return;
            }
            if (cmbCategory.SelectedValue == null)
            {
                MessageBox.Show("Необходимо выбрать 'Категорию'.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return;
            }
            if (cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Необходимо выбрать 'Статус'.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return;
            }
            if (cmbLocation.SelectedValue == null)
            {
                MessageBox.Show("Необходимо выбрать 'Местоположение'.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbLocation.Focus();
                return;
            }
            if (cmbAssignedUser.SelectedValue == null)
            {
                
                MessageBox.Show("Необходимо выбрать 'Пользователя' (или 'Не назначен').", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbAssignedUser.Focus();
                return;
            }
            

            
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

            
            long selectedAssignedUserId = (long)cmbAssignedUser.SelectedValue;
            equipmentData.AssignedUserId = selectedAssignedUserId == 0 ? (long?)null : selectedAssignedUserId;

           
            string notes = string.IsNullOrWhiteSpace(Notes.Text) ? null : Notes.Text.Trim();

            
            btnSave.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
               
                if (string.IsNullOrWhiteSpace(_performingUserId))
                {
                    throw new InvalidOperationException("Не удалось определить ID пользователя для сохранения операции.");
                }


                if (_currentEquipment == null) 
                {
                    
                    if (string.IsNullOrWhiteSpace(notes)) notes = "Запись создана через приложение";
                  
                    await SupabaseService.AddEquipmentAsync(equipmentData, _performingUserId, notes);
                    MessageBox.Show("Оборудование успешно добавлено.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else 
                {
                    
                    if (string.IsNullOrWhiteSpace(notes)) notes = "Запись обновлена через приложение";
                    
                    await SupabaseService.UpdateEquipmentAsync(equipmentData, _currentEquipment, _performingUserId, notes);
                    MessageBox.Show("Оборудование успешно обновлено.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK; 
                this.Close();                      
            }
            catch (ArgumentNullException argEx) when (argEx.ParamName == "changedByUserId")
            {
                
                MessageBox.Show($"Критическая ошибка: ID пользователя не определен для сохранения.\n{argEx.Message}",
                                "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException opEx)
            {
                MessageBox.Show($"Операция не может быть выполнена: {opEx.Message}",
                               "Ошибка операции", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Save Error: {ex}"); 
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}",
                                "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            finally
            {
                
                btnSave.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;  
            this.Close();                           
        }
    }
}