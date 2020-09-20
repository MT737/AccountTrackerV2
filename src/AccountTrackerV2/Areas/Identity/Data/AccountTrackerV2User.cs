using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace AccountTrackerV2.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the AccountTrackerV2User class
    public class AccountTrackerV2User : IdentityUser
    {
        [Required]
        [PersonalData]
        public string FirstName { get; set; }

        [Required]
        [PersonalData]
        public string LastName { get; set; }

        [Required]
        [PersonalData]
        public DateTime DOB { get; set; }
    }
}
