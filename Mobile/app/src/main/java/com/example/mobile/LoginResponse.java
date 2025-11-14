package com.example.mobile;

import com.google.gson.annotations.SerializedName;

public class LoginResponse {

    @SerializedName("token")
    private String token;

    @SerializedName("usuario")
    private User usuario;

    public String getToken() {
        return token;
    }

    public User getUsuario() {
        return usuario;
    }
}