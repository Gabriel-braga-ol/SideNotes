# DearNotes

Aplicativo de notas para Windows desenvolvido em **C# com WPF**, para organizar lembretes e anotações com edição simples e salvamento automático.

O projeto faz parte do meu aprendizado em C#/.NET, com foco em interfaces desktop, binding de dados, separação de responsabilidades e persistência local.

> **Em desenvolvimento:** os modos Janela e Desk compartilham as mesmas notas e o salvamento automático. O Desk permite fixar notas, visualizar prévias e abrir o editor em uma janela própria. O visual do leque e a suavidade das animações ainda serão refinados.

## Funcionalidades

- Criação, edição e exclusão de notas com título e conteúdo.
- Cores atribuídas automaticamente a partir de uma paleta.
- Lista com título, prévia do conteúdo e destaque da nota selecionada.
- Busca por título ou conteúdo, sem diferenciar maiúsculas e minúsculas.
- Salvamento automático após um segundo sem digitar, com novas tentativas em intervalos crescentes (até 30 s) quando a gravação falha.
- Persistência local em JSON.
- Janela redimensionável e mensagens para estados sem seleção ou sem resultados.
- Validação dos dados carregados e preservação de uma cópia de arquivos inválidos.
- Aviso único de falha de gravação, com novas tentativas automáticas, e confirmação antes de fechar quando não é possível salvar.
- Registro de falhas inesperadas em `%LocalAppData%\DearNotes\logs\app.log`, sem registrar o conteúdo das notas.
- Execução em instância única: uma segunda abertura apenas informa que o aplicativo já está em execução.
- Migração automática das notas da instalação anterior (`SideNotes`) na primeira execução, preservando os arquivos originais.
- Fixação de até cinco notas no Desk, compartilhadas com o modo Janela.
- Pílula lateral com abertura do leque por clique e prévias ao passar o mouse nas abas.
- Editor do Desk em janela própria, com salvamento automático independente da seleção na janela principal.
- Janela do editor aberta no mesmo monitor do Desk, arrastável pela barra superior e fechada junto com o Desk.
- Arraste da pílula com o botão direito entre monitores, encaixe nas laterais e restauração do monitor e da posição ao iniciar.
- Recuperação do Desk no monitor principal quando a tela utilizada é desconectada.
- Adaptação do tamanho e do posicionamento do Desk entre monitores com escalas de DPI diferentes.

## Tecnologias

- C# e .NET 10 (`net10.0-windows`).
- WPF e XAML.
- Binding de dados e `INotifyPropertyChanged`.
- `ObservableCollection` e `ICollectionView`.
- `DispatcherTimer` para o salvamento automático.
- Mutex nomeado para garantir instância única do aplicativo.
- `System.Text.Json` para leitura e gravação dos dados.
- APIs do Windows (Win32) via P/Invoke para consultar monitores e posicionar o Desk.
- Reconhecimento de DPI por monitor com `PerMonitorV2` no manifesto do aplicativo.

## Como executar

### Requisitos

- Windows.
- SDK do .NET 10.
- Para executar pelo Visual Studio: uma versão compatível com .NET 10 e a carga de trabalho **Desenvolvimento para desktop com .NET**.

### Pelo Visual Studio

1. Baixe ou clone este repositório.
2. Abra a solução `DearNotes.slnx`.
3. Defina `DearNotes` como projeto de inicialização, se necessário.
4. Execute com **F5**.

### Pelo terminal

Na pasta raiz do repositório, que contém `DearNotes.slnx`, execute:

```powershell
dotnet run --project ".\DearNotes\DearNotes.csproj"
```

## Como usar

1. Clique em **+ Nova nota** para criar uma anotação.
2. Edite o título e o conteúdo no painel à direita. As alterações são salvas automaticamente.
3. Use **Buscar notas** para localizar uma palavra no título ou no conteúdo. Limpe o campo para mostrar todas as notas novamente.
4. Para excluir, selecione uma nota na lista e pressione **Delete** com o foco na lista. Ao editar um texto, Delete mantém sua função normal de apagar caracteres.

