package com.example.mobile;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.TextView;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.DefaultRetryPolicy;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.JsonObjectRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class NewTicketActivity extends AppCompatActivity {

    private EditText editTextTitle;
    private EditText editTextDescription;
    private Button buttonAssistenciaIA;
    private ImageButton buttonAddAttachment;
    private TextView textViewAttachmentName;
    private RequestQueue requestQueue;

    private final List<Uri> anexosSelecionadosUris = new ArrayList<>();
    private final List<String> anexosSelecionadosNomes = new ArrayList<>();

    private final ActivityResultLauncher<Intent> pickFileLauncher = registerForActivityResult(
            new ActivityResultContracts.StartActivityForResult(),
            result -> {
                if (result.getResultCode() == Activity.RESULT_OK && result.getData() != null) {
                    anexosSelecionadosUris.clear();
                    anexosSelecionadosNomes.clear();

                    if (result.getData().getClipData() != null) {
                        int count = result.getData().getClipData().getItemCount();
                        for (int i = 0; i < count; i++) {
                            Uri uri = result.getData().getClipData().getItemAt(i).getUri();
                            anexosSelecionadosUris.add(uri);
                            anexosSelecionadosNomes.add(getFileName(uri));
                        }
                    } else if (result.getData().getData() != null) {
                        Uri uri = result.getData().getData();
                        anexosSelecionadosUris.add(uri);
                        anexosSelecionadosNomes.add(getFileName(uri));
                    }

                    if (!anexosSelecionadosNomes.isEmpty()) {
                        updateAttachmentTextView();
                        Toast.makeText(this, anexosSelecionadosNomes.size() + " anexo(s) selecionado(s)!", Toast.LENGTH_SHORT).show();
                    }
                }
            }
    );

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_new_ticket);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        Objects.requireNonNull(getSupportActionBar()).setDisplayHomeAsUpEnabled(true);

        requestQueue = Volley.newRequestQueue(this);

        editTextTitle = findViewById(R.id.editTextTitle);
        editTextDescription = findViewById(R.id.editTextDescription);
        buttonAssistenciaIA = findViewById(R.id.button_assistencia_ia);
        buttonAddAttachment = findViewById(R.id.button_add_attachment);
        textViewAttachmentName = findViewById(R.id.textView_attachment_name);

        buttonAddAttachment.setOnClickListener(v -> openFilePicker());
        textViewAttachmentName.setOnClickListener(v -> openFilePicker());

        buttonAssistenciaIA.setOnClickListener(v -> {
            String title = editTextTitle.getText().toString().trim();
            String description = editTextDescription.getText().toString().trim();
            if (validateInputs(title, description)) {
                createTicketAndGetAISuggestion(title, description);
            }
        });
    }

    private void updateAttachmentTextView() {
        if (anexosSelecionadosNomes.isEmpty()) {
            textViewAttachmentName.setVisibility(View.GONE);
            buttonAddAttachment.setVisibility(View.VISIBLE);
        } else {
            StringBuilder fileNames = new StringBuilder();
            for (String name : anexosSelecionadosNomes) {
                fileNames.append(name).append("\n");
            }
            textViewAttachmentName.setText(fileNames.toString().trim());
            textViewAttachmentName.setVisibility(View.VISIBLE);
            buttonAddAttachment.setVisibility(View.GONE);
        }
    }

    private void createTicketAndGetAISuggestion(String title, String description) {
        createTicket(title, description, newTicketId -> getAiSuggestion(newTicketId, title, description));
    }

    private void createTicket(String title, String description, final TicketCreationCallback callback) {
        buttonAssistenciaIA.setEnabled(false);
        buttonAssistenciaIA.setText("Criando chamado...");
        String url = ApiConfig.BASE_URL + "/api/Tickets";
        User loggedInUser = SessionManager.getInstance().getLoggedInUser();
        if (loggedInUser == null) {
            Toast.makeText(this, "Erro de sessão. Faça login novamente.", Toast.LENGTH_LONG).show();
            resetButtonState();
            return;
        }
        JSONObject postData = new JSONObject();
        try {
            postData.put("titulo", title);
            postData.put("descricao", description);
            postData.put("usuarioId", loggedInUser.getId());
            postData.put("status", "Aberto");
        } catch (JSONException e) {
            e.printStackTrace();
            resetButtonState();
            return;
        }
        JsonObjectRequest jsonObjectRequest = new JsonObjectRequest(Request.Method.POST, url, postData,
                response -> {
                    Log.d("API_RESPONSE", "Resposta da criação de ticket: " + response.toString());
                    try {
                        int novoTicketId = response.getInt("id");
                        callback.onSuccess(novoTicketId);
                    } catch (JSONException e) {
                        Toast.makeText(this, "Erro ao processar resposta do servidor.", Toast.LENGTH_SHORT).show();
                        Log.e("CREATE_TICKET_JSON", "Erro de JSON: ", e);
                        resetButtonState();
                    }
                },
                error -> {
                    Toast.makeText(this, "Falha ao criar o chamado.", Toast.LENGTH_LONG).show();
                    Log.e("CREATE_TICKET_API", "Erro: " + error.toString());
                    resetButtonState();
                }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return getAuthHeaders();
            }
        };
        jsonObjectRequest.setRetryPolicy(new DefaultRetryPolicy(15000, DefaultRetryPolicy.DEFAULT_MAX_RETRIES, DefaultRetryPolicy.DEFAULT_BACKOFF_MULT));
        requestQueue.add(jsonObjectRequest);
    }

    private void getAiSuggestion(int ticketId, String title, String description) {
        buttonAssistenciaIA.setText("Pensando...");
        String url = ApiConfig.BASE_URL + "/api/Ai/sugestao-solucao";
        JSONObject postData = new JSONObject();
        try {
            postData.put("descricao", title + ": " + description);
            postData.put("perfil", "Cliente");
        } catch (JSONException e) {
            e.printStackTrace();
            resetButtonState();
            return;
        }
        JsonObjectRequest jsonObjectRequest = new JsonObjectRequest(Request.Method.POST, url, postData,
                response -> {
                    try {
                        String aiResponse = response.getString("solucao");
                        Intent intent = new Intent(NewTicketActivity.this, AIResponseActivity.class);
                        intent.putExtra("TICKET_ID", ticketId);
                        intent.putExtra("TICKET_TITLE", title);
                        intent.putExtra("USER_PROBLEM", description);
                        intent.putExtra("AI_RESPONSE", aiResponse);
                        if (!anexosSelecionadosUris.isEmpty()) {
                            intent.putParcelableArrayListExtra("TICKET_ATTACHMENTS_URI", (ArrayList<Uri>) anexosSelecionadosUris);
                            intent.putStringArrayListExtra("TICKET_ATTACHMENTS_NAME", (ArrayList<String>) anexosSelecionadosNomes);
                            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                            for (Uri uri : anexosSelecionadosUris) {
                                grantUriPermission(getPackageName(), uri, Intent.FLAG_GRANT_READ_URI_PERMISSION);
                            }
                        }
                        startActivity(intent);
                        finish();
                    } catch (JSONException e) {
                        Toast.makeText(this, "Erro ao processar a resposta da IA.", Toast.LENGTH_SHORT).show();
                        resetButtonState();
                    }
                },
                error -> {
                    Toast.makeText(this, "Falha ao obter sugestão da IA.", Toast.LENGTH_LONG).show();
                    Log.e("AI_API_ERROR", "Erro: " + error.toString());
                    resetButtonState();
                }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return getAuthHeaders();
            }
        };
        jsonObjectRequest.setRetryPolicy(new DefaultRetryPolicy(20000, DefaultRetryPolicy.DEFAULT_MAX_RETRIES, DefaultRetryPolicy.DEFAULT_BACKOFF_MULT));
        requestQueue.add(jsonObjectRequest);
    }

    private void openFilePicker() {
        Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
        intent.setType("*/*");
        intent.putExtra(Intent.EXTRA_ALLOW_MULTIPLE, true);
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        pickFileLauncher.launch(Intent.createChooser(intent, "Selecione o(s) anexo(s)"));
    }

    private boolean validateInputs(String title, String description) {
        if (title.isEmpty() || description.isEmpty()) {
            Toast.makeText(this, "Por favor, preencha o título e a descrição.", Toast.LENGTH_SHORT).show();
            return false;
        }
        return true;
    }

    private void resetButtonState() {
        buttonAssistenciaIA.setEnabled(true);
        buttonAssistenciaIA.setText("Obter Assistência da IA");
    }

    private Map<String, String> getAuthHeaders() {
        Map<String, String> headers = new HashMap<>();
        String token = SessionManager.getInstance().getAuthToken();
        if (token != null && !token.isEmpty()) {
            headers.put("Authorization", "Bearer " + token);
        }
        return headers;
    }

    private String getFileName(Uri uri) {
        String result = null;
        if (Objects.equals(uri.getScheme(), "content")) {
            try (Cursor cursor = getContentResolver().query(uri, null, null, null, null)) {
                if (cursor != null && cursor.moveToFirst()) {
                    int nameIndex = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                    if (nameIndex != -1) {
                        result = cursor.getString(nameIndex);
                    }
                }
            }
        }
        if (result == null) {
            result = uri.getPath();
            int cut = result.lastIndexOf('/');
            if (cut != -1) {
                result = result.substring(cut + 1);
            }
        }
        return result;
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }

    interface TicketCreationCallback {
        void onSuccess(int newTicketId);
    }
}