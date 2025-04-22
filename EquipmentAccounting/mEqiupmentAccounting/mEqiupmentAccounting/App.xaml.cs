using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using mEquipmentAccounting.Services;
using mEquipmentAccounting.Views;
using System.Threading.Tasks;
using System.Linq; 

namespace mEquipmentAccounting
{
    public partial class App : Application
    {
        private static bool _supabaseInitialized = false;

        public App()
        {
            InitializeComponent();

            // Запускаем инициализацию асинхронно
            InitializeSupabaseAsync().ContinueWith(task =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (task.IsFaulted || !_supabaseInitialized)
                    {
                        // Показываем ошибку
                        string errorMessage = task.Exception?.InnerExceptions?.FirstOrDefault()?.Message ?? task.Exception?.Message ?? "Unknown error during initialization.";
                        Current.MainPage = new ContentPage(); // Нужна страница для DisplayAlert
                        Current.MainPage.DisplayAlert("Ошибка инициализации",
                            $"Не удалось подключиться к базе данных: {errorMessage}",
                            "OK")
                        .ContinueWith(_ => Application.Current.Quit()); // Закрываем приложение
                    }
                    else
                    {
                        // Устанавливаем главную страницу с навигацией
                        MainPage = new NavigationPage(new EquipmentListPage());
                    }
                });
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Выполняем ContinueWith в UI потоке

            // Можно показать индикатор загрузки, пока идет инициализация
            MainPage = new ContentPage { Content = new ActivityIndicator { IsRunning = true, VerticalOptions = LayoutOptions.CenterAndExpand } };
        }

        private async Task InitializeSupabaseAsync()
        {
            try
            {
                await SupabaseService.InitializeAsync();
                _supabaseInitialized = SupabaseService.IsInitialized;
            }
            catch (Exception ex)
            {
                _supabaseInitialized = false;
                System.Diagnostics.Debug.WriteLine($"!!!!!!!!!! Top level Initialization Error: {ex}");
                throw; // Пробрасываем для обработки в ContinueWith
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
