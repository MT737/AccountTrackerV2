using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface IAccountRepository : IBaseRepository<Account>
    {
        Account Get(int id, string userID, bool includeRelatedEntities = false);
        decimal GetBalance(int accountId, string userID, bool isAsset);
        int GetCount(string userID);
        int GetID(string name, string userID);
        IList<Account> GetList(string userID);
        bool NameExists(AccountViewModel vm, string userID);
        bool UserOwnsAccount(int id, string userID);
    }
}