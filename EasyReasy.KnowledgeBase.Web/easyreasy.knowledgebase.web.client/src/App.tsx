import { useState, useRef, useEffect, useCallback } from 'react';
import './App.css';

interface StreamChatResponse {
    message?: string;
    error?: string;
}

function App() {
    const [inputMessage, setInputMessage] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [chatHistory, setChatHistory] = useState<string[]>([]);
    const [currentResponse, setCurrentResponse] = useState('');
    const chatContainerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (chatContainerRef.current) {
            chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
        }
    }, [chatHistory, currentResponse]);

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

        try {
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: userMessage }),
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
            setChatHistory(prev => [...prev, `Error: ${error instanceof Error ? error.message : 'Unknown error'}`]);
        } finally {
            setIsStreaming(false);
        }
    };

    return (
        <div className="chat-container">
            <h1>EasyReasy Knowledge Base Chat</h1>
            
            <div className="chat-messages" ref={chatContainerRef}>
                {chatHistory.map((message, index) => (
                    <div key={index} className={`message ${message.startsWith('You:') ? 'user-message' : 'ai-message'}`}>
                        {message}
                    </div>
                ))}
                {isStreaming && (
                    <div className="message ai-message">
                        AI: {currentResponse || 'Thinking...'}
                    </div>
                )}
            </div>

            <form onSubmit={handleSubmit} className="chat-input-form">
                <input
                    type="text"
                    value={inputMessage}
                    onChange={(e) => setInputMessage(e.target.value)}
                    placeholder="Type your message here..."
                    disabled={isStreaming}
                    className="chat-input"
                />
                <button 
                    type="submit" 
                    disabled={!inputMessage.trim() || isStreaming}
                    className="chat-button"
                >
                    {isStreaming ? 'Sending...' : 'Send'}
                </button>
            </form>
        </div>
    );
}

export default App;