using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Client.Services
{
    public interface IStaffManagementService
    {
        Task<StaffListResponse> GetAllStaffs();
        Task<StaffOperationResponse> CreateStaff(CreateStaffRequest request);
        Task<StaffOperationResponse> UpdateStaff(UpdateStaffRequest request);
        Task<StaffOperationResponse> DeleteStaff(DeleteStaffRequest request);

        event Action<StaffDto>? OnStaffCreated;
        event Action<StaffDto>? OnStaffUpdated;
        event Action<Guid>? OnStaffDeleted;
    }

    public class StaffManagementService : IStaffManagementService
    {
        private readonly ISignalRConnectionService _connectionService;
        private HubConnection Connection => _connectionService.Connection;
        private bool _handlersRegistered = false;
        private readonly SemaphoreSlim _registerLock = new(1, 1);

        public event Action<StaffDto>? OnStaffCreated;
        public event Action<StaffDto>? OnStaffUpdated;
        public event Action<Guid>? OnStaffDeleted;

        public StaffManagementService(ISignalRConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task EnsureConnectedAsync()
        {
            await _connectionService.StartAsync();

            await _registerLock.WaitAsync();
            try
            {
                if (!_handlersRegistered)
                {
                    RegisterEventHandlers();
                    _handlersRegistered = true;
                    Console.WriteLine("[StaffManagementService] Event handlers registered");
                }
            }
            finally
            {
                _registerLock.Release();
            }
        }

        private void RegisterEventHandlers()
        {
            Connection.On<StaffDto>("OnStaffCreated", (staff) =>
            {
                Console.WriteLine($"[SignalR Event] Staff created: {staff.FullName}");
                OnStaffCreated?.Invoke(staff);
            });

            Connection.On<StaffDto>("OnStaffUpdated", (staff) =>
            {
                Console.WriteLine($"[SignalR Event] Staff updated: {staff.FullName}");
                OnStaffUpdated?.Invoke(staff);
            });

            Connection.On<Guid>("OnStaffDeleted", (staffId) =>
            {
                Console.WriteLine($"[SignalR Event] Staff deleted: {staffId}");
                OnStaffDeleted?.Invoke(staffId);
            });
        }

        public async Task<StaffListResponse> GetAllStaffs()
        {
            try
            {
                await EnsureConnectedAsync();

                Console.WriteLine("[GetAllStaffs] Requesting staff list");

                var response = await Connection.InvokeAsync<StaffListResponse>("GetAllStaffs");

                Console.WriteLine($"[GetAllStaffs] Received {response.TotalCount} staffs");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAllStaffs Error] {ex.Message}");
                return new StaffListResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}",
                    Staffs = new List<StaffDto>(),
                    TotalCount = 0
                };
            }
        }

        public async Task<StaffOperationResponse> CreateStaff(CreateStaffRequest request)
        {
            try
            {
                await EnsureConnectedAsync();

                Console.WriteLine($"[CreateStaff] Creating staff: {request.Username}");

                var response = await Connection.InvokeAsync<StaffOperationResponse>(
                    "CreateStaff",
                    request
                );

                Console.WriteLine($"[CreateStaff] Response: Success={response.Success}");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateStaff Error] {ex.Message}");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<StaffOperationResponse> UpdateStaff(UpdateStaffRequest request)
        {
            try
            {
                await EnsureConnectedAsync();

                Console.WriteLine($"[UpdateStaff] Updating staff: {request.StaffId}");

                var response = await Connection.InvokeAsync<StaffOperationResponse>(
                    "UpdateStaff",
                    request
                );

                Console.WriteLine($"[UpdateStaff] Response: Success={response.Success}");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStaff Error] {ex.Message}");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<StaffOperationResponse> DeleteStaff(DeleteStaffRequest request)
        {
            try
            {
                await EnsureConnectedAsync();

                Console.WriteLine($"[DeleteStaff] Deleting staff: {request.StaffId}");

                var response = await Connection.InvokeAsync<StaffOperationResponse>(
                    "DeleteStaff",
                    request
                );

                Console.WriteLine($"[DeleteStaff] Response: Success={response.Success}");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteStaff Error] {ex.Message}");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }
    }
}