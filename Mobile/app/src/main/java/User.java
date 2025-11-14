package com.example.mobile;

import com.google.gson.annotations.SerializedName; // <-- IMPORTANTE: Adicione este import
import java.io.Serializable;

public class User implements Serializable {

    // A anotação @SerializedName diz ao Gson qual campo do JSON corresponde a esta variável.
    @SerializedName("id")
    private int usuarioId;

    @SerializedName("nomeUsuario")
    private String nome;

    @SerializedName("matricula")
    private String matricula;

    @SerializedName("role")
    private String perfil;


    public int getId() {
        return usuarioId;
    }

    public String getNome() {
        return nome;
    }

    public String getMatricula() {
        return matricula;
    }

    public String getPerfil() {
        return perfil;
    }
}