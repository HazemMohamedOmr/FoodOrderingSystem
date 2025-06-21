using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQuery : IRequest<Result<List<UserListItemDto>>>
    {
        // Empty query - no filters needed
    }

    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string RoleName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<List<UserListItemDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<UserListItemDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var result = new List<UserListItemDto>();

            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserListItemDto>(user);
                
                // Add the role name as a string for easier display
                userDto.RoleName = user.Role.ToString();
                
                result.Add(userDto);
            }

            return Result<List<UserListItemDto>>.Success(result);
        }
    }
} 