Criar uma nota limpa a busca ativa para que a nova anotação fique visível. Ao excluir, a próxima seleção é escolhida entre as notas visíveis na lista.

### Modo Desk

1. Selecione uma nota na janela principal e use a ação de fixar no Desk. É possível manter até cinco notas fixadas.
2. Clique com o botão esquerdo na pílula lateral para abrir o leque. Apenas passar o mouse na pílula não abre as notas.
3. Passe o mouse sobre uma aba para visualizar a prévia e clique nela para abrir a janela do editor.
4. Edite o título e o conteúdo. A janela abre centralizada no mesmo monitor do Desk, pode ser arrastada pela barra superior e salva automaticamente, mesmo quando outra nota está selecionada na janela principal.
5. Use o botão **✕** para fechar o editor. Sem uma nota aberta, o leque recolhe após uma pequena pausa ao retirar o mouse.
6. Com a pílula recolhida, mantenha o botão direito pressionado e arraste para ajustar a altura, mudar de lado ou levar o Desk para outro monitor. Ao soltar, ele se encaixa na lateral da tela onde está o cursor e salva o monitor e a posição.

Uma nota aberta permanece no editor ao retirar o mouse. Desafixar ou excluir essa nota pela janela principal fecha o editor. Ao fechar a janela principal, as janelas do Desk e do editor também são fechadas.

O posicionamento considera a área útil do **monitor escolhido**, descontando o espaço da barra de tarefas. O Desk acompanha as mudanças de escala de DPI entre telas e recalcula o encaixe quando a configuração dos monitores muda. Se o monitor utilizado for desconectado, o Desk recolhe e é reposicionado no principal. Reconectar a tela não o move automaticamente de volta.

A recuperação automática após uma desconexão não substitui a preferência salva; um novo arraste salva a nova posição.

## Armazenamento

As notas são armazenadas neste caminho do usuário do Windows:

```text
%LocalAppData%\DearNotes\notes.json
```

Na gravação, o aplicativo escreve primeiro em `notes.json.tmp` e depois substitui o arquivo principal. Ao detectar JSON ou dados inválidos, preserva uma cópia chamada `notes-invalid-<identificador>.json` e informa sua localização.

A indicação de quais notas estão fixadas no Desk também é salva em `notes.json`. O identificador do monitor, o lado e a posição vertical da pílula ficam em um arquivo separado:

```text
%LocalAppData%\DearNotes\desk_position.json
```

A posição vertical é armazenada proporcionalmente à área disponível e restaurada ao iniciar. Se o monitor salvo não estiver disponível, ou se o arquivo antigo não contiver seu identificador, o Desk usa o monitor principal. Se esse arquivo não existir ou contiver dados inválidos, o Desk usa a posição padrão: lado direito, centralizado no monitor principal. Falhas de leitura ou gravação da posição são registradas na saída de depuração.

Na primeira execução com o nome **DearNotes**, se existir a pasta antiga `%LocalAppData%\SideNotes`, o aplicativo copia `notes.json` e `desk_position.json` para a nova pasta. Os arquivos antigos não são apagados e permanecem como cópia de segurança; uma marca interna evita repetir a cópia. Se a migração falhar, o aplicativo é encerrado sem prosseguir.

Falhas inesperadas do aplicativo são registradas em:

```text
%LocalAppData%\DearNotes\logs\app.log
```

O arquivo é rotacionado ao atingir cerca de 1 MB e não contém o título nem o conteúdo das notas.

## Organização do código

O projeto separa o modelo de dados, a interface, o estado da aplicação e o armazenamento, seguindo uma organização baseada em MVVM. Dentro de `DearNotes/`, os arquivos estão agrupados nas pastas `Models`, `ViewModels`, `Views` e `Services`, com namespaces correspondentes (`DearNotes.Models`, `DearNotes.ViewModels`, `DearNotes.Views` e `DearNotes.Services`).

