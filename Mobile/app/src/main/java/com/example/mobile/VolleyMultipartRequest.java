package com.example.mobile;
import com.android.volley.AuthFailureError;
import com.android.volley.NetworkResponse;
import com.android.volley.Request;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.HttpHeaderParser;
import java.io.ByteArrayOutputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.util.Map;

public class VolleyMultipartRequest extends Request<NetworkResponse> {
    private final String twoHyphens = "--";
    private final String lineEnd = "\r\n";
    private final String boundary = "apiclient-" + System.currentTimeMillis();
    private final Response.Listener<NetworkResponse> mListener;
    private final Response.ErrorListener mErrorListener;

    public VolleyMultipartRequest(int method, String url, Response.Listener<NetworkResponse> listener, Response.ErrorListener errorListener) {
        super(method, url, errorListener);
        this.mListener = listener;
        this.mErrorListener = errorListener;
    }

    @Override public String getBodyContentType() { return "multipart/form-data;boundary=" + boundary; }
    @Override public byte[] getBody() throws AuthFailureError {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        DataOutputStream dos = new DataOutputStream(bos);
        try {
            Map<String, DataPart> data = getByteData();
            if (data != null && data.size() > 0) {
                for (Map.Entry<String, DataPart> entry : data.entrySet()) {
                    buildDataPart(dos, entry.getValue(), entry.getKey());
                }
            }
            dos.writeBytes(twoHyphens + boundary + twoHyphens + lineEnd);
            return bos.toByteArray();
        } catch (IOException e) { e.printStackTrace(); }
        return null;
    }
    protected Map<String, DataPart> getByteData() throws AuthFailureError { return null; }
    @Override protected Response<NetworkResponse> parseNetworkResponse(NetworkResponse response) {
        try { return Response.success(response, HttpHeaderParser.parseCacheHeaders(response)); }
        catch (Exception e) { return Response.error(new VolleyError(e)); }
    }
    @Override protected void deliverResponse(NetworkResponse response) { mListener.onResponse(response); }
    @Override public void deliverError(VolleyError error) { mErrorListener.onErrorResponse(error); }
    private void buildDataPart(DataOutputStream dos, DataPart dataPart, String inputName) throws IOException {
        dos.writeBytes(twoHyphens + boundary + lineEnd);
        dos.writeBytes("Content-Disposition: form-data; name=\"" + inputName + "\"; filename=\"" + dataPart.getFileName() + "\"" + lineEnd);
        if (dataPart.getType() != null && !dataPart.getType().trim().isEmpty()) {
            dos.writeBytes("Content-Type: " + dataPart.getType() + lineEnd);
        }
        dos.writeBytes(lineEnd);
        dos.write(dataPart.getContent());
        dos.writeBytes(lineEnd);
    }
    public static class DataPart {
        private String fileName;
        private byte[] content;
        private String type;
        public DataPart(String name, byte[] data, String mimeType) { fileName = name; content = data; type = mimeType; }
        String getFileName() { return fileName; }
        byte[] getContent() { return content; }
        String getType() { return type; }
    }
}