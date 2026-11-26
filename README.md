# DearNotes

Aplicativo de notas para Windows desenvolvido em **C# com WPF**, para organizar lembretes e anotações com edição simples e salvamento automático.

O projeto faz parte do meu aprendizado em C#/.NET, com foco em interfaces desktop, binding de dados, separação de responsabilidades e persistência local.

> **Em desenvolvimento:** os modos Janela e Desk compartilham as mesmas notas e o salvamento automático. O Desk permite fixar notas, visualizar prévias, criar uma nota direto do baralho e abrir o editor em uma janela própria. O visual do leque e as animações foram redesenhados e ainda podem receber ajustes.

## Funcionalidades

- Criação, edição e exclusão de notas com título e conteúdo.
- Cores atribuídas automaticamente a partir de uma paleta.
- Lista com título, prévia do conteúdo, barra de cor, tempo relativo e destaque da nota selecionada.
- Busca por título ou conteúdo, sem diferenciar maiúsculas e minúsculas.
- Chips **Todas** e **No Desk** para filtrar a lista pelas notas fixadas, com contagem das notas visíveis.
- Exclusão pelo botão do editor ou pela tecla **Delete**, sempre com confirmação.
- Painel do editor com cartão na cor da nota, metadados de criação e atualização e ações de fixar no Desk e excluir.
- Moldura personalizada com cantos arredondados, barra de título arrastável e botões de minimizar, maximizar e fechar; a janela maximizada respeita a área útil do monitor.
- Salvamento automático após um segundo sem digitar, em segundo plano: apenas o snapshot mais recente é enfileirado, sem bloquear a interface, com novas tentativas em intervalos crescentes (até 30 s) quando a gravação falha.
- Persistência local em JSON cifrado com formato versionado, identificador estável e datas de criação e atualização por nota.
- Criptografia em repouso com AES-GCM de 256 bits; a chave aleatória é protegida pelo DPAPI no escopo do usuário atual do Windows.
- Mensagens para estados sem seleção ou sem resultados.
- Validação dos dados carregados e preservação de uma cópia de arquivos inválidos.
- Aviso único de falha de gravação, com novas tentativas automáticas, e confirmação antes de fechar quando não é possível salvar.
- Registro de falhas inesperadas em `%LocalAppData%\DearNotes\logs\app.log`, sem registrar o conteúdo das notas.
- Execução em instância única: uma segunda abertura apenas informa que o aplicativo já está em execução.
- Migração automática das notas da instalação anterior (`SideNotes`) na primeira execução, preservando os arquivos originais.
- Fixação de até cinco notas no Desk, compartilhadas com o modo Janela.
- Baralho lateral discreto, com uma pílula na cor de cada nota fixada, que abre o leque ao aproximar o ponteiro: as notas entram como telhas sobrepostas, uma 45 ms depois da outra, cada uma com sua cor e uma aba vertical com o nome.
- Carta escolhida no leque desliza para fora do baralho em tamanho completo, com título e texto.
- Criação de nota direto do Desk: o botão **+** cria a nota já fixada e abre o editor, respeitando o limite de cinco notas.
- Editor do Desk em janela própria, com salvamento automático independente da seleção na janela principal.
- Janela do editor aberta no mesmo monitor do Desk, arrastável pela barra superior e fechada junto com o Desk.
- Arraste do baralho com o botão direito entre monitores, encaixe nas laterais e restauração do monitor e da posição ao iniciar.
- Recuperação do Desk no monitor principal quando a tela utilizada é desconectada.
- Adaptação do tamanho e do posicionamento do Desk entre monitores com escalas de DPI diferentes.
- Navegação do Desk por teclado, com foco visível, abertura e recolhimento do leque e troca de notas.

## Tecnologias

- C# e .NET 10 (`net10.0-windows`).
- WPF e XAML.
- Binding de dados e `INotifyPropertyChanged`.
- `ObservableCollection` e `ICollectionView`.
- `DispatcherTimer` para o atraso do salvamento automático.
- Gravação das notas em segundo plano a partir de snapshots imutáveis.
- Mutex nomeado para garantir instância única do aplicativo.
- `System.Text.Json` para leitura e gravação dos dados.
- Criptografia AES-GCM e DPAPI (`System.Security.Cryptography.ProtectedData`, incluído no .NET 10 para Windows) para proteger as notas em repouso.
- xUnit para os testes automatizados do núcleo.
- APIs do Windows (Win32) via P/Invoke para consultar monitores, DPI efetivo, posicionar o Desk e ajustar a moldura da janela principal (cantos arredondados e maximização na área útil).
- `WindowChrome` para arraste, redimensionamento e botões da janela personalizada.
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

