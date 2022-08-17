using Microsoft.EntityFrameworkCore;
using webapione.Models;

namespace webapione.DBContext
{
    public class UserContext : DbContext
    {
        #pragma warning disable CS8618 
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        #pragma warning restore CS8618 
        {

        }

        public DbSet<User> Users { get; set; }
    }
}