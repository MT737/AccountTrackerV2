using AccountTrackerV2.Areas.Identity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.Models
{
    public class Category
    {
        //Properties
        public int CategoryID { get; set; }

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
        public Category()
        {
            Transactions = new List<Transaction>();
        }

        /// <summary>
        /// Constructor for creating category.
        /// </summary>
        /// <param name="user">The user for the category.</param>
        /// <param name="name">The name of the category.</param>
        /// <param name="isDefault">Bool representing the default status of the category.</param>
        /// <param name="isDisplayed">Bool representing of the category is displayed.</param>
        public Category(AccountTrackerV2User user, string name, bool isDefault, bool isDisplayed)
        {
            UserID = user.Id;
            Name = name;
            IsDefault = isDefault;
            IsDisplayed = isDisplayed;
        }
    }
}
