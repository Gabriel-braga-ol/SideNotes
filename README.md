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
- Arraste da pílula com o botão direito, encaixe nas laterais e restauração da posição ao iniciar.

## Tecnologias

- C# e .NET 10 (`net10.0-windows`).
- WPF e XAML.
- Binding de dados e `INotifyPropertyChanged`.
- `ObservableCollection` e `ICollectionView`.
- `DispatcherTimer` para o salvamento automático.
- `System.Text.Json` para leitura e gravação dos dados.

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
6. Com a pílula recolhida, mantenha o botão direito pressionado e arraste para ajustar a altura ou mudar de lado. Ao soltar, a posição é salva.

Uma nota aberta permanece no editor ao retirar o mouse. Desafixar ou excluir essa nota pela janela principal fecha seu editor no Desk. Ao fechar a janela principal, a janela do Desk também é fechada.

O posicionamento considera a área útil do **monitor principal**, descontando o espaço da barra de tarefas.

## Armazenamento

As notas são armazenadas neste caminho do usuário do Windows:

```text
%LocalAppData%\SideNotes\notes.json
```

Na gravação, o aplicativo escreve primeiro em `notes.json.tmp` e depois substitui o arquivo principal. Ao detectar JSON ou dados inválidos, preserva uma cópia chamada `notes-invalid-<identificador>.json` e informa sua localização.

A indicação de quais notas estão fixadas no Desk também é salva em `notes.json`. O lado e a posição vertical da pílula ficam em um arquivo separado:

```text
%LocalAppData%\SideNotes\desk_position.json
```

A posição vertical é armazenada proporcionalmente à área disponível e restaurada ao iniciar. Se esse arquivo não existir ou contiver dados inválidos, o Desk usa a posição padrão: lado direito, centralizado. Falhas de leitura ou gravação da posição são registradas na saída de depuração.

## Organização do código

O projeto separa o modelo de dados, a interface, o estado da aplicação e o armazenamento, seguindo uma organização baseada em MVVM. Dentro de `SideNotes/`, os arquivos estão agrupados nas pastas `Models`, `ViewModels`, `Views` e `Services`, com namespaces correspondentes (`SideNotes.Models`, `SideNotes.ViewModels`, `SideNotes.Views` e `SideNotes.Services`).

| Arquivo | Responsabilidade |
| --- | --- |
| `App.xaml` e `App.xaml.cs` | Inicialização do repositório compartilhado e dos ViewModels, criação das janelas, tratamento de falhas ao carregar os dados e encerramento dos serviços. |
| `Models/Note.cs` | Dados da nota, incluindo fixação no Desk, e notificações de alteração. |
| `Models/DeskPosition.cs` | Dados do lado e da posição vertical do Desk. |
| `ViewModels/MainViewModel.cs` | Seleção, busca e ações da janela principal; delegação da criação, exclusão e salvamento ao repositório. |
| `ViewModels/DeskViewModel.cs` | Visão das notas fixadas e estado do Desk: nota aberta, lado e expansão do leque. |
| `Views/MainWindow.xaml` | Layout, estilos e bindings da interface. |
| `Views/MainWindow.xaml.cs` | Interações visuais, foco, atalhos e apresentação de avisos. |
| `Views/DeskWindow.xaml` e `Views/DeskWindow.xaml.cs` | Interface do Desk, eventos de mouse, arraste, animações e cálculos de posicionamento na tela. |
| `Services/NotesRepository.cs` | Coleção compartilhada, acompanhamento das alterações, autosave e comunicação de falhas de salvamento. |
| `Services/NoteStorage.cs` | Leitura, validação e gravação do arquivo JSON das notas. |
| `Services/DeskSettingsStore.cs` | Leitura, validação e gravação de `desk_position.json`. |

`App.xaml`, `App.xaml.cs` e `AssemblyInfo.cs` permanecem na raiz do projeto.

Os dois ViewModels utilizam a mesma instância de `NotesRepository` e os mesmos objetos `Note`. O autosave fica centralizado no repositório; cada janela mantém seu próprio estado de interface.

## Próximas etapas

- [ ] Refinar o comportamento da busca durante a edição de notas.
- [ ] Melhorar o design do modo Janela.
- [x] Compartilhar as mesmas notas entre os modos **Desk** e Janela.
- [x] Permitir selecionar até cinco notas para exibição no Desk.
- [x] Permitir arrastar o conjunto de notas entre os lados esquerdo e direito da tela.
- [x] Respeitar a área útil do monitor principal e o espaço ocupado pela barra de tarefas.
- [x] Salvar as preferências do Desk, incluindo posição e notas escolhidas.
- [ ] Refinar o visual do leque do Desk.
- [ ] Suavizar as animações de abertura e recolhimento.

## Autor

Desenvolvido por **Gabriel Braga** como projeto de aprendizado e portfólio.
