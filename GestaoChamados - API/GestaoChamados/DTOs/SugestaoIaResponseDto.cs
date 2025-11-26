namespace GestaoChamados.DTOs
{
    // DTO de Resposta da IA.
    // Usado quando o Front-end pede: "Ei, IA, como resolvo esse chamado?"
    // O Back-end consulta a IA, processa e devolve a resposta neste formato.
    public class SugestaoIaResponseDto
    {
        // O texto mágico gerado pela Inteligência Artificial.
        // Ex: "Baseado na descrição 'Impressora piscando', sugiro verificar o toner ou atolamento de papel."
        // Por que usar uma classe e não devolver só uma string?
        // Porque JSONs estruturados ({ "sugestao": "texto" }) são mais fáceis de expandir no futuro.
        public string Sugestao { get; set; }
    }
}