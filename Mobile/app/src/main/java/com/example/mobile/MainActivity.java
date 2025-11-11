package com.example.mobile;

import androidx.appcompat.app.AppCompatActivity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonObjectRequest;
import com.android.volley.toolbox.Volley;
import com.google.gson.Gson;
import org.json.JSONException;
import org.json.JSONObject;

public class MainActivity extends AppCompatActivity {

    private EditText editTextMatricula;
    private EditText editTextSenha;
    private Button buttonLogin;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        SessionManager.getInstance(this);
        if (SessionManager.getInstance().isLoggedIn()) {
            startActivity(new Intent(MainActivity.this, HomeActivity.class));
            finish();
            return;
        }
        setContentView(R.layout.activity_main);

        editTextMatricula = findViewById(R.id.editTextMatricula);
        editTextSenha = findViewById(R.id.editTextSenha);
        buttonLogin = findViewById(R.id.buttonLogin);

        buttonLogin.setOnClickListener(v -> {
            String matricula = editTextMatricula.getText().toString().trim();
            String senha = editTextSenha.getText().toString().trim();
            if (matricula.isEmpty() || senha.isEmpty()) {
                Toast.makeText(MainActivity.this, "Por favor, preencha todos os campos.", Toast.LENGTH_SHORT).show();
            } else {
                performLogin(matricula, senha);
            }
        });
    }

    private void performLogin(String matricula, String senha) {
        // --- ANIMAÇÃO INICIA AQUI ---
        buttonLogin.setEnabled(false);
        buttonLogin.setText("Entrando...");
        // --------------------------

        String url = ApiConfig.BASE_URL + "/api/auth/login";
        RequestQueue queue = Volley.newRequestQueue(this);

        JSONObject loginData = new JSONObject();
        try {
            loginData.put("matricula", matricula);
            loginData.put("senha", senha);
        } catch (JSONException e) { e.printStackTrace(); }

        JsonObjectRequest jsonObjectRequest = new JsonObjectRequest(Request.Method.POST, url, loginData,
                response -> {
                    Log.d("API_LOGIN_RESPONSE", response.toString());
                    try {
                        Gson gson = new Gson();
                        LoginResponse loginResponse = gson.fromJson(response.toString(), LoginResponse.class);
                        if (loginResponse != null && loginResponse.getUsuario() != null && loginResponse.getToken() != null) {
                            SessionManager.getInstance().login(loginResponse.getUsuario(), loginResponse.getToken());
                            Intent intent = new Intent(MainActivity.this, HomeActivity.class);
                            startActivity(intent);
                            finish();
                            // Não precisamos resetar o botão aqui, pois a tela será fechada
                        } else {
                            Toast.makeText(MainActivity.this, "Resposta inválida do servidor.", Toast.LENGTH_SHORT).show();
                            resetButtonState(); // Reseta o botão em caso de erro de parsing
                        }
                    } catch (Exception e) {
                        Toast.makeText(MainActivity.this, "Erro ao processar a resposta.", Toast.LENGTH_SHORT).show();
                        resetButtonState(); // Reseta o botão em caso de erro de parsing
                    }
                },
                error -> {
                    Toast.makeText(MainActivity.this, "Matrícula ou senha inválida.", Toast.LENGTH_SHORT).show();
                    resetButtonState(); // Reseta o botão em caso de erro da API
                });

        queue.add(jsonObjectRequest);
    }

    // --- NOVO MÉTODO PARA RESTAURAR O BOTÃO ---
    private void resetButtonState() {
        buttonLogin.setEnabled(true);
        buttonLogin.setText("Entrar");
    }
}