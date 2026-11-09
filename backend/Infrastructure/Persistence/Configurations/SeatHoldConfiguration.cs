using backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Infrastructure.Persistence.Configurations;

public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("seat_holds");

        builder.HasKey(hold => hold.Id);

        builder.Property(hold => hold.Id)
            .HasColumnName("id")
            .HasMaxLength(64)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(hold => hold.ClassId)
            .HasColumnName("class_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(hold => hold.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(hold => hold.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(hold => hold.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(hold => hold.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(hold => hold.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(hold => hold.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(hold => new { hold.ClassId, hold.Status })
            .HasDatabaseName("ix_seat_holds_class_id_status");

        builder.HasIndex(hold => new { hold.StudentId, hold.CreatedAt })
            .HasDatabaseName("ix_seat_holds_student_id_created_at");
    }
}
