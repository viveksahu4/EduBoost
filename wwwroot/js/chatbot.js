/**
 * EduBoost 24/7 AI Assistant Client Script
 * Connects to /api/chatbot/message (powered by Google Gemini API)
 */
(function () {
    'use strict';

    var launcherBtn = document.getElementById('chatLauncherBtn');
    var chatWindow = document.getElementById('chatBoxWindow');
    var closeBtn = document.getElementById('closeChatBtn');
    var clearBtn = document.getElementById('clearChatBtn');
    var chatForm = document.getElementById('chatForm');
    var chatInput = document.getElementById('chatInputMessage');
    var chatSendBtn = document.getElementById('chatSendBtn');
    var messagesArea = document.getElementById('chatMessagesArea');
    var typingIndicator = document.getElementById('chatTypingIndicator');
    var suggestions = document.getElementById('chatSuggestions');

    if (!launcherBtn || !chatWindow) return;

    var conversationHistory = [];
    var isOpen = false;

    // Toggle chat window open / close
    function toggleChat(open) {
        if (typeof open === 'boolean') {
            isOpen = open;
        } else {
            isOpen = !isOpen;
        }

        if (isOpen) {
            chatWindow.classList.remove('d-none');
            launcherBtn.querySelector('.icon-chat').classList.add('d-none');
            launcherBtn.querySelector('.icon-close').classList.remove('d-none');
            scrollToBottom();
            setTimeout(function () { chatInput.focus(); }, 100);
        } else {
            chatWindow.classList.add('d-none');
            launcherBtn.querySelector('.icon-chat').classList.remove('d-none');
            launcherBtn.querySelector('.icon-close').classList.add('d-none');
        }
    }

    launcherBtn.addEventListener('click', function () {
        toggleChat();
    });

    closeBtn.addEventListener('click', function () {
        toggleChat(false);
    });

    // Clear chat
    clearBtn.addEventListener('click', function () {
        if (!confirm('Clear chat conversation?')) return;
        conversationHistory = [];
        messagesArea.innerHTML = `
            <div class="chat-msg bot-msg">
                <div class="msg-bubble">
                    <p class="mb-1"><strong>Conversation reset! ✨</strong></p>
                    <p class="mb-0 small text-muted">How can I assist you right now? Ask any question regarding EduBoost or teaching strategies.</p>
                </div>
                <div class="msg-time">Just now</div>
            </div>
        `;
        if (suggestions) {
            messagesArea.appendChild(suggestions);
        }
        scrollToBottom();
    });

    // Handle suggestion chips
    document.addEventListener('click', function (e) {
        var chip = e.target.closest('.suggestion-chip');
        if (chip) {
            var prompt = chip.getAttribute('data-prompt');
            if (prompt) {
                chatInput.value = prompt;
                sendMessage(prompt);
            }
        }
    });

    // Form submit
    chatForm.addEventListener('submit', function (e) {
        e.preventDefault();
        var msg = chatInput.value.trim();
        if (msg) {
            sendMessage(msg);
        }
    });

    function sendMessage(text) {
        if (!text) return;

        // Append user bubble
        appendMessage('user', text);
        chatInput.value = '';
        chatInput.disabled = true;
        chatSendBtn.disabled = true;

        // Hide suggestions once chat starts
        if (suggestions && suggestions.parentNode) {
            suggestions.style.display = 'none';
        }

        // Show typing indicator
        typingIndicator.classList.remove('d-none');
        scrollToBottom();

        // Send payload to backend
        var payload = {
            message: text,
            history: conversationHistory
        };

        fetch('/api/chatbot/message', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        })
            .then(function (res) {
                return res.json();
            })
            .then(function (data) {
                typingIndicator.classList.add('d-none');
                chatInput.disabled = false;
                chatSendBtn.disabled = false;
                chatInput.focus();

                if (data && data.reply) {
                    appendMessage('bot', data.reply);
                    // Add to history
                    conversationHistory.push({ role: 'user', text: text });
                    conversationHistory.push({ role: 'model', text: data.reply });
                    if (conversationHistory.length > 20) {
                        conversationHistory = conversationHistory.slice(-20);
                    }
                } else {
                    var errorMsg = (data && data.error) ? data.error : 'Sorry, I could not process your request at the moment.';
                    appendMessage('bot', errorMsg);
                }
            })
            .catch(function (err) {
                typingIndicator.classList.add('d-none');
                chatInput.disabled = false;
                chatSendBtn.disabled = false;
                chatInput.focus();
                appendMessage('bot', '⚠️ Connection error: Unable to reach the EduBoost AI service. Please check your network or try again.');
                console.error('Chat error:', err);
            });
    }

    function appendMessage(sender, text) {
        var msgDiv = document.createElement('div');
        msgDiv.className = 'chat-msg ' + (sender === 'user' ? 'user-msg' : 'bot-msg');

        var bubble = document.createElement('div');
        bubble.className = 'msg-bubble';

        if (sender === 'user') {
            bubble.textContent = text;
        } else {
            bubble.innerHTML = formatMarkdown(text);
        }

        var timeDiv = document.createElement('div');
        timeDiv.className = 'msg-time';
        timeDiv.textContent = getCurrentTimeString();

        msgDiv.appendChild(bubble);
        msgDiv.appendChild(timeDiv);

        messagesArea.appendChild(msgDiv);
        scrollToBottom();
    }

    function scrollToBottom() {
        setTimeout(function () {
            messagesArea.scrollTop = messagesArea.scrollHeight;
        }, 50);
    }

    function getCurrentTimeString() {
        var now = new Date();
        var h = now.getHours();
        var m = now.getMinutes();
        var ampm = h >= 12 ? 'PM' : 'AM';
        h = h % 12;
        h = h ? h : 12;
        var sm = m < 10 ? '0' + m : m;
        return h + ':' + sm + ' ' + ampm;
    }

    // Basic markdown formatter for clean bullet points, bolding, code and paragraphs
    function formatMarkdown(text) {
        if (!text) return '';

        // Escape HTML
        var escaped = text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');

        // Bold **text**
        escaped = escaped.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

        // Italic *text*
        escaped = escaped.replace(/\*(.*?)\*/g, '<em>$1</em>');

        // Inline code `code`
        escaped = escaped.replace(/`([^`]+)`/g, '<code style="background:rgba(120,120,120,0.15);padding:2px 4px;border-radius:4px;font-size:0.85em;">$1</code>');

        // Lines and bullets
        var lines = escaped.split('\n');
        var html = [];
        var inList = false;

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i].trim();

            if (line.startsWith('- ') || line.startsWith('* ')) {
                if (!inList) {
                    html.push('<ul class="mb-2">');
                    inList = true;
                }
                html.push('<li>' + line.substring(2) + '</li>');
            } else if (/^\d+\.\s/.test(line)) {
                if (!inList) {
                    html.push('<ol class="mb-2">');
                    inList = true;
                }
                var itemText = line.replace(/^\d+\.\s/, '');
                html.push('<li>' + itemText + '</li>');
            } else {
                if (inList) {
                    html.push('</ul>');
                    inList = false;
                }
                if (line.length > 0) {
                    html.push('<p class="mb-1">' + line + '</p>');
                }
            }
        }

        if (inList) {
            html.push('</ul>');
        }

        return html.join('');
    }
})();
