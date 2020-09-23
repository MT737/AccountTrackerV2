using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccountTrackerV2.Data
{
    public class AccountTrackerV2Context : IdentityDbContext<AccountTrackerV2User>
    {
        public AccountTrackerV2Context(DbContextOptions<AccountTrackerV2Context> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Vendor> Vendors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Transaction>()
                .Property(t => t.Amount).HasColumnType("decimal(18,2)");

            builder.Entity<TransactionType>().HasData(
                new TransactionType { TransactionTypeID = 1, Name = "Payment From" },
                new TransactionType { TransactionTypeID = 2, Name = "Payment To" }
                );

            builder.Entity("AccountTrackerV2.Models.Transaction", b =>
            {
                b.HasOne("AccountTrackerV2.Models.Account", "Account")
                    .WithMany("Transactions")
                    .HasForeignKey("AccountID")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("AccountTrackerV2.Models.Category", "Category")
                    .WithMany("Transactions")
                    .HasForeignKey("CategoryID")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("AccountTrackerV2.Models.TransactionType", "TransactionType")
                    .WithMany("Transactions")
                    .HasForeignKey("TransactionTypeID")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("AccountTrackerV2.Areas.Identity.Data.AccountTrackerV2User", "User")
                    .WithMany()
                    .HasForeignKey("UserID")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("AccountTrackerV2.Models.Vendor", "Vendor")
                    .WithMany("Transactions")
                    .HasForeignKey("VendorID")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

        }
    }
}
