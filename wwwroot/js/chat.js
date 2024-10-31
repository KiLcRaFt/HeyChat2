let currentContact = null;
let socketId = null;
let currentConversationChannel = null;
let conversationChannelName = null;

const newMessageTpl = `
            <div>
                <div id="msg-{{id}}" class="row __chat__par__">
                    <div class="__chat__">
                        <p>{{body}}</p>
                        <p class="delivery-status">Delivered</p>
                    </div>
                </div>
            </div>`;

// Pusher client setup
const pusher = new Pusher('1887209', { cluster: 'eu' });

pusher.connection.bind('connected', function () {
    socketId = pusher.connection.socket_id;
});

function getConvoChannel(user_id, contact_id) {
    return user_id > contact_id ? `private-chat-${contact_id}-${user_id}` : `private-chat-${user_id}-${contact_id}`;
}

function bindClientEvents() {
    currentConversationChannel.bind("new_message", function (msg) {
        if (msg.receiver_id == currentUserId) {
            displayMessage(msg);
        }
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
    $('#contacts').find('li').removeClass('active');
    $(this).addClass('active');

    $('#contact-name').text(currentContact.name); // Отображаем выбранное имя

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
    const template = newMessageTpl.replace("{{id}}", messageObj.id).replace("{{body}}", messageObj.message);
    const messageElement = $(template);

    messageElement.find('.__chat__').addClass(messageObj.sender_id == currentUserId ? 'from__chat' : 'receive__chat');
    if (messageObj.status == 1) {
        messageElement.find('.delivery-status').show();
    }

    $('#chat-messages').append(messageElement);
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
        displayMessage(data);
        $('#msg_box').val(''); // Clear message box
    });
});

// User is typing
const isTypingCallback = function () {
    currentConversationChannel.trigger("client-is-typing", {
        user_id: currentUserId,
        contact_id: currentContact.id,
    });
};

$('#msg_box').on('keyup', isTypingCallback);