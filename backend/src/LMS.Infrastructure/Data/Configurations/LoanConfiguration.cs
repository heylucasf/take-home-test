using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.HasKey(e => e.Id);
            
            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.CurrentBalance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.ApplicantName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsRequired(false);

            SeedData(builder);
        }

        private void SeedData(EntityTypeBuilder<Loan> builder)
        {
            var createdDate = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var updatedDate = new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Amount = 1500.00m,
                    CurrentBalance = 1500.00m,
                    ApplicantName = "Maria Silva",
                    Status = LoanStatus.Active,
                    CreatedAt = createdDate,
                    UpdatedAt = updatedDate
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Amount = 5000.00m,
                    CurrentBalance = 2500.00m,
                    ApplicantName = "João Santos",
                    Status = LoanStatus.Active,
                    CreatedAt = createdDate,
                    UpdatedAt = updatedDate
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Amount = 3000.00m,
                    CurrentBalance = 0.00m,
                    ApplicantName = "Ana Costa",
                    Status = LoanStatus.Paid,
                    CreatedAt = createdDate,
                    UpdatedAt = updatedDate
                },
                new
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Amount = 10000.00m,
                    CurrentBalance = 8500.00m,
                    ApplicantName = "Pedro Oliveira",
                    Status = LoanStatus.Active,
                    CreatedAt = createdDate,
                    UpdatedAt = updatedDate
                },
                new
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Amount = 800.00m,
                    CurrentBalance = 500.00m,
                    ApplicantName = "Carla Ferreira",
                    Status = LoanStatus.Active,
                    CreatedAt = createdDate,
                    UpdatedAt = updatedDate
                }
            );
        }
    }
}