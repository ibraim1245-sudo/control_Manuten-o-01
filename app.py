import webview
import sqlite3
import os
import sys
import json
from datetime import datetime

# LÓGICA PROFISSIONAL DE CAMINHOS: Salva dados no AppData para evitar erros de permissão
def get_db_path():
    app_name = "ManutencaoMHI_Pro"
    # No Windows, salva em C:\Users\Nome\AppData\Roaming\ManutencaoMHI_Pro
    app_data = os.path.join(os.environ.get('APPDATA', os.getcwd()), app_name)
    if not os.path.exists(app_data):
        os.makedirs(app_data)
    return os.path.join(app_data, "database.db")

DB_PATH = get_db_path()

def init_db():
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    cursor.execute('''CREATE TABLE IF NOT EXISTS ativos 
        (id INTEGER PRIMARY KEY, nome TEXT, tag TEXT, setor TEXT, status TEXT)''')
    cursor.execute('''CREATE TABLE IF NOT EXISTS ordens 
        (id INTEGER PRIMARY KEY, ativo TEXT, titulo TEXT, custo REAL, prioridade TEXT, status TEXT, data TEXT)''')
    
    # Inserir dados de teste se o banco estiver vazio (Para você ver o dashboard funcionando)
    count = cursor.execute("SELECT COUNT(*) FROM ativos").fetchone()[0]
    if count == 0:
        cursor.execute("INSERT INTO ativos (nome, tag, setor, status) VALUES ('Laser máquina', '001', 'Corte', 'Operacional')")
        cursor.execute("INSERT INTO ordens (ativo, titulo, custo, prioridade, status, data) VALUES ('Laser máquina', 'Motor', 230.0, 'Crítica', 'Concluída', '12/05/2026')")
        cursor.execute("INSERT INTO ordens (ativo, titulo, custo, prioridade, status, data) VALUES ('Laser máquina', 'Fonte Laser', 750.0, 'Alta', 'Concluída', '15/04/2026')")
    
    conn.commit()
    conn.close()

class Api:
    def get_dashboard_data(self):
        conn = sqlite3.connect(DB_PATH)
        c = conn.cursor()
        ativos = c.execute("SELECT COUNT(*) FROM ativos").fetchone()[0]
        concluidas = c.execute("SELECT COUNT(*) FROM ordens WHERE status='Concluída'").fetchone()[0]
        custo = c.execute("SELECT SUM(custo) FROM ordens").fetchone()[0] or 0
        
        # Dados para os gráficos
        prio_data = [
            c.execute("SELECT COUNT(*) FROM ordens WHERE prioridade='Crítica'").fetchone()[0],
            c.execute("SELECT COUNT(*) FROM ordens WHERE prioridade='Alta'").fetchone()[0],
            c.execute("SELECT COUNT(*) FROM ordens WHERE prioridade='Média'").fetchone()[0]
        ]
        
        conn.close()
        return {
            "ativos": ativos, "concluidas": concluidas, 
            "custo": f"{custo:,.2f}", "prio_chart": prio_data
        }