### Testes e qualidade

Os testes automatizados cobrem a geometria do Desk, a criptografia, a leitura e a gravação dos arquivos JSON. Execute na raiz do repositório:

```powershell
dotnet test ".\DearNotes.slnx"
```

O build aplica os analisadores oficiais do .NET no nível mais recente e trata qualquer aviso como erro. A formatação é verificada com:

```powershell
dotnet format ".\DearNotes.slnx" --verify-no-changes
```

Para corrigir a formatação automaticamente, execute o mesmo comando sem `--verify-no-changes`. A pipeline em `.github/workflows/ci.yml` roda restauração, verificação de formatação, build e testes em `windows-latest` a cada push e pull request.

## Como usar

1. Clique em **+ Nova nota** para criar uma anotação.
2. Edite o título e o conteúdo no painel à direita. As alterações são salvas automaticamente.
3. Use **Buscar notas** para localizar uma palavra no título ou no conteúdo. Limpe o campo para mostrar todas as notas novamente.
4. Use os chips **Todas** e **No Desk** para alternar entre a lista completa e apenas as notas fixadas. A contagem ao lado do título mostra quantas notas estão visíveis.
5. Use **Fixar no Desk** no topo do editor para enviar a nota ao Desk; o mesmo botão a desafixa.
6. Para excluir, use **Excluir** no topo do editor ou selecione uma nota na lista e pressione **Delete** com o foco na lista. A exclusão sempre pede confirmação. Ao editar um texto, Delete mantém sua função normal de apagar caracteres.

Criar uma nota limpa a busca e o filtro **No Desk** para que a nova anotação fique visível. Ao excluir, a próxima seleção é escolhida entre as notas visíveis na lista. Também é possível arrastar a janela pela barra de título personalizada, usar duplo clique nela para maximizar ou restaurar e acessar o menu do sistema com **Alt+Espaço**.

### Modo Desk

1. Selecione uma nota na janela principal e use a ação de fixar no Desk, ou use o botão **+** do Desk para criar uma nota já fixada e abrir o editor. É possível manter até cinco notas fixadas.
2. Recolhido, o Desk é uma faixa fina encostada na borda da tela, com uma pílula na cor de cada nota fixada. Aproxime o ponteiro dela para abrir o leque: as notas entram como telhas sobrepostas, de cima para baixo, uma 45 ms depois da outra, mostrando apenas a aba vertical com o nome. O botão **+** aparece logo abaixo da última carta. Clicar na faixa também abre o leque. Sem notas fixadas, a faixa dá lugar ao botão **+**.
3. Passe o mouse sobre uma aba para escolher a nota: a carta desliza para fora do baralho em tamanho completo, com título e texto, e volta ao lugar quando o ponteiro sai. Clique nela para abrir a janela do editor, que entra deslizando a partir do lado do Desk. Clicar na carta da nota já aberta traz o editor de volta à frente.
4. Edite o título e o conteúdo. A janela abre centralizada no mesmo monitor do Desk, pode ser arrastada pela barra superior e salva automaticamente, mesmo quando outra nota está selecionada na janela principal.
5. Use o botão **✕** ou **Escape** para fechar o editor. O leque recolhe após uma pequena pausa ao retirar o mouse; a nota aberta permanece no editor.
6. Com o leque recolhido, mantenha o botão direito pressionado sobre a faixa de pílulas e arraste para ajustar a altura, mudar de lado ou levar o Desk para outro monitor. Ao soltar, ele se encaixa na lateral da tela onde está o cursor e salva o monitor e a posição.

Também é possível operar o Desk pelo teclado. Com o foco no baralho, **Enter** ou **Espaço** abre e recolhe o leque, e **Escape** o recolhe. Com o leque aberto, use **Tab** ou as setas para percorrer as cartas (a carta com o foco desliza para fora do baralho, como no hover), **Enter** ou **Espaço** para abrir a nota e **Escape** para recolher o leque e devolver o foco ao baralho. O foco atual é indicado por um contorno no baralho e na carta.

