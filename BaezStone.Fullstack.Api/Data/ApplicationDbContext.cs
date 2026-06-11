using BaezStone.Fullstack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BaezStone.Fullstack.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Movie> Movies { get; set; }
    }
}
