using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace AccountTrackerV2.ViewModels
{
    public abstract class ListViewModel
    {
        public IList<Account> Accounts { get; set; }
        public IList<Category> Categories { get; set; }
        public IList<Transaction> Transactions { get; set; }
        public IList<Vendor> Vendors { get; set; }
        public SelectList AccountSelectList { get; set; }
        public SelectList CategorySelectList { get; set; }
        public SelectList TransactionTypesSelectList { get; set; }
        public SelectList VendorSelectList { get; set; }

        public SelectList InitAccountSelectList(IAccountRepository accountRepository, string userID)
        {
            return new SelectList(accountRepository.GetList(userID), "AccountID", "Name");
        }

        public SelectList InitCategorySelectList(ICategoryRepository categoryRepository, string userID)
        {
            return new SelectList(categoryRepository.GetList(userID), "CategoryID", "Name");
        }

        public SelectList InitTransactionTypeSelectList(ITransactionTypeRepository transactionTypeRepository)
        {
            return new SelectList(transactionTypeRepository.GetList(), "TransactionTypeID", "Name");
        }

        public SelectList InitVendorSelectList(IVendorRepository vendorRepository, string userID)
        {
            return new SelectList(vendorRepository.GetList(userID), "VendorID", "Name");
        }

        //Initialize all select lists.
        public void Init(string userID, IAccountRepository accountRepository, ICategoryRepository categoryRepository, ITransactionTypeRepository transactionTypeRepository, IVendorRepository vendorRepository)
        {
            AccountSelectList = InitAccountSelectList(accountRepository, userID);
            CategorySelectList = InitCategorySelectList(categoryRepository, userID);
            TransactionTypesSelectList = InitTransactionTypeSelectList(transactionTypeRepository);
            VendorSelectList = InitVendorSelectList(vendorRepository, userID);
        }
    }
}
