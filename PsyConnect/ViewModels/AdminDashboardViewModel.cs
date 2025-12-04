public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Failed { get; set; }
    public int Online { get; set; }
    public int Onsite { get; set; }

    public string AISummary { get; set; }
}
