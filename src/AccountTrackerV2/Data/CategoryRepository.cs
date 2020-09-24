using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace AccountTrackerV2.Data
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AccountTrackerV2Context context) : base(context)
        {
        }

        //TODO: Update all XML.

        /// <summary>
        /// Returns the category for the specified ID.
        /// </summary>
        /// <param name="id">Int: ID of the category to return.</param>
        /// <returns>A Category entity</returns>
        public Category Get(int id, string userID)
        {
            var category = Context.Categories.AsQueryable();

            return category
                .Where(c => c.CategoryID == id && c.UserID == userID)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns an IList of categories.
        /// </summary>
        /// <returns>An IList of category entities.</returns>
        public IList<Category> GetList(string userID)
        {
            return Context.Categories
                .Where(c => c.UserID == userID)
                .OrderBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Returns a count of category elements.
        /// </summary>
        /// <returns>Int: count of category elements.</returns>
        public int GetCount(string userID)
        {
            return Context.Categories.Count();
        }

        /// <summary>
        /// Indicates the existence of the category.
        /// </summary>
        /// <param name="category">Category entity</param>
        /// <param name="userID">String userID of the user for which to compare category names.</param>
        /// <returns>Bool: True if the category name already exists in the database. False if not.</returns>
        public bool NameExists(EntityViewModel vm, string userID)
        {
            return Context.Categories
                .Where(c => c.UserID == userID && c.Name.ToLower() == vm.EntityOfInterest.Name.ToLower() && c.CategoryID != vm.EntityOfInterest.EntityID)
                .Any();
        }

        /// <summary>
        /// Returns total spending in the specificed category for the specified user.
        /// </summary>
        /// <param name="categoryID">Int: categoryID for which to get total spending.</param>
        /// <param name="userID">String: userID for which to get category total spending.</param>
        /// <returns>Decimal: total user specific spending for the category.</returns>
        public decimal GetCategorySpending(int categoryID, string userID)
        {
            return Context.Transactions
                .Where(t => t.CategoryID == categoryID && t.UserID == userID)
                .ToList().Sum(t => t.Amount);
        }

        /// <summary>
        /// Indicates if the user owns the specified category.
        /// </summary>
        /// <param name="categoryId">Int: ID of the specified category.</param>
        /// <param name="userID">String: User's ID.</param>
        /// <returns></returns>
        public bool UserOwnsCategory(int categoryId, string userID)
        {
            return Context.Categories.Where(c => c.CategoryID == categoryId && c.UserID == userID).Any();
        }

        /// <summary>
        /// Returns the ID for the category specified by name.
        /// </summary>
        /// <param name="name">String: Name of the category for which to determine the ID.</param>
        /// <returns>Int: Id for the specified category.</returns>
        public int GetID(string name, string userID)
        {
            return Context.Categories
                .Where(c => c.Name == name && c.UserID == userID)
                .SingleOrDefault().CategoryID;
        }

        /// <summary>
        /// Converts the absorbed categoryID field in all transactions to the absorbing categoryID.
        /// </summary>
        /// <param name="absorbedID">Int: category ID that is being absorbed.</param>
        /// <param name="absorbingID">Int: category ID that is absorbing.</param>
        /// <param name="userID">String: UserID of the owner of the categories being adjusted.</param>
        public void Absorption(int absorbedID, int absorbingID, string userID)
        {
            //TODO: this works for a small database, but for large scale, this method should be updated to perform a bulk update.
            IQueryable<Transaction> catsToUpdate = Context.Transactions
                .Where(c => c.CategoryID == absorbedID && c.UserID == userID);

            foreach (Transaction transaction in catsToUpdate)
            {
                transaction.CategoryID = absorbingID;
            }
            Context.SaveChanges();
        }

        public void CreateDefaults(string userID)
        {
            if (!DefaultsExist(userID)) //Preventing duplication of defaults.
            {
                string[] categories = new string[] {"Account Transfer", "Account Correction", "New Account", "ATM Withdrawal", "Eating Out",
                "Entertainment", "Gas", "Groceries/Sundries", "Income", "Shopping", "Returns/Deposits", "Other"};

                foreach (string category in categories)
                {               
                    Category cat = new Category
                    {
                        UserID = userID,
                        Name = category,
                        IsDefault = true,
                    };
                    
                    if (category == "Account Transfer" || category == "Account Correction" || category == "New Account" || category == "Income")
                    {
                        cat.IsDisplayed = false;                        
                    }
                    else
                    {
                        cat.IsDisplayed = true;
                    }

                    Add(cat);
                } 
            }
        }

        public bool DefaultsExist(string userID)
        {
            return Context.Categories.Where(c => c.UserID == userID && c.IsDefault == true).Any();
        }

        public bool IsDefault(int entityID, string userID)
        {
            return Context.Categories
                .Where(c => c.CategoryID == entityID && c.UserID == userID && c.IsDefault == true)
                .Any();
        }
    }
}
