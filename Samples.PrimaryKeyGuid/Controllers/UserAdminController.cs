﻿using IdentitySample.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace IdentitySample.Controllers {
    [Authorize(Roles = "Admin")]
    public class UsersAdminController : Controller {
        public UsersAdminController() {
        }

        public UsersAdminController(ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager) {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager {
            get {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set {
                _userManager = value;
            }
        }

        private RoleManager<IdentityRole> _roleManager;
        public RoleManager<IdentityRole> RoleManager {
            get {
                return _roleManager ?? new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(HttpContext.GetOwinContext().Get<ApplicationDbContext>()));
            }
            private set {
                _roleManager = value;
            }
        }

        //
        // GET: /Users/
        public async Task<ActionResult> Index() {
            return View(await UserManager.Users.ToListAsync());
        }

        //
        // GET: /Users/Details/5
        public async Task<ActionResult> Details(string id) {
            if (id == null) {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(Guid.Parse(id));
            return View(user);
        }

        //
        // GET: /Users/Create
        public async Task<ActionResult> Create() {
            //Get the list of Roles
            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            return View();
        }

        //
        // POST: /Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel, string RoleId) {
            if (ModelState.IsValid) {
                var user = new ApplicationUser() { UserName = userViewModel.Email, Email = userViewModel.Email };
                var adminresult = await UserManager.CreateAsync(user, userViewModel.Password);

                //Add User Admin to Role Admin
                if (adminresult.Succeeded) {
                    if (!String.IsNullOrEmpty(RoleId)) {
                        var result = await UserManager.AddToRoleAsync(user.Id, RoleId);
                        if (!result.Succeeded) {
                            ModelState.AddModelError("", result.Errors.First().ToString());
                            ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
                            return View();
                        }
                    }
                }
                else {
                    ModelState.AddModelError("", adminresult.Errors.First().ToString());
                    ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
                    return View();

                }
                return RedirectToAction("Index");
            }
            else {
                ViewBag.RoleId = new SelectList(RoleManager.Roles, "Name", "Name");
                return View();
            }
        }

        //
        // GET: /Users/Edit/1
        public async Task<ActionResult> MakeAdmin(string id) {
            var result = await UserManager.AddToRoleAsync(Guid.Parse(id), "Admin");
            if (result.Succeeded) {
                return RedirectToAction("Edit", new { Id = id });
            }
            return View("Error");
        }

        //
        // GET: /Users/Edit/1
        public async Task<ActionResult> RemoveAdmin(string id) {
            var result = await UserManager.RemoveFromRoleAsync(Guid.Parse(id), "Admin");
            if (result.Succeeded) {
                return RedirectToAction("Edit", new { Id = id });
            }
            return View("Error");
        }

        //
        // GET: /Users/Edit/1
        public async Task<ActionResult> Edit(string id) {
            if (id == null) {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(Guid.Parse(id));
            if (user == null) {
                return HttpNotFound();
            }
            ViewBag.IsAdmin = await UserManager.IsInRoleAsync(Guid.Parse(id), "Admin");
            return View(user);
        }

        //
        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "UserName,Id")] ApplicationUser user) {
            if (ModelState.IsValid) {
                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded) {
                    ModelState.AddModelError("", result.Errors.First().ToString());
                    return View();
                }
                return RedirectToAction("Index");
            }
            else {
                ModelState.AddModelError("", "Something failed.");
                return View();
            }
        }

        //
        // GET: /Users/Delete/5
        public async Task<ActionResult> Delete(string id) {
            if (id == null) {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(Guid.Parse(id));
            if (user == null) {
                return HttpNotFound();
            }
            return View(user);
        }

        //
        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id) {
            if (ModelState.IsValid) {
                if (id == null) {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var user = await UserManager.FindByIdAsync(Guid.Parse(id));
                if (user == null) {
                    return HttpNotFound();
                }
                var result = await UserManager.DeleteAsync(user);
                if (!result.Succeeded) {
                    ModelState.AddModelError("", result.Errors.First().ToString());
                    return View();
                }
                return RedirectToAction("Index");
            }
            else {
                return View();
            }
        }
    }
}