Uma nota aberta permanece no editor ao retirar o mouse. Desafixar ou excluir essa nota pela janela principal fecha o editor. Ao fechar a janela principal, as janelas do Desk e do editor também são fechadas.

O posicionamento considera a área útil do **monitor escolhido**, descontando o espaço da barra de tarefas. O Desk fica encostado na borda lateral, para que as cartas pareçam sair de trás dela, e mantém uma pequena folga em relação ao topo e à base. A escala de DPI usada no cálculo vem do próprio monitor de destino, inclusive antes de a janela ser movida. O Desk acompanha as mudanças de escala de DPI entre telas e recalcula o encaixe quando a configuração dos monitores muda. Se o monitor utilizado for desconectado, o Desk recolhe e é reposicionado no principal. Reconectar a tela não o move automaticamente de volta.

A recuperação automática após uma desconexão não substitui a preferência salva; um novo arraste salva a nova posição.

## Armazenamento

As notas são armazenadas neste caminho do usuário do Windows:

```text
%LocalAppData%\DearNotes\notes.json
```

O arquivo é um envelope JSON com o documento de notas **cifrado**. O envelope identifica o esquema de proteção e a versão do formato:

```json
{
  "Protection": "dpapi-aes-gcm",
  "FormatVersion": 1,
  "KeyId": "A1B2C3D4E5F60708",
  "Nonce": "…",
  "Tag": "…",
  "Ciphertext": "…"
}
```

A chave de 256 bits fica em `%LocalAppData%\DearNotes\notes.key`, protegida pelo DPAPI no escopo do usuário atual. O AES-GCM usa um nonce aleatório a cada gravação e uma tag de autenticação que detecta alterações no arquivo. Se a chave não corresponder ao envelope, ou se o conteúdo tiver sido adulterado, o aplicativo informa a falha e encerra **sem sobrescrever** as notas existentes.

Depois de decifrado, o conteúdo é o documento versionado. A versão atual é a **1**:

```json
{
  "SchemaVersion": 1,
  "Notes": [
    {
      "Id": "0f8fad5b-d9cb-469f-a165-70867728950e",
      "Title": "Compras",
      "Content": "Leite",
      "Color": "#B8E6D0",
      "IsPinnedToDesk": false,
      "CreatedAt": "2026-11-16T12:34:56+00:00",
      "UpdatedAt": "2026-11-16T12:40:00+00:00"
    }
  ]
}
```

Arquivos antigos que contêm apenas a lista de notas (sem `SchemaVersion`) ou o documento versionado em texto puro continuam sendo lidos. O formato legado é migrado automaticamente na leitura: o aplicativo gera um identificador para cada nota e usa a data atual como criação e atualização. Todo conteúdo é gravado cifrado no próximo salvamento. Versões de formato mais novas que a suportada são tratadas como dados inválidos, com cópia de segurança preservada.

Na gravação, o aplicativo escreve primeiro em `notes.json.tmp` e depois substitui o arquivo principal. O autosave grava em segundo plano uma cópia das notas e, ao fechar a janela principal, aguarda a fila terminar antes de encerrar. Ao detectar JSON ou dados inválidos, preserva uma cópia chamada `notes-invalid-<identificador>.json` e informa sua localização. São mantidas no máximo **5** cópias inválidas; as mais antigas são removidas. A cópia preserva os bytes originais: arquivos cifrados continuam cifrados e arquivos legados em texto puro continuam em texto puro.

> [!IMPORTANT]
> Ao copiar as notas manualmente, copie `notes.json` e `notes.key` juntos. Sem a chave, o arquivo cifrado não pode ser recuperado. A recriação do perfil do Windows ou uma reinstalação do sistema também podem invalidar a proteção DPAPI da chave.


A indicação de quais notas estão fixadas no Desk também é salva em `notes.json`. O identificador do monitor, o lado e a posição vertical do baralho ficam em um arquivo separado:

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

O repositório é dividido em três projetos: `DearNotes` (aplicativo WPF), `DearNotes.Core` (modelos, persistência e geometria do Desk, sem dependência das janelas) e `DearNotes.Tests` (testes automatizados com xUnit). A interface segue uma organização baseada em MVVM, com namespaces correspondentes (`DearNotes.Models`, `DearNotes.ViewModels`, `DearNotes.Views` e `DearNotes.Services`). As dependências apontam para o núcleo: o aplicativo e os testes referenciam `DearNotes.Core`, nunca o contrário.

