using System;
using System.Windows.Forms;
using EquipmentAccounting.Services; 
using Supabase.Gotrue; 
using Supabase.Gotrue.Exceptions;

namespace EquipmentAccounting
{
    public partial class LoginForm : Form
    {
        
        public User AuthenticatedUser { get; private set; }


        public LoginForm()
        {
            InitializeComponent();
            
            ValidateInput();
        }

        
        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
           
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            
        }

        
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string emailOrUsername = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            lblError.Text = ""; 
            btnLogin.Enabled = false; 
            this.Cursor = Cursors.WaitCursor; 

            try
            {
                
                Session session = await SupabaseService.SignInAsync(emailOrUsername, password);

                if (session != null && session.User != null)
                {
                    
                    AuthenticatedUser = session.User; 
                    this.DialogResult = DialogResult.OK; 
                    this.Close(); 
                }
                else
                {
                    
                    lblError.Text = "Не удалось войти. Неверные данные или проблема с сервером.";
                }
            }
            catch (GotrueException gotrueEx)
            {
                
                Console.WriteLine($"Supabase Auth Error: {gotrueEx.Message} Status: {gotrueEx.StatusCode}");
                if (gotrueEx.Message.Contains("Invalid login credentials"))
                {
                    lblError.Text = "Неверный email/логин или пароль.";
                }
                else if (gotrueEx.Message.Contains("Email not confirmed"))
                {
                    lblError.Text = "Email не подтвержден. Проверьте почту.";
                }
                else
                {
                    lblError.Text = $"Ошибка аутентификации: {gotrueEx.Reason}";
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Generic Login Error: {ex}");
                lblError.Text = "Произошла ошибка при попытке входа. Проверьте соединение.";
            }
            finally
            {
                btnLogin.Enabled = true; 
                this.Cursor = Cursors.Default; 
                ValidateInput(); 
            }
        }

       
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            this.Close();
        }

       
        private void txtUsername_TextChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            
            btnLogin.Enabled = !string.IsNullOrWhiteSpace(txtUsername.Text) &&
                               !string.IsNullOrWhiteSpace(txtPassword.Text);
        }
    }
}