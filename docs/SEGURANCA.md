# Segurança (DevSecOps)

Práticas de segurança aplicadas no código, na imagem e no pipeline, e o risco que cada uma reduz.

## Práticas automatizadas

### 1. Verificação de dependências vulneráveis no CI

No job `build-test` do [ci.yml](../.github/workflows/ci.yml), logo após o `dotnet restore`:

```
dotnet list package --vulnerable --include-transitive
```

O comando consulta a base de vulnerabilidades do NuGet para todos os pacotes, inclusive as dependências indiretas (transitivas). Por padrão ele só lista os problemas e não falha, então o passo procura a mensagem de pacote vulnerável na saída e encerra com erro se encontrá-la.

**Como contribui:** um pacote com CVE conhecida bloqueia o pull request antes do merge, em vez de chegar à imagem publicada.

### 2. Dependabot

O [.github/dependabot.yml](../.github/dependabot.yml) verifica toda semana três ecossistemas:

| Ecossistema | O que atualiza |
|---|---|
| `nuget` | Pacotes do .NET (agrupados num único PR) |
| `docker` | Imagens base do Dockerfile (`sdk:10.0` e `aspnet:10.0`) |
| `github-actions` | Versões das actions usadas no workflow |

**Como contribui:** reduz o tempo em que o projeto usa bibliotecas, imagens ou actions com CVEs conhecidas. Cada atualização chega como pull request e passa pelo mesmo CI (build, testes e verificação de vulnerabilidades) antes do merge.

### 3. CodeQL (análise estática de código)

Ativado em *Settings → Code security → Code scanning → CodeQL analysis → Set up → Default*, com a linguagem C#. Os resultados aparecem em *Security → Code scanning*.

**Como contribui:** encontra padrões de código inseguro, como injeção, uso inseguro de dados vindos do usuário ou criptografia fraca, a cada push e pull request, sem depender de revisão manual.

## Práticas de configuração

### Nenhum segredo no repositório

- O login no GHCR usa o `GITHUB_TOKEN`, gerado automaticamente pelo GitHub a cada execução e que expira ao fim do job.
- Qualquer credencial extra deve ir em *Settings → Secrets and variables → Actions*, nunca no código.
- O `.env` está no `.gitignore` e no `.dockerignore`. Só o `.env.example`, sem valores sensíveis, é versionado.

**Como contribui:** um vazamento do código-fonte não expõe credenciais, e não há token de longa duração para ser roubado.

### Permissões mínimas no workflow

```yaml
permissions:
  contents: read
```

O workflow inteiro só pode ler o repositório. O job `docker` é o único que recebe também `packages: write`, que é o necessário para publicar a imagem.

**Como contribui:** se uma action ou dependência do pipeline for comprometida, o token que ela recebe não consegue alterar código, abrir releases nem mexer em outras áreas do repositório.

### Imagem com usuário não-root e build em múltiplos estágios

- O [Dockerfile](../Dockerfile) compila no estágio `build` (imagem `sdk`) e copia só o resultado para o estágio final (imagem `aspnet`). O SDK, o código-fonte e os testes não vão para a imagem publicada.
- A aplicação roda como o usuário `app` (uid 1654), não como `root`.

**Como contribui:** a imagem final tem menos ferramentas e pacotes para um atacante explorar. Se a aplicação for comprometida, o processo não tem privilégio de root dentro do container.

## Não implementado

- **Trivy** (scan de vulnerabilidades da imagem Docker): fica como próximo passo. Hoje as imagens base são cobertas indiretamente pelo Dependabot (ecossistema `docker`).
