# Food Ordering System API Documentation

This document provides detailed information about the Food Ordering System API endpoints. Each endpoint includes description, request parameters, response schema, and examples.

## Table of Contents

- [Authentication](#authentication)
- [Users](#users)
- [Restaurants](#restaurants)
- [Menu Items](#menu-items)
- [Orders](#orders)
- [Admin](#admin)

## Base URL

All API endpoints are relative to the base URL:

```
https://localhost:713/api
```

## Authentication

All endpoints except for authentication endpoints require a valid JWT token in the Authorization header.

```
Authorization: Bearer {your_jwt_token}
```

### Register User

Register a new user in the system.

- **URL**: `/auth/register`
- **Method**: `POST`
- **Auth Required**: No

**Request Body:**
```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "phoneNumber": "string"
}
```

**Constraints:**
- `name`: Required, max length 100
- `email`: Required, valid email format
- `password`: Required, min length 6
- `phoneNumber`: Optional

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "uuid",
    "name": "string",
    "email": "string",
    "phoneNumber": "string",
    "role": "string",
    "token": "string"
  }
}
```

**Status Codes:**
- `201 Created`: Registration successful
- `400 Bad Request`: Invalid input

### Login User

Authenticate a user and receive a JWT token.

- **URL**: `/auth/login`
- **Method**: `POST`
- **Auth Required**: No

**Request Body:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "uuid",
    "name": "string",
    "email": "string",
    "phoneNumber": "string",
    "role": "string",
    "token": "string"
  }
}
```

**Status Codes:**
- `200 OK`: Login successful
- `401 Unauthorized`: Invalid credentials

## Restaurants

### Get All Restaurants

Retrieve a list of all restaurants.

- **URL**: `/restaurants`
- **Method**: `GET`
- **Auth Required**: Yes

**Query Parameters:**
- `name`: Filter by name (optional)
- `sortBy`: Field to sort by (optional, default: "Name")
- `sortOrder`: "asc" or "desc" (optional, default: "asc")

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "name": "string",
      "description": "string",
      "address": "string",
      "phoneNumber": "string",
      "imageUrl": "string",
      "deliveryFee": "decimal"
    }
  ]
}
```

### Get Restaurant by ID

Retrieve a restaurant by its unique identifier.

- **URL**: `/restaurants/{id}`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the restaurant

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "uuid",
    "name": "string",
    "description": "string",
    "address": "string",
    "phoneNumber": "string",
    "imageUrl": "string",
    "deliveryFee": "decimal"
  }
}
```

**Status Codes:**
- `200 OK`: Restaurant found
- `404 Not Found`: Restaurant not found

### Create Restaurant

Create a new restaurant.

- **URL**: `/restaurants`
- **Method**: `POST`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "address": "string",
  "phoneNumber": "string",
  "imageUrl": "string",
  "deliveryFee": "decimal"
}
```

**Constraints:**
- `name`: Required, max length 100
- `description`: Required, max length 500
- `address`: Required, max length 200
- `phoneNumber`: Required
- `deliveryFee`: Required, must be positive

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": "uuid"
}
```

**Status Codes:**
- `201 Created`: Restaurant created
- `400 Bad Request`: Invalid input
- `403 Forbidden`: User does not have required role

### Update Restaurant

Update an existing restaurant.

- **URL**: `/restaurants/{id}`
- **Method**: `PUT`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Path Parameters:**
- `id`: UUID of the restaurant

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "address": "string",
  "phoneNumber": "string",
  "imageUrl": "string",
  "deliveryFee": "decimal"
}
```

**Response:**
```json
{
  "succeeded": true,
  "errors": []
}
```

**Status Codes:**
- `200 OK`: Restaurant updated
- `400 Bad Request`: Invalid input
- `403 Forbidden`: User does not have required role
- `404 Not Found`: Restaurant not found

## Menu Items

### Get Menu Items by Restaurant

Retrieve all menu items for a specific restaurant.

- **URL**: `/menuitems/restaurant/{restaurantId}`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `restaurantId`: UUID of the restaurant

**Query Parameters:**
- `sortBy`: Field to sort by (optional, default: "Name")
- `sortOrder`: "asc" or "desc" (optional, default: "asc")

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "restaurantId": "uuid",
      "name": "string",
      "description": "string",
      "price": "decimal",
      "imageUrl": "string",
      "category": "string"
    }
  ]
}
```

