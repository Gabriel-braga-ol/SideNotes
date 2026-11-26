# AGENTS.md

O **DearNotes** é um aplicativo de notas para Windows feito em **C# com WPF (.NET 10)**. Ele permite criar, editar, buscar e excluir notas com salvamento automático, além de fixar até cinco notas em um painel lateral chamado **Desk**, que pode ser arrastado entre monitores. As notas são compartilhadas entre as janelas em memória e persistidas localmente em um JSON cifrado em `%LocalAppData%\DearNotes`, com a chave protegida pelo DPAPI. O projeto segue uma organização MVVM (Models, ViewModels, Views e Services), sem dependências externas, e é mantido como projeto de aprendizado e portfólio.

## Antes de cada tarefa

1. Leia o `README.md` — ele descreve o comportamento atual do aplicativo.
2. Leia este `AGENTS.md` — ele descreve as regras de arquitetura, convenções e limites.
3. Só então consulte o código e os commits recentes (`git log`) para confirmar como a funcionalidade funciona hoje.
4. Se a documentação divergir do código, trate o código como a fonte da verdade e **corrija a documentação na mesma alteração**.

## Comandos

> [!IMPORTANT]
> O projeto usa WPF e `net10.0-windows`: **só compila e executa no Windows**. Não é possível validar build ou execução em Linux/macOS.

Execute a partir da raiz do repositório (onde está `DearNotes.slnx`):

| Objetivo | Comando |
| --- | --- |
| Compilar a solução | `dotnet build .\DearNotes.slnx` |
| Executar o aplicativo | `dotnet run --project .\DearNotes\DearNotes.csproj` |
| Executar os testes | `dotnet test .\DearNotes.slnx` |
| Verificar a formatação | `dotnet format .\DearNotes.slnx --verify-no-changes` |
| Corrigir a formatação | `dotnet format .\DearNotes.slnx` |
| Limpar artefatos | `dotnet clean .\DearNotes.slnx` |
| Restaurar dependências | `dotnet restore .\DearNotes.slnx` |

A solução tem três projetos: `DearNotes` (WPF), `DearNotes.Core` (modelos, persistência e geometria) e `DearNotes.Tests` (xUnit). Antes de considerar uma alteração concluída, o build deve terminar com **0 erros e 0 avisos** e os testes devem passar.

## Estrutura do repositório

