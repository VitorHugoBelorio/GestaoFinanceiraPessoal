# Segurança — estado atual e próximos passos

## Sessão e proteção das requisições

- A autenticação usa cookie `HttpOnly`, não persistente, com validade de 30 minutos de inatividade e renovação enquanto o usuário está ativo. Em produção, o cookie exige HTTPS.
- Fechar o site não inicia nem mantém processamento periódico no servidor. O navegador não avisa o servidor de forma confiável quando uma aba ou janela é fechada; alguns navegadores também restauram cookies de sessão. Portanto, **não há garantia de logout instantâneo ao fechar**. Use “Sair da conta” para encerrar explicitamente. Após 30 minutos sem requisições, o ticket expira.
- Toda requisição que altera dados em `/api` exige token antifalsificação no cabeçalho `X-CSRF-TOKEN`. O frontend obtém o token automaticamente e o renova se ficar inválido. O endpoint de emissão não deve ser cacheado.
- A política padrão exige autenticação para novos endpoints, salvo exceções explícitas como cadastro, login, token antifalsificação e health check.

## Dados de cada usuário

- Os controladores atuais usam o identificador da conta autenticada para consultar e alterar dados. Uma conta não pode selecionar registros de outra apenas conhecendo seu ID.
- `server/tests/SecuritySmoke.ps1` cria duas contas temporárias, verifica acesso anônimo, leitura, edição, exclusão, etiquetas e resumo entre contas, e remove os dados de teste ao final. Deve continuar fazendo parte da verificação de novos recursos.

## Criptografia e implantação

- Senhas são armazenadas com `PasswordHasher`, não em texto puro. O cookie autenticado é protegido pelo ASP.NET Core Data Protection. Isso **não significa** que descrições, valores e observações estejam criptografados individualmente no banco.
- Antes de publicar: configurar HTTPS de ponta a ponta (inclusive proxy e banco), chaves do Data Protection persistentes e protegidas em armazenamento apropriado, segredos fora do repositório, conta do banco com privilégios mínimos, backups criptografados e política de retenção.
- Próximas camadas: limite de tentativas de login/cadastro, revisão de cabeçalhos de segurança/CSP, auditoria de ações sensíveis e testes automatizados de autorização para cada novo endpoint.
