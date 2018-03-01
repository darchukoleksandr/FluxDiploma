namespace Domain.Repository
{
    using System.Threading.Tasks;

    public interface IRepository<T, K>
    {
        Task<T> GetById(K id);
        Task Create(T item);
        Task Update(T item);
        Task DeleteById(K id);
    }
}
