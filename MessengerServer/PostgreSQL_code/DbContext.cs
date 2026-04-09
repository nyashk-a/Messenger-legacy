using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessengerServer.PostgreSQL_code
{
    internal class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Participant> Participants { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=messenger_db;Username=postgres;Password=yourpass");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.SUID)
                .ValueGeneratedNever();

            modelBuilder.Entity<Message>()
                .Property(m => m.SUID)
                .ValueGeneratedNever();

            modelBuilder.Entity<Chat>()
                .Property(c => c.SUID)
                .ValueGeneratedNever();

            modelBuilder.Entity<Message>()
                .Property(m => m.Time)
                .HasColumnType("time without time zone");

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.Owner);
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.Membership);
            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.UserSUID);
            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.ChatSUID);
        }
    }
}
