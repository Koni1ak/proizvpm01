using System;
using System.Windows.Forms;
using EquipmentAccounting.Services; 

namespace EquipmentAccounting 
{
    static class Program
    {
       
        [STAThread]
        static async System.Threading.Tasks.Task Main() 
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                
                await SupabaseService.InitializeAsync();
                Console.WriteLine("Supabase client initialized successfully."); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать подключение к базе данных: {ex.Message}",
                                "Ошибка инициализации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }

            
            Application.Run(new MainForm());
        }
    }
}