```text
DearNotes.slnx              # Solução (formato .slnx)
README.md                   # Comportamento, armazenamento e organização do código
AGENTS.md                   # Este arquivo: regras para pessoas e agentes
.editorconfig               # Estilo, nomenclatura e severidade dos analisadores
Directory.Build.props       # Analisadores no nível mais recente e avisos como erro
.gitattributes              # Normalização de fim de linha (CRLF na cópia de trabalho)
.github/
  workflows/ci.yml          # Formatação, build e testes no Windows
.opencode/
  skills/                   # Skills locais do OpenCode (instruções para agentes)
DearNotes/
  App.xaml, App.xaml.cs     # Composition root: criptografia, repositório, VMs e janelas
  app.manifest              # asInvoker + reconhecimento de DPI por monitor (PerMonitorV2)
  AssemblyInfo.cs
  ViewModels/
    MainViewModel.cs        # Seleção, busca, filtro do Desk e ações da janela principal
    DeskViewModel.cs        # Notas fixadas, criação, leque e estado do Desk
    RelayCommand.cs         # ICommand usada pelas ações dos ViewModels
  Themes/
    Theme.xaml              # Paleta de brushes compartilhada, carregada em App.xaml
  Views/
    MainWindow.xaml(.cs)    # Moldura, lista, busca, filtros e editor da janela principal
    DeskWindow.xaml(.cs)    # Baralho lateral, cartas do leque e ciclo de vida
    DeskLayoutService.cs    # Posicionamento, baralho e disposição do leque
    DeskDragController.cs   # Arraste do baralho, captura do mouse e monitores
    DeskFanAnimator.cs      # Leque em telhas, deslize da carta escolhida e recolhimento
    DeskKeyboardNavigator.cs # Foco, atalhos e navegação por teclado do Desk
    NoteEditorWindow.xaml(.cs) # Editor do Desk com entrada animada
    Converters/
      RelativeTimeConverter.cs # Data de atualização como texto relativo
      ShortDateConverter.cs  # Data local no formato curto do rodapé
      PreviewTextConverter.cs # Prévia do conteúdo em uma única linha
      DeskFilterConverter.cs # Estado dos chips "Todas" e "No Desk"
      TabLabelConverter.cs   # Título em maiúsculas na aba vertical do Desk
  Services/
    INotesRepository.cs     # Contrato da coleção compartilhada e do autosave
    NotesRepository.cs      # Coleção compartilhada, autosave e eventos
    DpapiKeyProvider.cs     # Cria/abre notes.key com DPAPI CurrentUser
    DeskScreenService.cs    # Monitores, DPI e posicionamento via Win32
    IScreenService.cs       # Contrato de acesso a monitores e janelas
    WindowFrameService.cs   # Cantos arredondados (DWM) e maximização na área útil
    StorageMigration.cs     # Cópia única dos dados da instalação anterior
    AppLogger.cs            # Registro de falhas inesperadas em arquivo
DearNotes.Core/
  Models/
    Note.cs                 # Dados da nota, limite do Desk e notificações
    NoteDocument.cs         # Representação persistida de uma nota
    NotesDocument.cs        # Documento versionado de notes.json
    DeskPosition.cs         # Monitor, lado e posição vertical do Desk
  Services/
    AppPaths.cs             # Caminhos de armazenamento e de log
    NoteStorage.cs          # Leitura, validação, cifragem e gravação de notes.json
    INoteStore.cs           # Contrato de persistência das notas
    IKeyProvider.cs         # Contrato de fornecimento da chave de criptografia
    INoteEncryption.cs      # Contrato de criptografia das notas
    EncryptedNotePayload.cs # Nonce, tag, texto cifrado e identificador da chave
    AesGcmNoteEncryption.cs # Implementação AES-GCM de 256 bits
    ProtectedNotesFile.cs   # Envelope JSON que envolve o documento cifrado
    DeskPositionStore.cs    # Leitura e gravação de desk_position.json
    IDeskPositionStore.cs   # Contrato de persistência da posição do Desk
    DeskGeometry.cs         # Cálculos puros de encaixe, lado e posicionamento
    NotesAutoSaver.cs       # Fila de gravação em segundo plano com snapshots
DearNotes.Tests/
  TemporaryDirectory.cs     # Pasta temporária descartável para testes de IO
  FixedKeyProvider.cs       # Chave fixa para testes, sem DPAPI
  DeskGeometryTests.cs      # Testes da geometria do Desk e do editor
  NoteStorageTests.cs       # Testes de notes.json, criptografia e backups
  AesGcmNoteEncryptionTests.cs # Testes de AES-GCM, adulteração e chave errada
  DeskPositionStoreTests.cs # Testes de desk_position.json
  NotesAutoSaverTests.cs    # Testes da fila de autosave
  NoteTests.cs              # Identidade, metadados e cópia para o autosave
  MainViewModelTests.cs     # Busca, filtros, fixação, exclusão, fechamento e avisos
  DeskViewModelTests.cs     # Notas fixadas, criação, abertura e estado do leque
  FakeNotesRepository.cs    # Repositório falso para os testes de ViewModel
  RecordingDialogService.cs # Diálogos falsos para os testes de ViewModel
```

Dados persistidos (nunca versione esses arquivos nem os trate como código):

- `%LocalAppData%\DearNotes\notes.json` — envelope JSON com o documento de notas cifrado (AES-GCM), identificadores, datas e estado de fixação no Desk.
- `%LocalAppData%\DearNotes\notes.key` — chave de 256 bits protegida pelo DPAPI no escopo do usuário atual. Sem ela, `notes.json` não pode ser recuperado.
- `%LocalAppData%\DearNotes\desk_position.json` — monitor, lado e posição do Desk.
- `%LocalAppData%\DearNotes\notes-invalid-<id>.json` — cópia preservada quando o arquivo principal é inválido, mantendo no máximo as **5** mais recentes.
- `%LocalAppData%\SideNotes\...` — instalação anterior; origem da migração automática, mantida como cópia de segurança.

Limites conhecidos: no máximo **5 notas fixadas** no Desk, **5 cópias inválidas** preservadas e autosave **1 segundo após a última alteração**. A gravação ocorre em segundo plano, com apenas o snapshot mais recente pendente, e novas tentativas em intervalos crescentes (até 30 s) quando falha.

## Regras de arquitetura

