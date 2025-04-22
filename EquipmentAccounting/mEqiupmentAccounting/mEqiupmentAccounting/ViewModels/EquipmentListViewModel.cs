using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using mEquipmentAccounting.Models;
using mEquipmentAccounting.Services;
using mEquipmentAccounting.ViewModels;

namespace mEquipmentAccounting.ViewModels
{
    public class EquipmentListViewModel : BaseViewModel
    {
  
        public ObservableCollection<Equipment> EquipmentItems { get; }

        public EquipmentListViewModel()
        {
            Title = "Оборудование";
            EquipmentItems = new ObservableCollection<Equipment>();
        }

        
        public async Task LoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            Debug.WriteLine("Executing LoadItemsCommand...");

            try
            {
                EquipmentItems.Clear();
                var items = await SupabaseService.GetEquipmentListAsync();
                Debug.WriteLine($"Loaded {items.Count} items from service.");
                foreach (var item in items)
                {
                    EquipmentItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!!!!!!!!! FAILED to load items: {ex.Message} !!!!!!!!!!");
                
                await Application.Current.MainPage.DisplayAlert("Ошибка Загрузки", $"Не удалось получить список: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                Debug.WriteLine("LoadItemsCommand finished.");
            }
        }
    }
}