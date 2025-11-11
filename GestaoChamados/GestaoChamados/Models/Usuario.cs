using System;
using System.ComponentModel.DataAnnotations; // Adicione este using
using System.ComponentModel.DataAnnotations.Schema; // Adicione este using

namespace GestaoChamados.Models // Ou só GestaoChamados.Models
{
    [Table("Usuarios")] // Diz ao EF que o nome da tabela no SQL é "Usuarios"
    public class Usuario
    {
        [Key] // Diz que esta é a Chave Primária
        [Column("UsuarioId")] // Mapeia a propriedade 'Id' para a coluna 'UsuarioId'
        public int Id { get; set; }

        [Column("Nome")] // Mapeia a propriedade 'NomeUsuario' para a coluna 'Nome'
        public string NomeUsuario { get; set; }

        [Column("Senha")] // Mapeia a propriedade 'SenhaHash' para a coluna 'Senha'
        public string SenhaHash { get; set; }

        [Column("Perfil")] // Mapeia a propriedade 'Role' para a coluna 'Perfil'
        public string Role { get; set; }

        // Adicionando as colunas que faltavam
        [Column("Matricula")]
        public string Matricula { get; set; }

        [Column("Ativo")]
        public bool Ativo { get; set; }

        [Column("DataCadastro")]
        public DateTime DataCadastro { get; set; }

        [Column("Email")] // Mapeia para a coluna que criamos
        public string? Email { get; set; }
    }
}