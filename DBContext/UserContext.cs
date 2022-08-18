using Microsoft.EntityFrameworkCore;
using webapione.Models;

namespace webapione.DBContext
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
    }
}