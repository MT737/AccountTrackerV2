using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface ICategoryRepository : IBaseRepository<Category>
    {
        void Absorption(int absorbedID, int absorbingID, string userID);
        Category Get(int id, string userID);
        int GetCount(string userID);
        bool UserOwnsCategory(int categoryId, string userID);
        int GetID(string name, string userID);
        IList<Category> GetList(string userID);
        bool NameExists(Category category, string userID);
        void CreateDefaults(string userID);
        bool DefaultsExist(string userID);
        decimal GetCategorySpending(int categoryID, string userID);
    }
}