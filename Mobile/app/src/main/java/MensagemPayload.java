package com.example.mobile; // Verifique seu nome de pacote

import com.google.gson.annotations.SerializedName;

public class MensagemPayload {

    @SerializedName("mensagemId")
    private int mensagemId;

    @SerializedName("ticketId")
    private int ticketId;

    @SerializedName("usuarioId")
    private int usuarioId;

    @SerializedName("nomeUsuario")
    private String nomeUsuario;

    @SerializedName("authorRole")
    private String authorRole;

    @SerializedName("conteudo")
    private String conteudo;

    @SerializedName("dataEnvio")
    private String dataEnvio;

    // --- Getters e Setters ---
    // (Necessários para o GSON/SignalR)

    public int getMensagemId() { return mensagemId; }
    public void setMensagemId(int mensagemId) { this.mensagemId = mensagemId; }

    public int getTicketId() { return ticketId; }
    public void setTicketId(int ticketId) { this.ticketId = ticketId; }

    public int getUsuarioId() { return usuarioId; }
    public void setUsuarioId(int usuarioId) { this.usuarioId = usuarioId; }

    public String getNomeUsuario() { return nomeUsuario; }
    public void setNomeUsuario(String nomeUsuario) { this.nomeUsuario = nomeUsuario; }

    public String getAuthorRole() { return authorRole; }
    public void setAuthorRole(String authorRole) { this.authorRole = authorRole; }

    public String getConteudo() { return conteudo; }
    public void setConteudo(String conteudo) { this.conteudo = conteudo; }

    public String getDataEnvio() { return dataEnvio; }
    public void setDataEnvio(String dataEnvio) { this.dataEnvio = dataEnvio; }
}