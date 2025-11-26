namespace GestaoChamados.DTOs
{
    // DTO de Requisição para IA.
    // Esse objeto carrega os ingredientes necessários para montar o "Prompt" que enviaremos ao ChatGPT/Gemini.
    public class SugestaoRequestDto
    {
        // O problema em si.
        // Ex: "Minha internet não conecta e aparece erro 404".
        // A IA vai analisar esse texto procurando palavras-chave de erro.
        public string Descricao { get; set; }

        // *** A GRANDE SACADA (Contexto) ***
        // Este campo define QUEM está pedindo ajuda.
        // Se Perfil == "Usuario", a IA deve responder: "Tente reiniciar o modem." (Linguagem simples).
        // Se Perfil == "Tecnico", a IA deve responder: "Verifique o Ping e o DNS no servidor." (Linguagem técnica).
        public string Perfil { get; set; }
    }
}