| Arquivo | Responsabilidade |
| --- | --- |
| `DearNotes/App.xaml` e `DearNotes/App.xaml.cs` | Inicialização da criptografia, do repositório compartilhado e dos ViewModels, garantia de instância única, tratamento global de falhas, criação das janelas e encerramento dos serviços. |
| `DearNotes.Core/Models/Note.cs` | Dados da nota, incluindo identificador, datas, fixação no Desk, limite de notas fixadas e notificações de alteração. |
| `DearNotes.Core/Models/NotesDocument.cs` | Documento versionado que envolve a lista de notas em `notes.json`. |
| `DearNotes.Core/Models/NoteDocument.cs` | Representação persistida de uma nota, com identificador, conteúdo, cor, fixação e datas. |
| `DearNotes.Core/Models/DeskPosition.cs` | Identificador do monitor, lado e posição vertical do Desk. |
| `DearNotes/ViewModels/MainViewModel.cs` | Seleção, busca, filtro do Desk, contagem de notas visíveis e ações da janela principal, incluindo a confirmação de exclusão e a delegação da criação e do salvamento ao repositório. |
| `DearNotes/ViewModels/DeskViewModel.cs` | Visão das notas fixadas, estado do Desk, criação de nota direto do Desk e transições de abertura, fechamento, expansão e mudança de lado. |
| `DearNotes/Views/MainWindow.xaml` | Layout, estilos e bindings da interface, incluindo a moldura personalizada, a lista lateral, os chips de filtro e o editor em cartão. |
| `DearNotes/Views/MainWindow.xaml.cs` | Interações visuais, foco, atalhos e botões de minimizar, maximizar e fechar da janela principal. |
| `DearNotes/Views/Converters/RelativeTimeConverter.cs`, `ShortDateConverter.cs` e `PreviewTextConverter.cs` | Conversores de apresentação da lista e do editor: tempo relativo, data curta e prévia do conteúdo em uma linha. |
| `DearNotes/Views/Converters/TabLabelConverter.cs` | Título da nota em maiúsculas e em uma linha para a aba vertical das cartas do Desk. |
| `DearNotes/Views/Converters/DeskFilterConverter.cs` | Liga os chips **Todas** e **No Desk** ao estado de filtro do ViewModel. |
| `DearNotes/Views/DeskWindow.xaml` e `DearNotes/Views/DeskWindow.xaml.cs` | Baralho lateral, cartas do leque, eventos de mouse, mensagens do Windows, posicionamento do editor e ciclo de vida da janela. |
| `DearNotes/Views/DeskLayoutService.cs` | Estado de posicionamento do Desk: monitor atual, lado, largura, posição vertical, encaixe, passo e altura disponível das cartas do leque, posição da faixa de pílulas e do botão **+**, e persistência da posição. |
| `DearNotes/Views/DeskDragController.cs` | Arraste do baralho com o botão direito: captura do mouse, deslocamento, escolha do monitor, lado e recuperação de falhas do Win32. |
| `DearNotes/Views/DeskFanAnimator.cs` | Abertura e recolhimento do leque, entrada das telhas com 45 ms de intervalo, deslize da carta escolhida, transições do baralho e do botão **+**, largura da janela e atraso de recolhimento ao sair do Desk. |
| `DearNotes/Views/DeskKeyboardNavigator.cs` | Atalhos e navegação por teclado do Desk: abertura e recolhimento do leque, abertura de notas e retorno do foco ao baralho. |
| `DearNotes/Views/NoteEditorWindow.xaml` e `DearNotes/Views/NoteEditorWindow.xaml.cs` | Janela do editor aberta pelo Desk, com entrada animada na direção do baralho, posicionada no monitor atual e arrastável pela barra superior. |
| `DearNotes.Core/Services/DeskGeometry.cs` | Cálculos puros de encaixe, posição vertical normalizada, escolha de lado, disposição do leque (aba visível, deslize da carta e posição do botão **+**), centralização do editor e posição do arraste. |
| `DearNotes.Core/Services/NotesAutoSaver.cs` | Fila de gravação em segundo plano: mantém uma escrita por vez e substitui o snapshot pendente pelo mais recente. |
| `DearNotes.Core/Services/INoteStore.cs` e `NoteStorage.cs` | Contrato e implementação da leitura, validação, migração do formato antigo, criptografia do envelope e gravação do arquivo de notas. |
| `DearNotes.Core/Services/INoteEncryption.cs`, `IKeyProvider.cs`, `EncryptedNotePayload.cs` e `AesGcmNoteEncryption.cs` | Contratos de criptografia e de fornecimento da chave, payload cifrado e implementação AES-GCM com nonce, tag e identificador da chave. |
| `DearNotes.Core/Services/ProtectedNotesFile.cs` | Envelope JSON interno que envolve o documento cifrado de `notes.json`. |
| `DearNotes/Services/DpapiKeyProvider.cs` | Criação e leitura de `notes.key`, com a chave protegida pelo DPAPI no escopo do usuário atual. |
| `DearNotes.Core/Services/IDeskPositionStore.cs` e `DeskPositionStore.cs` | Contrato e implementação da leitura, validação e gravação de `desk_position.json`. |
| `DearNotes.Core/Services/AppPaths.cs` | Centralização dos caminhos de armazenamento e de log. |
| `DearNotes/Services/INotesRepository.cs` | Contrato da coleção compartilhada e do autosave consumido pelos ViewModels. |
| `DearNotes/Services/NotesRepository.cs` | Coleção compartilhada, acompanhamento das alterações, autosave e comunicação de falhas de salvamento. |
| `DearNotes/Services/IDialogService.cs` e `MessageBoxDialogService.cs` | Contrato e implementação dos avisos e confirmações exibidos ao usuário, sem `MessageBox` nas Views. |
| `DearNotes/ViewModels/RelayCommand.cs` | Implementação de `ICommand` usada pelas ações dos ViewModels. |
| `DearNotes/Themes/Theme.xaml` | Paleta de brushes compartilhada pelas janelas, carregada em `App.xaml`. |
| `DearNotes/Services/IScreenService.cs` e `DeskScreenService.cs` | Contrato e implementação da consulta dos monitores, área útil, DPI efetivo, posição do cursor e dimensões e posicionamento tolerante a falhas da janela via Win32. |
| `DearNotes/Services/StorageMigration.cs` | Cópia única dos dados da instalação anterior (`SideNotes`) para a pasta atual. |
| `DearNotes/Services/WindowFrameService.cs` | Cantos arredondados da janela via DWM e limites de maximização na área útil do monitor via `WM_GETMINMAXINFO`, com degradação silenciosa em versões anteriores do Windows. |
| `DearNotes/Services/AppLogger.cs` | Registro de falhas inesperadas em arquivo, com rotação por tamanho. |
| `DearNotes.Tests/DeskGeometryTests.cs` | Casos de encaixe, DPI, posição relativa, escolha de lado, disposição do leque, deslize da carta, botão **+**, editor e arraste. |
| `DearNotes.Tests/NoteStorageTests.cs` e `DeskPositionStoreTests.cs` | Gravação, leitura, validação, migração, criptografia, limites e preservação de arquivos inválidos. |
| `DearNotes.Tests/AesGcmNoteEncryptionTests.cs` | Ida e volta da criptografia, nonces únicos, adulteração de conteúdo e tag, chave incorreta e tamanhos inválidos. |
| `DearNotes.Tests/FixedKeyProvider.cs` | Chave fixa usada apenas nos testes para tornar a criptografia determinística e isolada do DPAPI. |
| `DearNotes.Tests/NotesAutoSaverTests.cs` | Gravação em segundo plano, coalescência de snapshots, falhas e gravação síncrona de emergência. |
| `DearNotes.Tests/NoteTests.cs` | Identidade, datas, notificação de atualização e independência das cópias usadas pelo autosave. |
| `DearNotes.Tests/MainViewModelTests.cs` e `DeskViewModelTests.cs` | Busca, filtros, seleção, fixação no Desk, exclusão confirmada, confirmação de fechamento, avisos e estado do leque. |
| `DearNotes.Tests/FakeNotesRepository.cs` e `RecordingDialogService.cs` | Dublês de `INotesRepository` e `IDialogService` usados nos testes de ViewModel. |
| `DearNotes/app.manifest` | Configuração de reconhecimento de DPI por monitor (`PerMonitorV2`). |
| `AGENTS.md` | Regras de arquitetura, convenções de código, comandos e restrições; deve ser consultado antes de alterar o projeto. |
| `.editorconfig` e `Directory.Build.props` | Estilo de código compartilhado, analisadores no nível mais recente e avisos tratados como erro. |
| `.gitattributes` | Normalização de fim de linha entre o índice e a cópia de trabalho no Windows. |
| `.github/workflows/ci.yml` | Pipeline de integração contínua: formatação, build e testes em `windows-latest`. |

