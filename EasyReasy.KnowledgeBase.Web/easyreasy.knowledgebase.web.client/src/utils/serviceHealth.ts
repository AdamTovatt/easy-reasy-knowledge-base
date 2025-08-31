import { authUtils } from './auth';
import type { ServiceHealthReport, ServiceHealthResponse } from './serviceHealthTypes';

export type { ServiceHealthReport, ServiceHealthResponse };

class ServiceHealthAPI {
    private static instance: ServiceHealthAPI;
    private lastCheck: Date | null = null;
    private cache: ServiceHealthResponse | null = null;
    private readonly cacheTimeoutMs = 30000; // 30 seconds

    private constructor() {}

    public static getInstance(): ServiceHealthAPI {
        if (!ServiceHealthAPI.instance) {
            ServiceHealthAPI.instance = new ServiceHealthAPI();
        }
        return ServiceHealthAPI.instance;
    }

    public async getHealthStatus(forceRefresh: boolean = false): Promise<ServiceHealthResponse> {
        // Return cached result if it's still valid and not forcing refresh
        if (!forceRefresh && this.cache && this.lastCheck) {
            const timeSinceLastCheck = Date.now() - this.lastCheck.getTime();
            if (timeSinceLastCheck < this.cacheTimeoutMs) {
                return this.cache;
            }
        }

        try {
            const authHeader = authUtils.getAuthHeader();
            const url = forceRefresh ? '/api/servicehealth?refresh=true' : '/api/servicehealth';
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...authHeader,
                },
            });

            if (!response.ok) {
                if (response.status === 401) {
                    // Clear the token and throw a specific error for 401
                    authUtils.clearToken();
                    throw new Error('AUTHENTICATION_REQUIRED');
                } else {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
            }

            const data: ServiceHealthResponse = await response.json();
            
            // Cache the result
            this.cache = data;
            this.lastCheck = new Date();
            
            return data;
        } catch (error) {
            console.error('Failed to fetch service health:', error);
            
            // Return a default unhealthy response if the health check itself fails
            const fallbackResponse: ServiceHealthResponse = {
                services: [],
                isHealthy: false,
                availableServicesCount: 0,
                totalServicesCount: 0,
            };
            
            this.cache = fallbackResponse;
            this.lastCheck = new Date();
            
            return fallbackResponse;
        }
    }

    public clearCache(): void {
        this.cache = null;
        this.lastCheck = null;
    }
}

export const serviceHealthAPI = ServiceHealthAPI.getInstance();
