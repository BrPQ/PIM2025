InfiniTI PRO - Sistema de Gestão de Chamados (PIM UNIP)
Este repositório contém o código-fonte do InfiniTI PRO, um Projeto Integrado Multidisciplinar (PIM) desenvolvido para a UNIP - Universidade Paulista.

O projeto consiste em um sistema integrado para gestão de chamados e suporte técnico, simulando um ambiente corporativo de Help Desk. A arquitetura é centralizada em uma API .NET que serve dados para três plataformas de cliente distintas: Desktop (WPF), Web (ASP.NET) e Mobile (Android).

Arquitetura e Plataformas
Todas as plataformas (Desktop, Web e Mobile) compartilham a mesma base de dados e se comunicam por meio da API central InfiniTI Pro, com banco SQL Server. Essa integração garante sincronização em tempo real e consistência das informações entre os diferentes dispositivos.

1. API Central (.NET Core)
O "cérebro" do sistema, responsável por toda a lógica de negócios, acesso aos dados e segurança.

Autenticação: Utiliza JWT (JSON Web Token) para controle de acesso seguro baseado em perfis (Admin, Gestor, Tecnico).

Comunicação: Expõe Serviços RESTful para troca de dados (CRUD) entre as plataformas.

Tempo Real: Implementa SignalR para notificar os clientes (especialmente o Mobile) sobre atualizações de tickets e chat.

Relatórios: A API é a fonte centralizada que consolida métricas operacionais para os dashboards e relatórios.

2. Módulo Desktop (WPF)
A versão Desktop foi desenvolvida em WPF (.NET), seguindo o padrão arquitetural MVVM (Model–View–ViewModel).

Público: Voltada aos Técnicos de Suporte.

Função: É a principal ferramenta de trabalho para gerenciar os chamados abertos, realizar atendimentos, interagir com os usuários (buscando dados) e registrar as soluções adotadas.

3. Módulo Web (ASP.NET Core)
A versão Web foi desenvolvida com ASP.NET Core Razor Pages, que consome dados da WebAPI .NET Core centralizada.

Público: Voltada aos Gestores, Admins e Técnicos (com permissões diferentes).

Função: Atua como o painel de controle e BI do sistema, permitindo:

Visualização de dashboards em tempo real (fila de chamados).

Análise de KPIs de desempenho (chamados por dia, SLA, tempo médio).

Geração e exportação de relatórios de tickets (Excel).

Gerenciamento de usuários (apenas Admin/Gestor).

4. Módulo Mobile (Android Nativo)
A versão Mobile foi desenvolvida de forma nativa para Android, utilizando Java e XML no ambiente do Android Studio.

Público: Voltada para a mobilidade do Técnico ou para Usuários (dependendo da versão).

Arquitetura: Segue o padrão nativo do Android, onde as Activities (UI) gerenciam a lógica de chamada a um API Client para consumir os endpoints da API.

Função: Implementa o SignalR para atualizações em tempo real, permitindo que o usuário receba novas mensagens e atualizações de tickets instantaneamente enquanto estiver com o aplicativo aberto.

Principais Funcionalidades
Sistema de autenticação seguro com JWT e acesso baseado em perfis (Roles).

CRUD completo para Tickets (Chamados).

Atualizações em tempo real (novos tickets e chat) via SignalR.

Dashboards de KPI (Tempo Real e Histórico) no painel Web.

Geração de relatórios em Excel a partir de dados do banco.

Controle de permissão granular por tipo de usuário (ex: Técnico não pode criar usuários).

Integrantes:

BRENO PEREIRA QUEIROZ

WILLIAN KAUAN DE OLIVEIRA 

RODRIGO RAMOS SPINOLA 

NATAN RODRIGUES ARAUJO 

GUSTAVO HENRY OLIVEIRA COSTA 

HENRIQUE PEREZ DUARTE
