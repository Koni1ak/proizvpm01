using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using mEquipmentAccounting.Models; // Нужна модель

namespace mEquipmentAccounting.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EquipmentDetailPage : ContentPage
    {
        private Equipment _equipment; // Храним переданный объект

        // Конструктор принимает объект Equipment
        public EquipmentDetailPage(Equipment equipment)
        {
            InitializeComponent();
            _equipment = equipment;
            // Устанавливаем заголовок страницы
            Title = equipment?.Name ?? "Детали";
            // Заполняем поля напрямую
            DisplayEquipmentDetails();
        }

        private void DisplayEquipmentDetails()
        {
            if (_equipment == null) return;

            NameLabel.Text = _equipment.Name ?? "-";
            InventoryLabel.Text = _equipment.InventoryNumber ?? "-";
            SerialLabel.Text = _equipment.SerialNumber ?? "-";

            // Отображаем данные из связанных объектов, если они были загружены
            CategoryLabel.Text = _equipment.Category?.CategoryName ?? "N/A";
            StatusLabel.Text = _equipment.Status?.StatusName ?? "N/A";
            LocationLabel.Text = _equipment.Location?.LocationName ?? "N/A";

            // Формируем имя пользователя
            if (_equipment.AssignedUser != null)
            {
                UserLabel.Text = $"{_equipment.AssignedUser.LastName} {_equipment.AssignedUser.FirstName}".Trim();
                if (string.IsNullOrWhiteSpace(UserLabel.Text)) // Если имя пустое, показать логин
                {
                    UserLabel.Text = _equipment.AssignedUser.Username ?? "-";
                }
            }
            else
            {
                UserLabel.Text = "Не назначен";
            }

            // Показываем описание, если оно есть
            if (!string.IsNullOrWhiteSpace(_equipment.Description))
            {
                DescriptionHeaderLabel.IsVisible = true;
                DescriptionLabel.Text = _equipment.Description;
            }
            else
            {
                DescriptionHeaderLabel.IsVisible = false;
                DescriptionLabel.Text = string.Empty;
            }
        }
    }
}