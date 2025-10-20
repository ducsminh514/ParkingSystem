using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Client.Services
{
    public interface IReportService
    {
        Task<List<ReportCategory>> GetCategoriesAsync();
        Task<CustomerReport> CreateReportAsync(CustomerReport report);
        Task<List<CustomerReport>> GetMyReportsAsync(Guid customerId);
        Task<CustomerReport> GetReportDetailAsync(Guid reportId);
        Task<ReportComment> AddCommentAsync(ReportComment comment);
        Task<bool> RateReportAsync(Guid reportId, int rating);
        Task<object> GetStatisticsAsync(Guid customerId);

        // Real-time events
        event Action<Guid, string>? OnReportStatusUpdated;
        event Action<ReportComment>? OnNewCommentAdded;
        event Action<CustomerReport>? OnNewReportCreated;
    }

    public class ReportService : IReportService
    {
        private readonly ISignalRConnectionService _connectionService;
        private HubConnection Connection => _connectionService.Connection;

        public event Action<Guid, string>? OnReportStatusUpdated;
        public event Action<ReportComment>? OnNewCommentAdded;
        public event Action<CustomerReport>? OnNewReportCreated;

        public ReportService(ISignalRConnectionService connectionService)
        {
            _connectionService = connectionService;

            // Register real-time event listeners ONE TIME
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // Listen to report status updates
            Connection.On<Guid, string>("OnReportStatusUpdated", (reportId, status) =>
            {
                Console.WriteLine($"[Real-time] Report {reportId} status: {status}");
                OnReportStatusUpdated?.Invoke(reportId, status);
            });

            // Listen to new comments
            Connection.On<ReportComment>("OnNewCommentAdded", (comment) =>
            {
                Console.WriteLine($"[Real-time] New comment on report {comment.ReportId}");
                OnNewCommentAdded?.Invoke(comment);
            });

            // Listen to new reports (for staff)
            Connection.On<CustomerReport>("OnNewReportCreated", (report) =>
            {
                Console.WriteLine($"[Real-time] New report created: {report.Title}");
                OnNewReportCreated?.Invoke(report);
            });
        }

        public async Task<List<ReportCategory>> GetCategoriesAsync()
        {
            return await Connection.InvokeAsync<List<ReportCategory>>("GetReportCategories");
        }

        public async Task<CustomerReport> CreateReportAsync(CustomerReport report)
        {
            return await Connection.InvokeAsync<CustomerReport>("CreateReport", report);
        }

        public async Task<List<CustomerReport>> GetMyReportsAsync(Guid customerId)
        {
            return await Connection.InvokeAsync<List<CustomerReport>>("GetMyReports", customerId);
        }

        public async Task<CustomerReport> GetReportDetailAsync(Guid reportId)
        {
            return await Connection.InvokeAsync<CustomerReport>("GetReportDetail", reportId);
        }

        public async Task<ReportComment> AddCommentAsync(ReportComment comment)
        {
            return await Connection.InvokeAsync<ReportComment>("AddReportComment", comment);
        }

        public async Task<bool> RateReportAsync(Guid reportId, int rating)
        {
            return await Connection.InvokeAsync<bool>("RateReport", reportId, rating);
        }

        public async Task<object> GetStatisticsAsync(Guid customerId)
        {
            return await Connection.InvokeAsync<object>("GetMyReportStatistics", customerId);
        }
    }
}