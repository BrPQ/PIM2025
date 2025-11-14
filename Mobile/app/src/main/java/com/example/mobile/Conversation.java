package com.example.mobile;

public class Conversation {
    private int ticketId;
    private String ticketTitle;
    private String professionalName;


    public Conversation(int ticketId, String ticketTitle, String professionalName) {
        this.ticketId = ticketId;
        this.ticketTitle = ticketTitle;
        this.professionalName = professionalName;
    }

    public int getTicketId() {
        return ticketId;
    }

    public String getTicketTitle() {
        return ticketTitle;
    }

    public String getProfessionalName() {
        return professionalName;
    }
}