﻿using System;
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
        }
    }
}