- **MVVM:** Models guardam dados; ViewModels guardam estado e ações; Views contêm apenas layout, bindings e comportamento visual; Services concentram IO, Win32 e regras de infraestrutura.
- **Ações e avisos:** botões disparam `ICommand` expostos pelo ViewModel, não `Click` no code-behind. Mensagens e confirmações ao usuário passam por `IDialogService`; ViewModels e Views não chamam `MessageBox` diretamente. Exclusões pedem confirmação no ViewModel, inclusive quando disparadas por atalho.
- **Tema:** cores e brushes compartilhados ficam em `Themes/Theme.xaml` e são referenciados por `StaticResource`. A cor individual da nota continua vindo dos dados (`Note.Color`).
- **Núcleo independente:** `DearNotes.Core` concentra modelos, persistência e geometria pura e **não referencia** o projeto WPF nem janelas. O aplicativo referencia o Core; os testes referenciam o Core e, para os testes de ViewModel, o projeto WPF — nunca o contrário.
- **Dependências apontam para dentro:** ViewModels não conhecem janelas nem `MessageBox`; Views não acessam arquivos nem APIs do Windows. Se uma View precisar de lógica, ela pertence ao ViewModel ou a um Service.
- **Fonte única de verdade:** `NotesRepository` é a coleção compartilhada e o único ponto de autosave. Os ViewModels dependem de `INotesRepository`, não da implementação. `NotesAutoSaver` apenas executa as gravações em segundo plano; não crie timers de salvamento nem cópias da lista em ViewModels ou Views.
- **Autosave em segundo plano:** a serialização e a escrita recebem um snapshot imutável das notas e nunca rodam na thread da interface. Uma única gravação pode estar em andamento; snapshots pendentes são substituídos pelo mais recente. Uma falha inesperada libera a fila e é comunicada como falha de gravação, para que o encerramento nunca fique esperando. Não grave notas diretamente na thread de UI.
- **Persistência isolada:** somente `Services` grava ou lê arquivos. Caminhos vêm de `AppPaths`; nunca hardcode caminhos de dados no código.
- **Criptografia em repouso:** `notes.json` é sempre gravado como envelope cifrado por `AesGcmNoteEncryption`, com chave fornecida por `IKeyProvider` e protegida pelo DPAPI. Não grave notas em texto puro no disco, não registre chaves, nonces ou conteúdo em log e não desative a validação da tag para "recuperar" um arquivo. A chave (`notes.key`) deve ser copiada junto de qualquer backup manual; sem ela, as notas cifradas são irrecuperáveis.
- **Envelope versionado:** mudanças no envelope exigem incremento de `ProtectedNotesFile.CurrentFormatVersion`, migração dos arquivos existentes (incluindo os cifrados), testes com o formato anterior e atualização do README. O `SchemaVersion` interno continua versionando o documento de notas.
- **`Note` é compartilhada por referência:** os objetos são os mesmos entre MainWindow e Desk. Não clone notas para editar; alterações disparam `PropertyChanged` e o autosave.
- **Geometria testável:** cálculos de encaixe, lado, posição normalizada, disposição do leque (aba visível, deslize da carta, escolha da carta pela posição do ponteiro e posição do botão **+**) e centralização do editor ficam em `DeskGeometry`, sem depender de janelas. Não duplique essa matemática no code-behind.
- **Layout do Desk:** o estado de posicionamento (monitor atual, lado, largura e posição vertical), o encaixe, o passo e a altura disponível das cartas do leque e a posição da faixa de pílulas e do botão **+** ficam em `DeskLayoutService`. O Desk encosta na borda lateral (`DeskGeometry.SideMargin`) e mantém folga vertical (`ScreenMargin`). O code-behind da janela apenas encaminha eventos para esse serviço.
- **Arraste do Desk:** o botão direito arrasta a partir de qualquer parte do Desk (a raiz da janela), já que a faixa de pílulas some quando o leque abre. `DeskDragController` conduz a captura do mouse, o deslocamento do baralho e a troca de monitor. A janela apenas conecta os eventos e mantém o estado visual.
- **Animação do Desk:** `DeskFanAnimator` concentra a abertura e o recolhimento do leque, a entrada das telhas com 45 ms de intervalo, o deslize da carta escolhida (pelo ponteiro, conforme a geometria fixa das abas e nunca pelo `IsMouseOver` das cartas, que se movem; ou pelo foco do teclado; liberado só depois que as telhas assentam), as transições das cartas, do baralho e do botão **+**, a largura da janela e o atraso de recolhimento. A janela não deve duplicar esses estados nem animar as cartas diretamente.
- **Acessibilidade do Desk:** `DeskKeyboardNavigator` concentra os atalhos e a navegação por teclado. O baralho e as cartas do leque devem ser focáveis, responder a Enter/Espaço e Escape, percorrer as notas com Tab e as setas e mostrar o foco atual; `AutomationProperties` descrevem as ações para leitores de tela.
- **Criação pelo Desk:** o botão **+** do Desk usa `CreateNoteCommand` do `DeskViewModel`, que cria a nota já fixada, respeita `Note.MaxDeskPinnedNotes`, avisa pelo `IDialogService` quando o limite é atingido e abre o editor. A View apenas faz o binding do comando.
- **Win32 isolado:** chamadas nativas ficam em `DeskScreenService`, atrás de `IScreenService`, e em `WindowFrameService`, que ajusta a moldura personalizada da janela principal. Code-behind não deve importar `user32.dll` diretamente. Verifique o retorno das APIs, valide identificadores de janela e use `TryMoveWindow`/`TrySetWindowBounds` para posicionamento; falhas esperadas são registradas em log de depuração e não devem derrubar o aplicativo. Callbacks nativos não podem lançar exceções.
- **Moldura da janela principal:** a barra nativa é substituída por `WindowChrome` no XAML; o arraste, a maximização e o menu do sistema continuam funcionando. `WindowFrameService` aplica os cantos arredondados do DWM quando disponíveis e limita a janela maximizada à área útil do monitor em `WM_GETMINMAXINFO`, para não cobrir a barra de tarefas. Em versões anteriores do Windows, a falha do DWM degrada para cantos retos sem encerrar o aplicativo. Os botões minimizar, maximizar e fechar são comportamento visual da View; as demais ações continuam em `ICommand`.
- **Formatação na View:** conversores de apresentação (tempo relativo, data curta, prévia de uma linha, rótulo da aba do Desk e estado dos chips) ficam em `Views/Converters`, sem regra de negócio e sem acessar arquivos. Textos gerados para a interface seguem o português.
- **DPI por monitor:** use `DeskScreen.Dpi`, consultado via `GetDpiForMonitor`, nos cálculos de posicionamento. Não dependa da DPI atual da janela antes de ela estar no monitor de destino; atualize a geometria em `OnDpiChanged` e em mudanças de monitor.
- **Testes:** comportamento novo em `DeskGeometry`, nos stores ou em regras do Core exige teste em `DearNotes.Tests`. Testes de IO usam `TemporaryDirectory` e nunca tocam em `%LocalAppData%`. Casos de monitores estreitos, margens maiores que a área útil e limites de DPI devem ser cobertos pela geometria pura.
- **Falhas explícitas:** erros de leitura/gravação de notas devem ser informados ao usuário e preservar os dados em memória; falhas de posição do Desk usam o padrão e são registradas em log de depuração. Nunca ignore uma falha de IO silenciosamente. Falhas de criptografia ou de integridade na abertura (chave ausente, chave incorreta ou envelope adulterado) devem encerrar o aplicativo **sem sobrescrever** `notes.json`. Falhas de gravação, inclusive de criptografia, geram um único aviso e novas tentativas com atraso crescente, sem substituir o arquivo válido; ao fechar, aguarde a fila terminar e ofereça a opção de fechar mesmo assim se a gravação falhar.
- **Log sem dados sensíveis:** falhas inesperadas são registradas em `%LocalAppData%\DearNotes\logs\app.log`; nunca registre título ou conteúdo de notas no log.
- **Instância única:** o aplicativo usa um `Mutex` nomeado. Não contorne essa proteção nem abra duas instâncias sobre o mesmo armazenamento.
- **Evolução:** novas dependências externas exigem justificativa no README; hoje a única dependência externa é o xUnit, restrito ao projeto de testes. DPAPI e AES-GCM são APIs do próprio .NET para Windows e não contam como dependências externas. Prefira extrair interfaces quando o código precisar ser testado, sem criar camadas cerimoniais.
- **Compatibilidade de dados:** `notes.json` é um envelope cifrado com `Protection` e `FormatVersion` (atualmente **1**). O documento decifrado usa `SchemaVersion` (atualmente **1**) e contém a lista de notas. Mudanças de formato exigem incremento da versão, migração dos dados existentes, testes com o formato anterior e atualização do README. Os formatos legados — lista simples de notas e documento versionado em texto puro — continuam sendo lidos e são cifrados no próximo salvamento. Versões futuras são tratadas como dados inválidos, com cópia de segurança preservada; são mantidas no máximo **5** cópias `notes-invalid-*`. Mudanças de nome ou de pasta de armazenamento exigem migração automática e preservação dos arquivos antigos.
- **Cada funcionalidade nova:** crie os arquivos nas pastas e namespaces corretos (`DearNotes.Models`, `DearNotes.ViewModels`, `DearNotes.Views`, `DearNotes.Services`) e atualize o README e este arquivo.

