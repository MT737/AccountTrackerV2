using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AccountTrackerV2.ViewModels;
using AccountTrackerV2.Data;
using AccountTrackerV2.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;

namespace AccountTrackerV2.Controllers
{
    public class HomeController : Microsoft.AspNetCore.Mvc.Controller
    {
        //TODO: review documentation regarding ILogger. (Should I be including this in all my controllers?)
        private readonly ILogger<HomeController> _logger;
        private IAccountRepository _accountRepository = null;
        private ICategoryRepository _categoryRepository = null;
        private ITransactionRepository _transactionRepository = null;
        private IVendorRepository _vendorRepository = null;
        
        public HomeController(ILogger<HomeController> logger, IAccountRepository accountRepository, ICategoryRepository categoryRepository, 
            ITransactionRepository transactionRepository, IVendorRepository vendorRepository)
        {
            _logger = logger;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
            _vendorRepository = vendorRepository;
        }

        //TODO: review documentation regarding this.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            //TODO: add vm to DI?
            //Instantiate viewmodel
            HomeViewModel vm = new HomeViewModel
            {
                Transactions = GetTransactionsWithDetails(userID),
                AccountsWithBalances = GetAccountWithBalances(userID),
                ByCategorySpending = GetCategorySpending(userID),
                ByVendorSpending = GetVendorSpending(userID)
            };

            return View(vm);
        }

        /// <summary>
        /// Gets a list of VendorSpending objects.
        /// </summary>
        /// <param name="userID">String: UserID for which to retrieve VendorSpending objects.</param>
        /// <returns>IList VendorSpending objects.</returns>
        private IList<HomeViewModel.VendorSpending> GetVendorSpending(string userID)
        {
            IList<HomeViewModel.VendorSpending> vendorSpending = new List<HomeViewModel.VendorSpending>();
            foreach (var vendor in _vendorRepository.GetList(userID))
            {
                if (vendor.IsDisplayed)
                {
                    HomeViewModel.VendorSpending vendorSpendingHolder = new HomeViewModel.VendorSpending
                    {
                        Name = vendor.Name,
                        Amount = _vendorRepository.GetAmount(vendor.VendorID, userID)
                    };
                    
                    vendorSpending.Add(vendorSpendingHolder);
                }
            }
            return vendorSpending;
        }

        /// <summary>
        /// Gets a list of CategorySpending objects.
        /// </summary>
        /// <param name="userID">String: UserID for which to rerieve CategorySpending objects.</param>
        /// <returns></returns>
        private IList<HomeViewModel.CategorySpending> GetCategorySpending(string userID)
        {
            IList<HomeViewModel.CategorySpending> categorySpending = new List<HomeViewModel.CategorySpending>();
            foreach (var category in _categoryRepository.GetList(userID))
            {
                if (category.IsDisplayed)
                {
                    HomeViewModel.CategorySpending categorySpendingHolder = new HomeViewModel.CategorySpending
                    {
                        Name = category.Name,
                        Amount = _categoryRepository.GetCategorySpending(category.CategoryID, userID)
                    };

                    categorySpending.Add(categorySpendingHolder);
                }
            }
            return categorySpending;
        }

        //TODO: This exact method is used in the account controller as well. Put in a central location (DRY).
        /// <summary>
        /// Retrieves a list of accounts with associated balances.
        /// </summary>
        /// <param name="userID">String: userID for which to return account balances.</param>
        /// <returns>IList of AccountWithBalance entities.</returns>
        private IList<HomeViewModel.AccountWithBalance> GetAccountWithBalances(string userID)
        {
            //Get list of accounts
            IList<HomeViewModel.AccountWithBalance> accountsWithBalances = new List<HomeViewModel.AccountWithBalance>();
            foreach (var account in _accountRepository.GetList(userID))
            {
                //Set detailed values and get amount
                HomeViewModel.AccountWithBalance accountWithBalanceHolder = new HomeViewModel.AccountWithBalance();
                accountWithBalanceHolder.AccountID = account.AccountID;
                accountWithBalanceHolder.Name = account.Name;
                accountWithBalanceHolder.IsAsset = account.IsAsset;
                accountWithBalanceHolder.IsActive = account.IsActive;
                accountWithBalanceHolder.Balance = _accountRepository.GetBalance(account.AccountID, userID, account.IsAsset);
                accountsWithBalances.Add(accountWithBalanceHolder);
            }

            return accountsWithBalances;
        }

        //TODO: This exact method is used in the transaction controller as well. Put in a central location (DRY).
        /// <summary>
        /// Retrieves a list of transactions associated to the specified user.
        /// </summary>
        /// <param name="userID">String: UserID for which to pull a list of transactions.</param>
        /// <returns>IList of Transaction objects.</returns>
        public IList<Transaction> GetTransactionsWithDetails(string userID)
        {
            //Get get a list of transactions to gain access to transaction ids
            IList<Transaction> transactions = new List<Transaction>();
            foreach (var transaction in _transactionRepository.GetList(userID))
            {
                //Get the detailed data for each transaction and add it to the IList of transactions
                transactions.Add(_transactionRepository.Get(transaction.TransactionID, userID));
            }

            return transactions;
        }
    }
}
