using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub 
    {

        // Đăng ký Customer
        public async Task<AuthResult> RegisterCustomer(RegisterRequest request)
        {
            try
            {

                // Kiểm tra trùng
                var exists = await _context.Customers
                    .AnyAsync(c => c.Phone == request.Phone );

                if (exists)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Phone number already exists!"
                    };
                }

                // Tạo customer mới
                var customer = new Customer
                {
                    CustomerId = Guid.NewGuid(),
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return new AuthResult
                {
                    Success = true,
                    Message = "Registration successful!",
                    UserInfo = new UserInfo
                    {
                        UserId = customer.CustomerId,
                        FullName = customer.FullName,
                        UserType = "Customer",
                        Email = customer.Email,
                        Phone = customer.Phone
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer");
                return new AuthResult { Success = false, Message = "System error!" };
            }
        }

        // Login
        public async Task<AuthResult> Login(LoginRequest request)
        {
            try
            {

            if (request.UsernameOrEmail == "admin" &&
                request.Password == "admin123")
            {
                return new AuthResult
                {
                    Success = true,
                    UserInfo = new UserInfo
                    {
                        UserId = Guid.Empty, // Or a specific admin GUID
                        FullName = "System Admin",
                        Email = "admin@parking.com",
                        UserType = "Admin"
                    }
                };
             }
            else
                {
                    if (request.IsStaff)
                    {
                        // Login Staff
                        var staff = await _context.Staff
                            .FirstOrDefaultAsync(s => s.Username == request.UsernameOrEmail);

                        if (staff == null || !BCrypt.Net.BCrypt.Verify(request.Password, staff.PasswordHash))
                        {
                            return new AuthResult
                            {
                                Success = false,
                                Message = "Invalid username or password!"
                            };
                        }

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Login successful!",
                            UserInfo = new UserInfo
                            {
                                UserId = staff.StaffId,
                                FullName = staff.FullName,
                                UserType = "Staff"
                            }
                        };
                    }
                    else
                    {
                        // Login Customer
                        var customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.Email == request.UsernameOrEmail ||
                                                      c.Phone == request.UsernameOrEmail);

                        if (customer == null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
                        {
                            return new AuthResult
                            {
                                Success = false,
                                Message = "Incorrect email/phone or password!"
                            };
                        }

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Login successful!",
                            UserInfo = new UserInfo
                            {
                                UserId = customer.CustomerId,
                                FullName = customer.FullName,
                                UserType = "Customer",
                                Email = customer.Email,
                                Phone = customer.Phone
                            }
                        };
                    }
                } 
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResult { Success = false, Message = "System error!" };
            }
        }

        // ============ GET ALL CUSTOMERS ============
        public async Task<List<Customer>> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => !c.IsDeleted) // Chỉ lấy khách hàng chưa bị xóa
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations)
                    .OrderBy(c => c.FullName)
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw new HubException("Không thể lấy danh sách khách hàng");
            }
        }

        // ============ GET CUSTOMER BY ID ============
        public async Task<Customer> GetCustomerById(Guid customerId)
        {
            var customer = await _context.Customers
                .Where(c => !c.IsDeleted) // Chỉ lấy khách hàng chưa bị xóa
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                throw new HubException("Không tìm thấy khách hàng");

            return customer;
        }

        // ============ ADD CUSTOMER ============
        public async Task<Customer> AddCustomer(Customer customer)
        {
            customer.CustomerId = Guid.NewGuid();
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(customer.PasswordHash);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerAdded", customer);
            return customer;
        }

        // ============ UPDATE CUSTOMER ============
        public async Task<Customer> UpdateCustomer(Customer customer)
        {
            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null)
                throw new HubException("Không tìm thấy khách hàng");

            existing.FullName = customer.FullName;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;

            await _context.SaveChangesAsync();
            await Clients.All.SendAsync("CustomerUpdated", existing);

            return existing;
        }


        // ============ DELETE CUSTOMER ============
        public async Task DeleteCustomer(Guid customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                throw new HubException("Không tìm thấy khách hàng");
            
            var hasActiveParking = await _context.Vehicles
                .Where(v => v.CustomerId == customerId)
                .SelectMany(v => v.ParkingRegistrations)
                .AnyAsync(pr => pr.Status == "Active" && pr.CheckOutTime == null);

            if (hasActiveParking)
            {
                throw new HubException("Không thể xóa khách hàng có xe đang đỗ trong bãi!");
            }

            customer.IsDeleted = true; // Đánh dấu là đã xóa
            _context.Customers.Update(customer); // Cập nhật trạng thái
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerDeleted", customerId);
        }


        // ============ SEARCH ============
        public async Task<List<Customer>> SearchCustomers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllCustomers();

            return await _context.Customers
                .Where(c=> !c.IsDeleted && (c.FullName.Contains(keyword)
                         || c.Phone.Contains(keyword)
                         || c.Email.Contains(keyword)))
                .Include(c => c.Vehicles)
                .ToListAsync();
        }


        public async Task<Customer?> GetCustomerWithFullDetails(Guid customerId)
        {
            try
            {
                var customer = await _context.Customers
                    .Where(c => !c.IsDeleted && c.CustomerId == customerId) // Chỉ lấy khách hàng chưa bị xóa
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations.OrderByDescending(pr => pr.CheckInTime))
                    .ThenInclude(pr => pr.Slot)
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations)
                    .ThenInclude(pr => pr.Payments)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting customer details {customerId}");
                throw new HubException("Không thể lấy thông tin chi tiết khách hàng");
            }
        }

        // Lấy tất cả xe của customer
        public async Task<List<ParkingSystem.Server.Models.Vehicle>> GetMyVehicles(Guid customerId)
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == customerId)
                    .Include(v => v.ParkingRegistrations)
                    .OrderBy(v => v.PlateNumber)
                    .ToListAsync();

                return vehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles for customer {CustomerId}", customerId);
                throw new HubException("Không thể lấy danh sách xe");
            }
        }

        public async Task<List<string>> GetActiveVehicleTypes()
        {
            try
            {
                return await _context.ParkingPrices
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.VehicleType)
                    .Select(p => p.VehicleType)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active vehicle types");
                throw new HubException("Error getting active vehicle types");
            }
        }


        // Thêm xe mới
        public async Task<ParkingSystem.Server.Models.Vehicle> AddVehicle(Guid customerId, string plateNumber, string vehicleType)
        {
            try
            {
                // Kiểm tra biển số đã tồn tại chưa
                var exists = await _context.Vehicles
                    .AnyAsync(v => v.PlateNumber == plateNumber);

                if (exists)
                {
                    throw new HubException("Biển số xe này đã tồn tại trong hệ thống!");
                }

                var vehicle = new ParkingSystem.Server.Models.Vehicle
                {
                    VehicleId = Guid.NewGuid(),
                    PlateNumber = plateNumber,
                    VehicleType = vehicleType,
                    CustomerId = customerId
                };

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {PlateNumber} added for customer {CustomerId}", plateNumber, customerId);

                // Gửi thông báo real-time
                await Clients.User(customerId.ToString()).SendAsync("VehicleAdded", vehicle);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vehicle");
                throw new HubException(ex.Message.Contains("đã tồn tại") ? ex.Message : "Không thể thêm xe");
            }
        }

        // Cập nhật xe
        public async Task<ParkingSystem.Server.Models.Vehicle> UpdateVehicle(Guid vehicleId, string plateNumber, string vehicleType)
        {
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId);

                if (vehicle == null)
                {
                    throw new HubException("Không tìm thấy xe");
                }

                // Kiểm tra biển số mới có trùng với xe khác không
                var duplicate = await _context.Vehicles
                    .AnyAsync(v => v.PlateNumber == plateNumber && v.VehicleId != vehicleId);

                if (duplicate)
                {
                    throw new HubException("Biển số xe này đã tồn tại!");
                }

                vehicle.PlateNumber = plateNumber;
                vehicle.VehicleType = vehicleType;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {VehicleId} updated", vehicleId);

                // Gửi thông báo real-time
                await Clients.User(vehicle.CustomerId.ToString()).SendAsync("VehicleUpdated", vehicle);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                throw new HubException(ex.Message.Contains("tồn tại") ? ex.Message : "Không thể cập nhật xe");
            }
        }

        // Xóa xe
        public async Task DeleteVehicle(Guid vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.ParkingRegistrations)
                    .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

                if (vehicle == null)
                {
                    throw new HubException("Không tìm thấy xe");
                }

                // Kiểm tra xe có đang đỗ không
                var hasActiveParking = vehicle.ParkingRegistrations
                    .Any(pr => pr.Status == "InUse" && pr.CheckOutTime == null);

                if (hasActiveParking)
                {
                    throw new HubException("Không thể xóa xe đang đỗ trong bãi!");
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {VehicleId} deleted", vehicleId);

                // Gửi thông báo real-time
                await Clients.User(vehicle.CustomerId.ToString()).SendAsync("VehicleDeleted", vehicleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle");
                throw new HubException(ex.Message.Contains("đang đỗ") ? ex.Message : "Không thể xóa xe");
            }
        }

        // Lấy chi tiết xe với lịch sử đỗ
        public async Task<ParkingSystem.Server.Models.Vehicle?> GetVehicleDetails(Guid vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.ParkingRegistrations.OrderByDescending(pr => pr.CheckInTime))
                        .ThenInclude(pr => pr.Slot)
                    .Include(v => v.ParkingRegistrations)
                        .ThenInclude(pr => pr.Payments)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle details");
                throw new HubException("Không thể lấy thông tin xe");
            }
        }

        // Đổi mật khẩu
        public async Task ChangePassword(Guid customerId, ChangePasswordModel model)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(model.CurrentPassword) || 
                    string.IsNullOrEmpty(model.NewPassword) || 
                    string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    throw new HubException("Vui lòng điền đầy đủ thông tin");
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    throw new HubException("Mật khẩu xác nhận không khớp");
                }

                if (model.NewPassword.Length < 6)
                {
                    throw new HubException("Mật khẩu mới phải có ít nhất 6 ký tự");
                }

                // Get customer
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    throw new HubException("Không tìm thấy thông tin khách hàng");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, customer.PasswordHash))
                {
                    throw new HubException("Mật khẩu hiện tại không đúng");
                }

                // Update password
                customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (HubException)
            {
                throw; // Re-throw HubException as is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for customer {CustomerId}", customerId);
                throw new HubException("Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại sau.");
            }
        }
    }
}
