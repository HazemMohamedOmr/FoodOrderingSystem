using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Application.Common.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);
    }
} 