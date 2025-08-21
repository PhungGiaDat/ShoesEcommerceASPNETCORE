using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Repositories.Interfaces;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;
using Microsoft.Extensions.Logging;

namespace ShoesEcommerce.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StaffRepository> _logger;

        public StaffRepository(AppDbContext context, ILogger<StaffRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Authentication Methods
        public async Task<Staff?> ValidateStaffAsync(string email, string password)
        {
            try
            {
                var staff = await _context.Staffs
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (staff != null && BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
                {
                    _logger.LogInformation("Staff validation successful for {Email}", email);
                    return staff;
                }

                _logger.LogWarning("Staff validation failed for {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating staff: {Email}", email);
                throw;
            }
        }

        // CRUD Operations
        public async Task<IEnumerable<Staff>> GetAllStaffsAsync()
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(int id)
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Staff> CreateStaffAsync(Staff staff)
        {
            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();
            return staff;
        }

        public async Task<Staff> UpdateStaffAsync(Staff staff)
        {
            _context.Entry(staff).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return staff;
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
                return false;

            // Remove associated UserRoles first
            var userRoles = await _context.UserRoles
                .Where(ur => ur.StaffId == id)
                .ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StaffExistsAsync(int id)
        {
            return await _context.Staffs.AnyAsync(s => s.Id == id);
        }

        // Role Management
        public async Task<IEnumerable<Role>> GetStaffRolesAsync(int staffId)
        {
            return await _context.UserRoles
                .Where(ur => ur.StaffId == staffId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToStaffAsync(int staffId, int roleId)
        {
            // Check if staff exists
            if (!await StaffExistsAsync(staffId))
                return false;

            // Check if role exists and is for staff
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null || role.UserType != UserType.Staff)
                return false;

            // Check if assignment already exists
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.StaffId == staffId && ur.RoleId == roleId);

            if (existingAssignment != null)
                return false; // Already assigned

            var userRole = new UserRole
            {
                StaffId = staffId,
                RoleId = roleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.StaffId == staffId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Role>> GetAvailableStaffRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.UserType == UserType.Staff)
                .ToListAsync();
        }

        // Department Management
        public async Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync()
        {
            return await _context.Departments
                .Include(d => d.Staffs)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsByDepartmentAsync(int departmentId)
        {
            return await _context.Staffs
                .Where(s => s.DepartmentId == departmentId)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId)
        {
            return await _context.Departments
                .Include(d => d.Staffs)
                .FirstOrDefaultAsync(d => d.Id == departmentId);
        }

        // Advanced Queries
        public async Task<IEnumerable<Staff>> SearchStaffsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllStaffsAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(s => 
                    s.FirstName.ToLower().Contains(searchTerm) ||
                    s.LastName.ToLower().Contains(searchTerm) ||
                    s.Email.ToLower().Contains(searchTerm) ||
                    s.Department.Name.ToLower().Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsWithRolesAsync()
        {
            return await _context.Staffs
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(s => s.Roles.Any())
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsWithDepartmentAsync()
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<int> GetTotalStaffCountAsync()
        {
            return await _context.Staffs.CountAsync();
        }

        public async Task<IEnumerable<Staff>> GetPaginatedStaffsAsync(int pageNumber, int pageSize)
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}