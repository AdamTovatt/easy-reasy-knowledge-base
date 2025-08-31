import React from 'react';
import { ServiceHealthNotification } from './ServiceHealthNotification';

export function ServiceHealthDemo() {
    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
            <h1>Service Health Notification Examples</h1>
            
            <div style={{ marginBottom: '40px' }}>
                <h2>1. Always Show (Default)</h2>
                <p>Shows the notification regardless of health status:</p>
                <ServiceHealthNotification 
                    showOnlyWhenUnhealthy={false}
                    autoRefresh={true}
                    refreshIntervalMs={10000}
                />
            </div>

            <div style={{ marginBottom: '40px' }}>
                <h2>2. Show Only When Unhealthy</h2>
                <p>Only appears when services are down (like in Chat.tsx):</p>
                <ServiceHealthNotification 
                    showOnlyWhenUnhealthy={true}
                    autoRefresh={true}
                    refreshIntervalMs={10000}
                />
            </div>

            <div style={{ marginBottom: '40px' }}>
                <h2>3. Manual Refresh Only</h2>
                <p>No auto-refresh, user must click refresh button:</p>
                <ServiceHealthNotification 
                    showOnlyWhenUnhealthy={false}
                    autoRefresh={false}
                />
            </div>

            <div style={{ marginBottom: '40px' }}>
                <h2>4. With Health Change Callback</h2>
                <p>Logs health changes to console:</p>
                <ServiceHealthNotification 
                    showOnlyWhenUnhealthy={false}
                    autoRefresh={true}
                    refreshIntervalMs={15000}
                    onHealthChange={(isHealthy) => {
                        console.log('Service health changed:', isHealthy ? 'Healthy' : 'Unhealthy');
                    }}
                />
            </div>

            <div style={{ marginBottom: '40px' }}>
                <h2>5. Custom Styling</h2>
                <p>With custom CSS class:</p>
                <ServiceHealthNotification 
                    showOnlyWhenUnhealthy={false}
                    autoRefresh={true}
                    refreshIntervalMs={20000}
                    className="demo-custom-style"
                />
            </div>

            <div style={{ marginTop: '40px', padding: '20px', backgroundColor: '#f5f5f5', borderRadius: '8px' }}>
                <h3>Usage Examples:</h3>
                <pre style={{ backgroundColor: '#fff', padding: '15px', borderRadius: '4px', overflow: 'auto' }}>
{`// Basic usage - always show
<ServiceHealthNotification />

// Only show when unhealthy (recommended for most pages)
<ServiceHealthNotification showOnlyWhenUnhealthy={true} />

// Custom refresh interval (30 seconds)
<ServiceHealthNotification refreshIntervalMs={30000} />

// No auto-refresh, manual only
<ServiceHealthNotification autoRefresh={false} />

// With callback for health changes
<ServiceHealthNotification 
    onHealthChange={(isHealthy) => {
        // Handle health status changes
        console.log('Health status:', isHealthy);
    }}
/>

// With custom CSS class
<ServiceHealthNotification className="my-custom-style" />`}
                </pre>
            </div>
        </div>
    );
}
