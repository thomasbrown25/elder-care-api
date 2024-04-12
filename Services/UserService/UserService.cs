using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using elder_care_api.DbLogger;
using elder_care_api.Dtos.User;
using elder_care_api.Dtos.UserSetting;
using elder_care_api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace elder_care_api.Data
{
    public class UserService : IUserService
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogging _logging;

        public UserService(
            DataContext context,
            IConfiguration configuration,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogging logging
        )
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logging = logging;
        }

        public async Task<ServiceResponse<LoadUserDto>> Register(User user, string password)
        {
            ServiceResponse<LoadUserDto> response = new();

            try
            {
                if (await UserExists(user.Email))
                {
                    response.Message = "A user with that email already exists.";
                    response.Success = false;
                    _logging.LogTrace("Register user failed: A user with that email already exists.");
                    return response;
                }

                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                // after we save user, we create and return the jwt token
                var token = CreateToken(user);

                response.Data = new LoadUserDto();
                response.Data = _mapper.Map<LoadUserDto>(user);
                response.Data.JWTToken = token;


            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<LoadUserDto>> Login(string email, string password)
        {
            ServiceResponse<LoadUserDto> response = new();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(
                    u => u.Email.ToLower().Equals(email.ToLower())
                );

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "Invalid email or password";
                }
                else if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    response.Success = false;
                    response.Message = "Invalid email or password";
                }
                else
                {
                    response.Data = new LoadUserDto
                    {
                        JWTToken = CreateToken(user)
                    };
                }
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public ServiceResponse<LoadUserDto> LoadUser()
        {
            ServiceResponse<LoadUserDto> response = new();

            try
            {
                User user = Utilities.GetCurrentUser(_context, _httpContextAccessor);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    return response;
                }

                response.Data = _mapper.Map<LoadUserDto>(user);
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public ServiceResponse<string> DeleteUser(int userId)
        {
            ServiceResponse<string> response = new();

            try
            {
                User user = _context.Users.FirstOrDefault(x => x.Id == userId);

                _context.Remove(user);

                _context.SaveChangesAsync();

                response.Data = "User Deleted: " + user.FirstName;
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<SettingsDto>> GetSettings()
        {
            var response = new ServiceResponse<SettingsDto>
            {
                Data = new SettingsDto() { DarkMode = true, SidenavMini = false }
            };

            try
            {
                var user = Utilities.GetCurrentUser(_context, _httpContextAccessor);

                UserSettings? dbSettings = new();

                if (user is not null)
                {
                    dbSettings = await _context.UserSettings
                                       .FirstOrDefaultAsync(s => s.UserId == user.Id);
                    response.Data = _mapper.Map<SettingsDto>(dbSettings);
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
            return response;
        }

        public async Task<ServiceResponse<SettingsDto>> SaveSettings(SettingsDto newSettings)
        {
            var response = new ServiceResponse<SettingsDto>();

            try
            {
                response.Data = new SettingsDto();

                var user = Utilities.GetCurrentUser(_context, _httpContextAccessor);

                var dbSettings = await _context.UserSettings
                                   .FirstOrDefaultAsync(s => s.UserId == user.Id);

                _mapper.Map<SettingsDto, UserSettings>(newSettings, dbSettings);

                await _context.SaveChangesAsync();

                response.Data = newSettings;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
            return response;
        }

        public async Task<bool> UserExists(string email)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logging.LogException(ex);
            }

            return false;
        }

        private void CreatePasswordHash(
            string password,
            out byte[] passwordHash,
            out byte[] passwordSalt
        )
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computeHash.SequenceEqual(passwordHash);
            }
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Name, user.Email)
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    _configuration.GetSection("AppSettings.Key").Value
                )
            );

            SigningCredentials creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha512Signature
            );

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Double.Parse(_configuration["AppSettings.JWTTokenExpiration"])
                ),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
