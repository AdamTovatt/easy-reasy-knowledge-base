import { useState, useRef, useEffect, useCallback } from 'react';
import { ArrowUp, Pause, AlertTriangle, MessageSquare, BookOpen, ChevronLeft, ChevronRight, Menu, LogOut } from 'feather-icons-react';
import { PuffLoader } from 'react-spinners';
import ReactMarkdown from 'react-markdown';
import EasyReasyIcon from './assets/icons/easy-reasy-icon-32.png';
import { authUtils } from './utils/auth';
import { ServiceHealthNotification } from './components/ServiceHealthNotification';

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

const BotIcon = () => (
    <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        width: '20px',
        height: '20px',
        transform: 'translateY(2px)' // Adjust this value as needed
    }}>
        <img src={EasyReasyIcon} alt="EasyReasy Bot" style={{ width: '100%', height: '100%' }} />
    </div>
);

const ThinkingIcon = () => (
    <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        width: '20px',
        height: '20px',
        transform: 'translateY(-2px) translateX(-5px)'
    }}>
        <PuffLoader size={14} color="var(--brand-color-light)" />
    </div>
);

interface StreamChatResponse {
    message?: string;
    error?: string;
}

function Chat({ onLogout }: { onLogout: () => void }) {
    const [inputMessage, setInputMessage] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [chatHistory, setChatHistory] = useState<string[]>([]);
    const [currentResponse, setCurrentResponse] = useState('');
    const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
    const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(() => {
        // Check if we're on mobile (screen width <= 768px)
        return window.innerWidth <= 768;
    });
    const [isEasyReasyHovered, setIsEasyReasyHovered] = useState(false);
    const chatContainerRef = useRef<HTMLDivElement>(null);
    const abortControllerRef = useRef<AbortController | null>(null);

    useEffect(() => {
        if (chatContainerRef.current && shouldAutoScroll) {
            chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
        }
    }, [chatHistory, currentResponse, shouldAutoScroll]);

    const handleScroll = () => {
        if (chatContainerRef.current) {
            const { scrollTop, scrollHeight, clientHeight } = chatContainerRef.current;
            const isAtBottom = scrollTop + clientHeight >= scrollHeight - 10; // 10px threshold
            setShouldAutoScroll(isAtBottom);
        }
    };

    // Set initial textarea height on mount
    useEffect(() => {
        const textarea = document.querySelector('.chat-input') as HTMLTextAreaElement;
        if (textarea) {
            textarea.style.height = 'auto';
            textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
        }
    }, []);

    // Handle window resize to adjust sidebar state
    useEffect(() => {
        const handleResize = () => {
            const isMobile = window.innerWidth <= 768;
            setIsSidebarCollapsed(isMobile);
        };

        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
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
        
        // Reset auto-scroll when user sends a new message
        setShouldAutoScroll(true);

        // Create abort controller for this request
        abortControllerRef.current = new AbortController();

        try {
            const authHeader = authUtils.getAuthHeader();
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...authHeader,
                },
                body: JSON.stringify({ message: userMessage }),
                signal: abortControllerRef.current.signal,
            });

            if (!response.ok) {
                if (response.status === 401) {
                    // Token expired or invalid
                    authUtils.clearToken();
                    onLogout();
                    throw new Error('Session expired. Please log in again.');
                } else {
                    // Try to extract error message from response body
                    let errorMessage = `HTTP error! status: ${response.status}`;
                    try {
                        const errorText = await response.text();
                        if (errorText) {
                            const errorData = JSON.parse(errorText);
                            if (errorData.error) {
                                errorMessage = errorData.error;
                            }
                        }
                    } catch (parseError) {
                        // If we can't parse the response or there's no error field, use the default message
                        console.log('Could not parse error response:', parseError);
                    }
                    throw new Error(errorMessage);
                }
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
            console.log('Error name:', error instanceof Error ? error.name : 'Unknown');
            
            // Don't add error messages for aborted requests since we handle them in handleCancelStream
            if (!(error instanceof Error && error.name === 'AbortError')) {
                console.log('Adding error message to chat history');
                setChatHistory(prev => [...prev, `Error: ${error instanceof Error ? error.message : 'Unknown error'}`]);
            } else {
                console.log('AbortError detected, not adding error message');
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
            
            // Add interruption message
            setChatHistory(prev => [...prev, `Error: Request was interrupted`]);
            
            setCurrentResponse('');
            abortControllerRef.current = null;
        }
    };

    const toggleSidebar = () => {
        setIsSidebarCollapsed(!isSidebarCollapsed);
    };

    const handleLogout = () => {
        authUtils.clearToken();
        onLogout();
    };

    return (
        <div className="chat-layout">
            {/* Mobile Menu Button */}
            <button 
                className={`mobile-menu-button mobile-only ${!isSidebarCollapsed ? 'hidden' : ''}`}
                onClick={toggleSidebar}
            >
                <Menu size={24} />
            </button>
             
             {/* Left Sidebar */}
                                       <div className={`chat-sidebar ${isSidebarCollapsed ? 'collapsed' : ''}`}>
                 <div className="sidebar-top">
                     <button 
                         className="top-nav-button"
                         onMouseEnter={() => setIsEasyReasyHovered(true)}
                         onMouseLeave={() => setIsEasyReasyHovered(false)}
                         onClick={isSidebarCollapsed ? toggleSidebar : undefined}
                     >
                         {isSidebarCollapsed && isEasyReasyHovered ? (
                             <ChevronRight size={24} />
                         ) : (
                             <img src={EasyReasyIcon} alt="EasyReasy Bot" style={{ width: '24px', height: '24px' }} />
                         )}
                     </button>
                     {!isSidebarCollapsed && (
                         <button className="top-nav-button" onClick={toggleSidebar}>
                             <ChevronLeft size={24} />
                         </button>
                     )}
                 </div>
                 
                                   <div className="sidebar-header">
                      <div className="sidebar-nav">
                          <div className="nav-item">
                              <span className="nav-icon">
                                  <MessageSquare size={18} />
                              </span>
                              {!isSidebarCollapsed && <span>New chat</span>}
                          </div>
                          <div className="nav-item">
                              <span className="nav-icon">
                                  <BookOpen size={18} />
                              </span>
                              {!isSidebarCollapsed && <span>Knowledge Base</span>}
                          </div>
                      </div>
                  </div>
                  
                  
                 
                                                                                      {!isSidebarCollapsed && (
                       <div className="sidebar-content">
                            <div className="today-section">
                                <h3>Today</h3>
                                <div className="chat-history-item">
                                    <span>Checking GitHub repositories availab...</span>
                                    <span className="dots">⋯</span>
                                </div>
                            </div>
                        </div>
                   )}
                   
                   {!isSidebarCollapsed && (
                       <div className="sidebar-header">
                           <div className="sidebar-nav">
                               <div className="nav-item" onClick={handleLogout}>
                                   <span className="nav-icon">
                                       <LogOut size={18} />
                                   </span>
                                   <span>Logout</span>
                               </div>
                           </div>
                       </div>
                   )}
                  
                  
                 
                 {!isSidebarCollapsed && (
                     <div className="sidebar-footer">
                         <span>This is a chat demo for EasyReasy.KnowledgeBase</span>
                     </div>
                 )}
             </div>

            {/* Main Chat Area */}
            <div className="chat-main">
                <div className="chat-messages" ref={chatContainerRef} onScroll={handleScroll}>
                    <div className="messages-container">
                        {/* Service Health Notification */}
                        <ServiceHealthNotification 
                            showOnlyWhenUnhealthy={false}
                            autoRefresh={true}
                            refreshIntervalMs={30000}
                            className="chat-health-notification"
                            onLogout={onLogout}
                        />
                        
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
                                                 <span className="ai-icon">
                                                     <BotIcon />
                                                 </span>
                                             )}
                                             <span>
                                                 <ReactMarkdown
                                                     components={{
                                                         p: ({ children }) => <span>{children}</span>
                                                     }}
                                                 >
                                                     {message.replace('AI: ', '').replace('Error: ', '')}
                                                 </ReactMarkdown>
                                             </span>
                                         </>
                                     )}
                                </div>
                            );
                        })}
                                                 {isStreaming && (
                             <div className="message ai-message">
                                 <span className="ai-icon">
                                     {currentResponse ? <BotIcon /> : <ThinkingIcon />}
                                 </span>
                                 <span>
                                     {currentResponse ? (
                                         <ReactMarkdown
                                             components={{
                                                 p: ({ children }) => <span>{children}</span>
                                             }}
                                         >
                                             {currentResponse}
                                         </ReactMarkdown>
                                     ) : (
                                         'Thinking...'
                                     )}
                                 </span>
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

