import { useState } from 'react';
import './App.css';
import Chat from './Chat';

function App() {
    const [currentPage, setCurrentPage] = useState<'home' | 'chat'>('home');

    if (currentPage === 'chat') {
        return (
            <div className="app">
                <Chat />
            </div>
        );
    }

    return (
        <div className="app">
            <div className="landing-page">
                <h1>EasyReasy Knowledge Base</h1>
                <button 
                    className="chat-nav-button"
                    onClick={() => setCurrentPage('chat')}
                >
                    Chat
                </button>
            </div>
        </div>
    );
}

export default App;