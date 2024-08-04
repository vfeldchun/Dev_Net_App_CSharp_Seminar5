using Microsoft.EntityFrameworkCore;

namespace ChatDB.Models
{
    public class ChatDbContext : DbContext
    {
        public virtual DbSet<User> Users {  get; set; }
        public virtual DbSet<Message> Messages { get; set; }

        public ChatDbContext()
        {            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.LogTo(Console.WriteLine).UseLazyLoadingProxies().UseNpgsql("Host=localhost;Username=postgres;Password=example;Database=ChatDB");
            optionsBuilder.UseLazyLoadingProxies().UseNpgsql("Host=localhost;Username=postgres;Password=example;Database=ChatDB");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id).HasName("user_pkey");
                entity.ToTable("Users");
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Name).HasMaxLength(255).HasColumnName("name");
                
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(x => x.Id)
                .HasName("message_pkey");
                entity.ToTable("Messages");
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Text).HasColumnName("text");
                entity.Property(x => x.FromUserId).HasColumnName("from_user_id");
                entity.Property(x => x.ToUserId).HasColumnName("to_user_id");

                entity.HasOne(d => d.FromUser)
                    .WithMany(p => p.FromMessages)
                    .HasForeignKey(e => e.FromUserId)
                    .HasConstraintName("messages_from_user_id_fkey");
                
                entity.HasOne(d => d.ToUser)
                    .WithMany(p => p.ToMessages)
                    .HasForeignKey(e => e.ToUserId)
                    .HasConstraintName("messages_to_user_id_fkey");
            });

            base.OnModelCreating(modelBuilder);

        }
    }
}
