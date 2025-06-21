using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Domain.Entities;

namespace FoodOrderingSystem.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<User> Users { get; }
        IRepository<Restaurant> Restaurants { get; }
        IRepository<MenuItem> MenuItems { get; }
        IRepository<Order> Orders { get; }
        IRepository<OrderItem> OrderItems { get; }
        IRepository<Payment> Payments { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
} 