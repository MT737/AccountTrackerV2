using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Data;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static AccountTrackerV2.ViewModels.ApplicationViewModel;
using AccountTrackerV2.Interfaces;
using System.Security.Claims;

namespace AccountTrackerV2.Controllers
{
    /// <summary>
    /// Controller for the "Account" section of the website.
    /// </summary>
    public class AccountController : Controller
    {
        private IAccountRepository _accountRepository = null;
        private ICategoryRepository _categoryRepository = null;
        private ITransactionRepository _transactionRepository = null;
        private ITransactionTypeRepository _transactionTypeRepository = null;
        private IVendorRepository _vendorRepository = null;
        
        //Constructor [This is an excessive amount of work required just to make use of DI]
        public AccountController(IAccountRepository accountRepository, ICategoryRepository categoryRepository, ITransactionRepository transactionRepository,
            ITransactionTypeRepository transactionTypeRepository, IVendorRepository vendorRepository)
        {
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
            _transactionTypeRepository = transactionTypeRepository;
            _vendorRepository = vendorRepository;
        }

        public IActionResult Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            IList<ApplicationViewModel.AccountWithBalance> accounts = new List<ApplicationViewModel.AccountWithBalance>();
            accounts = GetAccountWithBalances(userID);

            return View(accounts);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            //Instantiate an empty ViewModel
            ApplicationViewModel vm = new ApplicationViewModel();

            //Instantiate an empty AccountOfInterest property of the VM
            vm.AccountOfInterest = new Account();
            vm.AccountOfInterest.UserID = userID;
            vm.TransactionOfInterest = new Transaction();
            vm.TransactionOfInterest.UserID = userID;

            //Call the view and pass the VM.
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(ApplicationViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vm.AccountOfInterest.Name != null)
            {
                //Validate the new account
                vm.AccountOfInterest.UserID = userID;
                vm.TransactionOfInterest.UserID = userID;
                ValidateAccount(vm.AccountOfInterest, vm.AccountOfInterest.UserID);

                //Confirm valid modelstate
                if (ModelState.IsValid)
                {
                    //Add the account to the account table in the DB.
                    //TODO: refactor for DI
                    Account account = new Account();
                    account = vm.AccountOfInterest;
                    _accountRepository.Add(account);

                    //Add a transaction to the transaction table in the DB to create the initial account balance.                
                    CompleteAccountTransaction(vm, true);
                    _transactionRepository.Add(vm.TransactionOfInterest);

                    TempData["Message"] = "Account successfully added.";

                    return RedirectToAction("Index");
                }
            }

            return View(vm);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                //TODO: confirm that this works the way I think it does.
                return BadRequest();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm user owns the account.
            if (!_accountRepository.UserOwnsAccount((int)id, userID))
            {
                return NotFound();
            }

            ApplicationViewModel vm = new ApplicationViewModel();
            vm.TransactionOfInterest = new Transaction();
            vm.AccountOfInterest = _accountRepository.Get((int)id, userID);
            vm.TransactionOfInterest.Amount = _accountRepository.GetBalance((int)id, userID, vm.AccountOfInterest.IsAsset);
           
