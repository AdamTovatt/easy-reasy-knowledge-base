export interface ServiceHealthReport {
    serviceName: string;
    isAvailable: boolean;
    errorMessage?: string;
}

export interface ServiceHealthResponse {
    services: ServiceHealthReport[];
    isHealthy: boolean;
    availableServicesCount: number;
    totalServicesCount: number;
}
