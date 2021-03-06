﻿using IdentitySample.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace IdentitySample.Controllers {
    [Authorize]
    public class ManageController : Controller {
        public ManageController() {
        }

        public ManageController(ApplicationUserManager userManager) {
            UserManager = userManager;
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

        //
        // GET: /Account/Index
        public async Task<ActionResult> Index(ManageMessageId? message) {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two factor provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "The phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";
            // TODO: Add phone number getter

            var model = new IndexViewModel() {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(Guid.Parse(User.Identity.GetUserId())),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(Guid.Parse(User.Identity.GetUserId())),
                Logins = await UserManager.GetLoginsAsync(Guid.Parse(User.Identity.GetUserId())),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(User.Identity.GetUserId())
            };
            return View(model);
        }

        //
        // GET: /Account/RemoveLogin
        public ActionResult RemoveLogin() {
            var linkedAccounts = UserManager.GetLogins(Guid.Parse(User.Identity.GetUserId()));
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return View(linkedAccounts);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey) {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(Guid.Parse(User.Identity.GetUserId()), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded) {
                var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
                if (user != null) {
                    await SignInAsync(user, isPersistent: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Account/AddPhoneNumber
        public ActionResult AddPhoneNumber() {
            return View();
        }

        //
        // GET: /Manage/RememberBrowser
        public ActionResult RememberBrowser() {
            var rememberBrowserIdentity = AuthenticationManager.CreateTwoFactorRememberBrowserIdentity(User.Identity.GetUserId().ToString());
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = true }, rememberBrowserIdentity);
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/ForgetBrowser
        public ActionResult ForgetBrowser() {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/EnableTFA
        public async Task<ActionResult> EnableTFA() {
            await UserManager.SetTwoFactorEnabledAsync(Guid.Parse(User.Identity.GetUserId()), true);
            var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
            if (user != null) {
                await SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/DisableTFA
        public async Task<ActionResult> DisableTFA() {
            await UserManager.SetTwoFactorEnabledAsync(Guid.Parse(User.Identity.GetUserId()), false);
            var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
            if (user != null) {
                await SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Account/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model) {
            if (ModelState.IsValid) {
                // Send result of: UserManager.GetPhoneNumberCodeAsync(Guid.Parse(User.Identity.GetUserId()), phoneNumber);
                // Generate the token and send it
                var code = await UserManager.GenerateChangePhoneNumberTokenAsync(Guid.Parse(User.Identity.GetUserId()), model.Number);
                if (UserManager.SmsService != null) {
                    var message = new IdentityMessage() {
                        Destination = model.Number,
                        Body = "Your security code is: " + code
                    };
                    await UserManager.SmsService.SendAsync(message);
                }

                return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber) {
            if (phoneNumber == null) {
                return View("Error");
            }
            var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
            // To exercise the flow without actually sending codes, uncomment the following line
            //ViewBag.Status = "For DEMO purposes the current code is " + await UserManager.GenerateChangePhoneNumberTokenAsync(Guid.Parse(User.Identity.GetUserId()), phoneNumber);
            return View(new VerifyPhoneNumberViewModel() { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Account/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model) {
            if (ModelState.IsValid) {
                var result = await UserManager.ChangePhoneNumberAsync(Guid.Parse(User.Identity.GetUserId()), model.PhoneNumber, model.Code);
                if (result.Succeeded) {
                    var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
                    if (user != null) {
                        await SignInAsync(user, isPersistent: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
                }
                else {
                    // Something failed if we got here
                    ModelState.AddModelError("", "Failed to verify phone");
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/RemovePhoneNumber
        public async Task<ActionResult> RemovePhoneNumber() {
            var result = await UserManager.SetPhoneNumberAsync(Guid.Parse(User.Identity.GetUserId()), null);
            if (result.Succeeded) {
                var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
                if (user != null) {
                    await SignInAsync(user, isPersistent: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
            }
            else {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword() {
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model) {
            if (ModelState.IsValid) {
                IdentityResult result = await UserManager.ChangePasswordAsync(Guid.Parse(User.Identity.GetUserId()), model.OldPassword, model.NewPassword);
                if (result.Succeeded) {
                    var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
                    if (user != null) {
                        await SignInAsync(user, isPersistent: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                else {
                    AddErrors(result);
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword() {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model) {
            if (ModelState.IsValid) {
                IdentityResult result = await UserManager.AddPasswordAsync(Guid.Parse(User.Identity.GetUserId()), model.NewPassword);
                if (result.Succeeded) {
                    var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
                    if (user != null) {
                        await SignInAsync(user, isPersistent: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                else {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Manage
        public async Task<ActionResult> ManageLogins(ManageMessageId? message) {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));
            if (user != null) {
                var userLogins = await UserManager.GetLoginsAsync(Guid.Parse(User.Identity.GetUserId()));
                List<AuthenticationDescription> otherLogins = new List<AuthenticationDescription>();
                foreach (var auth in AuthenticationManager.GetExternalAuthenticationTypes()) {
                    if (!userLogins.Any(ul => auth.AuthenticationType == ul.LoginProvider)) {
                        otherLogins.Add(auth);
                    }
                }
                ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
                return View(new ManageLoginsViewModel() {
                    CurrentLogins = userLogins,
                    OtherLogins = otherLogins
                });
            }
            return View("Error");
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider) {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId().ToString());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback() {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId().ToString());
            if (loginInfo == null) {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            IdentityResult result = await UserManager.AddLoginAsync(Guid.Parse(User.Identity.GetUserId()), loginInfo.Login);
            if (result.Succeeded) {
                return RedirectToAction("ManageLogins");
            }
            return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing) {
            if (disposing && _userManager != null) {
                _userManager.Dispose();
                _userManager = null;
            }
            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager {
            get {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent) {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie, DefaultAuthenticationTypes.TwoFactorCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword() {
            var user = UserManager.FindById(Guid.Parse(User.Identity.GetUserId()));
            if (user != null) {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber() {
            var user = UserManager.FindById(Guid.Parse(User.Identity.GetUserId()));
            if (user != null) {
                return user.PhoneNumber != null;
            }
            return false;
        }

        private void SendEmail(string email, string callbackUrl, string subject, string message) {
            //Please see this fwlink to send email
        }

        public enum ManageMessageId {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl) {
            if (Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }
            else {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null) {
            }

            public ChallengeResult(string provider, string redirectUri, string userId) {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context) {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null) {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}