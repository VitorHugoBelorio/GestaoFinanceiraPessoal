import { Link } from 'react-router-dom'
import * as m from 'motion/react-m'
import { PublicHeader } from '../components/layout/PublicHeader'
import { fadeUpItem, panelVariants, staggerContainer } from '../motion/variants'

export function WelcomePage() {
  return (
    <div className="public-shell">
      <PublicHeader />
      <main className="welcome">
        <section className="welcome-hero">
          <div className="welcome-copy">
            <span className="eyebrow">01 / Controle financeiro pessoal</span>
            <h1>Clareza para cuidar do que é <em>seu.</em></h1>
          </div>
          <div className="welcome-detail">
            <p>Dê nome ao que entra e sai. Organize receitas, despesas e compromissos recorrentes para entender o que fica e decidir melhor o próximo passo.</p>
            <div className="welcome-actions">
              <Link className="button button-primary button-large" to="/cadastro">Começar agora <span aria-hidden="true">↗</span></Link>
              <Link className="button button-ghost button-large" to="/acesso">Acessar minha conta <span aria-hidden="true">→</span></Link>
            </div>
            <p className="welcome-note">Seu ponto de partida, suas regras.</p>
          </div>
        </section>

        <section className="welcome-steps" aria-labelledby="welcome-steps-title">
          <m.div
            className="steps-heading"
            variants={panelVariants}
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, amount: 0.35 }}
          >
            <span className="eyebrow">Do ponto de partida à clareza</span>
            <h2 id="welcome-steps-title">Comece em poucos passos.</h2>
          </m.div>
          <m.ol
            variants={staggerContainer}
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, amount: 0.3 }}
          >
            <m.li variants={fadeUpItem}><span>01</span><div><strong>Defina seu ponto de partida</strong><p>Informe o capital que deseja acompanhar no sistema.</p></div></m.li>
            <m.li variants={fadeUpItem}><span>02</span><div><strong>Personalize sua rotina</strong><p>Escolha entre registros manuais, fixos ou lembretes.</p></div></m.li>
            <m.li variants={fadeUpItem}><span>03</span><div><strong>Acompanhe com contexto</strong><p>Veja o saldo total e o resultado de cada mês.</p></div></m.li>
          </m.ol>
        </section>
      </main>
    </div>
  )
}
