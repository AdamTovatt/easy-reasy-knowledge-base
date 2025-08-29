import { useState, useRef, useEffect, useCallback } from 'react';
import { ArrowUp, Pause, AlertTriangle } from 'feather-icons-react';

const ErrorIcon = () => (
    <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        transform: 'translateY(2px)' // Adjust this value as needed
    }}>
        <AlertTriangle size={20} />
    </div>
);

interface StreamChatResponse {
    message?: string;
    error?: string;
}

function Chat() {
    const [inputMessage, setInputMessage] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [chatHistory, setChatHistory] = useState<string[]>([]);
    const [currentResponse, setCurrentResponse] = useState('');
    const chatContainerRef = useRef<HTMLDivElement>(null);
    const abortControllerRef = useRef<AbortController | null>(null);

    useEffect(() => {
        if (chatContainerRef.current) {
            chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
        }
    }, [chatHistory, currentResponse]);

    // Set initial textarea height on mount
    useEffect(() => {
        const textarea = document.querySelector('.chat-input') as HTMLTextAreaElement;
        if (textarea) {
            textarea.style.height = 'auto';
            textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
        }
    }, []);

    const updateCurrentResponse = useCallback((newResponse: string) => {
        setCurrentResponse(newResponse);
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!inputMessage.trim() || isStreaming) return;

        const userMessage = inputMessage.trim();
        setInputMessage('');
        setIsStreaming(true);
        setCurrentResponse('');

        // Add user message to chat history
        setChatHistory(prev => [...prev, `You: ${userMessage}`]);

        // Create abort controller for this request
        abortControllerRef.current = new AbortController();

        try {
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: userMessage }),
                signal: abortControllerRef.current.signal,
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reader = response.body?.getReader();
            if (!reader) {
                throw new Error('No response body');
            }

            const decoder = new TextDecoder();
            let responseText = '';
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) {
                    break;
                }

                const chunk = decoder.decode(value, { stream: true });
                buffer += chunk;

                const lines = buffer.split('\n');
                buffer = lines.pop() || ''; // Keep incomplete line in buffer

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            const data = line.slice(6);
                            const parsed: StreamChatResponse = JSON.parse(data);
                            
                            if (parsed.error) {
                                throw new Error(parsed.error);
                            }
                            
                            if (parsed.message) {
                                responseText += parsed.message;
                                updateCurrentResponse(responseText);
                            }
                        } catch (parseError) {
                            console.error('Failed to parse SSE data:', parseError, 'Raw data:', line.slice(6));
                        }
                    }
                }
            }

            // Add AI response to chat history
            setChatHistory(prev => [...prev, `AI: ${responseText}`]);
            setCurrentResponse('');

        } catch (error) {
            console.error('Error:', error);
            console.log('Error type:', typeof error);
            console.log('Error message:', error instanceof Error ? error.message : 'Unknown error');
            
            // Don't add error messages for aborted requests since we handle them in handleCancelStream
            if (!(error instanceof Error && error.name === 'AbortError')) {
                setChatHistory(prev => [...prev, `Error: ${error instanceof Error ? error.message : 'Unknown error'}`]);
            }
        } finally {
            setIsStreaming(false);
            abortControllerRef.current = null;
        }
    };

    const handleCancelStream = () => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
            setIsStreaming(false);
            
            // Save the partial response to chat history
            if (currentResponse.trim()) {
                setChatHistory(prev => [...prev, `AI: ${currentResponse}`]);
            }
            
            setCurrentResponse('');
            abortControllerRef.current = null;
        }
    };

    return (
        <div className="chat-layout">
            {/* Left Sidebar */}
            <div className="chat-sidebar">
                <div className="sidebar-header">
                    <div className="sidebar-nav">
                        <div className="nav-item">
                            <span className="nav-icon">🏠</span>
                            <span>Home</span>
                        </div>
                        <div className="nav-item">
                            <span className="nav-icon">🔲</span>
                            <span>Spaces</span>
                            <span className="preview-tag">Preview</span>
                        </div>
                    </div>
                </div>
                
                <div className="sidebar-content">
                    <div className="today-section">
                        <h3>Today</h3>
                        <div className="chat-history-item">
                            <span>Checking GitHub repositories availab...</span>
                            <span className="dots">⋯</span>
                        </div>
                    </div>
                </div>
                
                <div className="sidebar-footer">
                    <a href="#" className="upgrade-link">Upgrade to Pro</a>
                    <span>to access higher limits and premium models.</span>
                </div>
            </div>

            {/* Main Chat Area */}
            <div className="chat-main">
                <div className="chat-header">
                    <div className="header-actions">
                        <button className="header-icon">⋯</button>
                        <button className="header-icon">⬈</button>
                    </div>
                </div>
                
                <div className="chat-messages" ref={chatContainerRef}>
                    <div className="messages-container">
                        {chatHistory.map((message, index) => {
                            const isUserMessage = message.startsWith('You:');
                            const isErrorMessage = message.startsWith('Error:');
                            
                            return (
                                <div key={index} className={`message ${isUserMessage ? 'user-message' : 'ai-message'} ${isErrorMessage ? 'error-message' : ''}`}>
                                    {isUserMessage ? (
                                        <span>{message.replace('You: ', '')}</span>
                                    ) : (
                                        <>
                                            {isErrorMessage ? (
                                                <span className="ai-icon">
                                                    <ErrorIcon />
                                                </span>
                                            ) : (
                                                <span className="ai-icon">🤖</span>
                                            )}
                                            <span>
                                                {message.replace('AI: ', '').replace('Error: ', '')}
                                            </span>
                                        </>
                                    )}
                                </div>
                            );
                        })}
                        {isStreaming && (
                            <div className="message ai-message">
                                <span className="ai-icon">🤖</span>
                                <span>{currentResponse || 'Thinking...'}</span>
                            </div>
                        )}
                    </div>
                </div>

                <div className="chat-input-container">
                    <form onSubmit={handleSubmit} className="chat-input-form">
                        <textarea
                            value={inputMessage}
                            onChange={(e) => {
                                setInputMessage(e.target.value);
                                // Auto-resize the textarea
                                e.target.style.height = 'auto';
                                e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px';
                            }}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter' && !e.shiftKey) {
                                    e.preventDefault();
                                    handleSubmit(e as any);
                                }
                            }}
                            onFocus={(e) => {
                                // Ensure proper height on focus
                                e.target.style.height = 'auto';
                                e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px';
                            }}
                            placeholder="How can I help you?"
                            className="chat-input"
                        />
                        <div className="input-actions">
                            <button 
                                type={isStreaming ? "button" : "submit"}
                                disabled={!isStreaming && !inputMessage.trim()}
                                className="send-button"
                                onClick={isStreaming ? handleCancelStream : undefined}
                            >
                                {isStreaming ? <Pause size={16} /> : <ArrowUp size={16} />}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}

export default Chat;
