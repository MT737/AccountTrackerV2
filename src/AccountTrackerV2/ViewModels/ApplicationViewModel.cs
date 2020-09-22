using AccountTrackerV2.Data;
using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.ViewModels
{
    public class ApplicationViewModel
    {
        //TODO: This view model is doing too much.
        //TODO: Should also refactor to remove the need to pass userID to the view.

        //Properties
        public Transaction TransactionOfInterest { get; set; }
        public Account AccountOfInterest { get; set; }
        public Category CategoryOfInterest { get; set; }
        public Category AbsorptionCategory { get; set; }
        public Vendor VendorOfInterest { get; set; }
        public Vendor AbsorptionVendor { get; set; }
        public IList<Transaction> Transactions { get; set; }
        public IList<AccountWithBalance> AccountsWithBalances { get; set; }
        public IList<CategorySpending> ByCategorySpending { get; set; }
        public IList<VendorSpending> ByVendorSpending { get; set; }
        public SelectList TransactionTypesSelectList { get; set; }
        public SelectList AccountSelectList { get; set; }
        public SelectList CategorySelectList { get; set; }
        public SelectList VendorSelectList { get; set; }

        //BalanceByAccount class. Leaving it as a member of the DashboardViewModel for now as it's only needed for the dashboard.
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

        //TODO: Create individual list inits so as to not mass pull information when not necessary.
        //Initialize the select lists.
        public void Init(string userID, ITransactionTypeRepository transactionTypeRepository, IAccountRepository accountRepository, ICategoryRepository categoryRepository, IVendorRepository vendorRepository)
        {
            TransactionTypesSelectList = new SelectList(transactionTypeRepository.GetList(), "TransactionTypeID", "Name");
            AccountSelectList = new SelectList(accountRepository.GetList(userID), "AccountID", "Name");
            VendorSelectList = InitVendorSelectList(vendorRepository, userID);
            CategorySelectList = InitCategorySelectList(categoryRepository, userID);
        }

        public SelectList InitCategorySelectList(ICategoryRepository categoryRepository, string userID)
        {
            return new SelectList(categoryRepository.GetList(userID), "CategoryID", "Name");
        }

        public SelectList InitVendorSelectList(IVendorRepository vendorRepository, string userID)
        {
            return new SelectList(vendorRepository.GetList(userID), "VendorId", "Name");
        }

    }
}
