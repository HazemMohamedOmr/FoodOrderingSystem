# Food Ordering System

A collaborative food ordering platform designed for offices and groups to efficiently coordinate restaurant orders.

## Overview

The Food Ordering System simplifies the process of ordering food as a group by providing a centralized platform where:

- Managers can initiate group orders from restaurants
- Users can add items to active orders
- Orders are tracked, receipts are generated, and notifications are sent
- Payment status is tracked for each participant

This system solves the common problem of coordinating food orders in office environments, reducing errors, ensuring accurate payment tracking, and providing transparent order management.

## Key Features

### User Management
- Multiple user roles: Admin, Manager, and End User
- Authentication using JWT tokens
- Role-based permissions and access control

### Restaurant Management
- Browse available restaurants
- View restaurant details and menus
- Filter restaurants by various criteria

### Menu Item Management
- View menu items for each restaurant
- Detailed item information (description, price, etc.)
- Support for item customization through notes

### Order Management
- Start new group orders (Managers/Admins)
- Add items to active orders
- Modify or remove items (while order is active)
- Close orders and generate receipts
- View order history and details
- Track payment status for each participant

### Notifications
- Real-time email notifications for order events
- Order start notifications
- Order closure notifications
- Individual receipts for participants
- Comprehensive order summary for managers
- Background processing for better performance

### Payment Tracking
- Track payment status for each participant
- Update payment status (Managers/Admins only)
- View payment history and pending payments

### Administration
- User management and role assignment
- System monitoring and maintenance
- Access to Hangfire dashboard for job monitoring

## Technical Architecture

### Clean Architecture
The system follows clean architecture principles with clear separation of concerns:

- **Domain Layer**: Core entities and business logic
- **Application Layer**: Use cases, DTOs, and interfaces
- **Infrastructure Layer**: External services implementation
- **API Layer**: Controllers and endpoints

### CQRS Pattern
The application implements the Command Query Responsibility Segregation pattern:
- Commands: For operations that modify state
- Queries: For operations that retrieve data

### Technologies Used

- **Backend**: ASP.NET Core 9.0
- **Architecture**: Clean Architecture with CQRS
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Documentation**: Swagger/OpenAPI
- **Email**: MailKit for email services
- **Background Processing**: Hangfire
- **Validation**: FluentValidation
- **Object Mapping**: AutoMapper
- **Logging**: Built-in ASP.NET Core logging

## Setup Instructions

### Prerequisites
- .NET 9.0 SDK or later
- SQL Server (Local or Express)
- Visual Studio 2022 or other IDE with .NET support

### Database Setup
1. Update the connection string in `appsettings.json`
2. Run database migrations:
```
dotnet ef database update
```

### Email Configuration
Configure email settings in `appsettings.json`:
```json
"NotificationSettings": {
  "EmailSenderAddress": "your-email@example.com",
  "EmailSmtpServer": "smtp.example.com",
  "EmailSmtpPort": 587,
  "EmailUsername": "your-username",
  "EmailPassword": "your-password"
}
```

### JWT Configuration
Configure JWT settings in `appsettings.json`:
```json
"JwtSettings": {
  "Secret": "your-secret-key-at-least-16-characters",
  "Issuer": "FoodOrderingSystem",
  "Audience": "FoodOrderingSystemClients",
  "ExpiresInMinutes": 60
}
```

### Running the Application
1. Build the solution:
```
dotnet build
```
2. Run the API:
```
dotnet run --project FoodOrderingSystem.API
```
3. Access Swagger UI at: `https://localhost:5001` or `http://localhost:5000`
4. Access Hangfire Dashboard at: `https://localhost:5001/hangfire` or `http://localhost:5000/hangfire`

## API Endpoints

### Authentication
- `POST /api/auth/register`: Register a new user
- `POST /api/auth/login`: Login and receive JWT token

### Restaurants
- `GET /api/restaurants`: Get all restaurants
- `GET /api/restaurants/{id}`: Get restaurant by ID
- `POST /api/restaurants`: Create a new restaurant (Admin/Manager)
- `PUT /api/restaurants/{id}`: Update restaurant (Admin/Manager)

### Menu Items
- `GET /api/menuitems/{restaurantId}`: Get menu items by restaurant
- `GET /api/menuitems/{id}`: Get menu item by ID
- `POST /api/menuitems`: Create menu item (Admin/Manager)
- `PUT /api/menuitems/{id}`: Update menu item (Admin/Manager)

### Orders
- `POST /api/orders/start`: Start a new order (Admin/Manager)
- `GET /api/orders/active`: Get active orders
- `GET /api/orders/{id}`: Get order by ID
- `GET /api/orders/{id}/my-items`: Get current user's items in order
- `POST /api/orders/items`: Add item to order
- `PUT /api/orders/items/{id}`: Update order item
- `DELETE /api/orders/items/{id}`: Delete order item
- `POST /api/orders/{id}/close`: Close an order (Admin/Manager)
- `PUT /api/orders/{orderId}/users/{userId}/payment-status`: Update payment status (Admin/Manager)
- `GET /api/orders/history`: Get order history
- `GET /api/orders/my-history`: Get current user's order history
- `GET /api/orders/{id}/receipt`: Get receipt for order

### Admin
- `GET /api/admin/users`: Get all users (Admin)
- `PUT /api/admin/users/{userId}/role`: Update user role (Admin)

## Payment Flow

1. Manager starts an order from a restaurant
2. Users add items to the order
3. Manager closes the order when ready
4. System generates receipts for all participants
5. System generates a comprehensive summary for the manager
6. Manager collects payments from participants
7. Manager updates payment status for each participant

## Development 

### Project Structure
```
FoodOrderingSystem/
  ├── FoodOrderingSystem.API/           # REST API layer
  ├── FoodOrderingSystem.Application/   # Application use cases layer
  │   ├── Common/                       # Shared models, interfaces
  │   └── Features/                     # CQRS features by domain
  ├── FoodOrderingSystem.Domain/        # Domain models and logic
  ├── FoodOrderingSystem.Infrastructure/# External services implementation
  └── FoodOrderingSystem.Persistence/   # Data access layer
```

## Future Improvements

- Mobile application for easier ordering
- Real-time notifications using SignalR
- Integration with payment gateways
- Support for recurring orders
- Order templates and favorites
- Advanced reporting and analytics
- Localization support
- Dietary preferences and allergen tracking

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## Acknowledgements

Developed as a solution for streamlining group food orders in office environments. 