`App.xaml`, `App.xaml.cs`, `AssemblyInfo.cs` e `app.manifest` permanecem na raiz do projeto WPF. O `README.md` e o `AGENTS.md` ficam na raiz do repositório.

Os dois ViewModels utilizam a mesma instância de `NotesRepository` por meio de `INotesRepository` e compartilham os mesmos objetos `Note`. A janela do editor usa o `DeskViewModel` já existente, sem manter cópia própria das notas. O autosave fica centralizado no repositório; cada janela mantém seu próprio estado de interface. O repositório expõe o evento `NoteChanged`, usado pelo Desk para acompanhar alterações de fixação sem duplicar o acompanhamento individual das notas. A geometria do Desk vive em `DeskGeometry`, permitindo testar o posicionamento sem abrir janelas. O `DeskLayoutService` mantém o monitor atual, a largura, a posição vertical e o encaixe, aplicando essa geometria à janela, ao baralho e às cartas do leque. O `DeskDragController` cuida do arraste do baralho, da captura do mouse e da troca de monitor. O `DeskFanAnimator` centraliza a abertura e o recolhimento do leque, as animações escalonadas das cartas, o deslize da carta escolhida, as transições do baralho e do botão **+** e o atraso de recolhimento. O `DeskKeyboardNavigator` concentra os atalhos e a movimentação de foco entre o baralho e as cartas. O `DeskViewModel` também cria a nota do botão **+**, aplicando o limite de notas fixadas e abrindo o editor. O acesso a arquivos e telas é injetado por `INoteStore`, `IDeskPositionStore` e `IScreenService`. O `NotesRepository` recebe a fila `NotesAutoSaver` e a forma de postar ações na interface em vez de criá-las internamente; a fila recebe cópias das notas, executa a gravação em segundo plano e descarta snapshots intermediários quando já existe um mais recente; ao fechar, a janela principal aguarda a fila terminar antes de encerrar.

