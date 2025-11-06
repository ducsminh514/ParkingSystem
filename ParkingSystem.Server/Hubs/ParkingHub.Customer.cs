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
                    .AnyAsync(c => c.Phone == request.Phone || c.Email == request.Email);

                if (exists)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Số điện thoại hoặc email đã tồn tại!"
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
                    Message = "Đăng ký thành công!",
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
                return new AuthResult { Success = false, Message = "Lỗi hệ thống!" };
            }
        }

        // Đăng nhập
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

                        //if (staff == null || !BCrypt.Net.BCrypt.Verify(request.Password, staff.PasswordHash))
                        //{
                        //    return new AuthResult
                        //    {
                        //        Success = false,
                        //        Message = "Tên đăng nhập hoặc mật khẩu không đúng!"
                        //    };
                        //}

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Đăng nhập thành công!",
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
                                Message = "Email/SĐT hoặc mật khẩu không đúng!"
                            };
                        }

                        return new AuthResult
                        {
                            Success = true,
                            Message = "Đăng nhập thành công!",
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
                return new AuthResult { Success = false, Message = "Lỗi hệ thống!" };
            }
        }

        // ============ GET ALL CUSTOMERS ============
        public async Task<List<Customer>> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Vehicles)
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
            existing.PasswordHash = customer.PasswordHash;

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

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("CustomerDeleted", customerId);
        }

        // ============ SEARCH ============
        public async Task<List<Customer>> SearchCustomers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllCustomers();

            return await _context.Customers
                .Where(c => c.FullName.Contains(keyword)
                         || c.Phone.Contains(keyword)
                         || c.Email.Contains(keyword))
                .Include(c => c.Vehicles)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerWithFullDetails(Guid customerId)
        {
            try
            {
                // CÁCH 1: Gộp chung 1 chain Include
                var customer = await _context.Customers
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations.OrderByDescending(pr => pr.CheckInTime))
                    .ThenInclude(pr => pr.Slot)
                    .Include(c => c.Vehicles)
                    .ThenInclude(v => v.ParkingRegistrations)
                    .ThenInclude(pr => pr.Payments)
                    .AsNoTracking()
                    .AsSplitQuery() // QUAN TRỌNG: Tránh cartesian explosion
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

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
    }
}
