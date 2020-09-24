using System.Collections.Generic;

namespace AccountTrackerV2.ViewModels
{
    public class HomeViewModel : ListViewModel
    {
        public IList<AccountWithBalance> AccountsWithBalances { get; set; }
        public IList<CategorySpending> ByCategorySpending { get; set; }
        public IList<VendorSpending> ByVendorSpending { get; set; }


        public class AccountWithBalance
        {
            public int AccountID { get; set; }
            public string Name { get; set; }
            public decimal Balance { get; set; }
            public bool IsAsset { get; set; }
            public bool IsActive { get; set; }
        }

        //CategorySpending class. Leaving it as a member of the DashboardViewModel for now as it's only needed for the dashboard.
        public class CategorySpending
        {
            public string Name { get; set; }
            public decimal Amount { get; set; }
        }

        //VendorSpending class. Leaving it as a member of the DashboardViewModel for now as it's only needed for the dashboard.
        public class VendorSpending
        {
            public string Name { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
