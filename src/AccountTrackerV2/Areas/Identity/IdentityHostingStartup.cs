using System;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(AccountTrackerV2.Areas.Identity.IdentityHostingStartup))]
namespace AccountTrackerV2.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<AccountTrackerV2UserContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("AccountTrackerV2UserContextConnection")));

                services.AddDefaultIdentity<AccountTrackerV2User>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<AccountTrackerV2UserContext>();
            });
        }
    }
}