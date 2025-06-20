namespace DocumentAnalyticsCloudApp.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public double TotalSizeInMB { get; set; }
        public double SearchExecutionMs { get; set; }

        public double TotalClassificationTimeMs { get; set; }
    }
}
