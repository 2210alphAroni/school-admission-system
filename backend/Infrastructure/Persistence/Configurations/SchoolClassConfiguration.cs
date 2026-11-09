using backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("school_classes", table =>
            table.HasCheckConstraint(
                "ck_school_classes_total_seats_positive",
                "total_seats > 0"
            )
        );

        builder.HasKey(schoolClass => schoolClass.Id);

        builder.Property(schoolClass => schoolClass.Id)
            .HasColumnName("id")
            .HasMaxLength(64)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(schoolClass => schoolClass.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(schoolClass => schoolClass.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(schoolClass => schoolClass.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasMany(schoolClass => schoolClass.Holds)
            .WithOne(hold => hold.SchoolClass)
            .HasForeignKey(hold => hold.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
