using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface ITransactionTypeRepository : IBaseRepository<TransactionType>
    {
        TransactionType GetTransactionType(string name);
        IList<TransactionType> GetList();
    }
}