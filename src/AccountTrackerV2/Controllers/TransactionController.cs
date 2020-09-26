using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using X.PagedList;

namespace AccountTrackerV2.Controllers
{
    public class TransactionController : Controller
    {        
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

        public IActionResult Index(string sortOrder, DateTime currentFilter, DateTime searchDate, int? page)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParam = String.IsNullOrEmpty(sortOrder) ? "Date" : "";
            ViewBag.AccountSortParam = sortOrder == "Account" ? "acct_desc" : "Account";
            ViewBag.CategorySortParam = sortOrder == "Category" ? "cat_desc" : "Category";
            ViewBag.VendorSortParam = sortOrder == "Vendor" ? "vend_desc" : "Vendor";
            ViewBag.AmountSortParam = sortOrder == "Amount" ? "amt_desc" : "Amount";

            //TODO: tell user to create an account if none exist.

            //Instantiate viewmodel with list of transactions.
            TransactionViewModel vm = new TransactionViewModel
            {
                Transactions = GetTransactionsWithDetails(userID)
            };

            //If user has searched for a new date, then start pagenation over
            if (searchDate != default)
            {
                page = 1;
            }
            //Otherwise set the searchDate value to the current filter to pass along and maintain the previously set filtering option.
            else
            {
                searchDate = currentFilter;
            }

            ViewBag.CurrentFilter = searchDate;

            //If search date entered, filter on the search date.
            if (searchDate != default)
            {
                vm.Transactions = vm.Transactions.Where(t => t.TransactionDate == searchDate).ToList();
            }

            //Sort as instructed
            if (!(vm.Transactions.Count == 0))
            {
                switch (sortOrder)
                {
                    case "Account":
                        vm.Transactions = vm.Transactions.OrderBy(t => t.Account.Name).ToList();
                        break;

                    case "acct_desc":
                        vm.Transactions = vm.Transactions.OrderByDescending(t => t.Account.Name).ToList();
                        break;

                    case "Amount":
                        vm.Transactions = vm.Transactions.OrderBy(t => t.Amount).ToList();
                        break;

                    case "amt_desc":
                        vm.Transactions = vm.Transactions.OrderByDescending(t => t.Amount).ToList();
                        break;

                    case "Category":
                        vm.Transactions = vm.Transactions.OrderBy(t => t.Category.Name).ToList();
                        break;

                    case "cat_desc":
                        vm.Transactions = vm.Transactions.OrderByDescending(t => t.Category.Name).ToList();
                        break;

                    case "Date":
                        vm.Transactions = vm.Transactions.OrderBy(t => t.TransactionDate).ToList();
                        break;

                    case "Vendor":
                        vm.Transactions = vm.Transactions.OrderBy(t => t.Vendor.Name).ToList();
                        break;

                    case "vend_desc":
                        vm.Transactions = vm.Transactions.OrderByDescending(t => t.Vendor.Name).ToList();
                        break;

                    default:
                        vm.Transactions = vm.Transactions.OrderByDescending(t => t.TransactionDate).ToList();
                        break;
                }
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            ViewBag.SinglePageTransaction = vm.Transactions.ToPagedList(pageNumber, pageSize);
            return View();
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