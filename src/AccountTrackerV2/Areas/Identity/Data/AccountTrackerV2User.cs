using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

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
