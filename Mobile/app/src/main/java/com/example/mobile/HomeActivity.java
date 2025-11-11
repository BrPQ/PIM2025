package com.example.mobile;

import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.RecyclerView;
import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.ImageButton;
import android.widget.TextView;
import android.widget.Toast;
import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.Volley;
import com.google.android.material.bottomnavigation.BottomNavigationView;
import com.google.android.material.chip.Chip;
import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.google.gson.Gson;
import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;
import com.microsoft.signalr.HubConnectionState;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture; // (Necessário para o token)
import io.reactivex.Single; // (Necessário para o token)


public class HomeActivity extends AppCompatActivity {

    private RecyclerView recyclerViewTickets;
    private TicketAdapter ticketAdapter;
    private Chip chipAberto, chipAceito;
    private BottomNavigationView bottomNavigationView;
    private FloatingActionButton fabChat;
    private TextView textViewUserName;
    private List<Ticket> allUserTickets = new ArrayList<>();
    private HubConnection hubConnection;

    private ImageButton buttonNotifications;
    private ArrayList<String> notificationMessages = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_home);

        chipAberto = findViewById(R.id.chipAberto);
        chipAceito = findViewById(R.id.chipAceito);
        bottomNavigationView = findViewById(R.id.bottom_navigation);
        fabChat = findViewById(R.id.fabChat);
        textViewUserName = findViewById(R.id.textViewUserName);

        buttonNotifications = findViewById(R.id.buttonNotifications);
        buttonNotifications.setOnClickListener(v -> showNotificationDialog());

        setupRecyclerView();
        iniciarConexaoSignalR(); // <-- Este método foi MODIFICADO

        chipAberto.setOnClickListener(v -> showOpenTickets());
        chipAceito.setOnClickListener(v -> showAcceptedTickets());

        bottomNavigationView.setOnItemSelectedListener(item -> {
            int itemId = item.getItemId();
            if (itemId == R.id.nav_add) {
                startActivity(new Intent(HomeActivity.this, NewTicketActivity.class));
                return true;
            } else if (itemId == R.id.nav_logout) {
                SessionManager.getInstance().logout();
                Intent intent = new Intent(HomeActivity.this, MainActivity.class);
                intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                startActivity(intent);
                finish();
                return true;
            }
            return false;
        });
        fabChat.setOnClickListener(v -> startActivity(new Intent(HomeActivity.this, ChatListActivity.class)));
    }

    private void setupRecyclerView() {
        recyclerViewTickets = findViewById(R.id.recyclerViewTickets);

        ticketAdapter = new TicketAdapter(ticket -> {
            Intent intent;
            if (ticket.getStatus().equalsIgnoreCase("Aberto")) {
                intent = new Intent(HomeActivity.this, OpenTicketDetailActivity.class);
            }
            else if (ticket.getStatus().equalsIgnoreCase("Aceito")) {
                intent = new Intent(HomeActivity.this, AcceptedTicketDetailActivity.class);
            }
            else {
                Toast.makeText(this, "Não há detalhes para este status de ticket.", Toast.LENGTH_SHORT).show();
                return;
            }
            intent.putExtra("TICKET_OBJECT", ticket);
            startActivity(intent);
        });

        recyclerViewTickets.setAdapter(ticketAdapter);
    }

    // --- MÉTODO MODIFICADO (COM CORREÇÕES) ---
    private void iniciarConexaoSignalR() {
        String token = SessionManager.getInstance().getAuthToken();
        if (token == null) {
            Log.e("SignalR", "Token é nulo. Conexão com TicketHub não autenticada.");
        }

        hubConnection = HubConnectionBuilder.create(ApiConfig.BASE_URL + "/ticketHub")
                .withAccessTokenProvider(Single.just(token != null ? token : ""))
                .build();

        // --- CORREÇÃO APLICADA AQUI ---
        // Trocamos ticket.id por ticket.getChamadoId()
        // Mantivemos ticket.getStatus() (pois ele existe no seu código)
        hubConnection.on("ReceberAtualizacaoTicket", (ticket) -> {
            String fullMessage = "O ticket #" + ticket.getChamadoId() + " foi atualizado para: " + ticket.getStatus();
            handleRealTimeUpdate("Um ticket foi atualizado!", fullMessage);
        }, Ticket.class);

        hubConnection.on("ReceberNovoTicket", (ticket) -> {
            String fullMessage = "Um novo ticket foi criado: #" + ticket.getChamadoId();
            handleRealTimeUpdate("Novo ticket recebido!", fullMessage);
        }, Ticket.class);
        // --- FIM DA CORREÇÃO ---

        hubConnection.on("ReceberTicketDeletado", (ticketId) -> {
            String fullMessage = "O ticket #" + ticketId + " foi removido.";
            handleRealTimeUpdate("Um ticket foi removido.", fullMessage);
        }, Integer.class);

        try {
            hubConnection.start().subscribe(
                    () -> Log.d("SignalR", "Conexão estabelecida!"),
                    (error) -> Log.e("SignalR", "Erro ao conectar: " + error.getMessage())
            );
        } catch (Exception e) {
            Log.e("SignalR", "Erro geral: " + e.getMessage());
        }
    }
    // --- FIM DO MÉTODO MODIFICADO ---


    private void handleRealTimeUpdate(String toastMessage, String notificationMessage) {
        runOnUiThread(() -> {
            Toast.makeText(HomeActivity.this, toastMessage, Toast.LENGTH_SHORT).show();

            if(notificationMessage != null && !notificationMessage.isEmpty()) {
                notificationMessages.add(notificationMessage);
                updateNotificationBell();
            }
            refreshTickets();
        });
    }

    // --- MÉTODOS NOVOS ADICIONADOS ---

    private void updateNotificationBell() {
        if (notificationMessages.isEmpty()) {
            buttonNotifications.clearColorFilter();
        } else {
            buttonNotifications.setColorFilter(Color.YELLOW);
        }
    }

    private void showNotificationDialog() {
        if (notificationMessages.isEmpty()) {
            Toast.makeText(this, "Nenhuma notificação nova.", Toast.LENGTH_SHORT).show();
            return;
        }

        CharSequence[] messages = notificationMessages.toArray(new CharSequence[0]);

        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle("Notificações");
        builder.setItems(messages, null);
        builder.setPositiveButton("Limpar", (dialog, which) -> {
            notificationMessages.clear();
            updateNotificationBell();
            dialog.dismiss();
        });

        AlertDialog dialog = builder.create();
        dialog.show();
    }
    // --- FIM DOS MÉTODOS NOVOS ---


    private void refreshTickets() {
        User user = SessionManager.getInstance().getLoggedInUser();
        if (user != null) {
            textViewUserName.setText("Olá, " + user.getNome());
            fetchTicketsFromApi(user.getId());
        } else {
            Intent intent = new Intent(HomeActivity.this, MainActivity.class);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(intent);
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        bottomNavigationView.setSelectedItemId(R.id.nav_home);
        refreshTickets();
    }

    @Override
    protected void onDestroy() {
        if (hubConnection != null && hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
            hubConnection.stop();
        }
        super.onDestroy();
    }

    private void fetchTicketsFromApi(int userId) {
        String url = ApiConfig.BASE_URL + "/api/Tickets/por-usuario/" + userId;
        RequestQueue queue = Volley.newRequestQueue(this);

        JsonArrayRequest jsonArrayRequest = new JsonArrayRequest(Request.Method.GET, url, null,
                response -> {
                    Log.d("API_TICKETS_RESPONSE", response.toString());
                    try {
                        Gson gson = new Gson();
                        Ticket[] tickets = gson.fromJson(response.toString(), Ticket[].class);
                        allUserTickets.clear();
                        allUserTickets.addAll(Arrays.asList(tickets));
                    } catch (Exception e) {
                        e.printStackTrace(); // Correção do ponto e vírgula
                        Toast.makeText(this, "Erro ao processar os tickets.", Toast.LENGTH_SHORT).show();
                    }
                    updateChatButtonVisibility();
                    if (chipAceito.isChecked()) {
                        showAcceptedTickets();
                    } else {
                        showOpenTickets();
                    }
                },
                error -> {
                    Toast.makeText(this, "Erro ao buscar tickets.", Toast.LENGTH_SHORT).show();
                }) {
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
        queue.add(jsonArrayRequest);
    }

    private void updateChatButtonVisibility() {
        boolean hasAcceptedTickets = allUserTickets.stream().anyMatch(t -> "Aceito".equalsIgnoreCase(t.getStatus()));
        fabChat.setVisibility(hasAcceptedTickets ? View.VISIBLE : View.GONE);
    }

    private void showOpenTickets() {
        List<Ticket> openTickets = new ArrayList<>();
        for (Ticket ticket : allUserTickets) {
            if ("Aberto".equalsIgnoreCase(ticket.getStatus())) {
                openTickets.add(ticket);
            }
        }
        ticketAdapter.setTickets(openTickets);
    }

    private void showAcceptedTickets() {
        List<Ticket> acceptedTickets = new ArrayList<>();
        for (Ticket ticket : allUserTickets) {
            if ("Aceito".equalsIgnoreCase(ticket.getStatus())) {
                acceptedTickets.add(ticket);
            }
        }
        ticketAdapter.setTickets(acceptedTickets);
    }
}