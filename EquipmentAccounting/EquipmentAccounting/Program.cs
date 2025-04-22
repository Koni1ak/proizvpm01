using System;
using System.Windows.Forms;
using EquipmentAccounting.Services;
using Supabase.Gotrue; 

namespace EquipmentAccounting
{
    static class Program
    {
     
        [STAThread]
        static async System.Threading.Tasks.Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            User authenticatedUser = null; 
            try
            {
                await SupabaseService.InitializeAsync(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать подключение: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }

            
            while (authenticatedUser == null)
            {
                using (var loginForm = new LoginForm())
                {
                    var dialogResult = loginForm.ShowDialog(); 

                    if (dialogResult == DialogResult.OK)
                    {
                        
                        authenticatedUser = loginForm.AuthenticatedUser; 
                    }
                    else 
                    {
                        
                        Application.Exit(); 
                        return;
                    }
                }
            }

            
            if (authenticatedUser != null)
            {
                
                Application.Run(new MainForm(authenticatedUser));
            }
        }
    }
}