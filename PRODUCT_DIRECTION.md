# Contorno — Direção do produto

## Objetivo inicial

Ajudar o usuário a dar nome às receitas e despesas, entender o que entra e sai e tomar decisões melhores com o dinheiro que já tem. O foco desta fase é validar regras e experiência em uma aplicação executada localmente antes de preparar o sistema como produto hospedado.

## Jornada principal

```text
Boas-vindas → Login/Cadastro → Onboarding → Dashboard
```

O onboarding coleta:

- capital total disponível no início do acompanhamento;
- perfil profissional CLT ou PJ;
- forma preferida de registrar a renda: manual, fixa mensal ou lembrete mensal.

CLT e PJ são informações de personalização, não níveis de permissão. Ambos começam com registro manual; CLT pode optar por salário fixo e PJ pode configurar um lembrete mensal.

## Regras adotadas

- O saldo total é derivado de `capital inicial + todas as receitas confirmadas - todas as despesas confirmadas`.
- O saldo do mês é derivado de `receitas confirmadas do mês - despesas confirmadas do mês`.
- Lançamentos apenas planejados não alteram o saldo total nem o saldo do mês até serem confirmados.
- Receitas e despesas usam uma única entidade de lançamento, diferenciada pelo tipo.
- Um lançamento fixo possui uma regra recorrente separada das ocorrências mensais.
- Despesas parceladas têm valor por parcela, data inicial e mês final; o valor total da compra é opcional e informativo, sem entrar novamente no saldo.
- Cada parcela aparece no mês correspondente e a série para automaticamente após a última. Parcelas futuras são planejadas até a data chegar.
- Lançamentos recorrentes visualizados em meses futuros são planejados; ao chegar a data, tornam-se confirmados.
- A renda fixa informada durante o onboarding começa no mês seguinte, evitando somá-la duas vezes ao capital que o usuário informou como atual.
- No panorama, “Recebimento do mês” mostra apenas o salário ou recebimento principal confirmado e identificado pelo usuário; “Receitas do mês” inclui esse valor e as demais entradas. Contas CLT e PJ podem identificar ou corrigir manualmente esse recebimento. A renda fixa CLT configurada no cadastro já nasce identificada, mas só conta quando sua ocorrência estiver confirmada.
- O lembrete mensal de renda PJ usa um dia fixo entre 1 e 28, válido em todos os meses. Ele aparece a partir desse dia enquanto o recebimento do mês não estiver registrado como confirmado; outras receitas não dispensam o aviso.
- O usuário PJ pode registrar o recebimento antes do dia do lembrete, usando o botão no dashboard.
- Etiquetas de receita e despesa são criadas pelo próprio usuário e podem ser usadas para filtrar os lançamentos. Cada etiqueta e todos os dados financeiros pertencem ao usuário autenticado.
- Cada lançamento pode ter uma observação opcional. Em séries mensais ou parceladas, ela acompanha as ocorrências criadas automaticamente.
- O capital informado no cadastro já inclui os investimentos existentes. O valor aplicado inicial é apenas uma distribuição desse capital, não uma nova receita.
- Aportes e resgates transferem valor entre disponível e aplicado sem alterar o saldo total. O valor aplicado nesta fase representa o principal informado e movimentado manualmente, sem rentabilidade de mercado.
- Um aporte pode ser identificado como vindo da renda recebida no mês, seja salário CLT ou recebimento PJ. A receita deve ser registrada antes; essa identificação não cria receita nem aumenta o saldo total. O painel mostra separadamente quanto dos aportes do mês foi informado como vindo dessa renda.
- O valor aplicado pode ser dividido em grupos pessoais, como CDBs, fundos imobiliários e imóveis. Essa divisão é uma fotografia manual do principal aplicado: a soma dos grupos não pode superar o aplicado, a diferença aparece como “sem classificar”, e excluir um grupo não elimina o investimento nem altera o saldo. Cotações e rendimentos automáticos ficam para uma etapa posterior.
- Imóveis ou outros bens que não estavam incluídos no capital/aplicado informado não devem ser somados a essa divisão; para eles, será necessário um controle patrimonial próprio, separado do saldo financeiro, antes de incorporar avaliações e patrimônio líquido.
- A experiência de organização do capital apresenta três partes independentes: o que já estava investido no cadastro, o que foi investido ou resgatado no mês e a divisão atual da carteira. O planejamento mensal separa a escolha do limite da decisão de acumular a sobra, deixando claro que metas de gastos não transferem dinheiro para investimentos.
- O limite de gastos é definido por mês e não movimenta dinheiro. O restante do limite considera as despesas confirmadas do mês.
- O usuário pode marcar, em cada limite mensal, que a sobra seja somada ao valor que definir para o mês seguinte. O acúmulo só entra após o fechamento do mês e pode continuar enquanto os meses seguintes também estiverem marcados.
- Sem essa opção, o próximo limite não aumenta. Em ambos os casos, a sobra já integra o saldo disponível; o planejamento não cria receita nem aporte automático.

## Dashboard inicial

O dashboard foi dividido em três áreas:

- **Visão geral:** saldo total, saldo do mês, receitas, despesas, resumo entre disponível e aplicado e lançamentos recentes.
- **Investimentos:** tela própria para informar o valor aplicado inicial e registrar aportes ou resgates entre disponível e aplicado.
- **Planejamento mensal:** tela exclusiva para definir o limite de gastos de cada mês e optar pelo acúmulo da sobra no mês seguinte.
- **Períodos:** consulta por hoje, últimos 7 ou 30 dias, mês atual, mês anterior, ano atual ou intervalo personalizado de até 366 dias. Os totais consideram apenas lançamentos confirmados.
- **Lançamentos:** tabela mensal e cadastro de receita ou despesa, com opção de repetição mensal.
- **Gráficos:** análise mensal do mês selecionado (entradas e saídas por semana e despesas por etiqueta) e visão anual (comparação mês a mês e resultado acumulado no ano). Consideram apenas lançamentos confirmados; o resultado acumulado não é o saldo total nem inclui o capital inicial.

## Direção visual

- Interface elegante e minimalista, com hierarquia clara e bastante respiro entre os elementos.
- Azul reservado principalmente para interações, foco, contornos e detalhes de destaque.
- Tema claro com azul mais luminoso e tema escuro com um azul mais profundo.
- Alternância de tema persistida no navegador e disponível em toda a jornada.
- Animações curtas e discretas, respeitando a preferência do usuário por movimento reduzido.
- Componentes responsivos e estados de foco visíveis para manter o uso intuitivo e acessível.

## Próximas decisões de produto

1. Diferenciar saldo real, valores previstos, contas pagas e contas pendentes.
2. Definir edição e encerramento de séries recorrentes.
3. Definir edição e exclusão de etiquetas, preservando o histórico dos lançamentos.
4. Decidir como contas bancárias, cartões e faturas entrarão no domínio.
5. Evoluir os gráficos com comparativos e metas adicionais após validar a utilidade das visões mensal e anual.
6. Aplicar a identidade visual definitiva.
7. Preparar testes automatizados, proteção CSRF e estratégia de publicação.
8. Evoluir a distribuição manual para uma carteira de investimentos com posições, valor de mercado, rendimentos e projeções; avaliar integrações oficiais apenas depois de consolidar a base e o tratamento de consentimento.
