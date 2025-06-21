using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdQuery : IRequest<Result<MenuItemDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetMenuItemByIdQueryHandler : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMenuItemByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<MenuItemDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(request.Id, cancellationToken);

            if (menuItem == null)
            {
                return Result<MenuItemDto>.Failure($"Menu item with ID {request.Id} not found.");
            }

            var menuItemDto = _mapper.Map<MenuItemDto>(menuItem);
            return Result<MenuItemDto>.Success(menuItemDto);
        }
    }
} 