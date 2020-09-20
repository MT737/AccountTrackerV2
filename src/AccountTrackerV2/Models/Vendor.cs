using AccountTrackerV2.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.Models
{
    public class Vendor
    {
        //Properties
        public int VendorID { get; set; }

        public AccountTrackerV2User User { get; set; }

        [Required]
        public string UserID { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public bool IsDefault { get; set; }

        [Required]
        public bool IsDisplayed { get; set; }


        //Navigation Property
        public ICollection<Transaction> Transactions { get; set; }

        //Instantiation of navigation collection.
        public Vendor()
        {
            Transactions = new List<Transaction>();
        }

        public Vendor(AccountTrackerV2User user, string name, bool isDefault, bool isDisplayed)
        {
            UserID = user.Id;
            Name = name;
            IsDefault = isDefault;
            IsDisplayed = isDisplayed;
        }
    }
}
