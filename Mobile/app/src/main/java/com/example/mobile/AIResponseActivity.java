package com.example.mobile;

import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.Toolbar;

import android.content.Intent;
import android.graphics.Color;
import android.graphics.PorterDuff;
import android.graphics.drawable.Drawable;
import android.net.Uri;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import com.android.volley.AuthFailureError;
import com.android.volley.DefaultRetryPolicy;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class AIResponseActivity extends AppCompatActivity {

    private RequestQueue requestQueue;
    private int ticketId;
    private String ticketTitle;
    private String userProblem;

    private List<Uri> anexosParaUploadUris = new ArrayList<>();
    private List<String> anexosParaUploadNomes = new ArrayList<>();
    private int uploadCounter = 0;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_ai_response);

        Toolbar toolbar = findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);

        if (getSupportActionBar() != null) {
            getSupportActionBar().setDisplayHomeAsUpEnabled(false);
            getSupportActionBar().setTitle("Sugestão da IA");
        }

        requestQueue = Volley.newRequestQueue(this);

        TextView textViewTicketTitle = findViewById(R.id.textView_ticket_title);
        TextView textViewUserProblem = findViewById(R.id.textView_user_problem);
        TextView textViewAiResponse = findViewById(R.id.textView_ai_response);
        Button buttonResolver = findViewById(R.id.button_resolver);
        Button buttonNeedHelp = findViewById(R.id.button_need_help);

        Intent intent = getIntent();
        ticketId = intent.getIntExtra("TICKET_ID", -1);
        ticketTitle = intent.getStringExtra("TICKET_TITLE");
        userProblem = intent.getStringExtra("USER_PROBLEM");
        String aiResponse = intent.getStringExtra("AI_RESPONSE");

        anexosParaUploadUris = intent.getParcelableArrayListExtra("TICKET_ATTACHMENTS_URI");
        anexosParaUploadNomes = intent.getStringArrayListExtra("TICKET_ATTACHMENTS_NAME");
        if (anexosParaUploadUris == null) anexosParaUploadUris = new ArrayList<>();
        if (anexosParaUploadNomes == null) anexosParaUploadNomes = new ArrayList<>();

        textViewTicketTitle.setText(ticketTitle);
        textViewUserProblem.setText(userProblem);
        textViewAiResponse.setText(aiResponse);

        buttonResolver.setOnClickListener(v -> {
            if (ticketId != -1) resolverTicket();
        });

        buttonNeedHelp.setOnClickListener(v -> {
            if (!anexosParaUploadUris.isEmpty()) {
                uploadCounter = 0;
                uploadNextAttachment(ticketId);
            } else {
                Toast.makeText(this, "Seu chamado está aberto e aguardando um técnico.", Toast.LENGTH_LONG).show();
                navigateToHome();
            }
        });
    }

    @Override
    public void onBackPressed() {
        Toast.makeText(this, "Por favor, escolha uma das opções abaixo.", Toast.LENGTH_SHORT).show();
    }

    @Override
    public boolean onSupportNavigateUp() {
        onBackPressed();
        return true;
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.menu_ai_response, menu);
        MenuItem cancelItem = menu.findItem(R.id.action_cancel_ticket);
        if (cancelItem != null) {
            Drawable icon = cancelItem.getIcon();
            if (icon != null) {
                icon = icon.mutate();
                icon.setColorFilter(Color.parseColor("#D32F2F"), PorterDuff.Mode.SRC_IN);
            }
        }
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == R.id.action_cancel_ticket) {
            showCancelConfirmationDialog();
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    private void showCancelConfirmationDialog() {
        new AlertDialog.Builder(this)
                .setTitle("Cancelar Chamado")
                .setMessage("Você tem certeza que deseja excluir este chamado?")
                .setPositiveButton("Sim, excluir", (dialog, which) -> {
                    if (ticketId != -1) deleteTicket();
                })
                .setNegativeButton("Não", null)
                .setIcon(R.drawable.ic_delete)
                .show();
    }

    private void deleteTicket() {
        String url = ApiConfig.BASE_URL + "/api/Tickets/" + ticketId;
        StringRequest deleteRequest = new StringRequest(Request.Method.DELETE, url,
                response -> {
                    Toast.makeText(this, "Chamado excluído com sucesso!", Toast.LENGTH_LONG).show();
                    navigateToHome();
                },
                error -> {
                    Toast.makeText(this, "Falha ao excluir o chamado.", Toast.LENGTH_SHORT).show();
                    Log.e("DELETE_TICKET_API", "Erro: " + error.toString());
                }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return getAuthHeaders();
            }
        };
        requestQueue.add(deleteRequest);
    }

    private void resolverTicket() {
        String url = ApiConfig.BASE_URL + "/api/Tickets/" + ticketId;
        final JSONObject ticketData = new JSONObject();
        try {
            ticketData.put("chamadoId", ticketId);
            ticketData.put("titulo", ticketTitle);
            ticketData.put("descricao", userProblem);
            ticketData.put("status", "Finalizado");
            ticketData.put("solucao", "Solucionado pela Assistência IA");
            if (SessionManager.getInstance().getLoggedInUser() != null) {
                ticketData.put("usuarioId", SessionManager.getInstance().getLoggedInUser().getId());
            } else {
                Toast.makeText(this, "Sessão inválida.", Toast.LENGTH_LONG).show();
                return;
            }
        } catch (JSONException e) { e.printStackTrace(); return; }

        StringRequest stringRequest = new StringRequest(Request.Method.PUT, url,
                response -> {
                    Toast.makeText(this, "Chamado finalizado com sucesso!", Toast.LENGTH_LONG).show();
                    navigateToHome();
                },
                error -> {
                    Toast.makeText(this, "Falha ao finalizar o chamado.", Toast.LENGTH_SHORT).show();
                }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError { return getAuthHeaders(); }
            @Override
            public String getBodyContentType() { return "application/json; charset=utf-8"; }
            @Override
            public byte[] getBody() throws AuthFailureError { return ticketData.toString().getBytes(StandardCharsets.UTF_8); }
        };
        requestQueue.add(stringRequest);
    }

    private void uploadNextAttachment(final int ticketId) {
        if (uploadCounter >= anexosParaUploadUris.size()) {
            Toast.makeText(this, "Anexos enviados! Um técnico cuidará do seu chamado.", Toast.LENGTH_LONG).show();
            navigateToHome();
            return;
        }
        try {
            Uri fileUri = anexosParaUploadUris.get(uploadCounter);
            String fileName = anexosParaUploadNomes.get(uploadCounter);
            String url = ApiConfig.BASE_URL + "/api/Anexos/upload/" + ticketId;
            Toast.makeText(this, "Enviando anexo " + (uploadCounter + 1) + "...", Toast.LENGTH_SHORT).show();
            InputStream inputStream = getContentResolver().openInputStream(fileUri);
            byte[] fileData = getBytesFromInputStream(inputStream);
            VolleyMultipartRequest multipartRequest = new VolleyMultipartRequest(Request.Method.POST, url,
                    response -> {
                        uploadCounter++;
                        uploadNextAttachment(ticketId);
                    },
                    error -> {
                        Toast.makeText(this, "Falha ao enviar o anexo: " + fileName, Toast.LENGTH_SHORT).show();
                        uploadCounter++;
                        uploadNextAttachment(ticketId);
                    }) {
                @Override
                protected Map<String, DataPart> getByteData() {
                    Map<String, DataPart> params = new HashMap<>();
                    params.put("file", new DataPart(fileName, fileData, getContentResolver().getType(fileUri)));
                    return params;
                }
                @Override
                public Map<String, String> getHeaders() throws AuthFailureError { return getAuthHeaders(); }
            };
            multipartRequest.setRetryPolicy(new DefaultRetryPolicy(30000, 0, DefaultRetryPolicy.DEFAULT_BACKOFF_MULT));
            requestQueue.add(multipartRequest);
        } catch (Exception e) {
            Log.e("UPLOAD_PREPARATION_ERROR", "Erro ao preparar o anexo.", e);
            Toast.makeText(this, "Erro crítico ao tentar ler o anexo.", Toast.LENGTH_LONG).show();
            navigateToHome();
        }
    }

    private Map<String, String> getAuthHeaders() {
        Map<String, String> headers = new HashMap<>();
        String token = SessionManager.getInstance().getAuthToken();
        if (token != null && !token.isEmpty()) { headers.put("Authorization", "Bearer " + token); }
        return headers;
    }

    private void navigateToHome() {
        Intent intent = new Intent(AIResponseActivity.this, HomeActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_NEW_TASK);
        startActivity(intent);
        finish();
    }

    public byte[] getBytesFromInputStream(InputStream inputStream) throws IOException {
        if (inputStream == null) return new byte[0];
        ByteArrayOutputStream byteBuffer = new ByteArrayOutputStream();
        byte[] buffer = new byte[4096];
        int len;
        while ((len = inputStream.read(buffer)) != -1) {
            byteBuffer.write(buffer, 0, len);
        }
        return byteBuffer.toByteArray();
    }
}