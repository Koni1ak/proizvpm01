using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using mEquipmentAccounting.ViewModels;
using mEquipmentAccounting.Models; // Нужна модель для приведения типа

namespace mEquipmentAccounting.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EquipmentListPage : ContentPage
    {
        EquipmentListViewModel ViewModel => BindingContext as EquipmentListViewModel;

        public EquipmentListPage()
        {
            InitializeComponent();
            // Устанавливаем ViewModel, если не сделано в XAML
            if (BindingContext == null)
                BindingContext = new EquipmentListViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Загружаем данные при появлении страницы
            if (ViewModel != null && ViewModel.EquipmentItems.Count == 0) // Грузим только если пусто
            {
                await ViewModel.LoadItemsCommand();
            }
        }

        // Обработчик нажатия на элемент списка
        private async void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            // Получаем выбранный элемент
            if (e.SelectedItem is Equipment selectedEquipment)
            {
                // Переходим на страницу деталей, передавая ВЕСЬ ОБЪЕКТ
                // Это проще, чем передавать ID и загружать заново
                await Navigation.PushAsync(new EquipmentDetailPage(selectedEquipment));

                // Сбрасываем выделение в ListView
                ((ListView)sender).SelectedItem = null;
            }
        }
    }
}