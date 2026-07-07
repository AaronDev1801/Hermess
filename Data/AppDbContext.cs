using Microsoft.EntityFrameworkCore;
using Hermess.Models;

namespace Hermess.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileEntity> Files { get; set; }
    }
}
