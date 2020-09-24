using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace AccountTrackerV2.Data
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AccountTrackerV2Context context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves the transaction associated to the passed transactionID if it belongs to the passed UserID.
        /// </summary>
        /// <param name="id">Int: TransactionID</param>
        /// <param name="userID">String: UserID</param>
        /// <param name="includeRelatedEntities">Bool: boolean indicator of desire to include additional transaction details.</param>
        /// <returns>Returns a transaction entity and includes related entities if includeRelatedEntities = true.</returns>
        public Transaction Get(int id, string userID)
        {
            var transaction = Context.Transactions.AsQueryable();
            
            return transaction
                .Include(t => t.TransactionType)
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Include(t => t.Vendor)
                .Where(t => t.TransactionID == id && t.UserID == userID)
                .SingleOrDefault();
        }

        /// <summary>
        /// Retrieves a list of transactions belonging to the passed userID.
        /// </summary>
        /// <param name="userID">String: UserID for which to pull a list of transactions.</param>
        /// <returns>Returns IList of Transaction entities.</returns>
        public IList<Transaction> GetList(string userID)
        {
            return Context.Transactions
                .Where(t => t.UserID == userID)
                .OrderByDescending(t => t.TransactionID)
                .ToList();
        }

        /// <summary>
        /// Returns an integer representing the count of transactions belonging to the passed user.
        /// </summary>
        /// <param name="userID">String: UserID of which to get a count of transactions.</param>
        /// <returns>Integer: count of transactions belonging to the user.</returns>
        public int GetCount(string userID)
        {
            return Context.Transactions.Where(t => t.UserID == userID).Count();
        }

        /// <summary>
        /// Idicates the user's ownership status of a given transaction.
        /// </summary>
        /// <param name="id">Int: TransactionID of the transaction to be inspected.</param>
        /// <param name="userID">String: UserID of the user to be compared to the transaction.</param>
        /// <returns>Bool: true if the user does own the transaction and false if not.</returns>
        public bool UserOwnsTransaction(int id, string userID)
        {
            return Context.Transactions.Where(t => t.TransactionID == id && t.UserID == userID).Any();
        }
    }
}
