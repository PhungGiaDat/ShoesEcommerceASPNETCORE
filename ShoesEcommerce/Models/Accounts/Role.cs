namespace ShoesEcommerce.Models.Accounts
{
    public class Role
    {
        public int Id { get; set; } 
        public string Name { get; set; } 

        public ICollection<RoleStaff > RoleStaffs { get; set; } 

        public ICollection<RolePermission> RolePermissions { get; set; }

    }
}
