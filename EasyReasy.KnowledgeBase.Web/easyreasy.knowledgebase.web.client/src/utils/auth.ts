interface AuthToken {
    token: string;
    expiresAt: string;
}

const AUTH_STORAGE_KEY = 'easyreasy_auth_token';

export const authUtils = {
    // Save token to localStorage
    saveToken: (token: string, expiresAt: string): void => {
        const authData: AuthToken = { token, expiresAt };
        localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authData));
    },

    // Get token from localStorage
    getToken: (): string | null => {
        const authData = localStorage.getItem(AUTH_STORAGE_KEY);
        if (!authData) return null;

        try {
            const parsed: AuthToken = JSON.parse(authData);
            return parsed.token;
        } catch {
            return null;
        }
    },

    // Check if token is valid (not expired)
    isTokenValid: (): boolean => {
        const authData = localStorage.getItem(AUTH_STORAGE_KEY);
        if (!authData) return false;

        try {
            const parsed: AuthToken = JSON.parse(authData);
            const expirationDate = new Date(parsed.expiresAt);
            const now = new Date();
            
            return expirationDate > now;
        } catch {
            return false;
        }
    },

    // Get valid token or null if expired
    getValidToken: (): string | null => {
        if (!authUtils.isTokenValid()) {
            authUtils.clearToken();
            return null;
        }
        return authUtils.getToken();
    },

    // Clear token from localStorage
    clearToken: (): void => {
        localStorage.removeItem(AUTH_STORAGE_KEY);
    },

    // Get authorization header for API requests
    getAuthHeader: (): { Authorization: string } | {} => {
        const token = authUtils.getValidToken();
        return token ? { Authorization: `Bearer ${token}` } : {};
    }
};
