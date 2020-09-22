using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface ITransactionRepository : IBaseRepository<Transaction>
    {
        Transaction Get(int id, string userID, bool includeRelatedEntities = true);
        int GetCount(string userID);
        IList<Transaction> GetList(string userID);
        bool UserOwnsTransaction(int id, string userID);
    }
}