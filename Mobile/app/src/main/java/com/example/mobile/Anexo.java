package com.example.mobile;
import java.io.Serializable;
public class Anexo implements Serializable {
    private int anexoId;
    private String nomeArquivo;
    public int getAnexoId() { return anexoId; }
    public String getNomeArquivo() { return nomeArquivo; }
}