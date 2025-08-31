import { useState, useEffect } from 'react';
import { ChevronLeft, ChevronRight, Menu, LogOut, MessageSquare, BookOpen, Upload } from 'feather-icons-react';
import EasyReasyIcon from './assets/icons/easy-reasy-icon-32.png';
import { authUtils } from './utils/auth';

interface KnowledgeBaseProps {
    onLogout: () => void;
    onNavigateToChat: () => void;
}

function KnowledgeBase({ onLogout, onNavigateToChat }: KnowledgeBaseProps) {
    const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(() => {
        // Check if we're on mobile (screen width <= 768px)
        return window.innerWidth <= 768;
    });
    const [isEasyReasyHovered, setIsEasyReasyHovered] = useState(false);

    // Handle window resize to adjust sidebar state
    useEffect(() => {
        const handleResize = () => {
            const isMobile = window.innerWidth <= 768;
            setIsSidebarCollapsed(isMobile);
        };

        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
    }, []);

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
                        <div className="nav-item" onClick={onNavigateToChat}>
                            <span className="nav-icon">
                                <MessageSquare size={18} />
                            </span>
                            {!isSidebarCollapsed && <span>New chat</span>}
                        </div>
                        <div className="nav-item active">
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
                            <h3>Recent</h3>
                            <div className="chat-history-item">
                                <span>Document analysis results</span>
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
                        <span>This is a knowledge base demo for EasyReasy.KnowledgeBase</span>
                    </div>
                )}
            </div>

            {/* Main Knowledge Base Area */}
            <div className="chat-main">
                <div className="knowledge-base-content">
                    <div className="knowledge-base-inner">
                        <div className="knowledge-base-header">
                            <h1>Knowledge Base</h1>
                            <p>Manage and explore your knowledge repository</p>
                        </div>
                        
                        <div className="upload-zone">
                            <div className="upload-zone-content">
                                <Upload size={48} />
                                <h3>Upload Documents</h3>
                                <p>Drag and drop files here, or click to browse</p>
                                <button className="upload-button">Choose Files</button>
                            </div>
                        </div>
                        
                        <div className="knowledge-base-sections">
                            <div className="kb-section">
                                <div className="kb-section-header">
                                    <BookOpen size={24} />
                                    <h2>Documents</h2>
                                </div>
                                <p>Browse and manage your indexed documents</p>
                                <button className="kb-section-button">View Documents</button>
                            </div>
                            
                            <div className="kb-section">
                                <div className="kb-section-header">
                                    <BookOpen size={24} />
                                    <h2>Search</h2>
                                </div>
                                <p>Search through your knowledge base</p>
                                <button className="kb-section-button">Search Knowledge Base</button>
                            </div>
                            
                            <div className="kb-section">
                                <div className="kb-section-header">
                                    <BookOpen size={24} />
                                    <h2>Manage</h2>
                                </div>
                                <p>Organize and maintain your knowledge base</p>
                                <button className="kb-section-button">Manage Settings</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default KnowledgeBase;
