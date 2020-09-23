using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.ViewModels
{
    public class AccountViewModel : ListViewModel
    {
        public IList<AccountWithBalance> AccountsWithBalances { get; set; }
        public VMAccount AccountOfInterest { get; set; }
        public VMAccountTransaction AccountTransaction { get; set; }

        public class AccountWithBalance
        {
            public int AccountID { get; set; }
            public string Name { get; set; }
            public decimal Balance { get; set; }
            public bool IsAsset { get; set; }
            public bool IsActive { get; set; }
        }

        public class VMAccount
        {
            public int AccountID { get; set; }
            [Required]
            public string Name { get; set; }
            [Display(Name = "Is Asset")]
            public bool IsAsset { get; set; }
            [Display(Name = "Is Active")]
            public bool IsActive { get; set; }
        }

        public class VMAccountTransaction
        {
            public int AccountID { get; set; }
            public int TransactionTypeID { get; set; }
            public int CategoryID { get; set; }
            public int VendorID { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Description { get; set; }

            //TODO: Look for a better way to set decimal.
            [Required]
            [RegularExpression(@"^\d+\.\d{0,2}$")]
            [Range(-9999999999999999.99, 9999999999999999.99)]
            public decimal Amount { get; set; }
        }
    }
}
