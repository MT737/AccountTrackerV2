using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountTrackerV2.Data;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using AccountTrackerV2.Models;
using Microsoft.AspNetCore.Identity;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace AccountTrackerV2.Controllers
{
    public class TransactionController : Controller
    {
        //TODO: Need to account for instances where the user deletes their user account! Cascade delete works for all other tables but transactions!

        private IAccountRepository _accountRepository = null;
        private ICategoryRepository _categoryRepository = null;
        private ITransactionRepository _transactionRepository = null;
        private ITransactionTypeRepository _transactionTypeRepository = null;
        private IVendorRepository _vendorRepository = null;

        public TransactionController(IAccountRepository accountRepository, ICategoryRepository categoryRepository, ITransactionRepository transactionRepository,
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

            //TODO: tell user to create an account if none exist.

            //Instantiate viewmodel with list of transactions.
            //TODO: Refactor for DI?
            TransactionViewModel vm = new TransactionViewModel
            {
                Transactions = GetTransactionsWithDetails(userID)
            };

            return View(vm);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: add a check for an account and warn the user to create one if none exist.

            //Instantiate viewmodel
            TransactionViewModel vm = new TransactionViewModel();

            //Instantiate Transaction of Interest property
            vm.TransactionOfInterest = new TransactionViewModel.VMTransaction
            {
                //Prefill defaults.
                TransactionDate = DateTime.Now.Date,
                Amount = 0.00M,
            };

            //TODO: Consider limiting the select list items. Probably shouldn't allow users to select new account balance.
            //Initialize set list items.
            vm.Init(userID, _accountRepository, _categoryRepository, _transactionTypeRepository, _vendorRepository);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(TransactionViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: Confirm validation requirements.
            if (ModelState.IsValid)
            {
                //Convert VMTransaction to DBTransaction
                Transaction transaction = ConvertToDBTransaction(vm.TransactionOfInterest, userID);

                //Add transaction to the DB
                _transactionRepository.Add(transaction);

                TempData["Message"] = "Transaction successfully added.";

                return RedirectToAction("Index");
            }

            vm.Init(userID, _accountRepository, _categoryRepository, _transactionTypeRepository, _vendorRepository);
            return View(vm);
        }

        public IActionResult Edit(int? id)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm id is not null
            if (id == null)
            {
                return BadRequest();
            }

            //Get transaction if it exists.
            Transaction transaction = _transactionRepository.Get((int)id, userID);

            //Confirm transaction exists. This doubles as ensuring that the user owns the transaction, as the object will be null if the UserID and TransactionID combo don't exist.            
            if (transaction == null)
            {
                return NotFound();
            }

            //Convert DBTransaction to VMTransaction
            TransactionViewModel vm = new TransactionViewModel();
            vm.TransactionOfInterest = ConvertToVMTransaction(transaction);

            //Initialize select list items
            vm.Init(userID, _accountRepository, _categoryRepository, _transactionTypeRepository, _vendorRepository);

            //Return the view
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TransactionViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: Currently allowing edit even when nothing is changed. Fix that.
            //TODO: Confirm additional validation requirements.

            //Confirm user owns the transaction
            if (!_transactionRepository.UserOwnsTransaction(vm.TransactionOfInterest.TransactionID, userID))
            {
                return NotFound();
            }

            //If model state is valid, update the db and redirect to index.
            if (ModelState.IsValid)
            {
                //Convert VMTransaction to DBTransaction
                Transaction transaction = ConvertToDBTransaction(vm.TransactionOfInterest, userID);

                //Update the transaction in the DB.
                _transactionRepository.Update(transaction);
                TempData["Message"] = "Your transaction was updated successfully.";

                return RedirectToAction("Index");
            }

            //If model state is in error, reinit the select lists and call the edit view again.
            vm.Init(userID, _accountRepository, _categoryRepository, _transactionTypeRepository, _vendorRepository);
            return View(vm);
        }

        public IActionResult Delete(int? id)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm id is not null
            if (id == null)
            {
                return BadRequest();
            }

            //Get transaction if it exists.
            Transaction transaction = _transactionRepository.Get((int)id, userID);

            //Confirm transaction exists. This doubles as ensuring that the user owns the transaction, as the object will be null if the UserID and TransactionID combo don't exist.
            if (transaction == null)
            {
                return NotFound();
            }

            //Instantiate viewmodel and convert DBTransaction to VMTransaction
            TransactionViewModel vm = new TransactionViewModel
            {
                TransactionOfInterest = ConvertToVMTransaction(transaction)
            };

            vm.TransactionOfInterest.TransactionType = transaction.TransactionType.Name;
            vm.TransactionOfInterest.Account = transaction.Account.Name;
            vm.TransactionOfInterest.Category = transaction.Category.Name;
            vm.TransactionOfInterest.Vendor = transaction.Vendor.Name;


            //Return the view
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Make sure the user owns the transaction before deleting it.
            if (!_transactionRepository.UserOwnsTransaction(id, userID))
            {
                return NotFound();
            }

            _transactionRepository.Delete(id);
            TempData["Message"] = "Transaction successfully deleted.";
            return RedirectToAction("Index");
        }

        //TODO: This exact method is used in the Dashboard controller as well. Put in a central location (DRY).
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

        private TransactionViewModel.VMTransaction ConvertToVMTransaction(Transaction dbTransaction)
        {
            TransactionViewModel.VMTransaction transaction = new TransactionViewModel.VMTransaction
            {
                TransactionID = dbTransaction.TransactionID,
                TransactionDate = dbTransaction.TransactionDate,
                TransactionTypeID = dbTransaction.TransactionTypeID,
                AccountID = dbTransaction.AccountID,
                CategoryID = dbTransaction.CategoryID,
                VendorID = dbTransaction.VendorID,
                Amount = dbTransaction.Amount,
                Description = dbTransaction.Description
            };

            return transaction;
        }

        private Transaction ConvertToDBTransaction(TransactionViewModel.VMTransaction vmTransaction, string userID)
        {
            Transaction transaction = new Transaction
            {
                TransactionID = vmTransaction.TransactionID,
                UserID = userID,
                TransactionDate = vmTransaction.TransactionDate,
                TransactionTypeID = vmTransaction.TransactionTypeID,
                AccountID = vmTransaction.AccountID,
                CategoryID = vmTransaction.CategoryID,
                VendorID = vmTransaction.VendorID,
                Amount = vmTransaction.Amount,
                Description = vmTransaction.Description
            };

            return transaction;
        }
    }
}