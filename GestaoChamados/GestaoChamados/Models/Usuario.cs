using System;
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace GestaoChamados.Models 
{
    [Table("Usuarios")] 
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

        [Column("Matricula")]
        public string Matricula { get; set; }

        [Column("Ativo")]
        public bool Ativo { get; set; }

        [Column("DataCadastro")]
        public DateTime DataCadastro { get; set; }

        [Column("Email")] 
        public string? Email { get; set; }
    }
}