            //TODO: Refactor to remove need to pass userID.
            vm.TransactionOfInterest.UserID = userID;
            vm.AccountOfInterest.UserID = userID;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vm.AccountOfInterest.Name != null)
            {
                //Validate the account
                vm.AccountOfInterest.UserID = userID;
                vm.TransactionOfInterest.UserID = userID;
                ValidateAccount(vm.AccountOfInterest, vm.AccountOfInterest.UserID);

                //Confirm user owns the account.
                if (!_accountRepository.UserOwnsAccount(vm.AccountOfInterest.AccountID, vm.AccountOfInterest.UserID))
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    //Grab current balance before updating the DB
                    decimal currentBalance = _accountRepository.GetBalance(vm.AccountOfInterest.AccountID, vm.AccountOfInterest.UserID, vm.AccountOfInterest.IsAsset);

                    //Update the account table in the DB
                    Account account = new Account();
                    account = vm.AccountOfInterest;
                    _accountRepository.Update(account);

                    //If balance changed, add an adjustment transaction.
                    if (currentBalance != vm.TransactionOfInterest.Amount)
                    {
                        //Determine adjustment
                        vm.TransactionOfInterest.Amount = vm.TransactionOfInterest.Amount - currentBalance;

                        //Add a transaction to the transaction table in the DB to create an account balance adjustment transaction.                
                        CompleteAccountTransaction(vm, false);
                        _transactionRepository.Add(vm.TransactionOfInterest);

                    }

                    TempData["Message"] = "Account successfully edited.";

                    return RedirectToAction("Index");
                }
            }

            return View(vm);
        }

        /// <summary>
        /// Performs basic validation on the account entity being added and edited.
        /// </summary>
        /// <param name="accountOfInterest">Account: account entity being added or edited.</param>
        /// <param name="userID">String: userID of the owner of the account.</param>
        private void ValidateAccount(Account accountOfInterest, string userID)
        {
            //New or updated Accounts cannot have the same name as currently existing accounts (except for the same account if editing).
            if (_accountRepository.NameExists(accountOfInterest, userID))
            {
                ModelState.AddModelError("accountOfInterest.Name", "The provided account name already exsits.");
            }
        }

        /// <summary>
        /// Fills the transaction information required when adding or editing a transaction.
        /// </summary>
        /// <param name="vm">ViewModel: viewmodel entity containing the required transaction and account information.</param>
        /// <param name="newAccount">Bool: indication as to if the account is new.</param>
        private void CompleteAccountTransaction(ApplicationViewModel vm, bool newAccount)
        {
            vm.TransactionOfInterest.AccountID = _accountRepository.GetID(vm.AccountOfInterest.Name, vm.AccountOfInterest.UserID);
            vm.TransactionOfInterest.VendorID = _vendorRepository.GetID("N/A");
            vm.TransactionOfInterest.TransactionDate = DateTime.Now.Date;

            if (newAccount)
            {
                vm.TransactionOfInterest.CategoryID = _categoryRepository.GetID("New Account");
                vm.TransactionOfInterest.Description = "New Account";
            }
            else
            {
                vm.TransactionOfInterest.CategoryID = _categoryRepository.GetID("Account Correction");
                vm.TransactionOfInterest.Description = "Account Balance Adjustment";
            }

            if (vm.AccountOfInterest.IsAsset)
            {
                vm.TransactionOfInterest.TransactionTypeID = _transactionTypeRepository.GetID("Payment To");
            }
            else
            {
                vm.TransactionOfInterest.TransactionTypeID = _transactionTypeRepository.GetID("Payment From");
            }
        }

        //TODO: This exact method is used in the dashboard controller as well. Put in a central location (DRY).
        /// <summary>
        /// Retrieves a list of accounts with associated balances.
        /// </summary>
        /// <param name="userID">String: userID for which to return account balances.</param>
        /// <returns>IList of AccountWithBalance entities.</returns>
        private IList<AccountWithBalance> GetAccountWithBalances(string userID)
        {
            //Get list of accounts
            IList<AccountWithBalance> accountsWithBalances = new List<AccountWithBalance>();
            foreach (var account in _accountRepository.GetList(userID))
            {
                //Set detailed values and get amount
                AccountWithBalance accountWithBalanceHolder = new AccountWithBalance();
                accountWithBalanceHolder.AccountID = account.AccountID;
                accountWithBalanceHolder.Name = account.Name;
                accountWithBalanceHolder.IsAsset = account.IsAsset;
                accountWithBalanceHolder.IsActive = account.IsActive;
                accountWithBalanceHolder.Balance = _accountRepository.GetBalance(account.AccountID, userID, account.IsAsset);
                accountsWithBalances.Add(accountWithBalanceHolder);
            }

            return accountsWithBalances;
        }
    }
}