A criptografia também é injetada: o Core define `INoteEncryption` e `IKeyProvider`, `AesGcmNoteEncryption` implementa o envelope AES-GCM e o aplicativo WPF fornece a chave por meio de `DpapiKeyProvider`. Assim, os testes usam uma chave fixa e não dependem do DPAPI nem do perfil do Windows. A chave nunca é gravada em texto puro e é zerada da memória após cada operação de criptografia ou descriptografia.

A janela principal combina o `WindowChrome` declarado no XAML, responsável por arraste, redimensionamento e botões da janela, com o `WindowFrameService`, que concentra os ajustes nativos de cantos arredondados e maximização. Os conversores de `Views/Converters` apenas formatam os dados para exibição e não alteram as notas.

## Próximas etapas

- [ ] Refinar o comportamento da busca durante a edição de notas.
- [x] Melhorar o design do modo Janela.
- [x] Compartilhar as mesmas notas entre os modos **Desk** e Janela.
- [x] Permitir selecionar até cinco notas para exibição no Desk.
- [x] Permitir arrastar o Desk entre monitores e os lados esquerdo e direito da tela.
- [x] Respeitar a área útil do monitor escolhido e o espaço ocupado pela barra de tarefas.
- [x] Recuperar o Desk no monitor principal quando a tela utilizada for desconectada.
- [x] Adaptar o Desk a monitores com escalas de DPI diferentes.
- [x] Salvar as preferências do Desk, incluindo monitor, posição e notas escolhidas.
- [x] Gravar as notas em segundo plano sem bloquear a interface.
- [x] Versionar o formato de `notes.json` e migrar automaticamente o formato antigo.
- [x] Cifrar as notas em repouso com chave protegida pelo DPAPI.
- [x] Consultar o DPI efetivo de cada monitor e sobreviver a falhas de posicionamento.
- [x] Refinar o visual do leque do Desk.
- [x] Suavizar as animações de abertura e recolhimento.

## Autor

Desenvolvido por **Gabriel Braga** como projeto de aprendizado e portfólio.
