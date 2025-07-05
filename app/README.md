# Procurement Management System - Frontend

A modern Angular application for managing procurement processes, including Request for Quotes (RFQs) and Suppliers management.

## Features

### RFQ Management
- **RFQ List**: View all RFQs with advanced filtering and search capabilities
- **RFQ Details**: Comprehensive view of individual RFQs including:
  - Basic information (title, status, dates, estimated value)
  - Line items with item details
  - Invited suppliers
  - Received quotes
- **Advanced Filtering**: Filter by status, date range, and search terms
- **Pagination**: Efficient data loading with server-side pagination

### Supplier Management
- **Supplier List**: View all suppliers with filtering and search
- **Supplier Details**: Detailed supplier information including:
  - Contact information
  - Location details
  - Performance metrics
  - Rating system
- **Advanced Filtering**: Filter by country, rating, and active status
- **Pagination**: Server-side pagination for large datasets

## Technology Stack

- **Angular 20**: Latest version with standalone components
- **TypeScript**: Strict type checking
- **Tailwind CSS**: Modern utility-first CSS framework
- **AG Grid**: Enterprise-grade data grid for complex data display
- **RxJS**: Reactive programming for state management
- **Angular Signals**: Modern state management approach

## Architecture

### Project Structure
```
src/app/
├── shared/
│   ├── models/          # TypeScript interfaces
│   └── services/        # Shared services
├── features/
│   ├── rfqs/           # RFQ feature module
│   │   ├── components/ # RFQ components
│   │   ├── services/   # RFQ services
│   │   └── routes.ts   # RFQ routes
│   └── suppliers/      # Suppliers feature module
│       ├── components/ # Supplier components
│       ├── services/   # Supplier services
│       └── routes.ts   # Supplier routes
└── app.*              # Main app files
```

### Key Design Patterns

1. **Lazy Loading**: Feature modules are loaded on-demand
2. **Standalone Components**: Modern Angular approach without NgModules
3. **Signal-based State**: Using Angular signals for reactive state management
4. **Service Layer**: Centralized API communication
5. **Type Safety**: Full TypeScript integration with strict typing

## Getting Started

### Prerequisites
- Node.js (v18 or higher)
- Angular CLI (v20 or higher)

### Installation
```bash
# Install dependencies
npm install

# Start development server
npm start
```

### API Configuration
The application expects the backend API to be running on `http://localhost:5001`. Update the base URL in `src/app/shared/services/api.service.ts` if needed.

## Development

### Running the Application
```bash
# Development server
npm start

# Build for production
npm run build

# Run tests
npm test
```

### Code Style
- Follow Angular style guide
- Use standalone components
- Implement proper TypeScript typing
- Follow Tailwind CSS conventions
- Use Angular signals for state management

## API Integration

The application integrates with the following API endpoints:

### RFQs
- `GET /api/rfqs` - List RFQs with pagination and filtering
- `GET /api/rfqs/{id}` - Get RFQ details
- `GET /api/rfqs/statuses` - Get available RFQ statuses
- `GET /api/rfqs/summary` - Get RFQ summary statistics

### Suppliers
- `GET /api/suppliers` - List suppliers with pagination and filtering
- `GET /api/suppliers/{id}` - Get supplier details
- `GET /api/suppliers/countries` - Get available countries

## Features in Detail

### RFQ Management
- **Status Tracking**: Draft, Open, Closed, Awarded
- **Date Management**: Issue date, due date, award date
- **Value Tracking**: Estimated values with currency support
- **Line Items**: Detailed item specifications
- **Supplier Invitations**: Track invited suppliers
- **Quote Management**: View and compare received quotes

### Supplier Management
- **Contact Information**: Name, email, phone
- **Location Data**: City, state, country
- **Rating System**: 1-5 star rating with visual display
- **Status Tracking**: Active/Inactive status
- **Performance Metrics**: Quote acceptance rates, delivery performance

### Data Grid Features
- **Sorting**: Multi-column sorting
- **Filtering**: Built-in column filters
- **Pagination**: Server-side pagination
- **Responsive Design**: Mobile-friendly layout
- **Custom Cell Renderers**: Status badges, ratings, dates

## Future Enhancements

- [ ] Create/Edit RFQ functionality
- [ ] Create/Edit Supplier functionality
- [ ] Quote comparison tools
- [ ] Dashboard with analytics
- [ ] Export functionality
- [ ] Real-time notifications
- [ ] Advanced reporting
- [ ] User authentication and authorization
