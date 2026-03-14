namespace ArtemisBanking.WebApp.ViewModels.Admin;

public class DashboardViewModel
{
    // Usuarios
    public int TotalActiveClients { get; set; }
    public int TotalInactiveClients { get; set; }
    
    // Productos
    public int TotalAssignedProducts { get; set; } // Cuentas + Préstamos + Tarjetas
    
    // Préstamos
    public int TotalActiveLoans { get; set; }
    
    // Transacciones
    public int TotalTransactions { get; set; }
    public int TodayPayments { get; set; }
    
    // Financiero
    public decimal TotalSavingsBalance { get; set; }
    
    // Tarjetas
    public int TotalActiveCreditCards { get; set; }
}
