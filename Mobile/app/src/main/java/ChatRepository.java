package com.example.mobile;

import java.util.ArrayList;
import java.util.List;

public class ChatRepository {
    private static ChatRepository instance;
    private List<Message> conversationWithWillian;

    private ChatRepository() {
        conversationWithWillian = new ArrayList<>();

    }

    public static synchronized ChatRepository getInstance() {
        if (instance == null) {
            instance = new ChatRepository();
        }
        return instance;
    }

    public List<Message> getConversationWithWillian() {
        return conversationWithWillian;
    }

    public Message getLastMessageWithWillian() {
        if (conversationWithWillian.isEmpty()) {
            return null;
        }
        return conversationWithWillian.get(conversationWithWillian.size() - 1);
    }

    public void addMessageToWillian(Message message) {
        conversationWithWillian.add(message);
    }
}