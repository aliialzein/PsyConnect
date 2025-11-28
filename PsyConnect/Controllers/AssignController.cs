using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PsyConnect.Models;
using System.Linq;
using System.Threading.Tasks;


namespace PsyConnect.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AssignController : Controller
    {
        public RoleManager<IdentityRole> _roleManager;
        public UserManager<IdentityUser> _userManager;
        public AssignController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        // GET: AssignController
        public ActionResult Index()
        {
            return View(new List<AssignVM>());
        }

        // GET: AssignController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AssignController/Create
        public ActionResult Create()
        {
            ViewBag.UserList= new SelectList(_userManager.Users.ToList(), "Id", "UserName");
            ViewBag.RolesList= new SelectList(_roleManager.Roles.ToList(), "Id", "Name");
            return View();
        }

        // POST: AssignController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync(IFormCollection collection)
        {
            try
            {
                var userId = collection["User"];
                var roleId = collection["Role"];

                // 1. Get user and role
                IdentityUser selectedUser = await _userManager.FindByIdAsync(userId);
                IdentityRole selectedRole = await _roleManager.FindByIdAsync(roleId);

                if (selectedUser == null || selectedRole == null)
                {
                    // Rebuild dropdowns if something went wrong
                    ViewBag.UserList = new SelectList(_userManager.Users.ToList(), "Id", "UserName");
                    ViewBag.RolesList = new SelectList(_roleManager.Roles.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "User or role not found.");
                    return View();
                }

                // 2. Remove ALL existing roles from this user
                var currentRoles = await _userManager.GetRolesAsync(selectedUser);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(selectedUser, currentRoles);
                }

                // 3. Add the newly selected role
                await _userManager.AddToRoleAsync(selectedUser, selectedRole.Name);

                TempData["Message"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Rebuild dropdowns on error so the view still works
                ViewBag.UserList = new SelectList(_userManager.Users.ToList(), "Id", "UserName");
                ViewBag.RolesList = new SelectList(_roleManager.Roles.ToList(), "Id", "Name");
                return View();
            }
        }


        // GET: AssignController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AssignController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, IFormCollection collection)
        {
            try
            {
                var UserId = collection["User"];
                var RoleId = collection["Role"];
                IdentityUser selectedUser = await _userManager.FindByIdAsync(UserId);
                IdentityRole selectedRole = await _roleManager.FindByIdAsync(RoleId);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AssignController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AssignController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
