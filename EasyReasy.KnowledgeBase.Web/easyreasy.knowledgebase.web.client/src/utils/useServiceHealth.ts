import { useState, useEffect, useCallback } from 'react';
import { serviceHealthAPI } from './serviceHealth';
import type { ServiceHealthResponse } from './serviceHealthTypes';

export interface UseServiceHealthOptions {
    autoRefresh?: boolean;
    refreshIntervalMs?: number;
    onHealthChange?: (health: ServiceHealthResponse) => void;
    onLogout?: () => void;
}

export interface UseServiceHealthReturn {
    health: ServiceHealthResponse | null;
    isLoading: boolean;
    error: string | null;
    refresh: () => Promise<void>;
    lastUpdated: Date | null;
}

export function useServiceHealth(options: UseServiceHealthOptions = {}): UseServiceHealthReturn {
    const {
        autoRefresh = true,
        refreshIntervalMs = 30000, // 30 seconds
        onHealthChange,
        onLogout,
    } = options;

    const [health, setHealth] = useState<ServiceHealthResponse | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);
    const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

    // Memoize the onHealthChange callback to prevent infinite loops
    const memoizedOnHealthChange = useCallback((healthData: ServiceHealthResponse) => {
        if (onHealthChange) {
            onHealthChange(healthData);
        }
    }, [onHealthChange]);

    const fetchHealth = useCallback(async (forceRefresh: boolean = false) => {
        try {
            setIsLoading(true);
            setError(null);
            
            const healthData = await serviceHealthAPI.getHealthStatus(forceRefresh);
            setHealth(healthData);
            setLastUpdated(new Date());
            
            memoizedOnHealthChange(healthData);
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to fetch service health';
            
            // Handle authentication error
            if (errorMessage === 'AUTHENTICATION_REQUIRED' && onLogout) {
                onLogout();
                return; // Don't set error state for auth errors
            }
            
            setError(errorMessage);
            console.error('Service health fetch error:', err);
        } finally {
            setIsLoading(false);
        }
    }, [memoizedOnHealthChange]);

    const refresh = useCallback(async () => {
        await fetchHealth(true);
    }, [fetchHealth]);

    // Initial fetch
    useEffect(() => {
        fetchHealth();
    }, [fetchHealth]);

    // Auto-refresh setup
    useEffect(() => {
        if (!autoRefresh) return;

        const interval = setInterval(() => {
            fetchHealth();
        }, refreshIntervalMs);

        return () => clearInterval(interval);
    }, [autoRefresh, refreshIntervalMs, fetchHealth]);

    return {
        health,
        isLoading,
        error,
        refresh,
        lastUpdated,
    };
}
