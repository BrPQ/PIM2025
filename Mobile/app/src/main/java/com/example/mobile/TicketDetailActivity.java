package com.example.mobile;

import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

public class TicketDetailActivity extends AppCompatActivity {

    private Ticket currentTicket;
    private RequestQueue requestQueue;

    // IDs de componentes que presumo existirem no seu layout activity_ticket_detail.xml
    private TextView buttonTicketNumber; // Usando TextView pois parece não ser um botão clicável
    private TextView textViewDescription;
    private Button buttonEdit;
    private Button buttonDelete;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_ticket_detail);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);
        getSupportActionBar().setTitle("Detalhes do Ticket");

        requestQueue = Volley.newRequestQueue(this);

        // Conectar os componentes da UI
        buttonTicketNumber = findViewById(R.id.buttonTicketNumber);
        textViewDescription = findViewById(R.id.textViewDescription);
        buttonEdit = findViewById(R.id.buttonEdit);
        buttonDelete = findViewById(R.id.buttonDelete);

        // Recebe o objeto Ticket da HomeActivity
        currentTicket = (Ticket) getIntent().getSerializableExtra("TICKET_OBJECT");

        if (currentTicket == null) {
            Toast.makeText(this, "Erro ao carregar o ticket.", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }

        // --- CORREÇÃO APLICADA AQUI ---
        // Preenche a UI com os dados do ticket, usando o método correto getChamadoId()
        buttonTicketNumber.setText("TICKET#" + currentTicket.getChamadoId());
        textViewDescription.setText(currentTicket.getDescription());

        // Configura o clique do botão Editar
        buttonEdit.setOnClickListener(v -> {
            Intent intent = new Intent(TicketDetailActivity.this, EditTicketActivity.class);
            // Passa o objeto Ticket inteiro para a tela de edição
            intent.putExtra("TICKET_OBJECT", currentTicket);
            startActivityForResult(intent, 1); // Usamos startActivityForResult para saber quando a edição terminou
        });

        // Configura o clique do botão Deletar
        buttonDelete.setOnClickListener(v -> {
            showDeleteConfirmationDialog();
        });
    }

    private void showDeleteConfirmationDialog() {
        new AlertDialog.Builder(this)
                .setTitle("Excluir Chamado")
                .setMessage("Você tem certeza que deseja excluir este chamado? Esta ação não pode ser desfeita.")
                .setPositiveButton("Sim, excluir", (dialog, which) -> deleteTicketFromApi())
                .setNegativeButton("Não", null)
                .setIcon(android.R.drawable.ic_dialog_alert)
                .show();
    }

    private void deleteTicketFromApi() {
        if (currentTicket == null) return;

        // --- LÓGICA DE API SUBSTITUINDO O TicketRepository ---
        String url = ApiConfig.BASE_URL + "/api/Tickets/" + currentTicket.getChamadoId();

        StringRequest deleteRequest = new StringRequest(Request.Method.DELETE, url,
                response -> {
                    Toast.makeText(this, "Ticket excluído com sucesso!", Toast.LENGTH_LONG).show();
                    setResult(Activity.RESULT_OK); // Informa à HomeActivity que uma alteração foi feita
                    finish(); // Fecha a tela de detalhes
                },
                error -> {
                    Toast.makeText(this, "Falha ao excluir o ticket.", Toast.LENGTH_SHORT).show();
                    Log.e("DELETE_TICKET_API", "Erro: " + error.toString());
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
        };

        requestQueue.add(deleteRequest);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        // Se a tela de edição retornou "OK", significa que algo mudou.
        if (requestCode == 1 && resultCode == Activity.RESULT_OK) {
            setResult(Activity.RESULT_OK); // Repassa o "OK" para a HomeActivity
            finish(); // Fecha a tela de detalhes para forçar a Home a recarregar tudo
        }
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }
}