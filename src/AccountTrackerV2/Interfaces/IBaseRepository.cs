namespace AccountTrackerV2.Interfaces
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        void Add(TEntity entity);
        void Delete(int id);
        void Update(TEntity entity);
    }
}