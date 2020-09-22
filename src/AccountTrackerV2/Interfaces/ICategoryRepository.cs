using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface ICategoryRepository : IBaseRepository<Category>
    {
        void Absorption(int absorbedID, int absorbingID, string userID);
        Category Get(int id);
        decimal GetCategorySpending(int categoryID, string userID);
        int GetCount(string userID);
        bool UserOwnsCategory(int categoryId, string userID);
        int GetID(string name);
        IList<Category> GetList(string userID);
        bool NameExists(Category category, string userID);
    }
}