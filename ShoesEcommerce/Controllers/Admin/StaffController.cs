using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Staff;

namespace ShoesEcommerce.Controllers.Admin
{
    public class StaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IStaffService staffService, ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        // GET: Staff
        public async Task<IActionResult> Index(string searchTerm, int? departmentId, int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Qu?n lý Nhân viên";

            try
            {
                _logger.LogInformation("Loading staff index page with search: {SearchTerm}, department: {DepartmentId}, page: {Page}", 
                    searchTerm, departmentId, page);

                var viewModel = await _staffService.GetStaffsAsync(searchTerm, departmentId, page, pageSize);
                
                // Get departments for filter dropdown
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name", departmentId);

                _logger.LogInformation("Successfully loaded {StaffCount} staff members", viewModel.Staffs.Count());

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff index page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i danh sách nhân viên. Chi ti?t: " + ex.Message;
                
                // Return an empty view model to prevent further errors
                var emptyViewModel = new StaffListViewModel();
                ViewBag.Departments = new SelectList(new List<object>(), "Id", "Name");
                return View(emptyViewModel);
            }
        }

        // GET: Staff/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi ti?t Nhân viên";

            try
            {
                var staff = await _staffService.GetStaffByIdAsync(id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y nhân viên!";
                    return RedirectToAction(nameof(Index));
                }

                var staffRoles = await _staffService.GetStaffRolesAsync(id);
                ViewBag.StaffRoles = staffRoles;

                return View(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff details for ID: {StaffId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin nhân viên: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Staff/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm Nhân viên m?i";

            try
            {
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name");

                return View(new CreateStaffViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff create page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i form thêm nhân viên: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffViewModel model)
        {
            ViewData["Title"] = "Thêm Nhân viên m?i";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdStaff = await _staffService.CreateStaffAsync(model);
                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Details), new { id = createdStaff.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating staff");
                    ModelState.AddModelError("", "Có l?i x?y ra khi thêm nhân viên: " + ex.Message);
                }
            }

            // Reload departments if validation fails
            try
            {
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name", model.DepartmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments for create form");
                ModelState.AddModelError("", "Không th? t?i danh sách phòng ban: " + ex.Message);
                ViewBag.Departments = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Staff/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Ch?nh s?a Nhân viên";

            try
            {
                var staff = await _staffService.GetStaffByIdAsync(id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y nhân viên!";
                    return RedirectToAction(nameof(Index));
                }

                var editModel = new EditStaffViewModel
                {
                    Id = staff.Id,
                    FirebaseUid = staff.FirebaseUid,
                    Email = staff.Email,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    PhoneNumber = staff.PhoneNumber,
                    DepartmentId = staff.DepartmentId
                };

                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name", staff.DepartmentId);

                return View(editModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff edit page for ID: {StaffId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin nhân viên: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditStaffViewModel model)
        {
            ViewData["Title"] = "Ch?nh s?a Nhân viên";

            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "D? li?u không h?p l?!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _staffService.UpdateStaffAsync(id, model);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "C?p nh?t thông tin nhân viên thành công!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không th? c?p nh?t nhân viên. Vui lòng ki?m tra l?i thông tin.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating staff ID: {StaffId}", id);
                    ModelState.AddModelError("", "Có l?i x?y ra khi c?p nh?t: " + ex.Message);
                }
            }

            try
            {
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name", model.DepartmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments for edit form");
                ModelState.AddModelError("", "Không th? t?i danh sách phòng ban: " + ex.Message);
            }

            return View(model);
        }

        // GET: Staff/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            ViewData["Title"] = "Xóa Nhân viên";

            try
            {
                var staff = await _staffService.GetStaffByIdAsync(id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y nhân viên!";
                    return RedirectToAction(nameof(Index));
                }

                return View(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff delete page for ID: {StaffId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin nhân viên: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Staff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _staffService.DeleteStaffAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không th? xóa nhân viên!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff ID: {StaffId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi xóa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Staff/ManageRoles/5
        public async Task<IActionResult> ManageRoles(int id)
        {
            ViewData["Title"] = "Qu?n lý Quy?n";

            try
            {
                var staffRoles = await _staffService.GetStaffRolesAsync(id);
                return View(staffRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff roles for ID: {StaffId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin quy?n: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Staff/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int staffId, int roleId)
        {
            try
            {
                var result = await _staffService.AssignRoleToStaffAsync(staffId, roleId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Gán quy?n thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không th? gán quy?n ho?c quy?n ?ã ???c gán!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to staff ID: {StaffId}", roleId, staffId);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
            }

            return RedirectToAction(nameof(ManageRoles), new { id = staffId });
        }

        // POST: Staff/RemoveRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(int staffId, int roleId)
        {
            try
            {
                var result = await _staffService.RemoveRoleFromStaffAsync(staffId, roleId);
                if (result)
                {
                    TempData["SuccessMessage"] = "G? quy?n thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không th? g? quy?n!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from staff ID: {StaffId}", roleId, staffId);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
            }

            return RedirectToAction(nameof(ManageRoles), new { id = staffId });
        }

        // GET: Staff/Departments
        public async Task<IActionResult> Departments()
        {
            ViewData["Title"] = "Phòng ban";

            try
            {
                var departments = await _staffService.GetAllDepartmentsAsync();
                return View(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments list");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i danh sách phòng ban: " + ex.Message;
                return View(new List<ShoesEcommerce.Models.Departments.Department>());
            }
        }

        // GET: Staff/DepartmentDetails/5
        public async Task<IActionResult> DepartmentDetails(int id)
        {
            ViewData["Title"] = "Chi ti?t Phòng ban";

            try
            {
                var department = await _staffService.GetDepartmentByIdAsync(id);
                if (department == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y phòng ban!";
                    return RedirectToAction(nameof(Departments));
                }

                var departmentStaffs = await _staffService.GetStaffsByDepartmentAsync(id);
                ViewBag.DepartmentStaffs = departmentStaffs;

                return View(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading department details for ID: {DepartmentId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i thông tin phòng ban: " + ex.Message;
                return RedirectToAction(nameof(Departments));
            }
        }

        // AJAX: Get staff by Firebase UID
        [HttpGet]
        public async Task<IActionResult> GetStaffByFirebaseUid(string firebaseUid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firebaseUid))
                {
                    return BadRequest("Firebase UID is required");
                }

                var staff = await _staffService.GetStaffByFirebaseUidAsync(firebaseUid);
                if (staff == null)
                {
                    return NotFound();
                }

                return Json(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff by Firebase UID: {FirebaseUid}", firebaseUid);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // AJAX: Check if staff exists
        [HttpGet]
        public async Task<IActionResult> CheckStaffExists(int id)
        {
            try
            {
                var exists = await _staffService.StaffExistsAsync(id);
                return Json(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if staff exists ID: {StaffId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Test endpoint to check if the controller is reachable
        public IActionResult Test()
        {
            _logger.LogInformation("Staff controller test endpoint called");
            return Json(new { message = "Staff controller is working", timestamp = DateTime.Now });
        }
    }
}