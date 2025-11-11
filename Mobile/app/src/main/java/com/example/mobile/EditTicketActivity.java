package com.example.mobile;

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONException;
import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

public class EditTicketActivity extends AppCompatActivity {

    private EditText editTextDescription;
    private Button buttonSalvar;
    private Ticket currentTicket;
    private RequestQueue requestQueue;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_edit_ticket);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);
        getSupportActionBar().setTitle("Editar Chamado");

        requestQueue = Volley.newRequestQueue(this);

        editTextDescription = findViewById(R.id.editTextDescription);
        buttonSalvar = findViewById(R.id.buttonSalvar);

        // Recebe o objeto Ticket inteiro, que é mais moderno e eficiente
        currentTicket = (Ticket) getIntent().getSerializableExtra("TICKET_OBJECT");

        if (currentTicket != null) {
            // Preenche o campo de texto com a descrição atual
            editTextDescription.setText(currentTicket.getDescription());
        } else {
            Toast.makeText(this, "Erro: Não foi possível carregar o ticket.", Toast.LENGTH_SHORT).show();
            finish(); // Fecha a tela se não houver ticket
            return;
        }

        buttonSalvar.setOnClickListener(v -> {
            String newDescription = editTextDescription.getText().toString().trim();

            if (newDescription.isEmpty()) {
                Toast.makeText(this, "A descrição não pode estar vazia.", Toast.LENGTH_SHORT).show();
                return;
            }

            // NOVO: Chama o método para salvar as alterações na API
            updateTicketOnApi(newDescription);
        });
    }

    // --- NOVO MÉTODO PARA ATUALIZAR VIA API ---
    private void updateTicketOnApi(String newDescription) {
        // Desabilita o botão para evitar cliques duplos
        buttonSalvar.setEnabled(false);
        buttonSalvar.setText("Salvando...");

        String url = ApiConfig.BASE_URL + "/api/Tickets/" + currentTicket.getChamadoId();

        // Monta o corpo da requisição PUT.
        // É importante reenviar todos os dados que a API espera para uma atualização.
        final JSONObject ticketData = new JSONObject();
        try {
            ticketData.put("chamadoId", currentTicket.getChamadoId());
            ticketData.put("titulo", currentTicket.getTitulo()); // Reenvia o título original
            ticketData.put("descricao", newDescription); // Envia a NOVA descrição
            ticketData.put("status", currentTicket.getStatus()); // Reenvia o status original
            ticketData.put("profissionalDesignado", currentTicket.getProfessionalName()); // Reenvia o profissional
            ticketData.put("solucao", currentTicket.getSolucao()); // Reenvia a solução
            // Adicione outros campos se sua API exigir
        } catch (JSONException e) {
            e.printStackTrace();
            resetButtonState();
            return;
        }

        // Usa StringRequest para a requisição PUT, que não espera uma resposta JSON
        StringRequest stringRequest = new StringRequest(Request.Method.PUT, url,
                response -> {
                    Toast.makeText(this, "Ticket atualizado com sucesso!", Toast.LENGTH_SHORT).show();
                    setResult(Activity.RESULT_OK); // Informa à tela anterior que a operação foi um sucesso
                    finish(); // Fecha a tela
                },
                error -> {
                    Toast.makeText(this, "Falha ao atualizar o ticket.", Toast.LENGTH_SHORT).show();
                    Log.e("UPDATE_TICKET_API", "Erro: " + error.toString());
                    resetButtonState();
                }
        ) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                Map<String, String> headers = new HashMap<>();
                String token = SessionManager.getInstance().getAuthToken();
                if (token != null && !token.isEmpty()) {
                    headers.put("Authorization", "Bearer " + token);
                }
                return headers;
            }

            @Override
            public String getBodyContentType() {
                return "application/json; charset=utf-8";
            }

            @Override
            public byte[] getBody() {
                return ticketData.toString().getBytes(StandardCharsets.UTF_8);
            }
        };

        requestQueue.add(stringRequest);
    }

    private void resetButtonState() {
        buttonSalvar.setEnabled(true);
        buttonSalvar.setText("Salvar Alterações");
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }
}