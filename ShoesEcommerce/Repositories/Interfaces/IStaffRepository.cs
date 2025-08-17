using ShoesEcommerce.Models.Accounts;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        // CRUD Operations
        Task<IEnumerable<Staff>> GetAllStaffsAsync();
        Task<Staff?> GetStaffByIdAsync(int id);
        Task<Staff?> GetStaffByFirebaseUidAsync(string firebaseUid);
        Task<Staff> CreateStaffAsync(Staff staff);
        Task<Staff> UpdateStaffAsync(Staff staff);
        Task<bool> DeleteStaffAsync(int id);
        Task<bool> StaffExistsAsync(int id);

        // Role Management
        Task<IEnumerable<Role>> GetStaffRolesAsync(int staffId);
        Task<bool> AssignRoleToStaffAsync(int staffId, int roleId);
        Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId);
        Task<IEnumerable<Role>> GetAvailableStaffRolesAsync();

        // Department Management
        Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync();
        Task<IEnumerable<Staff>> GetStaffsByDepartmentAsync(int departmentId);
        Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId);

        // Advanced Queries
        Task<IEnumerable<Staff>> SearchStaffsAsync(string searchTerm);
        Task<IEnumerable<Staff>> GetStaffsWithRolesAsync();
        Task<IEnumerable<Staff>> GetStaffsWithDepartmentAsync();
        Task<int> GetTotalStaffCountAsync();
        Task<IEnumerable<Staff>> GetPaginatedStaffsAsync(int pageNumber, int pageSize);
    }
}