| Arquivo | Responsabilidade |
| --- | --- |
| `App.xaml` e `App.xaml.cs` | Inicialização do repositório compartilhado e dos ViewModels, garantia de instância única, tratamento global de falhas, criação das janelas e encerramento dos serviços. |
| `Models/Note.cs` | Dados da nota, incluindo fixação no Desk, e notificações de alteração. |
| `Models/DeskPosition.cs` | Identificador do monitor, lado e posição vertical do Desk. |
| `ViewModels/MainViewModel.cs` | Seleção, busca e ações da janela principal; delegação da criação, exclusão e salvamento ao repositório. |
| `ViewModels/DeskViewModel.cs` | Visão das notas fixadas, estado do Desk e transições de abertura, fechamento, expansão e mudança de lado. |
| `Views/MainWindow.xaml` | Layout, estilos e bindings da interface. |
| `Views/MainWindow.xaml.cs` | Interações visuais, foco, atalhos e apresentação de avisos. |
| `Views/DeskWindow.xaml` e `Views/DeskWindow.xaml.cs` | Pílula lateral, leque das notas fixadas, eventos de mouse, arraste, animações, posicionamento e reação a mudanças de monitores e DPI. |
| `Views/NoteEditorWindow.xaml` e `Views/NoteEditorWindow.xaml.cs` | Janela do editor aberta pelo Desk, posicionada no monitor atual e arrastável pela barra superior. |
| `Services/NotesRepository.cs` | Coleção compartilhada, acompanhamento das alterações, autosave e comunicação de falhas de salvamento. |
| `Services/NoteStorage.cs` | Leitura, validação e gravação do arquivo JSON das notas. |
| `Services/DeskPositionStore.cs` | Leitura, validação e gravação de `desk_position.json`. |
| `Services/DeskScreenService.cs` | Consulta dos monitores, área útil, posição do cursor e dimensões e posicionamento da janela via Win32. |
| `Services/AppPaths.cs` | Centralização dos caminhos de armazenamento e de log. |
| `Services/StorageMigration.cs` | Cópia única dos dados da instalação anterior (`SideNotes`) para a pasta atual. |
| `Services/AppLogger.cs` | Registro de falhas inesperadas em arquivo, com rotação por tamanho. |
| `app.manifest` | Configuração de reconhecimento de DPI por monitor (`PerMonitorV2`). |
| `AGENTS.md` | Regras de arquitetura, convenções de código, comandos e restrições; deve ser consultado antes de alterar o projeto. |

`App.xaml`, `App.xaml.cs`, `AssemblyInfo.cs` e `app.manifest` permanecem na raiz do projeto. O `README.md` e o `AGENTS.md` ficam na raiz do repositório.

Os dois ViewModels utilizam a mesma instância de `NotesRepository` e os mesmos objetos `Note`. A janela do editor usa o `DeskViewModel` já existente, sem manter cópia própria das notas. O autosave fica centralizado no repositório; cada janela mantém seu próprio estado de interface. O repositório expõe o evento `NoteChanged`, usado pelo Desk para acompanhar alterações de fixação sem duplicar o acompanhamento individual das notas.

## Próximas etapas

- [ ] Refinar o comportamento da busca durante a edição de notas.
- [ ] Melhorar o design do modo Janela.
- [x] Compartilhar as mesmas notas entre os modos **Desk** e Janela.
- [x] Permitir selecionar até cinco notas para exibição no Desk.
- [x] Permitir arrastar o Desk entre monitores e os lados esquerdo e direito da tela.
- [x] Respeitar a área útil do monitor escolhido e o espaço ocupado pela barra de tarefas.
- [x] Recuperar o Desk no monitor principal quando a tela utilizada for desconectada.
- [x] Adaptar o Desk a monitores com escalas de DPI diferentes.
- [x] Salvar as preferências do Desk, incluindo monitor, posição e notas escolhidas.
- [ ] Refinar o visual do leque do Desk.
- [ ] Suavizar as animações de abertura e recolhimento.

## Autor

Desenvolvido por **Gabriel Braga** como projeto de aprendizado e portfólio.
