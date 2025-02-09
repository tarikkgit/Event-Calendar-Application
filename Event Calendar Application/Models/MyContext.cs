using Microsoft.EntityFrameworkCore;
namespace EventPlanner.Models
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Time> Times { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Invite> Invites { get; set; }
        public DbSet<RequestedInvite> RequestedInvites { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Feedback> Feedback { get; set; } // Bu sat�r� ekleyin
    }
}
