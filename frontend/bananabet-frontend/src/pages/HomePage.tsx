import { useNavigate } from "react-router-dom";

function HomePage() {
  const navigate = useNavigate();

  return (
    <div className="page-content" style={{ paddingBottom: 0 }}>
      {/* Hero Section */}
      <section style={{ 
        textAlign: 'center', 
        padding: '60px 20px', 
        background: 'radial-gradient(circle at center, rgba(241, 196, 15, 0.1) 0%, rgba(16, 16, 20, 0) 70%)' 
      }}>
        <h1 style={{ 
          fontSize: 64, 
          margin: '0 0 24px', 
          background: 'linear-gradient(to right, #fff, #f1c40f)', 
          WebkitBackgroundClip: 'text', 
          WebkitTextFillColor: 'transparent',
          fontWeight: 900,
          letterSpacing: '-2px'
        }}>
          BananaBet
        </h1>
        <p style={{ 
          fontSize: 20, 
          maxWidth: 600, 
          margin: '0 auto 40px', 
          color: 'var(--color-text-muted)', 
          lineHeight: 1.6 
        }}>
          Наступне покоління децентралізованих ставок. Ставте peer-to-peer на смарт-контрактах з чесною ліквідністю та прозорими результатами.
        </p>
        
        <div style={{ display: 'flex', gap: 16, justifyContent: 'center' }}>
          <button 
            className="btn-primary" 
            style={{ fontSize: 18, padding: '14px 32px' }}
            onClick={() => navigate('/matches')}
          >
            Запустити App 🚀
          </button>
          <button 
            className="btn-secondary" 
            style={{ fontSize: 18, padding: '14px 32px' }}
            onClick={() => window.open('https://sepolia.etherscan.io/address/0x4338f8B2a2cc11b46e25522e23ab41c344c86E4B', '_blank')}
          >
            Smart Contract 📜
          </button>
        </div>
      </section>

      {/* Features Grid */}
      <section className="container" style={{ margin: '60px auto' }}>
        <div style={{ 
          display: 'grid', 
          gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', 
          gap: 24 
        }}>
          <div className="card">
            <div style={{ fontSize: 32, marginBottom: 16 }}>⛓️</div>
            <h3 style={{ color: 'var(--color-primary)', marginTop: 0 }}>On-Chain Liquidity</h3>
            <p style={{ color: 'var(--color-text-muted)', lineHeight: 1.5 }}>
              Всі депозити здійснюються в BananaUSD що прив'язаний до долара США. Кошти користувачів завжди знаходяться на смарт-контракті, а не у букмекера.
            </p>
          </div>
          
          <div className="card">
            <div style={{ fontSize: 32, marginBottom: 16 }}>⚡</div>
            <h3 style={{ color: 'var(--color-primary)', marginTop: 0 }}>Instant Settlements</h3>
            <p style={{ color: 'var(--color-text-muted)', lineHeight: 1.5 }}>
              Виплати доступні автоматично після резолюції події оракулом. Ніяких затримок чи ручних перевірок.
            </p>
          </div>

          <div className="card">
            <div style={{ fontSize: 32, marginBottom: 16 }}>📊</div>
            <h3 style={{ color: 'var(--color-primary)', marginTop: 0 }}>Transparent Orderbook</h3>
            <p style={{ color: 'var(--color-text-muted)', lineHeight: 1.5 }}>
              Статичні P2P коефіцієнти розраховані власним Machine Learning алгоритмом. Ви бачите реальний розподіл ставок у "стакані".
            </p>
          </div>
        </div>
      </section>

      <footer style={{ 
        textAlign: 'center', 
        padding: 40, 
        borderTop: '1px solid var(--color-border)',
        color: '#555',
        marginTop: 60
      }}>
        <p>© 2026 BananaBet Protocol. All rights reserved.</p>
        <p style={{ fontSize: 12 }}>Running on Sepolia Testnet</p>
      </footer>
    </div>
  );
}

export default HomePage;

