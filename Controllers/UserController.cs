using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using elder_care_api.Dtos.User;
using elder_care_api.Services.UserService;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using elder_care_api.Data;
using Microsoft.Extensions.Options;
using elder_care_api.Dtos.UserSetting;

namespace elder_care_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtGenerator _jwtGenerator;

        public UserController(
            IUserService userService,
            IConfiguration configuration,
            IOptionsSnapshot<UserSettings> options
        )
        {
            _userService = userService;
            _jwtGenerator = new JwtGenerator(configuration["JwtPrivateSigningKey"]);
        }

        [HttpPost("register")]
        public async Task<ActionResult<ServiceResponse<LoadUserDto>>> Register(UserRegisterDto request)
        {
            var response = await _userService.Register(
                new User
                {
                    FirstName = request.Firstname,
                    LastName = request.Lastname,
                    Email = request.Email
                },
                request.Password
            );

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ServiceResponse<LoadUserDto>>> Login(UserLoginDto request)
        {
            var response = await _userService.Login(request.Email, request.Password);

            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // Load current user
        [Authorize]
        [HttpGet("load-user")]
        public ActionResult<ServiceResponse<LoadUserDto>> LoadUser()
        {
            var response = _userService.LoadUser();

            if (!response.Success)
            {
                return Unauthorized(response);
            }
            return Ok(response);
        }

        [Authorize]
        [HttpDelete("{userId}")]
        public ActionResult<ServiceResponse<string>> DeleteUser(int userId)
        {
            var response = _userService.DeleteUser(userId);

            if (!response.Success)
            {
                return Unauthorized(response);
            }
            return Ok(response);
        }

        [HttpGet("settings")]
        public async Task<ActionResult<ServiceResponse<SettingsDto>>> GetSettings()
        {
            var response = await _userService.GetSettings();

            if (!response.Success)
            { // need to set this to server error
                return BadRequest(response);
            }
            return Ok(response);
        }

        [Authorize]
        [HttpPost("settings")]
        public async Task<ActionResult<ServiceResponse<SettingsDto>>> SaveSettings(SettingsDto newSettings)
        {
            var response = await _userService.SaveSettings(newSettings);

            if (!response.Success)
            { // need to set this to server error
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}
