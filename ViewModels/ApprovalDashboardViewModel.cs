namespace QuanLyKTX.ViewModels
{
    public class ApprovalDashboardViewModel
    {
        public int PendingStudentRegistrationCount { get; set; }
        public int PendingFormCount { get; set; }
        public int PendingVehicleCount { get; set; }

        public int TotalPending => PendingStudentRegistrationCount + PendingFormCount + PendingVehicleCount;

        public List<ApprovalListItemViewModel> PendingStudentRegistrations { get; set; } = new();
        public List<ApprovalListItemViewModel> PendingForms { get; set; } = new();
        public List<ApprovalListItemViewModel> PendingVehicles { get; set; } = new();
    }

    public class ApprovalListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
