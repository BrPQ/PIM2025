package com.example.mobile;

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;
import android.graphics.Color;
import android.os.Bundle;
import android.util.Log;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;
import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.Volley;
import com.google.android.material.button.MaterialButton;
import com.google.android.material.chip.Chip;
import com.google.gson.Gson;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class AcceptedTicketDetailActivity extends AppCompatActivity {

    private RequestQueue requestQueue;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_accepted_ticket_detail);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);

        requestQueue = Volley.newRequestQueue(this);
        Ticket ticket = (Ticket) getIntent().getSerializableExtra("TICKET_OBJECT");

        if (ticket == null) {
            Toast.makeText(this, "Erro ao carregar detalhes.", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }

        MaterialButton buttonTicketNumber = findViewById(R.id.buttonTicketNumber);
        Chip chipProfessional = findViewById(R.id.chipProfessional);
        Chip chipStatus = findViewById(R.id.chipStatus);
        TextView textViewDescription = findViewById(R.id.textViewDescription);

        buttonTicketNumber.setText("TICKET#" + ticket.getChamadoId());

        // --- CORREÇÃO APLICADA AQUI ---
        // Usamos o nome correto do método: getProfessionalName()
        chipProfessional.setText(ticket.getProfessionalName());
        // -----------------------------

        chipStatus.setText(ticket.getStatus());
        textViewDescription.setText(ticket.getDescription());

        carregarAnexos(ticket.getChamadoId());
    }

    private void carregarAnexos(int ticketId) {
        String url = ApiConfig.BASE_URL + "/api/Anexos/" + ticketId + "?tipoAnexo=Usuario";
        JsonArrayRequest jsonArrayRequest = new JsonArrayRequest(Request.Method.GET, url, null,
                response -> {
                    Gson gson = new Gson();
                    Anexo[] anexoArray = gson.fromJson(response.toString(), Anexo[].class);
                    List<Anexo> listaDeAnexos = Arrays.asList(anexoArray);
                    LinearLayout anexosContainer = findViewById(R.id.anexos_container);
                    anexosContainer.removeAllViews();
                    if (listaDeAnexos.isEmpty()) {
                        TextView semAnexosView = new TextView(this);
                        semAnexosView.setText("Nenhum anexo enviado pelo usuário.");
                        semAnexosView.setTextColor(Color.parseColor("#BDBDBD"));
                        anexosContainer.addView(semAnexosView);
                    } else {
                        for (Anexo anexo : listaDeAnexos) {
                            TextView anexoTextView = new TextView(this);
                            anexoTextView.setText("📎 " + anexo.getNomeArquivo());
                            anexoTextView.setTextColor(Color.WHITE);
                            anexoTextView.setTextSize(16);
                            anexoTextView.setPadding(12, 12, 12, 12);
                            anexosContainer.addView(anexoTextView);
                        }
                    }
                },
                error -> {
                    Log.e("CarregarAnexos", "Erro: " + error.toString());
                }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                Map<String, String> headers = new HashMap<>();
                String token = SessionManager.getInstance().getAuthToken();
                if (token != null) { headers.put("Authorization", "Bearer " + token); }
                return headers;
            }
        };
        requestQueue.add(jsonArrayRequest);
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }
}