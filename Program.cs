using System;
using System.Windows.Forms;

namespace RestaurantManagementSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Initialize database
            DatabaseHelper.InitializeDatabase();
            
            // Show login form
            Application.Run(new LoginForm());
        }
    }
}