## Convenções de código

- O código deve seguir os princípios **SOLID** e **Clean Code**, mantendo o estilo já existente no projeto (C# moderno, `Nullable` habilitado, tipos explícitos quando ajudam a leitura).
- **Métodos não devem acumular funções:** cada método faz uma única coisa; se a descrição precisa de "e", divida.
- **Comentários explicam o porquê** de uma decisão não óbvia (transação, cópia histórica, tolerância de relógio); nunca repetem o que o código já diz.
- **Nomes de variáveis dizem o que armazenam**, sem abreviações confusas e sem o tipo no nome.
- Use `PascalCase` para tipos, propriedades, métodos e eventos; `camelCase` para parâmetros, locais e campos (seguindo o existente, sem prefixo `_`). Sufixe métodos assíncronos com `Async`.
- Identificadores e código em **inglês**; textos de interface e mensagens ao usuário em **português**.
- Uma classe por arquivo, nome do arquivo igual ao da classe. Evite `using` não utilizado e código comentado.
- Evite números mágicos: nomeie constantes ou use configuração quando o valor tiver significado de domínio.
- **Estilo verificado por ferramenta:** `.editorconfig` define formatação, nomenclatura e severidade; `Directory.Build.props` liga os analisadores e trata avisos como erro. Não desative uma regra nem use `#pragma` para passar o build — corrija o código. Nomes de teste usam `Metodo_Cenario_Resultado` e por isso `CA1707` está desativado apenas em `DearNotes.Tests/.editorconfig`.
- **Antes de commitar:** rode `dotnet format` e confirme que `dotnet format --verify-no-changes` termina com sucesso. A CI repete a verificação, o build e os testes.

## Documentação

Atualize o **README.md** e este **AGENTS.md** no **mesmo commit** sempre que uma alteração mudar:

- comportamento visível ao usuário (fluxos, atalhos, mensagens, limites);
- arquivos, pastas ou responsabilidades de classes;
- formato ou local dos dados persistidos;
- comandos de build, execução ou teste;
- dependências, requisitos de sistema ou plataforma suportada.

## O que não fazer

### Código

- **Não usar tipos genéricos/inseguros para "calar" erros:** proibido usar `any`, `@ts-ignore`, `# type: ignore`, `dynamic` ou suprimir warnings do linter/compilador para fazer o código passar. Em C#, isso inclui `#pragma warning disable`, `[SuppressMessage]` sem justificativa real, o operador `!` (null-forgiving) sem garantia documentada e `object` para evitar tipagem correta.
- **Não deixar código morto ou placeholders:** nunca entregue respostas com `// TODO: implementar aqui`, `pass`, `NotImplementedError`, `throw new NotImplementedException()` ou funções vazias simulando comportamento.
- **Não engolir exceções:** proibido usar blocos `try/catch` vazios ou capturar exceções genéricas (`Exception`, `catch (e) { }`) sem logar ou tratar adequadamente. Prefira exceções específicas com filtro `when`; quando a falha for esperada, retorne um resultado explícito.
- **Não hardcodar valores sensíveis ou de ambiente:** nunca insira URLs, chaves de API, credenciais, portas ou caminhos absolutos locais diretamente no código. Use `AppPaths`/`Environment` para caminhos. Chaves de criptografia nunca devem ser fixas no código de produção; nos testes, use apenas as chaves fixas isoladas em `FixedKeyProvider`.
- **Não enfraquecer a criptografia:** não remova a autenticação AES-GCM, não reutilize nonces, não grave a chave em texto puro e não trate falhas de descriptografia como arquivo vazio para forçar a abertura do aplicativo.

### Git

- **Não commitar direto na `main`/`master`:** antes de commitar, confirme que está em uma branch de feature/fix (`git branch --show-current`). Se não estiver, crie uma antes.
- **Não fazer `git push` sem confirmação:** alterações ficam apenas locais até o desenvolvedor revisar o `git diff` e autorizar o envio ao remoto.
- **Mensagens de commit:** seguem a skill local `conventional-commit-review`, que define tipo, escopo e tamanho a partir do histórico do repositório.
