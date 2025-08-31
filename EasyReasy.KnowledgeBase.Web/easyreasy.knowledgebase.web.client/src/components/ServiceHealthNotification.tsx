import { useCallback, useState, useEffect } from 'react';
import { AlertTriangle, CheckCircle, RefreshCw, ChevronDown, ChevronRight } from 'feather-icons-react';
import { useServiceHealth } from '../utils/useServiceHealth';
import type { ServiceHealthResponse } from '../utils/serviceHealthTypes';

export interface ServiceHealthNotificationProps {
    className?: string;
    showOnlyWhenUnhealthy?: boolean;
    autoRefresh?: boolean;
    refreshIntervalMs?: number;
    onHealthChange?: (isHealthy: boolean) => void;
    onLogout?: () => void;
}

export function ServiceHealthNotification({
    className = '',
    showOnlyWhenUnhealthy = false,
    autoRefresh = true,
    refreshIntervalMs = 30000,
    onHealthChange,
    onLogout,
}: ServiceHealthNotificationProps) {
    // State to force re-render for time updates
    const [, forceUpdate] = useState({});
    // State to track which service items are expanded
    const [expandedServices, setExpandedServices] = useState<Set<string>>(new Set());
    // State to track if this is the initial load
    const [isInitialLoad, setIsInitialLoad] = useState(true);
    // State to control showing healthy notification on initial load
    const [showHealthyInitial, setShowHealthyInitial] = useState(true);
    // State to control fade animation
    const [isFadingOut, setIsFadingOut] = useState(false);

    // Memoize the callback to prevent infinite loops
    const memoizedOnHealthChange = useCallback((healthData: ServiceHealthResponse) => {
        if (onHealthChange) {
            onHealthChange(healthData.isHealthy);
        }
    }, [onHealthChange]);

    const { health, isLoading, error, refresh, lastUpdated } = useServiceHealth({
        autoRefresh,
        refreshIntervalMs,
        onHealthChange: memoizedOnHealthChange,
        onLogout,
    });

    // Update the display every second to show elapsed time
    useEffect(() => {
        if (!lastUpdated) return;

        const interval = setInterval(() => {
            forceUpdate({});
        }, 1000);

        return () => clearInterval(interval);
    }, [lastUpdated]);

    // Handle initial load - show healthy notification for 5 seconds then fade out
    useEffect(() => {
        if (!health || isLoading) return;

        if (isInitialLoad) {
            const timer = setTimeout(() => {
                if (health.isHealthy) {
                    // Start fade out animation first
                    setIsFadingOut(true);
                    // Hide completely after fade animation (0.5s)
                    const hideTimer = setTimeout(() => {
                        setShowHealthyInitial(false);
                        setIsFadingOut(false);
                        setIsInitialLoad(false);
                    }, 500);
                    
                    return () => clearTimeout(hideTimer);
                } else {
                    // If unhealthy, just mark initial load as complete
                    setIsInitialLoad(false);
                }
            }, 5000);

            return () => clearTimeout(timer);
        }
    }, [health?.isHealthy, isLoading, isInitialLoad]);

    // Reset states when health changes from healthy to unhealthy
    useEffect(() => {
        if (health && !health.isHealthy) {
            setShowHealthyInitial(true);
            setIsFadingOut(false);
        }
    }, [health?.isHealthy]);

    const toggleServiceExpansion = (serviceName: string) => {
        setExpandedServices(prev => {
            const newSet = new Set(prev);
            if (newSet.has(serviceName)) {
                newSet.delete(serviceName);
            } else {
                newSet.add(serviceName);
            }
            return newSet;
        });
    };

    // Determine if we should show the notification
    const shouldShow = () => {
        // Don't show anything if there's no health data yet
        if (!health) return false;
        
        // Always show if loading
        if (isLoading) return true;
        
        // Always show if unhealthy
        if (!health.isHealthy) return true;
        
        // For healthy services:
        if (showOnlyWhenUnhealthy) {
            // Only show healthy during initial load period when showHealthyInitial is true
            return showHealthyInitial;
        } else {
            // Always show if not configured to hide when healthy
            return true;
        }
    };

    if (!shouldShow()) {
        return null;
    }

    const isHealthy = health?.isHealthy ?? false;
    const unhealthyServices = health?.services.filter(service => !service.isAvailable) ?? [];

    const getStatusIcon = () => {
        if (isLoading) {
            return <RefreshCw size={16} className="animate-spin" />;
        }
        return isHealthy ? <CheckCircle size={16} /> : <AlertTriangle size={16} />;
    };

    const getStatusText = () => {
        if (isLoading) {
            return 'Checking service health...';
        }
        
        if (isHealthy) {
            return `All ${health?.totalServicesCount ?? 0} services are healthy`;
        }
        
        return `${health?.availableServicesCount ?? 0}/${health?.totalServicesCount ?? 0} services available`;
    };

    const getStatusClass = () => {
        if (isLoading) {
            return 'status-loading';
        }
        return isHealthy ? 'status-healthy' : 'status-unhealthy';
    };

    const formatLastUpdated = () => {
        if (!lastUpdated) return '';
        
        const now = new Date();
        const diffMs = now.getTime() - lastUpdated.getTime();
        const diffSeconds = Math.floor(diffMs / 1000);
        
        if (diffSeconds < 60) {
            return `${diffSeconds}s ago`;
        }
        
        const diffMinutes = Math.floor(diffSeconds / 60);
        if (diffMinutes < 60) {
            return `${diffMinutes}m ago`;
        }
        
        const diffHours = Math.floor(diffMinutes / 60);
        return `${diffHours}h ago`;
    };

    return (
        <div className={`service-health-notification ${getStatusClass()} ${className} ${isFadingOut ? 'fade-out' : ''}`}>
            <div className="notification-header">
                <div className="status-info">
                    <span className="status-icon">
                        {getStatusIcon()}
                    </span>
                    <span className="status-text">
                        {getStatusText()}
                    </span>
                </div>
                
                <div className="notification-actions">
                    <button
                        className="refresh-button"
                        onClick={refresh}
                        disabled={isLoading}
                        title="Refresh service health"
                    >
                        <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
                    </button>
                    
                    {lastUpdated && (
                        <span className="last-updated">
                            {formatLastUpdated()}
                        </span>
                    )}
                </div>
            </div>

            {!isHealthy && unhealthyServices.length > 0 && (
                <div className="unhealthy-services">
                    <div className="services-header">
                        Unhealthy Services:
                    </div>
                    <div className="services-list">
                        {unhealthyServices.map((service, index) => {
                            const isExpanded = expandedServices.has(service.serviceName);
                            const hasErrorMessage = service.errorMessage && service.errorMessage.trim();
                            
                            return (
                                <div key={index} className="service-item unhealthy">
                                    <div 
                                        className="service-header"
                                        onClick={() => toggleServiceExpansion(service.serviceName)}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        <span className="service-name">{service.serviceName}</span>
                                        <span className="expand-icon">
                                            {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                                        </span>
                                    </div>
                                    {isExpanded && (
                                        <div className="service-error-details">
                                            {hasErrorMessage ? (
                                                <span className="service-error">{service.errorMessage}</span>
                                            ) : (
                                                <span className="service-error">No further information available.</span>
                                            )}
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}

            {error && (
                <div className="health-check-error">
                    <span className="error-text">Failed to check service health: {error}</span>
                </div>
            )}
        </div>
    );
}
