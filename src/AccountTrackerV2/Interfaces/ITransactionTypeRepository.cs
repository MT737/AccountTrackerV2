using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface ITransactionTypeRepository : IBaseRepository<TransactionType>
    {
        int GetID(string name);
        IList<TransactionType> GetList();
    }
}