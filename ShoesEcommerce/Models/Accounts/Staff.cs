using ShoesEcommerce.Models.Departments;
using ShoesEcommerce.Models.Interactions;

namespace ShoesEcommerce.Models.Accounts
{
    public class Staff
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set;  }

        public string Email { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhnoneNumber { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; }  

        public ICollection<RoleStaff> RoleStaffs { get; set; } 
        public ICollection<QA> QAs { get; set; }



    }
}
