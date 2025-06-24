namespace ShoesEcommerce.Models.Accounts
{
    public class RoleStaff
    {
        public int Id { get; set; }
        public Staff Staff { get; set; }

        public int StaffId { get; set; }

        public Role Role { get; set; } 
    }
}
