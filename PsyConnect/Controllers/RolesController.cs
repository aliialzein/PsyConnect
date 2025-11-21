using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PsyConnect.Models;
using System.Linq;

namespace PsyConnect.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        public RoleManager<IdentityRole> _roleManager;
        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }
        // GET: RolesController
        public ActionResult Index()
        {
            var rolesList = _roleManager.Roles.Select(r => new RoleVM { RoleName = r.Name }).ToList();
            return View(rolesList);
        }

        // GET: RolesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoleVM myrole)
        {
            try
            {
                var IdentityRole = new IdentityRole() { Name = myrole.RoleName };
                var result =  _roleManager.CreateAsync(IdentityRole).Result;
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                else return View();
            }
            catch
            {
                return View();
            }
        }

        // GET: RolesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: RolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: RolesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: RolesController/Delete/5
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
