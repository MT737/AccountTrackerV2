using AccountTrackerV2.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountTrackerV2.Interfaces;

namespace AccountTrackerV2.Data
{
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        //Properties
        protected AccountTrackerV2Context Context { get; private set; }

        //Base constructor
        public BaseRepository(AccountTrackerV2Context context)
        {
            Context = context;
        }

        //CUD of CRUD
        /// <summary>
        /// Add entity to the DbSet and then save the changes to the database.
        /// </summary>
        /// <param name="entity">Generic type TEntity: will be determined by the sub-repository.</param>
        public void Add(TEntity entity)
        {
            Context.Set<TEntity>().Add(entity);
            Context.SaveChanges();
        }

        //TODO: According to my reading, EF core, unlike prior EF, doesn't need 
        /// <summary>
        /// Set entity's state to modified and save the changes to the database.
        /// </summary>
        /// <param name="entity">Generic type TEntity: will be determined by the sub-repository.</param>
        public void Update(TEntity entity)
        {
            Context.Entry(entity).State = EntityState.Modified;
            Context.SaveChanges();
        }

        /// <summary>
        /// Find the specified entity in the DbSet, remove it, and save the changes to the database.
        /// </summary>
        /// <param name="id">Int: Primary key of the entity to be removed.</param>
        public void Delete(int id)
        {
            var set = Context.Set<TEntity>();
            var entity = set.Find(id);
            set.Remove(entity);
            Context.SaveChanges();
        }
    }
}
