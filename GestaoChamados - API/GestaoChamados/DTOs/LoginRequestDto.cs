namespace GestaoChamados.DTOs
{
    // DTO de Login (Data Transfer Object).
    // Serve para mapear o JSON que vem do corpo da requisição (Body) para o C#.
    // Exemplo de JSON recebido: { "matricula": "12345", "senha": "minhasenha" }
    public class LoginRequestDto
    {
        // Por que String e não Int?
        // Matrículas muitas vezes podem começar com zero (ex: "00543") ou ter letras (ex: "A123").
        // Usar string preserva a formatação exata.
        public string Matricula { get; set; }

        // *** DADO SENSÍVEL ***
        // Esta é a senha em TEXTO PURO (Raw Password).
        // Ela viaja da tela do usuário até aqui.
        // O Controller vai usar isso APENAS para comparar com o Hash do banco.
        // Regra de Ouro: Esse DTO nunca deve ser salvo em log ("Console.WriteLine"), 
        // senão a senha do usuário fica gravada num arquivo de texto no servidor.
        public string Senha { get; set; }
    }
}