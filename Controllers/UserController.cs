using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public UserController(UserContext context)
        {
            _context = context;
        }

        static bool InvalidInput(string? input)
        {
            return input == "string" || input == "";
        }

        //funktioniert aber sieht scheiße aus, aufklappen auf eigene gefahr

        [HttpPost("CreateUser")] 
        public async Task<ActionResult<List<User>>> CreateUser(User user)
        {
            try
            {
                if (_context.Users.Any(x => x.UserName == user.UserName))
                    return BadRequest("Username already taken");

                if(InvalidInput(user.FirstName) || InvalidInput(user.LastName) || InvalidInput(user.UserName))
                    return BadRequest("Please fill in all the required Data");

                if (!ValidPassword(user.Password))
                    return BadRequest("Password invalid: Password has to be => 8 and <= 32 characters and contain atleast 1 special character");

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return await ReadUser(user.Id);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("ReadUser")]
        public async Task<ActionResult<List<User>>> ReadUser(int id)
        {
            try
            {
                var result = await _context.Users.Where(x => x.Id == id).ToListAsync();

                return result.Count switch
                {
                    0 => (ActionResult<List<User>>)BadRequest("User does not exist"),
                    1 => (ActionResult<List<User>>)Ok(result[0]),
                    _ => (ActionResult<List<User>>)BadRequest("Unknown error"),
                };

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
        public async Task<ActionResult<List<User>>> UpdateUser(User user)
        {
            try
            {
                var result = await _context.Users.Where(x => x.Id == user.Id).ToListAsync();

                result[0].FirstName = InvalidInput(user.FirstName) == true ? result[0].FirstName : user.FirstName;
                result[0].LastName = InvalidInput(user.LastName) == true ? result[0].LastName : user.LastName;
                result[0].UserName = InvalidInput(user.UserName) == true ? result[0].UserName : user.UserName;
                result[0].Password = InvalidInput(user.Password) == true ? result[0].Password : user.Password;

                if (!(_context.Users.Any(x => x.Id == user.Id) && ValidPassword(result[0].Password)))
                    return BadRequest("Überprüfe deine Angaben nochmal du Opfer");

                _context.Update(result[0]);
                await _context.SaveChangesAsync();

                return await ReadUser(result[0].Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<List<User>>> DeleteUser(int id)
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
- Einbindung
*/