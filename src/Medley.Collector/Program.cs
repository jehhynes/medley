using Medley.Collector.Data;
using Microsoft.EntityFrameworkCore;

namespace Medley.Collector;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        // Initialize database on startup
        InitializeDatabase();
        
        System.Windows.Forms.Application.Run(new MainForm());
    }
    
    private static void InitializeDatabase()
    {
        try
        {
            using var context = new AppDbContext();
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize database: {ex.Message}", "Database Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}