### Get Menu Item by ID

Retrieve a menu item by its unique identifier.

- **URL**: `/menuitems/{id}`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the menu item

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "uuid",
    "restaurantId": "uuid",
    "name": "string",
    "description": "string",
    "price": "decimal",
    "imageUrl": "string",
    "category": "string"
  }
}
```

**Status Codes:**
- `200 OK`: Menu item found
- `404 Not Found`: Menu item not found

### Create Menu Item

Create a new menu item for a restaurant.

- **URL**: `/menuitems`
- **Method**: `POST`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Request Body:**
```json
{
  "restaurantId": "uuid",
  "name": "string",
  "description": "string",
  "price": "decimal",
  "imageUrl": "string",
  "category": "string"
}
```

**Constraints:**
- `restaurantId`: Required, must be a valid restaurant ID
- `name`: Required, max length 100
- `description`: Required, max length 500
- `price`: Required, must be positive
- `category`: Optional

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": "uuid"
}
```

**Status Codes:**
- `201 Created`: Menu item created
- `400 Bad Request`: Invalid input
- `403 Forbidden`: User does not have required role

### Update Menu Item

Update an existing menu item.

- **URL**: `/menuitems/{id}`
- **Method**: `PUT`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Path Parameters:**
- `id`: UUID of the menu item

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "price": "decimal",
  "imageUrl": "string",
  "category": "string"
}
```

**Response:**
```json
{
  "succeeded": true,
  "errors": []
}
```

**Status Codes:**
- `200 OK`: Menu item updated
- `400 Bad Request`: Invalid input
- `403 Forbidden`: User does not have required role
- `404 Not Found`: Menu item not found

## Orders

### Start Order

Start a new group order from a restaurant.

- **URL**: `/orders/start`
- **Method**: `POST`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Request Body:**
```json
{
  "restaurantId": "uuid"
}
```

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": "uuid"
}
```

**Status Codes:**
- `201 Created`: Order started
- `400 Bad Request`: Invalid input
- `403 Forbidden`: User does not have required role

### Get Active Orders

Retrieve all active (open) orders.

- **URL**: `/orders/active`
- **Method**: `GET`
- **Auth Required**: Yes

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "restaurantId": "uuid",
      "restaurantName": "string",
      "managerId": "uuid",
      "managerName": "string",
      "status": "Open",
      "createdAt": "datetime",
      "closedAt": null,
      "orderDate": "datetime",
      "deliveryFee": "decimal",
      "orderItems": [],
      "totalAmount": "decimal"
    }
  ]
}
```

### Get Order by ID

Retrieve an order by its unique identifier.

- **URL**: `/orders/{id}`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "uuid",
    "restaurantId": "uuid",
    "restaurantName": "string",
    "managerId": "uuid",
    "managerName": "string",
    "status": "string",
    "createdAt": "datetime",
    "closedAt": "datetime",
    "orderDate": "datetime",
    "orderItems": [
      {
        "id": "uuid",
        "userId": "uuid",
        "userName": "string",
        "menuItemId": "uuid",
        "menuItemName": "string",
        "quantity": "integer",
        "note": "string",
        "price": "decimal"
      }
    ],
    "deliveryFee": "decimal",
    "totalAmount": "decimal"
  }
}
```

**Status Codes:**
- `200 OK`: Order found
- `404 Not Found`: Order not found

### Get Order Items

Retrieve all items for a specific order.

