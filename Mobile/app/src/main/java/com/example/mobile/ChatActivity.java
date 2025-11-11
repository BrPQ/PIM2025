package com.example.mobile; // Certifique-se que o nome do pacote está correto

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;
import androidx.recyclerview.widget.RecyclerView;

import android.os.Bundle;
import android.util.Log;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.JsonObjectRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

// --- IMPORTS DO SIGNALR E RXJAVA ---
import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;
import com.microsoft.signalr.HubConnectionState;
import io.reactivex.Single; // <-- IMPORT NECESSÁRIO PARA A CORREÇÃO

public class ChatActivity extends AppCompatActivity {

    private RecyclerView recyclerViewChat;
    private ChatAdapter chatAdapter;
    private EditText editTextMessage;
    private ImageButton buttonSend;
    private RequestQueue requestQueue;
    private String professionalName;
    private int ticketId;

    private HubConnection hubConnection;
    private List<Message> messageList;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_chat);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);

        // Recebe os dados da tela anterior (ChatListActivity)
        professionalName = getIntent().getStringExtra("PROFESSIONAL_NAME");
        ticketId = getIntent().getIntExtra("TICKET_ID", -1);
        getSupportActionBar().setTitle(professionalName);

        requestQueue = Volley.newRequestQueue(this);

        setupRecyclerView(); // Configura o RecyclerView

        editTextMessage = findViewById(R.id.editTextMessage);
        buttonSend = findViewById(R.id.buttonSend);

        // Busca o histórico de mensagens assim que a tela abre
        fetchMessages();

        // Inicia a conexão com o SignalR
        startSignalRConnection();

        // Listener do botão Enviar
        buttonSend.setOnClickListener(v -> {
            String messageText = editTextMessage.getText().toString().trim();
            if (!messageText.isEmpty()) {
                sendMessage(messageText);
                editTextMessage.setText("");
            }
        });
    }

    private void setupRecyclerView() {
        // Inicializa a lista e o adapter
        messageList = new ArrayList<>();
        recyclerViewChat = findViewById(R.id.recyclerViewChat);
        chatAdapter = new ChatAdapter();
        chatAdapter.setMessages(messageList); // Passa a lista para o adapter
        recyclerViewChat.setAdapter(chatAdapter);
    }

    private void fetchMessages() {
        if (ticketId == -1) {
            Toast.makeText(this, "ID do Ticket inválido.", Toast.LENGTH_SHORT).show();
            return;
        }
        String url = ApiConfig.BASE_URL + "/api/Chat/" + ticketId;

        JsonArrayRequest jsonArrayRequest = new JsonArrayRequest(Request.Method.GET, url, null,
                response -> {
                    // Atualiza a lista local
                    messageList.clear(); // Limpa a lista antes de adicionar
                    try {
                        for (int i = 0; i < response.length(); i++) {
                            JSONObject msgJson = response.getJSONObject(i);
                            messageList.add(new Message(
                                    msgJson.getString("conteudo"),
                                    msgJson.getString("nomeUsuario"),
                                    msgJson.getString("authorRole")
                            ));
                        }
                        chatAdapter.notifyDataSetChanged(); // Notifica o adapter
                        // Rola a lista para a última mensagem
                        if (!messageList.isEmpty()) {
                            recyclerViewChat.scrollToPosition(messageList.size() - 1);
                        }
                    } catch (JSONException e) {
                        e.printStackTrace();
                        Toast.makeText(this, "Erro ao processar mensagens.", Toast.LENGTH_SHORT).show();
                    }
                },
                error -> {
                    Log.e("API_ERROR", "Erro ao buscar mensagens: " + error.toString());
                    Toast.makeText(this, "Falha ao carregar histórico.", Toast.LENGTH_SHORT).show();
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
        requestQueue.add(jsonArrayRequest);
    }

    private void sendMessage(String text) {
        String url = ApiConfig.BASE_URL + "/api/Chat";

        JSONObject postData = new JSONObject();
        try {
            postData.put("ticketId", ticketId);
            postData.put("usuarioId", SessionManager.getInstance().getLoggedInUser().getId());
            postData.put("conteudo", text);
        } catch (JSONException e) {
            e.printStackTrace();
        }

        JsonObjectRequest jsonObjectRequest = new JsonObjectRequest(Request.Method.POST, url, postData,
                response -> {
                    // Não fazemos mais fetchMessages() aqui.
                    // O SignalR cuidará de receber a mensagem de volta.
                    Log.d("SendMessage", "Mensagem enviada com sucesso. Aguardando SignalR.");
                },
                error -> {
                    Log.e("API_ERROR", "Erro ao enviar mensagem: " + error.toString());
                    Toast.makeText(this, "Erro ao enviar mensagem.", Toast.LENGTH_SHORT).show();
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
        requestQueue.add(jsonObjectRequest);
    }

    /**
     * Inicia a conexão com o Hub SignalR, entra no grupo do chat
     * e configura o ouvinte (listener) para novas mensagens.
     */
    private void startSignalRConnection() {
        String token = SessionManager.getInstance().getAuthToken();
        if (ticketId == -1 || token == null) {
            Log.e("SignalR", "Não foi possível iniciar. TicketID ou Token inválido.");
            return;
        }

        // Constrói a conexão com o Hub
        hubConnection = HubConnectionBuilder.create(ApiConfig.BASE_URL + "/chathub")
                // CORREÇÃO: Usamos Single.just() do RxJava para prover o token
                .withAccessTokenProvider(Single.just(token))
                .build();

        // *** OUVINTE (LISTENER) ***
        // Ouve o evento "ReceberNovaMensagem" que definimos na API
        // (Certifique-se que o MensagemPayload.java foi criado e está no mesmo pacote)
        hubConnection.on("ReceberNovaMensagem", (payload) -> {
            Log.d("SignalR", "Nova mensagem recebida: " + payload.getConteudo());

            // Cria o objeto Message que o seu Adapter entende
            Message newMessage = new Message(
                    payload.getConteudo(),
                    payload.getNomeUsuario(),
                    payload.getAuthorRole()
            );

            // IMPORTANTE: O SignalR roda em outra thread.
            // Para atualizar a UI (Adapter/RecyclerView), precisamos voltar para a Thread Principal.
            runOnUiThread(() -> {
                messageList.add(newMessage);
                chatAdapter.notifyItemInserted(messageList.size() - 1);
                recyclerViewChat.scrollToPosition(messageList.size() - 1);
            });

        }, MensagemPayload.class); // <-- A classe DTO que você criou


        // Inicia a conexão (de forma assíncrona) usando o padrão RxJava
        hubConnection.start().subscribe(
                () -> { // OnComplete (sucesso na conexão)
                    Log.d("SignalR", "Conexão com Hub estabelecida.");
                    // Após conectar, entra no grupo do chat
                    try {
                        String groupName = "ticket-" + ticketId;
                        // Chama o método "JoinChatGroup" que criamos no ChatHub.cs
                        hubConnection.invoke("JoinChatGroup", groupName);
                        Log.d("SignalR", "Entrou no grupo: " + groupName);
                    } catch (Exception e) {
                        Log.e("SignalR", "Erro ao tentar entrar no grupo: " + e.getMessage());
                    }
                },
                (ex) -> { // OnError (falha na conexão)
                    Log.e("SignalR", "Erro ao conectar com o Hub: " + ex.getMessage());
                    // Aqui você pode, por exemplo, exibir um Toast
                    runOnUiThread(() -> Toast.makeText(ChatActivity.this, "Falha na conexão do chat.", Toast.LENGTH_SHORT).show());
                }
        );
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }

    /**
     * Limpa a conexão com o SignalR ao fechar a Activity.
     */
    @Override
    protected void onDestroy() {
        super.onDestroy();
        // Para o SignalR quando a Activity é destruída
        if (hubConnection != null && hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
            Log.d("SignalR", "Parando conexão com o Hub.");
            try {
                String groupName = "ticket-" + ticketId;
                hubConnection.invoke("LeaveChatGroup", groupName); // Sai do grupo
            } catch (Exception e) {
                Log.e("SignalR", "Erro ao sair do grupo: " + e.getMessage());
            } finally {
                hubConnection.stop(); // Para a conexão
            }
        }
    }
}