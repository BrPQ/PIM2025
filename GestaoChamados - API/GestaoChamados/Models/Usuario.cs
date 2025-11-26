using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    // [Table("Usuarios")]: Garante que a tabela se chame "Usuarios" (plural) no SQL.
    [Table("Usuarios")]
    public class Usuario
    {
        // Chave Primária.
        [Key]
        // Mapeamento: No C# usamos "Id" (padrão limpo), no Banco é "UsuarioId".
        [Column("UsuarioId")]
        public int Id { get; set; }

        // Mapeamento: No C# é "NomeUsuario", no Banco é apenas "Nome".
        [Column("Nome")]
        public string NomeUsuario { get; set; }

        // *** PONTO CRÍTICO DE SEGURANÇA ***
        // No Banco, a coluna chama-se "Senha".
        // No C#, chamamos de "SenhaHash" para lembrar o programador, toda vez que ele ler o código,
        // que aquilo NÃO É a senha original (123456), mas sim o hash criptografado ($2a$10$...).
        // É uma técnica de "Clean Code" (Código Limpo) através da nomenclatura.
        [Column("Senha")]
        public string SenhaHash { get; set; }

        // Mapeamento: Inglês no C# (Role) vs Português no Banco (Perfil).
        // Role armazena "Admin", "Tecnico", etc.
        [Column("Perfil")]
        public string Role { get; set; }

        // Identificador único de login (Matrícula da faculdade/empresa).
        [Column("Matricula")]
        public string Matricula { get; set; }

        // Flag de "Soft Delete" (Exclusão Lógica).
        // Se precisarmos demitir/bloquear um usuário, nós NÃO deletamos a linha do banco
        // (senão perderíamos o histórico dos chamados que ele atendeu).
        // Nós apenas mudamos Ativo = false. O sistema de login verifica isso antes de deixar entrar.
        [Column("Ativo")]
        public bool Ativo { get; set; }

        [Column("DataCadastro")]
        public DateTime DataCadastro { get; set; }

        // Campo Opcional (Nullable).
        // O sistema funciona mesmo se o usuário não tiver e-mail cadastrado.
        [Column("Email")]
        public string? Email { get; set; }
    }
}