- **URL**: `/orders/{id}/items`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "orderId": "uuid",
    "restaurantName": "string",
    "deliveryFee": "decimal",
    "orderStatus": "string",
    "items": [
      {
        "id": "uuid",
        "menuItemId": "uuid",
        "menuItemName": "string",
        "menuItemDescription": "string",
        "price": "decimal",
        "quantity": "integer",
        "note": "string",
        "itemTotal": "decimal",
        "userId": "uuid",
        "userName": "string"
      }
    ]
  }
}
```

**Status Codes:**
- `200 OK`: Items retrieved
- `404 Not Found`: Order not found

### Get User's Order Items

Get the current user's items in a specific order.

- **URL**: `/orders/{id}/my-items`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "orderId": "uuid",
    "restaurantName": "string",
    "orderStatus": "string",
    "items": [
      {
        "id": "uuid",
        "menuItemId": "uuid",
        "menuItemName": "string",
        "menuItemDescription": "string",
        "price": "decimal",
        "quantity": "integer",
        "note": "string",
        "itemTotal": "decimal",
        "canBeDeleted": "boolean"
      }
    ],
    "subtotal": "decimal",
    "deliveryFeeShare": "decimal",
    "total": "decimal"
  }
}
```

**Status Codes:**
- `200 OK`: Items retrieved
- `404 Not Found`: Order not found

### Add Order Item

Add an item to an active order.

- **URL**: `/orders/items`
- **Method**: `POST`
- **Auth Required**: Yes

**Request Body:**
```json
{
  "orderId": "uuid",
  "menuItemId": "uuid",
  "quantity": "integer",
  "note": "string"
}
```

**Constraints:**
- `orderId`: Required, must be a valid order ID
- `menuItemId`: Required, must be a valid menu item ID
- `quantity`: Required, must be positive
- `note`: Optional

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": "uuid"
}
```

**Status Codes:**
- `200 OK`: Item added
- `400 Bad Request`: Invalid input or order closed
- `404 Not Found`: Order or menu item not found

### Update Order Item

Update an existing order item.

- **URL**: `/orders/items/{id}`
- **Method**: `PUT`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order item

**Request Body:**
```json
{
  "orderItemId": "uuid",
  "quantity": "integer",
  "note": "string"
}
```

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": "uuid"
}
```

**Status Codes:**
- `200 OK`: Item updated
- `400 Bad Request`: Invalid input or order closed
- `403 Forbidden`: Not the owner of the item
- `404 Not Found`: Order item not found

### Delete Order Item

Delete an item from an active order.

- **URL**: `/orders/items/{id}`
- **Method**: `DELETE`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order item

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "message": "Order item deleted successfully"
}
```

**Status Codes:**
- `200 OK`: Item deleted
- `400 Bad Request`: Order closed
- `403 Forbidden`: Not the owner of the item
- `404 Not Found`: Order item not found

### Close Order

Close an active order.

- **URL**: `/orders/{id}/close`
- **Method**: `POST`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Path Parameters:**
- `id`: UUID of the order

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "message": "Order closed successfully"
}
```

**Status Codes:**
- `200 OK`: Order closed
- `400 Bad Request`: Order already closed
- `403 Forbidden`: User does not have required role
- `404 Not Found`: Order not found

### Update Payment Status

Update the payment status for a user in an order.

- **URL**: `/orders/{orderId}/users/{userId}/payment-status`
- **Method**: `PUT`
- **Auth Required**: Yes
- **Authorization**: Admin or Manager role

**Path Parameters:**
- `orderId`: UUID of the order
- `userId`: UUID of the user

**Request Body:**
```json
{
  "status": "integer"
}
```

**Notes:**
- `status` values: 1 (Pending), 2 (Paid), 3 (Failed)

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "message": "Payment status updated successfully"
}
```

**Status Codes:**
- `200 OK`: Status updated
- `400 Bad Request`: Invalid status
- `403 Forbidden`: User does not have required role
- `404 Not Found`: Order or user not found

### Get Order History

Retrieve order history with optional filtering.

- **URL**: `/orders/history`
- **Method**: `GET`
- **Auth Required**: Yes

**Query Parameters:**
- `userId`: Filter by user ID (optional)
- `restaurantId`: Filter by restaurant ID (optional)
- `includeOtherParticipants`: Include other participants' items (optional, default: false)
- `showAllOrders`: Show all orders regardless of user (optional, default: false)

**Notes:**
- If no userId or restaurantId is provided, all orders will be returned by default (equivalent to showAllOrders=true)

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "restaurantName": "string",
      "orderDate": "datetime",
      "closedAt": "datetime",
      "status": "string",
      "managerName": "string",
      "deliveryFee": "decimal",
      "deliveryFeeShare": "decimal",
      "userItems": [
        {
          "id": "uuid",
          "menuItemName": "string",
          "price": "decimal",
          "quantity": "integer",
          "note": "string",
          "total": "decimal",
          "userId": "uuid",
          "userName": "string"
        }
      ],
      "userPaymentStatus": "string",
      "userTotal": "decimal"
    }
  ]
}
```

