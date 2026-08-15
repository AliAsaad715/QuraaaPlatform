using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Persistence.Configurations;

public sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.ToTable("PushDevices", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_PushDevices_Token_NotBlank",
                "btrim(\"Token\") <> ''");
            tableBuilder.HasCheckConstraint(
                "CK_PushDevices_Token_NoWhitespace",
                "\"Token\" !~ '[[:space:]]'");
            tableBuilder.HasCheckConstraint(
                "CK_PushDevices_TokenHash_Valid",
                "char_length(\"TokenHash\") = 64 AND \"TokenHash\" ~ '^[0-9A-F]{64}$'");
        });

        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id).ValueGeneratedNever();

        builder.Property(device => device.UserId).IsRequired();
        builder.Property(device => device.Token)
            .HasMaxLength(PushDevice.TokenMaxLength)
            .IsRequired();
        builder.Property(device => device.TokenHash)
            .HasMaxLength(PushDevice.TokenHashLength)
            .IsRequired();
        builder.Property(device => device.CreationTime).IsRequired();

        builder.HasOne<UserAggregate>()
            .WithMany()
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(device => device.UserId)
            .HasDatabaseName("IX_PushDevices_UserId");
        builder.HasIndex(device => device.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_PushDevices_TokenHash");
    }
}
