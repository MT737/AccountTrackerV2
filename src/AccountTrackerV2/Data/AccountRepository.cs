using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountTrackerV2.Data
{
    public class AccountRepository : BaseRepository<Account>, IAccountRepository
    {
        public AccountRepository(AccountTrackerV2Context context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves a record of a single account. 
        /// </summary>
        /// <param name="id">Int: the AccountID of the account to be retrieved.</param>
        /// <param name="userID">String: the UserID of which account to be retrieved.</param>
        /// <param name="includeRelatedEntities">Bool: determination to pull associated entities. Not currently relevant to Accounts and defaults to false.</param>
        /// <returns></returns>
        public Account Get(int id, string userID, bool includeRelatedEntities = false)
        {
            var account = Context.Accounts.AsQueryable();

            if (includeRelatedEntities)
            {
                throw new NotImplementedException();
            }

            return account
                .Where(a => a.AccountID == id && a.UserID == userID)
                .SingleOrDefault();
        }

        /// <summary>
        /// Retrieves a list of accounts that exist in the database.
        /// </summary>
        /// <param name="userID">String: UserID of which to pull accounts.</param>
        /// <returns>Returns and IList of Account entities.</returns>
        public IList<Account> GetList(string userID)
        {
            return Context.Accounts
                .Where(a => a.UserID == userID)
                .OrderBy(a => a.Name)
                .ToList();
        }

        /// <summary>
        /// Retrieves the ID of the account based on the name.
        /// </summary>
        /// <param name="name">String: name of the account of which the ID is desired.</param>
        /// <param name="userID">String: UserID of the account.</param>
        /// <returns></returns>
        public int GetID(string name, string userID)
        {
            return Context.Accounts
                .Where(a => a.Name == name && a.UserID == userID)
                .SingleOrDefault().AccountID;
        }

        /// <summary>
        /// Gets a count of accounts in the database.
        /// </summary>
        /// <param name="userID">String: UserID of which to get a count of accounts.</param>
        /// <returns>Returns an integer representing the count of accounts in the database.</returns>
        public int GetCount(string userID)
        {
            return Context.Accounts.Where(a => a.UserID == userID).Count();
        }

        /// <summary>
        /// Calculates an account balance based on transactions in the database.
        /// </summary>
        /// <param name="accountId">Int: AccountID of the account balance to be calculated.</param>
        /// <param name="userID">String: UserID associated to the account. Only included here for security reasons.</param>
        /// <param name="isAsset">Bool: IsAsset classification of the account balance to be calculated.</param>
        /// <returns></returns>
        public decimal GetBalance(int accountId, string userID, bool isAsset)
        {
            //TODO: I really want to simplify this to a single query (there's already enough communications with the DB happening as is).
            decimal balance;

            //TODO: Further test account balances
            var paymentTo = Context.Transactions
                 .Where(t => t.AccountID == accountId && t.UserID == userID && t.TransactionType.Name == "Payment To")
                 .ToList()
                 .Sum(t => t.Amount);

            var paymentFrom = Context.Transactions
                .Where(t => t.AccountID == accountId && t.UserID == userID && t.TransactionType.Name == "Payment From")
                .ToList()
                .Sum(t => t.Amount);

            ////Asset balance = payments to less payments from.
            if (isAsset)
            {
                return balance = paymentTo - paymentFrom;
            }
            //Liability balance = payments from - payments to.
            else
            {
                return paymentFrom - paymentTo;
            }
        }

        /// <summary>
        /// Indicates the existence of the passed account name.
        /// </summary>
        /// <param name="account">Account: account object of which the name existence is desired.</param>
        /// <param name="userID">String: UserID of the account.</param>
        /// <returns></returns>
        public bool NameExists(AccountViewModel vm, string userID)
        {
            return Context.Accounts
                 .Where(a => a.UserID == userID && a.Name.ToLower() == vm.AccountOfInterest.Name.ToLower() && a.AccountID != vm.AccountOfInterest.AccountID)
                 .Any();
        }

        /// <summary>
        /// Indicates if the user owns the specified account.
        /// </summary>
        /// <param name="id">Int: ID of the specified account.</param>
        /// <param name="userID">String: User's ID.</param>
        /// <returns></returns>
        public bool UserOwnsAccount(int id, string userID)
        {
            return Context.Accounts.Where(a => a.AccountID == id && a.UserID == userID).Any();
        }
    }
}
