# Service Health Notification System

This system provides a comprehensive way to monitor and display service health status in the frontend application.

## Components

### ServiceHealthNotification

A React component that displays service health status with automatic polling and user-friendly notifications.

#### Props

- `className?: string` - Additional CSS classes
- `showOnlyWhenUnhealthy?: boolean` - Only show notification when services are unhealthy (default: false)
- `autoRefresh?: boolean` - Enable automatic health checks (default: true)
- `refreshIntervalMs?: number` - Interval between health checks in milliseconds (default: 30000)
- `onHealthChange?: (isHealthy: boolean) => void` - Callback when health status changes

#### Usage Examples

```tsx
// Basic usage - always show health status
<ServiceHealthNotification />

// Only show when services are down (recommended for most pages)
<ServiceHealthNotification showOnlyWhenUnhealthy={true} />

// Custom refresh interval
<ServiceHealthNotification refreshIntervalMs={60000} />

// Manual refresh only
<ServiceHealthNotification autoRefresh={false} />

// With health change callback
<ServiceHealthNotification 
    onHealthChange={(isHealthy) => {
        console.log('Health status:', isHealthy);
    }}
/>
```

## Utilities

### useServiceHealth Hook

A React hook that manages service health state with automatic polling.

#### Options

- `autoRefresh?: boolean` - Enable automatic health checks (default: true)
- `refreshIntervalMs?: number` - Interval between health checks (default: 30000)
- `onHealthChange?: (health: ServiceHealthResponse) => void` - Callback when health data changes

#### Return Value

- `health: ServiceHealthResponse | null` - Current health data
- `isLoading: boolean` - Loading state
- `error: string | null` - Error message if any
- `refresh: () => Promise<void>` - Manual refresh function
- `lastUpdated: Date | null` - Last update timestamp

#### Usage Example

```tsx
import { useServiceHealth } from '../utils/useServiceHealth';

function MyComponent() {
    const { health, isLoading, error, refresh } = useServiceHealth({
        autoRefresh: true,
        refreshIntervalMs: 30000,
        onHealthChange: (healthData) => {
            console.log('Health updated:', healthData);
        }
    });

    if (isLoading) return <div>Loading health status...</div>;
    if (error) return <div>Error: {error}</div>;
    if (!health) return <div>No health data</div>;

    return (
        <div>
            <p>Services: {health.availableServicesCount}/{health.totalServicesCount}</p>
            <p>Status: {health.isHealthy ? 'Healthy' : 'Unhealthy'}</p>
            <button onClick={refresh}>Refresh</button>
        </div>
    );
}
```

### serviceHealthAPI

A singleton API client for fetching service health data with caching.

#### Methods

- `getHealthStatus(forceRefresh?: boolean): Promise<ServiceHealthResponse>` - Fetch health status
- `clearCache(): void` - Clear cached data

#### Usage Example

```tsx
import { serviceHealthAPI } from '../utils/serviceHealth';

// Fetch health status (uses cache if available)
const health = await serviceHealthAPI.getHealthStatus();

// Force refresh
const freshHealth = await serviceHealthAPI.getHealthStatus(true);

// Clear cache
serviceHealthAPI.clearCache();
```

## Types

### ServiceHealthReport

```tsx
interface ServiceHealthReport {
    serviceName: string;
    isAvailable: boolean;
    errorMessage?: string;
}
```

### ServiceHealthResponse

```tsx
interface ServiceHealthResponse {
    services: ServiceHealthReport[];
    isHealthy: boolean;
    availableServicesCount: number;
    totalServicesCount: number;
}
```

## Features

- **Automatic Polling**: Configurable intervals for health checks
- **Caching**: 30-second cache to reduce API calls
- **Error Handling**: Graceful fallback when health checks fail
- **Responsive Design**: Mobile-friendly notifications
- **Flexible Display**: Show always or only when unhealthy
- **Manual Refresh**: User can manually refresh health status
- **Health Change Callbacks**: React to health status changes
- **Custom Styling**: Support for custom CSS classes

## Integration

The system is already integrated into the Chat component as an example. To add it to other pages:

1. Import the component:
   ```tsx
   import { ServiceHealthNotification } from './components/ServiceHealthNotification';
   ```

2. Add it to your component:
   ```tsx
   <ServiceHealthNotification 
       showOnlyWhenUnhealthy={true}
       className="my-page-health-notification"
   />
   ```

3. Add any custom CSS if needed:
   ```css
   .my-page-health-notification {
       /* Custom positioning or styling */
   }
   ```

## Backend Integration

The system expects the backend to provide a `/api/servicehealth` endpoint that returns data in the `ServiceHealthResponse` format. The endpoint should be protected by authentication and return health status for all monitored services.
