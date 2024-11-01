let currentContact = null;
let socketId = null;
let currentConversationChannel = null;
let conversationChannelName = null;

const newMessageTpl = `
    <div class="message {{messageClass}}" id="msg-{{id}}">
        <p>{{body}}</p>
        <p class="delivery-status"> * Delivered</p>
    </div>`;

// Pusher client setup
const pusher = new Pusher('c38879c0eb230cbf0252', { cluster: 'eu' });

function getConvoChannel(user_id, contact_id) {
    return user_id > contact_id ? `private-chat-${contact_id}-${user_id}` : `private-chat-${user_id}-${contact_id}`;
}

function bindClientEvents() {
    if (!currentConversationChannel) {
        console.warn('No current conversation channel found.');
        return;
    }

    currentConversationChannel.bind("new_message", function (msg) {
        console.log("New message received: ", msg);
        if (msg.sender_id === currentUserId && msg.receiver_id === currentContact.id) {
            return;
        }

        displayMessage(msg);
    });

    currentConversationChannel.bind("message_delivered", function (msg) {
        $('#msg-' + msg.id).find('.delivery-status').show();
    });

    currentConversationChannel.bind("client-is-typing", function (data) {
        if (data.user_id == currentContact.id && data.contact_id == currentUserId) {
            $('#typerDisplay').text(`${currentContact.name} is typing...`).fadeIn(100).delay(1000).fadeOut(300);
        }
    });
}

// Select contact to chat with
$('.user-item').click(function (e) {
    e.preventDefault();

    currentContact = {
        id: $(this).data('contact-id'),
        name: $(this).data('contact-name')
    };

    if (conversationChannelName) {
        pusher.unsubscribe(conversationChannelName);
    }

    conversationChannelName = getConvoChannel(currentUserId, currentContact.id);
    currentConversationChannel = pusher.subscribe(conversationChannelName);

    bindClientEvents();

    $('#contacts').find('.user-item').removeClass('active');

    $(this).addClass('active');

    $('#contact-name').text(currentContact.name);

    getChat(currentContact.id);
});



function getChat(contact_id) {
    $.get(`/chat/ConversationWithContact?contact=${contact_id}`)
        .done(function (resp) {
            const chatData = resp.data || [];
            loadChat(chatData);
        });
}

function loadChat(chatData) {
    $('#chat-messages').empty();
    chatData.forEach(displayMessage);
}

function displayMessage(messageObj) {
    const isCurrentUser = messageObj.sender_id == currentUserId;
    const messageClass = isCurrentUser ? 'from__chat' : 'receive__chat';
    const template = newMessageTpl.replace("{{id}}", messageObj.id)
        .replace("{{body}}", messageObj.message)
        .replace("{{messageClass}}", messageClass);

    const messageElement = $(template);

    messageElement.find('.__chat__').addClass(messageObj.sender_id == currentUserId ? 'from__chat' : 'receive__chat');
    if (messageObj.status == 1) {
        messageElement.find('.delivery-status').show();
    }

    $('#chat-messages').append(messageElement);

    const chatMessages = $('#chat-messages');
    chatMessages.scrollTop(chatMessages[0].scrollHeight);
}


// Send button's click event
$('#sendMessage').click(function () {
    const message = $('#msg_box').val();
    if (!message || !currentContact) return;

    $.post("/chat/SendMessage", {
        message: message,
        contact: currentContact.id,
        socket_id: socketId,
    }).done(function (data) {
        if (data.status === 'success') {
            // If sending was successful, display the new message
            displayMessage(data.data);
        } else {
            console.error(data.message);
        }
        $('#msg_box').val(''); // Clear the message box
    }).fail(function (jqXHR, textStatus, errorThrown) {
        console.error("Failed to send message: ", textStatus, errorThrown);
    });
});

// User is typing
const isTypingCallback = function () {
    if (currentConversationChannel) {
        currentConversationChannel.trigger("client-is-typing", {
            user_id: currentUserId,
            contact_id: currentContact.id,
        });
    }
};

$('#msg_box').on('keyup', isTypingCallback);
