﻿using WebApp.Data;
using WebApp.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Auth.Controllers
{
    [Route("")]
    [ApiController]
    public class AccountAuth : ControllerBase
    {
        private ILogger<AccountAuth> logger;
        public AccountAuth(ILogger<AccountAuth> _logger)
        {
            logger = _logger;
        }

        [HttpPost("login")]
        public async Task<IResult> Login(SignInManager<ApplicationUser> signInManager,UserManager<ApplicationUser> userManager, LoginModel loginModel)
        {
            using var myActivity = OpenTelemetryData.MyActivitySource.StartActivity("login");
            var user = await userManager.FindByEmailAsync(loginModel.Email);

            if(user == null)
            {
                return Results.BadRequest(new { Message = "Email or Password are incorrect" });
            }

            var result = await signInManager.PasswordSignInAsync(user, loginModel.Password, loginModel.RememberMe, false);

            if(result.Succeeded)
            {
                OpenTelemetryData.SuccessfulLoginsCounter.Add(1);
                return Results.Ok(new { Message = "Login successful" });
            }
            else
            {
                OpenTelemetryData.FailedLoginsCounter.Add(1);
                return Results.BadRequest(new { Message = "Email or Password are incorrect" });
            }
        }


        [HttpPost("logout"),Authorize]
        public async Task<IResult> Logout(SignInManager<ApplicationUser> signInManager)
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { Message = "Signout successful"});
        }

        [HttpPost("register")]
        public async Task<IResult> Register(UserManager<ApplicationUser> userManager, UserRegisterModel user, DataContext dbContext)
        {
            using var myActivity = OpenTelemetryData.MyActivitySource.StartActivity("register");
            var bDate = user.BirthDate;
            if (bDate.Year <= 0 || bDate.Month <= 0 || bDate.Day <= 0)
                return Results.BadRequest(new { Message = "Invalid Birthday Date" });
            
            var identityUser = new ApplicationUser { Email = user.Email, UserName = user.Name };
            var registerResult = await userManager.CreateAsync(identityUser, user.Password);

            if(!registerResult.Succeeded) 
            {
                return Results.BadRequest(new { Messages = registerResult.Errors });
            }

            try
            {
                var newUser = new User { ApplicationUser = identityUser, BirthDate = new DateOnly(bDate.Year, bDate.Month, bDate.Day), PhoneNumber = user.PhoneNumber };
                dbContext.UsersList.Add(newUser);
                await dbContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                logger.LogError($"Unable to create user account - {ex.Message}");
                await userManager.DeleteAsync(identityUser);
                return Results.BadRequest(new {Message =  ex.Message});
            }

            OpenTelemetryData.RegistrationsCounter.Add(1);
            return Results.Ok(new { Message = "Register successful" });

        }

        [HttpGet("checkLogIn"),Authorize]
        public async Task<IResult> isLoggedIn()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return Results.Ok(new { Email = email });
        }

        [HttpGet("getUserInfo"),Authorize]
        public async Task<IResult> getUserInfo(DataContext dbContext)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var id = dbContext.Users.Where(_u => _u.Email == email).First().Id;
            var user = dbContext.UsersList.Include(c => c.ApplicationUser).FirstOrDefault(c => c.ApplicationUser.Id == id);
            if(user == null)
            {
                return Results.BadRequest(new { Message = "Error" });
            }

            return Results.Ok(new {PhoneNumber = user.PhoneNumber,BirthDay = user.BirthDate, Email = user.ApplicationUser.Email, UserName = user.ApplicationUser.UserName});
        }
    }
}
