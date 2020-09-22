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

        public IActionResult Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Instantiate viewmodel
            //TODO: Refactor for DI?
            var vm = new ApplicationViewModel();

            //Complete viewmodel property required for transaction view
            vm.Transactions = GetTransactionsWithDetails(userID);

            return View(vm);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Instantiate viewmodel
            var vm = new ApplicationViewModel();

            //Instantiate Transaction of Interest property
            vm.TransactionOfInterest = new Transaction();

            //Preset default values
            vm.TransactionOfInterest.TransactionDate = DateTime.Now.Date;
            vm.TransactionOfInterest.Amount = 0.00M;
            vm.TransactionOfInterest.UserID = userID;

            //TODO: Consider limiting the select list items. Probably shouldn't allow users to select new account balance.
            //Initialize set list items.
            vm.Init(vm.TransactionOfInterest.UserID, _transactionTypeRepository, _accountRepository, _categoryRepository, _vendorRepository);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(ApplicationViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Don't trust the passed userID. 
            vm.TransactionOfInterest.UserID = userID;

            //TODO: Confirm validation requirements.
            if (ModelState.IsValid)
            {
                _transactionRepository.Add(vm.TransactionOfInterest);

                TempData["Message"] = "Transaction successfully added.";

                return RedirectToAction("Index");
            }

            vm.Init(vm.TransactionOfInterest.UserID, _transactionTypeRepository, _accountRepository, _categoryRepository, _vendorRepository);
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
            Transaction transaction = _transactionRepository.Get((int)id, userID, true);

            //Confirm transaction exists. This doubles as ensuring that the user owns the transaction, as the object will be null if the UserID and TransactionID combo don't exist.            
            if (transaction == null)
            {
                return NotFound();
            }

            //Instantiate viewmodel and set transactionofinterest property
            var vm = new ApplicationViewModel { TransactionOfInterest = transaction };

            //Initialize select list items
            vm.Init(vm.TransactionOfInterest.UserID, _transactionTypeRepository, _accountRepository, _categoryRepository, _vendorRepository);

            //TODO: Refactor to remove the need to pass userID to the view.
            vm.TransactionOfInterest.UserID = userID;

            //Return the view
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: Currently allowing edit even when nothing is changed. Fix that.
            //TODO: Confirm additional validation requirements.

            //Don't trust client passed userID
            vm.TransactionOfInterest.UserID = userID;

            //Confirm user owns the transaction
            if (!_transactionRepository.UserOwnsTransaction(vm.TransactionOfInterest.TransactionID, vm.TransactionOfInterest.UserID))
            {
                return NotFound();
            }
            
            //If model state is valid, update the db and redirect to index.
            if (ModelState.IsValid)
            {
                _transactionRepository.Update(vm.TransactionOfInterest);
                TempData["Message"] = "Your transaction was updated successfully.";

                return RedirectToAction("Index");
            }

            //If model state is in error, reinit the select lists and call the edit view again.
            vm.Init(vm.TransactionOfInterest.UserID, _transactionTypeRepository, _accountRepository, _categoryRepository, _vendorRepository);
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
            Transaction transaction = _transactionRepository.Get((int)id, userID, true);

            //Confirm transaction exists. This doubles as ensuring that the user owns the transaction, as the object will be null if the UserID and TransactionID combo don't exist.
            if (transaction == null)
            {
                return NotFound();
            }

            //Instantiate viewmodel and set transactionofinterest property
            var vm = new ApplicationViewModel { TransactionOfInterest = transaction };  

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
                transactions.Add(_transactionRepository.Get(transaction.TransactionID, userID, true));
            }

            return transactions;
        }
    }
}