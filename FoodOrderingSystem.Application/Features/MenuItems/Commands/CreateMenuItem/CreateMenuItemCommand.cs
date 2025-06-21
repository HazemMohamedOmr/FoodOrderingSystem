using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using MediatR;

namespace FoodOrderingSystem.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public Guid RestaurantId { get; set; }
    }

    public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public CreateMenuItemCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.RestaurantId, cancellationToken);

            if (restaurant == null)
            {
                return Result<Guid>.Failure($"Restaurant with ID {request.RestaurantId} not found.");
            }

            var userName = _currentUserService.UserName ?? "System";

            var menuItem = new MenuItem
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                RestaurantId = request.RestaurantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName,
                LastModifiedBy = userName,
                LastModifiedAt = DateTime.UtcNow
            };

            await _unitOfWork.MenuItems.AddAsync(menuItem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(menuItem.Id);
        }
    }
} 