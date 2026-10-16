# SideNotes

Aplicativo de notas para Windows desenvolvido em **C# com WPF**, para organizar lembretes e anotações com edição simples e salvamento automático.

O projeto faz parte do meu aprendizado em C#/.NET, com foco em interfaces desktop, binding de dados, separação de responsabilidades e persistência local.

> **Em desenvolvimento:** os modos Janela e Desk compartilham as mesmas notas e o salvamento automático. O Desk permite fixar notas, visualizar prévias e editar pela lateral da tela. O visual do leque e a suavidade das animações ainda serão refinados.

## Interface atual

![Interface do SideNotes com lista de notas, busca e editor](docs/images/sidenotes.png)

## Funcionalidades

- Criação, edição e exclusão de notas com título e conteúdo.
- Cores atribuídas automaticamente a partir de uma paleta.
- Lista com título, prévia do conteúdo e destaque da nota selecionada.
- Busca por título ou conteúdo, sem diferenciar maiúsculas e minúsculas.
- Salvamento automático após um segundo sem digitar.
- Persistência local em JSON.
- Janela redimensionável e mensagens para estados sem seleção ou sem resultados.
- Validação dos dados carregados e preservação de uma cópia de arquivos inválidos.
- Avisos de falha de gravação e confirmação antes de fechar quando não é possível salvar.
- Fixação de até cinco notas no Desk, compartilhadas com o modo Janela.
- Pílula lateral com abertura do leque por clique e prévias ao passar o mouse nas abas.
- Edição no Desk independente da seleção na janela principal, com salvamento automático.
- Arraste da pílula com o botão direito entre monitores, encaixe nas laterais e restauração do monitor e da posição ao iniciar.
- Recuperação do Desk no monitor principal quando a tela utilizada é desconectada.
- Adaptação do tamanho e do posicionamento do Desk entre monitores com escalas de DPI diferentes.

## Tecnologias

- C# e .NET 10 (`net10.0-windows`).
- WPF e XAML.
- Binding de dados e `INotifyPropertyChanged`.
- `ObservableCollection` e `ICollectionView`.
- `DispatcherTimer` para o salvamento automático.
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
2. Abra a solução `SideNotes.slnx`.
3. Defina `SideNotes` como projeto de inicialização, se necessário.
4. Execute com **F5**.

### Pelo terminal

Na pasta raiz do repositório, que contém `SideNotes.slnx`, execute:

```powershell
dotnet run --project ".\SideNotes\SideNotes.csproj"
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
3. Passe o mouse sobre uma aba para visualizar a prévia e clique nela para abrir o editor.
4. Edite o título e o conteúdo. As alterações são salvas automaticamente, mesmo quando outra nota está selecionada na janela principal.
5. Use o botão **✕** para fechar o editor. Sem uma nota aberta, o leque recolhe após uma pequena pausa ao retirar o mouse.
6. Com a pílula recolhida, mantenha o botão direito pressionado e arraste para ajustar a altura, mudar de lado ou levar o Desk para outro monitor. Ao soltar, ele se encaixa na lateral da tela onde está o cursor e salva o monitor e a posição.

Uma nota aberta permanece no editor ao retirar o mouse. Desafixar ou excluir essa nota pela janela principal fecha seu editor no Desk. Ao fechar a janela principal, a janela do Desk também é fechada.

O posicionamento considera a área útil do **monitor escolhido**, descontando o espaço da barra de tarefas. O Desk acompanha as mudanças de escala de DPI entre telas e recalcula o encaixe quando a configuração dos monitores muda. Se o monitor utilizado for desconectado, o Desk recolhe e é reposicionado no principal. Reconectar a tela não o move automaticamente de volta.

A recuperação automática após uma desconexão não substitui a preferência salva; um novo arraste salva a nova posição.

## Armazenamento

As notas são armazenadas neste caminho do usuário do Windows:

```text
%LocalAppData%\SideNotes\notes.json
```

Na gravação, o aplicativo escreve primeiro em `notes.json.tmp` e depois substitui o arquivo principal. Ao detectar JSON ou dados inválidos, preserva uma cópia chamada `notes-invalid-<identificador>.json` e informa sua localização.

A indicação de quais notas estão fixadas no Desk também é salva em `notes.json`. O identificador do monitor, o lado e a posição vertical da pílula ficam em um arquivo separado:

```text
%LocalAppData%\SideNotes\desk_position.json
```

A posição vertical é armazenada proporcionalmente à área disponível e restaurada ao iniciar. Se o monitor salvo não estiver disponível, ou se o arquivo antigo não contiver seu identificador, o Desk usa o monitor principal. Se esse arquivo não existir ou contiver dados inválidos, o Desk usa a posição padrão: lado direito, centralizado no monitor principal. Falhas de leitura ou gravação da posição são registradas na saída de depuração.

## Organização do código

O projeto separa o modelo de dados, a interface, o estado da aplicação e o armazenamento, seguindo uma organização baseada em MVVM. Dentro de `SideNotes/`, os arquivos estão agrupados nas pastas `Models`, `ViewModels`, `Views` e `Services`, com namespaces correspondentes (`SideNotes.Models`, `SideNotes.ViewModels`, `SideNotes.Views` e `SideNotes.Services`).

| Arquivo | Responsabilidade |
| --- | --- |
| `App.xaml` e `App.xaml.cs` | Inicialização do repositório compartilhado e dos ViewModels, criação das janelas, tratamento de falhas ao carregar os dados e encerramento dos serviços. |
| `Models/Note.cs` | Dados da nota, incluindo fixação no Desk, e notificações de alteração. |
| `Models/DeskPosition.cs` | Identificador do monitor, lado e posição vertical do Desk. |
| `ViewModels/MainViewModel.cs` | Seleção, busca e ações da janela principal; delegação da criação, exclusão e salvamento ao repositório. |
| `ViewModels/DeskViewModel.cs` | Visão das notas fixadas, estado do Desk e transições de abertura, fechamento, expansão e mudança de lado. |
| `Views/MainWindow.xaml` | Layout, estilos e bindings da interface. |
| `Views/MainWindow.xaml.cs` | Interações visuais, foco, atalhos e apresentação de avisos. |
| `Views/DeskWindow.xaml` e `Views/DeskWindow.xaml.cs` | Interface do Desk, eventos de mouse, arraste, animações, posicionamento e reação a mudanças de monitores e DPI. |
| `Services/NotesRepository.cs` | Coleção compartilhada, acompanhamento das alterações, autosave e comunicação de falhas de salvamento. |
| `Services/NoteStorage.cs` | Leitura, validação e gravação do arquivo JSON das notas. |
| `Services/DeskPositionStore.cs` | Leitura, validação e gravação de `desk_position.json`. |
| `Services/DeskScreenService.cs` | Consulta dos monitores, área útil, posição do cursor e dimensões e posicionamento da janela via Win32. |
| `Services/AppPaths.cs` | Centralização dos caminhos de armazenamento local. |
| `app.manifest` | Configuração de reconhecimento de DPI por monitor (`PerMonitorV2`). |

`App.xaml`, `App.xaml.cs`, `AssemblyInfo.cs` e `app.manifest` permanecem na raiz do projeto.

Os dois ViewModels utilizam a mesma instância de `NotesRepository` e os mesmos objetos `Note`. O autosave fica centralizado no repositório; cada janela mantém seu próprio estado de interface. O repositório expõe o evento `NoteChanged`, usado pelo Desk para acompanhar alterações de fixação sem duplicar o acompanhamento individual das notas.

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
