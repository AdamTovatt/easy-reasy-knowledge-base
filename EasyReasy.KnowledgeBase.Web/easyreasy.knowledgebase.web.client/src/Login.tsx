import { useState } from 'react';
import { LogIn, Eye, EyeOff, AlertTriangle } from 'feather-icons-react';
import EasyReasyIcon from './assets/icons/easy-reasy-icon-256.png';

interface LoginResponse {
    token: string;
    expiresAt: string;
}

function Login({ onLogin }: { onLogin: (token: string, expiresAt: string) => void }) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [isAnimating, setIsAnimating] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!username.trim() || !password.trim()) {
            setError('Please enter both username and password');
            return;
        }

        // Start the collapse animation immediately
        setIsAnimating(true);
        setIsLoading(true);
        setError('');

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, password }),
            });

            if (!response.ok) {
                if (response.status === 401) {
                    throw new Error('Invalid username or password');
                } else {
                    throw new Error(`Login failed: ${response.status}`);
                }
            }

            const data: LoginResponse = await response.json();
            
            // Wait for animation to complete before calling onLogin
            setTimeout(() => {
                onLogin(data.token, data.expiresAt);
            }, 600); // Match the animation duration
            
        } catch (error) {
            console.error('Login error:', error);
            setError(error instanceof Error ? error.message : 'Login failed');
            setIsLoading(false);
            // Reset animation if there's an error
            setIsAnimating(false);
        }
    };

    return (
        <div className="login-page">
            <div className={`login-logo ${isAnimating ? 'logo-merge' : ''}`}>
                <div className={`animated-border ${isAnimating ? 'border-visible' : ''}`}></div>
                <img src={EasyReasyIcon} alt="EasyReasy" draggable="false" />
            </div>
            <div className={`login-container ${isAnimating ? 'login-collapse' : ''}`}>
                <form onSubmit={handleSubmit} className="login-form">
                    {error && (
                        <div className="login-error">
                            <AlertTriangle size={16} />
                            <span>{error}</span>
                        </div>
                    )}

                    <div className="form-group">
                        <label htmlFor="username">Username</label>
                        <input
                            type="text"
                            id="username"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="Enter your username"
                            disabled={isLoading}
                            required
                            autoComplete="off"
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <div className="password-input-container">
                            <input
                                type={showPassword ? 'text' : 'password'}
                                id="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                placeholder="Enter your password"
                                disabled={isLoading}
                                required
                                autoComplete="off"
                            />
                            <button
                                type="button"
                                className="password-toggle"
                                onClick={() => setShowPassword(!showPassword)}
                                disabled={isLoading}
                            >
                                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                            </button>
                        </div>
                    </div>

                    <button
                        type="submit"
                        className="login-button"
                        disabled={isLoading || !username.trim() || !password.trim()}
                    >
                        {isLoading ? (
                            <span>Signing in...</span>
                        ) : (
                            <>
                                <LogIn size={16} />
                                <span>Sign In</span>
                            </>
                        )}
                    </button>
                </form>
            </div>
        </div>
    );
}

export default Login;
