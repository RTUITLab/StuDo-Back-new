﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using studo.Data;
using studo.Models;
using studo.Models.Requests.Users;
using studo.Models.Responses.Users;
using studo.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace studo.Controllers.Users
{
    [Produces("application/json")]
    [Route("api/user")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IMapper mapper;
        private readonly IEmailSender emailSender;
        private readonly ILogger<AccountController> logger;

        public AccountController(UserManager<User> userManager, IMapper mapper,
            IEmailSender emailSender, ILogger<AccountController> logger)
        {
            this.userManager = userManager;
            this.mapper = mapper;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        // TODO: check who wants to reset or change password

        // TODO: change email

        [AllowAnonymous]
        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPaswwordAsync ([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            var user = await userManager.FindByEmailAsync(resetPasswordRequest.Email);
            if (user == null)
                return BadRequest($"User with email {resetPasswordRequest.Email} doesn't exist");

            var resetPassToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, token = resetPassToken },
                protocol: Request.Scheme);

            await emailSender.SendResetPasswordEmail(resetPasswordRequest.Email, callbackUrl);
            return Ok();
        }

        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePasswordAsync ([FromBody] ChangePasswordRequest changePasswordRequest)
        {
            // TODO: IF IT WASN'T YOU PLEASE GO HERE TO RESET PASSWORD
            // send email that your password was reseted!!!
            var user = await userManager.FindByEmailAsync(changePasswordRequest.Email);
            if (user == null)
                return BadRequest($"User with email {changePasswordRequest.Email} doesn't exist");

            if (user.Id != GetCurrentUserId())
                return Forbid(JwtBearerDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);

            if (!await userManager.CheckPasswordAsync(user, changePasswordRequest.OldPassword))
                return BadRequest("Old password doesn't match current password");

            if (changePasswordRequest.NewPassword.Equals(changePasswordRequest.OldPassword))
                return BadRequest("New password can't match old password");

            var result = await userManager.ChangePasswordAsync(user, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
            if (result.Succeeded)
            {
                // send email that password was reseted
                return Ok();
            }
            else
            {
                foreach (var er in result.Errors)
                {
                    logger.LogError($"Result of changing password for user with email {user.Email} is {er}");
                }
                return BadRequest($"Something went wrong");
            }
        }

        [HttpPost("change")]
        public async Task<ActionResult<UserView>> ChangeUserInfromationAsync([FromBody] ChangeUserInformationRequest changeUserInformationRequest)
        {
            try
            {
                if (changeUserInformationRequest.Id != GetCurrentUserId())
                    throw new MethodAccessException();

                var currentUser = await userManager.FindByIdAsync(changeUserInformationRequest.Id.ToString())
                    ?? throw new ArgumentNullException();

                mapper.Map(changeUserInformationRequest, currentUser);
                var result = await userManager.UpdateAsync(currentUser);
                if (result.Succeeded)
                {
                    return Ok(mapper.Map<UserView>(currentUser));
                }
                else
                {
                    foreach (var er in result.Errors)
                    {
                        logger.LogError($"Result of changing user information is {er}");
                    }
                    return BadRequest("Something went wrong");
                }
            }
            catch(ArgumentNullException ane)
            {
                logger.LogDebug(ane.Message + "\n" + ane.StackTrace);
                return NotFound($"Can't find current user");
            }
            catch(MethodAccessException mae)
            {
                logger.LogDebug(mae.Message + "\n" + mae.StackTrace);
                logger.LogDebug($"User {GetCurrentUserId()} can't change {changeUserInformationRequest.Id} user information");
                return Forbid(JwtBearerDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        private Guid GetCurrentUserId()
            => Guid.Parse(userManager.GetUserId(User));
    }
}
