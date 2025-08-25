using CosmoChess.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CosmoChess.Infrastructure.Persistence.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Username).IsRequired().HasMaxLength(32);
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.HasIndex(x => x.Username).IsUnique();
        }
    }
}
