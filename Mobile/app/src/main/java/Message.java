package com.example.mobile;

public class Message {
    private String content;
    private String authorName;
    private String authorRole;

    public Message(String content, String authorName, String authorRole) {
        this.content = content;
        this.authorName = authorName;
        this.authorRole = authorRole;
    }

    public String getContent() {
        return content;
    }

    public String getAuthorName() {
        return authorName;
    }

    public String getAuthorRole() {
        return authorRole;
    }
}