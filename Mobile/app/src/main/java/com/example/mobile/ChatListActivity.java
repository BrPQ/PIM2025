package com.example.mobile;

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class ChatListActivity extends AppCompatActivity {

    private RecyclerView recyclerViewChats;
    private ConversationAdapter conversationAdapter;
    private TextView textViewEmpty;
    private RequestQueue requestQueue;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_chat_list);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);

        recyclerViewChats = findViewById(R.id.recyclerViewChats);
        textViewEmpty = findViewById(R.id.textViewEmpty);
        requestQueue = Volley.newRequestQueue(this);

        setupRecyclerView();
        fetchConversations();
    }

    private void setupRecyclerView() {
        conversationAdapter = new ConversationAdapter(conversation -> {
            Intent intent = new Intent(ChatListActivity.this, ChatActivity.class);
            intent.putExtra("TICKET_ID", conversation.getTicketId());
            intent.putExtra("PROFESSIONAL_NAME", conversation.getProfessionalName());
            startActivity(intent);
        });
        recyclerViewChats.setAdapter(conversationAdapter);
    }

    private void fetchConversations() {
        String url = ApiConfig.BASE_URL + "/api/Chat/contatos";

        JsonArrayRequest jsonArrayRequest = new JsonArrayRequest(Request.Method.GET, url, null,
                response -> {
                    List<Conversation> conversations = new ArrayList<>();
                    try {
                        for (int i = 0; i < response.length(); i++) {
                            JSONObject ticketJson = response.getJSONObject(i);

                            // --- CORREÇÃO FINAL AQUI ---
                            // Trocamos "ticketId" para "id" para corresponder exatamente à resposta da API
                            conversations.add(new Conversation(
                                    ticketJson.getInt("id"),
                                    ticketJson.getString("titulo"),
                                    ticketJson.getString("profissionalDesignado")
                            ));
                        }
                    } catch (JSONException e) {
                        e.printStackTrace();
                        Toast.makeText(this, "Erro ao processar as conversas.", Toast.LENGTH_SHORT).show();
                    }

                    if (conversations.isEmpty()) {
                        textViewEmpty.setVisibility(View.VISIBLE);
                        recyclerViewChats.setVisibility(View.GONE);
                    } else {
                        textViewEmpty.setVisibility(View.GONE);
                        recyclerViewChats.setVisibility(View.VISIBLE);
                        conversationAdapter.setConversations(conversations);
                    }
                },
                error -> {
                    Log.e("API_ERROR", "Erro ao buscar conversas: " + error.toString());
                    textViewEmpty.setText("Erro ao carregar conversas.");
                    textViewEmpty.setVisibility(View.VISIBLE);
                    recyclerViewChats.setVisibility(View.GONE);
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

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }
}