# INTERFACE DARK GREEN (ESTILO DAS SUAS FOTOS)
html_content = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>ManutençãoPro MHI</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
    <style>
        :root { --bg: #0e120e; --side: #090c09; --card: #161d16; --green: #4ade80; --text: #e2e8f0; }
        body { font-family: 'Segoe UI', sans-serif; background: var(--bg); color: var(--text); margin: 0; display: flex; height: 100vh; overflow: hidden; }
        
        .sidebar { width: 260px; background: var(--side); border-right: 1px solid #222; display: flex; flex-direction: column; }
        .logo { padding: 30px; font-size: 20px; font-weight: bold; color: var(--green); border-bottom: 1px solid #1a221a; }
        
        nav { flex: 1; padding: 20px 0; }
        .n-btn { width: 100%; padding: 15px 25px; background: none; border: none; color: #8e9aaf; text-align: left; cursor: pointer; display: flex; align-items: center; gap: 15px; font-size: 15px; transition: 0.3s; }
        .n-btn:hover, .n-btn.active { background: #1a221a; color: var(--green); border-left: 4px solid var(--green); }
        
        .main { flex: 1; padding: 35px; overflow-y: auto; }
        .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 30px; }
        .card { background: var(--card); padding: 20px; border-radius: 12px; border: 1px solid #222; position: relative; }
        .kpi span { font-size: 13px; color: #8e9aaf; }
        .kpi h2 { font-size: 32px; margin: 10px 0 0; }
        
        .row { display: grid; grid-template-columns: 1.5fr 1fr; gap: 20px; margin-bottom: 20px; }
        .chart-container { background: var(--card); padding: 20px; border-radius: 15px; border: 1px solid #222; }
        h3 { margin-top: 0; font-size: 16px; color: var(--text); }
        
        .tab { display: none; } .tab.active { display: block; }
    </style>
</head>
<body>
    <div class="sidebar">
        <div class="logo"><i class="fas fa-wrench"></i> Manutenção MHI</div>
        <nav>
            <button onclick="tab('dash')" id="l-dash" class="n-btn active"><i class="fas fa-th-large"></i> Painel</button>
            <button onclick="tab('ativos')" id="l-ativos" class="n-btn"><i class="fas fa-microchip"></i> Ativos</button>
            <button onclick="tab('os')" id="l-os" class="n-btn"><i class="fas fa-file-invoice"></i> Ordens</button>
            <button onclick="tab('rel')" id="l-rel" class="n-btn"><i class="fas fa-chart-bar"></i> Relatórios</button>
        </nav>
    </div>
    <div class="main">
        <div id="tab-dash" class="tab active">
            <h1>Painel</h1>
            <div class="kpi-grid">
                <div class="card kpi"><span>Ativos Totais</span><h2 id="k-ativos">0</h2></div>
                <div class="card kpi"><span>Ordens Concluídas</span><h2 id="k-os">0</h2></div>
                <div class="card kpi" style="border-bottom: 3px solid var(--green)"><span>Custo Total</span><h2 id="k-custo">R$ 0,00</h2></div>
                <div class="card kpi"><span>SLA Médio</span><h2>98%</h2></div>
            </div>
            <div class="row">
                <div class="chart-container"><canvas id="c1"></canvas></div>
                <div class="chart-container"><canvas id="c2"></canvas></div>
            </div>
        </div>
        <div id="tab-ativos" class="tab"><h1>Ativos</h1><div class="card">Laser máquina - Tag 001 - Setor Corte</div></div>
    </div>
    <script>
        function tab(t) {
            document.querySelectorAll('.tab').forEach(x => x.classList.remove('active'));
            document.querySelectorAll('.n-btn').forEach(x => x.classList.remove('active'));
            document.getElementById('tab-'+t).classList.add('active');
            document.getElementById('l-'+t).classList.add('active');
        }
        async function load() {
            const d = await pywebview.api.get_dashboard_data();
            document.getElementById('k-ativos').innerText = d.ativos;
            document.getElementById('k-os').innerText = d.concluidas;
            document.getElementById('k-custo').innerText = "R$ " + d.custo;
            
            new Chart(document.getElementById('c1'), {
                type: 'bar', data: { labels:['Concluídas'], datasets:[{label:'OS', data:[d.concluidas], backgroundColor:'#4ade80'}] },
                options: { scales: { y: { beginAtZero: true, grid: { color: '#222' } } } }
            });
            new Chart(document.getElementById('c2'), {
                type: 'doughnut', data: { labels:['Crítica','Alta','Média'], datasets:[{data:d.prio_chart, backgroundColor:['#ef4444','#f97316','#3b82f6'], borderWidth:0}] }
            });
        }
        window.addEventListener('pywebviewready', load);
    </script>
</body>
</html>
"""

if __name__ == "__main__":
    init_db()
    api = Api()
    window = webview.create_window('Manutenção MHI Pro', html=html_content, js_api=api, width=1300, height=850)
    webview.start()