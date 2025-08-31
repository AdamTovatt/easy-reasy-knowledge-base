import React, { useCallback, useState, useEffect } from 'react';
import { AlertTriangle, CheckCircle, RefreshCw, X, ChevronDown, ChevronRight } from 'feather-icons-react';
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
    // State to track if healthy notification should be hidden
    const [hideHealthy, setHideHealthy] = useState(false);
    // State to track if element should be removed from DOM
    const [removeFromDOM, setRemoveFromDOM] = useState(false);
    // State to track previous health status to detect changes
    const [previousHealthStatus, setPreviousHealthStatus] = useState<boolean | undefined>(undefined);

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

    // Auto-hide healthy notifications after 5 seconds
    useEffect(() => {
        if (!health || isLoading) return;

        // Check if health status has changed
        const healthStatusChanged = previousHealthStatus !== undefined && previousHealthStatus !== health.isHealthy;
        
        if (health.isHealthy) {
            // Reset states and start timer if health status changed from unhealthy to healthy
            // OR if this is the first time we're seeing healthy status (previousHealthStatus is undefined)
            if (healthStatusChanged || previousHealthStatus === undefined) {
                setHideHealthy(false);
                setRemoveFromDOM(false);
                
                // Set timer to start fade after 5 seconds
                const timer = setTimeout(() => {
                    setHideHealthy(true);
                    // Remove from DOM after fade animation completes (0.5s)
                    const removeTimer = setTimeout(() => {
                        setRemoveFromDOM(true);
                    }, 500);
                    
                    return () => clearTimeout(removeTimer);
                }, 5000);

                return () => clearTimeout(timer);
            }
        } else {
            // Reset states when health becomes unhealthy
            setHideHealthy(false);
            setRemoveFromDOM(false);
        }
        
        // Update previous health status
        setPreviousHealthStatus(health.isHealthy);
    }, [health?.isHealthy, isLoading, previousHealthStatus]);

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

    // Don't render anything if we're still loading and should only show when unhealthy
    if (isLoading && showOnlyWhenUnhealthy) {
        return null;
    }

    // Don't render anything if all services are healthy and we only want to show unhealthy
    if (health?.isHealthy && showOnlyWhenUnhealthy) {
        return null;
    }

    // Don't render anything if there's no health data yet
    if (!health) {
        return null;
    }

    // Don't render anything if healthy notification should be removed from DOM
    if (health.isHealthy && removeFromDOM) {
        return null;
    }

    const isHealthy = health.isHealthy;
    const unhealthyServices = health.services.filter(service => !service.isAvailable);
    const healthyServices = health.services.filter(service => service.isAvailable);

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
            return `All ${health.totalServicesCount} services are healthy`;
        }
        
        return `${health.availableServicesCount}/${health.totalServicesCount} services available`;
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
        <div className={`service-health-notification ${getStatusClass()} ${className} ${health.isHealthy && hideHealthy ? 'fade-out' : ''}`}>
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
