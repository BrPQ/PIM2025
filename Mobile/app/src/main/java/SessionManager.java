package com.example.mobile;

import android.content.Context;
import android.content.SharedPreferences;

import com.google.gson.Gson;

public class SessionManager {

    private static final String PREF_NAME = "AppSession";
    private static final String KEY_USER = "user";
    private static final String KEY_TOKEN = "token";
    private static SessionManager instance;
    private final SharedPreferences pref;
    private final SharedPreferences.Editor editor;
    private final Gson gson = new Gson();

    private SessionManager(Context context) {
        pref = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE);
        editor = pref.edit();
    }

    public static synchronized SessionManager getInstance(Context context) {
        if (instance == null) {
            instance = new SessionManager(context.getApplicationContext());
        }
        return instance;
    }

    // Usado na MainActivity para obter a instância após o login
    public static synchronized SessionManager getInstance() {
        if (instance == null) {
            throw new IllegalStateException("SessionManager não foi inicializado. Chame getInstance(Context) primeiro.");
        }
        return instance;
    }

    public void login(User user, String token) {
        String userJson = gson.toJson(user);
        editor.putString(KEY_USER, userJson);
        editor.putString(KEY_TOKEN, token);
        editor.commit();
    }

    public boolean isLoggedIn() {
        return pref.getString(KEY_TOKEN, null) != null;
    }

    public User getLoggedInUser() {
        String userJson = pref.getString(KEY_USER, null);
        if (userJson != null) {
            return gson.fromJson(userJson, User.class);
        }
        return null;
    }

    public String getAuthToken() {
        return pref.getString(KEY_TOKEN, null);
    }

    public void logout() {
        editor.clear();
        editor.commit();
    }
}