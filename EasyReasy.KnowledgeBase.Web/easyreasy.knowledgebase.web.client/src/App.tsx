import { useState, useEffect } from 'react';
import Chat from './Chat';
import Login from './Login';
import { authUtils } from './utils/auth';
import './App.css';

function App() {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // Check if user is authenticated on app load
        const checkAuth = () => {
            const isValid = authUtils.isTokenValid();
            setIsAuthenticated(isValid);
            setIsLoading(false);
        };

        checkAuth();
    }, []);

    const handleLogin = (token: string, expiresAt: string) => {
        authUtils.saveToken(token, expiresAt);
        setIsAuthenticated(true);
    };

    const handleLogout = () => {
        authUtils.clearToken();
        setIsAuthenticated(false);
    };

    if (isLoading) {
        return (
            <div className="app">
                <div className="loading-container">
                    <div className="loading-spinner"></div>
                    <p>Loading...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="app">
            {isAuthenticated ? (
                <Chat onLogout={handleLogout} />
            ) : (
                <Login onLogin={handleLogin} />
            )}
        </div>
    );
}

export default App;