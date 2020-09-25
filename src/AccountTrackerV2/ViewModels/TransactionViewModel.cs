using Microsoft.AspNetCore.Mvc.Diagnostics;
using System;
using System.ComponentModel.DataAnnotations;

namespace AccountTrackerV2.ViewModels
{
    public class TransactionViewModel : ListViewModel
    {

        public VMTransaction TransactionOfInterest { get; set; }

        public class VMTransaction
        {
            public int TransactionID { get; set; }

            [Required]
            [Display(Name = "Transaction Date")]
            public DateTime TransactionDate { get; set; }

            [Required]
            [Display(Name = "Transaction Type")]
            public int TransactionTypeID { get; set; }

            public string TransactionType { get; set; }

            [Required]
            [Display(Name = "Account")]
            public int AccountID { get; set; }
            
            public string Account { get; set; }

            [Required]
            [Display(Name = "Category")]
            public int CategoryID { get; set; }

            public string Category { get; set; }

            [Required]
            [Display(Name = "Vendor")]
            public int VendorID { get; set; }

            public string Vendor { get; set; }

            [Required]
            [RegularExpression(@"^\d+\.\d{0,2}$")]
            [Range(-9999999999999999.99, 9999999999999999.99)]
            public decimal Amount { get; set; }

            public string Description { get; set; }
        }
    }
}