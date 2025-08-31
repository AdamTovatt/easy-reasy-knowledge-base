import { useState, useEffect } from 'react';
import Chat from './Chat';
import Login from './Login';
import KnowledgeBase from './KnowledgeBase';
import { authUtils } from './utils/auth';
import './App.css';

type AppPage = 'chat' | 'knowledgeBase';

function App() {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState<AppPage>('chat');

    // Get current page from URL
    const getCurrentPageFromUrl = (): AppPage => {
        const path = window.location.pathname;
        if (path === '/knowledge-base' || path === '/knowledgebase') {
            return 'knowledgeBase';
        }
        return 'chat';
    };

    useEffect(() => {
        // Check if user is authenticated on app load and set current page from URL
        const checkAuth = () => {
            const isValid = authUtils.isTokenValid();
            setIsAuthenticated(isValid);
            if (isValid) {
                setCurrentPage(getCurrentPageFromUrl());
            }
            setIsLoading(false);
        };

        checkAuth();

        // Listen for browser back/forward navigation
        const handlePopState = () => {
            if (isAuthenticated) {
                setCurrentPage(getCurrentPageFromUrl());
            }
        };

        window.addEventListener('popstate', handlePopState);
        return () => window.removeEventListener('popstate', handlePopState);
    }, [isAuthenticated]);

    const handleLogin = (token: string, expiresAt: string) => {
        authUtils.saveToken(token, expiresAt);
        setIsAuthenticated(true);
    };

    const handleLogout = () => {
        authUtils.clearToken();
        setIsAuthenticated(false);
        setCurrentPage('chat');
        window.history.pushState({}, '', '/');
    };

    const navigateToChat = () => {
        setCurrentPage('chat');
        window.history.pushState({}, '', '/');
    };

    const navigateToKnowledgeBase = () => {
        setCurrentPage('knowledgeBase');
        window.history.pushState({}, '', '/knowledge-base');
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
                currentPage === 'chat' ? (
                    <Chat 
                        onLogout={handleLogout} 
                        onNavigateToKnowledgeBase={navigateToKnowledgeBase} 
                    />
                ) : (
                    <KnowledgeBase 
                        onLogout={handleLogout} 
                        onNavigateToChat={navigateToChat} 
                    />
                )
            ) : (
                <Login onLogin={handleLogin} />
            )}
        </div>
    );
}

export default App;