### Get User's Order History

Retrieve the current user's order history.

- **URL**: `/orders/my-history`
- **Method**: `GET`
- **Auth Required**: Yes

**Query Parameters:**
- `restaurantId`: Filter by restaurant ID (optional)

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "restaurantName": "string",
      "orderDate": "datetime",
      "closedAt": "datetime",
      "status": "string",
      "managerName": "string",
      "deliveryFee": "decimal",
      "deliveryFeeShare": "decimal",
      "userItems": [
        {
          "id": "uuid",
          "menuItemName": "string",
          "price": "decimal",
          "quantity": "integer",
          "note": "string",
          "total": "decimal",
          "userId": "uuid",
          "userName": "string"
        }
      ],
      "userPaymentStatus": "string",
      "userTotal": "decimal"
    }
  ]
}
```

### Get Order Receipt

Retrieve a detailed receipt for an order.

- **URL**: `/orders/{id}/receipt`
- **Method**: `GET`
- **Auth Required**: Yes

**Path Parameters:**
- `id`: UUID of the order

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "orderId": "uuid",
    "restaurantName": "string",
    "orderDate": "datetime",
    "items": [
      {
        "userNumber": "integer",
        "userName": "string",
        "userPhoneNumber": "string",
        "items": [
          {
            "menuItemName": "string",
            "quantity": "integer",
            "price": "decimal",
            "note": "string",
            "total": "decimal"
          }
        ],
        "deliveryFeeShare": "decimal",
        "subtotal": "decimal"
      }
    ],
    "deliveryFee": "decimal",
    "grandTotal": "decimal",
    "userCount": "integer",
    "deliveryFeePerUser": "decimal"
  }
}
```

**Status Codes:**
- `200 OK`: Receipt retrieved
- `404 Not Found`: Order not found
- `400 Bad Request`: Order not closed

## Admin

### Get All Users

Retrieve a list of all users.

- **URL**: `/admin/users`
- **Method**: `GET`
- **Auth Required**: Yes
- **Authorization**: Admin role

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "uuid",
      "name": "string",
      "email": "string",
      "phoneNumber": "string",
      "role": "integer",
      "roleName": "string",
      "createdAt": "datetime"
    }
  ]
}
```

**Notes:**
- `role` values: 1 (Admin), 2 (Manager), 3 (End User)

**Status Codes:**
- `200 OK`: Users retrieved
- `403 Forbidden`: User does not have required role

### Update User Role

Update a user's role.

- **URL**: `/admin/users/{userId}/role`
- **Method**: `PUT`
- **Auth Required**: Yes
- **Authorization**: Admin role

**Path Parameters:**
- `userId`: UUID of the user

**Request Body:**
```json
{
  "role": "integer"
}
```

**Notes:**
- `role` values: 2 (Manager), 3 (End User)
- Cannot set users to Admin role via API

**Response:**
```json
{
  "succeeded": true,
  "errors": [],
  "message": "User role updated to Manager"
}
```

**Status Codes:**
- `200 OK`: Role updated
- `400 Bad Request`: Invalid role
- `403 Forbidden`: User does not have required role
- `404 Not Found`: User not found

## Error Responses

When an error occurs, the API returns a consistent error response format:

```json
{
  "succeeded": false,
  "errors": ["Error message 1", "Error message 2"]
}
```

## HTTP Status Codes

- `200 OK`: Request succeeded
- `201 Created`: Resource created
- `400 Bad Request`: Invalid request parameters
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error 