using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

            AccountViewModel vm = new AccountViewModel();
            vm.AccountsWithBalances = GetAccountsWithBalances(userID);

            //TODO: May want to reconsider this placement. Could create a drag to keep pinging the DB if the user doesn't create an account.
            //If no accounts, this is likely a new user. Run the default builders.
            if (vm.AccountsWithBalances.Count == 0)
            {
                _vendorRepository.CreateDefaults(userID);
                _categoryRepository.CreateDefaults(userID);
            }

            return View(vm);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Instantiate an empty ViewModel
            AccountViewModel vm = new AccountViewModel();

            //Instantiate empty AccountOfInterest and TransactionOfInterest properties in the VM
            vm.AccountOfInterest = new AccountViewModel.VMAccount();
            vm.AccountTransaction = new AccountViewModel.VMAccountTransaction();

            //Call the view and pass the VM.
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(AccountViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vm.AccountOfInterest.Name != null)
            {
                //Validate the new account                
                ValidateAccount(vm, userID);

                //Confirm valid modelstate
                if (ModelState.IsValid)
                {
                    //Convert the VMAccount to a DBAccount.
                    Account account = new Account
                    {
                        Name = vm.AccountOfInterest.Name,
                        UserID = userID,
                        IsAsset = vm.AccountOfInterest.IsAsset,
                        IsActive = vm.AccountOfInterest.IsActive
                    };

                    //Add the account to the account table in the DB.
                    _accountRepository.Add(account);

                    //Gathering the data required to complete a transaction.
                    CompleteAccountTransaction(vm, newAccount: true, userID);

                    //Convert the VMTransaction to a DBTransaction
                    Transaction transaction = new Transaction
                    {
                        UserID = userID,
                        TransactionDate = vm.AccountTransaction.TransactionDate,
                        TransactionTypeID = vm.AccountTransaction.TransactionTypeID,
                        AccountID = vm.AccountTransaction.AccountID,
                        CategoryID = vm.AccountTransaction.CategoryID,
                        VendorID = vm.AccountTransaction.VendorID,
                        Amount = vm.AccountTransaction.Amount,
                        Description = vm.AccountTransaction.Description
                    };

                    //Add the transaction to the transaction table in the DB to create the initial account balance.                
                    _transactionRepository.Add(transaction);

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
                //TODO: Can I create a dynamic version to give the user a more detailed response.
                return BadRequest();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm user owns the account
            if (!_accountRepository.UserOwnsAccount((int)id, userID))
            {
                return NotFound();
            }

            //Get the account
            Account account = _accountRepository.Get((int)id, userID);

            //Convert the DBAccount to a VMAccount
            AccountViewModel vm = new AccountViewModel();
            vm.AccountOfInterest = new AccountViewModel.VMAccount
            {
                AccountID = account.AccountID,
                Name = account.Name,
                IsAsset = account.IsAsset,
                IsActive = account.IsActive
            };
            vm.AccountTransaction = new AccountViewModel.VMAccountTransaction
            {
                Amount = _accountRepository.GetBalance(account.AccountID, userID, account.IsAsset)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AccountViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vm.AccountOfInterest.Name != null)
            {
                //Validate the account
                ValidateAccount(vm, userID);

                //Confirm user owns the account.
                if (!_accountRepository.UserOwnsAccount(vm.AccountOfInterest.AccountID, userID))
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    //Grab current balance before updating the DB
                    decimal currentBalance = _accountRepository.GetBalance(vm.AccountOfInterest.AccountID, userID, vm.AccountOfInterest.IsAsset);

                    //Convert the VMAccount to DBAccount
                    Account account = new Account
                    {
                        AccountID = vm.AccountOfInterest.AccountID,
                        Name = vm.AccountOfInterest.Name,
                        IsAsset = vm.AccountOfInterest.IsAsset,
                        IsActive = vm.AccountOfInterest.IsActive,
                        UserID = userID
                    };

                    //Update the account table in the DB
                    _accountRepository.Update(account);

                    //If balance changed, add an adjustment transaction.
                    if (currentBalance != vm.AccountTransaction.Amount)
                    {
                        //Determine adjustment
                        vm.AccountTransaction.Amount = vm.AccountTransaction.Amount - currentBalance;

                        //Gathering the data required to complete a transaction.
                        CompleteAccountTransaction(vm, newAccount: false, userID);

                        //Convert the VMTransaction to DBTransaction
                        Transaction transaction = new Transaction
                        {
                            UserID = userID,
                            TransactionDate = vm.AccountTransaction.TransactionDate,
                            TransactionTypeID = vm.AccountTransaction.TransactionTypeID,
                            AccountID = vm.AccountTransaction.AccountID,
                            CategoryID = vm.AccountTransaction.CategoryID,
                            VendorID = vm.AccountTransaction.VendorID,
                            Amount = vm.AccountTransaction.Amount,
                            Description = vm.AccountTransaction.Description
                        };

                        //Add a transaction to the transaction table in the DB to create an account balance adjustment transaction.                
                        _transactionRepository.Add(transaction);
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
        private void ValidateAccount(AccountViewModel vm, string userID)
        {
            //New or updated Accounts cannot have the same name as currently existing accounts (except for the same account if editing).
            if (_accountRepository.NameExists(vm, userID))
            {
                ModelState.AddModelError("accountOfInterest.Name", "The provided account name already exsits.");
            }
        }

        /// <summary>
        /// Fills the transaction information required when adding or editing a transaction.
        /// </summary>
        /// <param name="vm">ViewModel: viewmodel containing the required transaction and account information.</param>
        /// <param name="newAccount">Bool: indication as to if the account is new.</param>
        private void CompleteAccountTransaction(AccountViewModel vm, bool newAccount, string userID)
        {
            vm.AccountTransaction.AccountID = _accountRepository.GetID(vm.AccountOfInterest.Name, userID);
            vm.AccountTransaction.VendorID = _vendorRepository.GetID("N/A", userID);
            vm.AccountTransaction.TransactionDate = DateTime.Now.Date;

            if (newAccount)
            {
                vm.AccountTransaction.CategoryID = _categoryRepository.GetID("New Account", userID);
                vm.AccountTransaction.Description = "New Account";
            }
            else
            {
                vm.AccountTransaction.CategoryID = _categoryRepository.GetID("Account Correction", userID);
                vm.AccountTransaction.Description = "Account Balance Adjustment";
            }

            if (vm.AccountOfInterest.IsAsset)
            {
                vm.AccountTransaction.TransactionTypeID = _transactionTypeRepository.GetTransactionType("Payment To").TransactionTypeID;
            }
            else
            {
                vm.AccountTransaction.TransactionTypeID = _transactionTypeRepository.GetTransactionType("Payment From").TransactionTypeID;
            }
        }

        //TODO: This exact method is used in the dashboard controller as well. Put in a central location (DRY).
        /// <summary>
        /// Retrieves a list of accounts with associated balances.
        /// </summary>
        /// <param name="userID">String: userID for which to return account balances.</param>
        /// <returns>IList of AccountWithBalance entities.</returns>
        private IList<AccountViewModel.AccountWithBalance> GetAccountsWithBalances(string userID)
        {
            //Get list of accounts
            IList<AccountViewModel.AccountWithBalance> accountsWithBalances = new List<AccountViewModel.AccountWithBalance>();
            foreach (var account in _accountRepository.GetList(userID))
            {
                //Set detailed values and get amount
                AccountViewModel.AccountWithBalance accountWithBalanceHolder = new AccountViewModel.AccountWithBalance
                {
                    AccountID = account.AccountID,
                    Name = account.Name,
                    IsAsset = account.IsAsset,
                    IsActive = account.IsActive,
                    Balance = _accountRepository.GetBalance(account.AccountID, userID, account.IsAsset)
                };

                accountsWithBalances.Add(accountWithBalanceHolder);
            }

            return accountsWithBalances;
        }
    }
}
