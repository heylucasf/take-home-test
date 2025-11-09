using Fundo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Infrastructure.Data.Configurations
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
            var now = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 1500.00m,
                    CurrentBalance = 1500.00m,
                    ApplicantName = "Maria Silva",
                    Status = LoanStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 5000.00m,
                    CurrentBalance = 2500.00m,
                    ApplicantName = "João Santos",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-15),
                    UpdatedAt = (DateTime?)now.AddDays(-5)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 3000.00m,
                    CurrentBalance = 0.00m,
                    ApplicantName = "Ana Costa",
                    Status = LoanStatus.Paid,
                    CreatedAt = now.AddDays(-30),
                    UpdatedAt = (DateTime?)now.AddDays(-2)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 10000.00m,
                    CurrentBalance = 8500.00m,
                    ApplicantName = "Pedro Oliveira",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-20),
                    UpdatedAt = (DateTime?)now.AddDays(-10)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 800.00m,
                    CurrentBalance = 500.00m,
                    ApplicantName = "Carla Ferreira",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-7),
                    UpdatedAt = (DateTime?)now.AddDays(-3)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 7500.00m,
                    CurrentBalance = 7500.00m,
                    ApplicantName = "Ricardo Alves",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 2000.00m,
                    CurrentBalance = 0.00m,
                    ApplicantName = "Juliana Martins",
                    Status = LoanStatus.Paid,
                    CreatedAt = now.AddDays(-45),
                    UpdatedAt = (DateTime?)now.AddDays(-1)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 4200.00m,
                    CurrentBalance = 3000.00m,
                    ApplicantName = "Fernando Lima",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-12),
                    UpdatedAt = (DateTime?)now.AddDays(-6)
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 1200.00m,
                    CurrentBalance = 1200.00m,
                    ApplicantName = "Beatriz Sousa",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Amount = 6000.00m,
                    CurrentBalance = 2400.00m,
                    ApplicantName = "Lucas Mendes",
                    Status = LoanStatus.Active,
                    CreatedAt = now.AddDays(-25),
                    UpdatedAt = (DateTime?)now.AddDays(-8)
                }
            );
        }
    }
}