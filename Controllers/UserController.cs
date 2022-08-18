﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using webapione.DBContext;
using webapione.Models;

namespace webapione.Controllers
{

    [Route("api/[Controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        readonly Regex rgx = new("[^A-Za-z0-9]");

        bool ContainsSpecialCharacter(string input)
        {
            return rgx.IsMatch(input);
        }

        bool ValidPassword(string? password)
        {
            return password!.Length >= 8 && password.Length <= 32 && ContainsSpecialCharacter(password);
        }

        private readonly UserContext _context;
        private IConfiguration _configuration;


        public UserController(UserContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        static bool InvalidInput(string? input)
        {
            return input == "string" || input == "";
        }

        public static User user = new User();

        //funktioniert aber sieht scheiße aus, aufklappen auf eigene gefahr

        [HttpPost("CreateUser")] 
        public async Task<ActionResult<List<User>>> CreateUser(UserDto request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            try
            {
                if (_context.Users.Any(x => x.UserName == request.UserName))
                    return BadRequest("Username already taken");

                if(InvalidInput(request.FirstName) || InvalidInput(request.LastName) || InvalidInput(request.UserName))
                    return BadRequest("Please fill in all the required Data");

                if (!ValidPassword(request.Password))
                    return BadRequest("Password invalid: Password has to be => 8 and <= 32 characters and contain atleast 1 special character");

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.UserName = request.UserName;
                user.Password = request.Password;
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(user);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            try
            {
                var searchedUser = _context.Users.Where(x => x.UserName == request.UserName).Single();

                if (searchedUser.UserName != request.UserName || searchedUser == null)
                {
                    return BadRequest("User not found.");
                }

                if(!VerifyPasswordHash(request.Password, searchedUser.PasswordHash, searchedUser.PasswordSalt))
                {
                    return BadRequest("Wrong Password.");
                }
                
                string token = CreateToken(searchedUser);

                return Ok(token);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computecHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computecHash.SequenceEqual(passwordHash);
            }
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        [HttpGet("ReadUser")]
        public async Task<ActionResult<User>> ReadUser(int id)
        {
            try
            {
                var result = _context.Users.Where(x => x.Id == id).Single();

                return result;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAllUsers")]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var result = await _context.Users.ToListAsync();
            Console.WriteLine(result);
            return Ok(result);
        }

        [HttpPut("UpdateUser")]
        public async Task<ActionResult<User>> UpdateUser(User user)
        {
            try
            {
                var result = _context.Users.Where(x => x.Id == user.Id).Single();

                result.FirstName = InvalidInput(user.FirstName) == true ? result.FirstName : user.FirstName;
                result.LastName = InvalidInput(user.LastName) == true ? result.LastName : user.LastName;
                result.UserName = InvalidInput(user.UserName) == true ? result.UserName : user.UserName;
                result.Password = InvalidInput(user.Password) == true ? result.Password : user.Password;

                if (!(_context.Users.Any(x => x.Id == user.Id) && ValidPassword(result.Password)))
                    return BadRequest("Überprüfe deine Angaben nochmal du Opfer");

                _context.Update(result);
                await _context.SaveChangesAsync();

                return await ReadUser(result.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            try
            {
                var userToRemove = await _context.Users.Where(x => x.Id == id).ToListAsync();
                _context.Users.Remove(userToRemove[0]);

                await _context.SaveChangesAsync();
                return await ReadUser(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("ClearDatabase")]
        public async Task<ActionResult<List<User>>> ClearDatabase()
        {
            try
            {
                foreach (var user in _context.Users)
                {
                    var userToRemove = await _context.Users.Where(x => x.Id == user.Id).ToListAsync();
                    _context.Users.Remove(userToRemove[0]);
                }

                await _context.SaveChangesAsync();
                Console.WriteLine(_context.Users.Count());
                return Ok("Database cleared");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

/*
Todo:
- Validation IsEmpty
- API security (Json Net token, allg Token Systeme)
*/