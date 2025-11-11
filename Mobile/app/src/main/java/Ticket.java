package com.example.mobile;

import com.google.gson.annotations.SerializedName;
import java.io.Serializable;

public class Ticket implements Serializable {

    // A anotação agora procura pelo campo "id", que é o que sua API envia.
    @SerializedName("id")
    private int chamadoId;

    @SerializedName("titulo")
    private String titulo;

    @SerializedName("descricao")
    private String descricao;

    @SerializedName("status")
    private String status;

    @SerializedName("profissionalDesignado")
    private String profissionalDesignado;

    // --- CORREÇÃO APLICADA AQUI ---
    // Removido o hífen do nome da anotação.
    @SerializedName("solucao")
    private String solucao;
    // -----------------------------

    // Getters
    public int getChamadoId() { return chamadoId; }
    public String getTitulo() { return titulo; }
    public String getDescription() { return descricao; }
    public String getStatus() { return status; }
    public String getProfessionalName() { return profissionalDesignado; }
    public String getSolucao() { return solucao; }
}