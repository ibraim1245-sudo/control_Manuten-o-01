import webview
import sqlite3
import os
import sys
import json
from datetime import datetime

# Lógica de Programador: Caminhos Absolutos
if getattr(sys, 'frozen', False):
    dir_path = os.path.dirname(sys.executable)
else:
    dir_path = os.path.dirname(os.path.abspath(__file__))

DB_PATH = os.path.join(dir_path, "mhi_pro.db")

def init_db():
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    cursor.execute('''CREATE TABLE IF NOT EXISTS ativos 
        (id INTEGER PRIMARY KEY, nome TEXT, tag TEXT, setor TEXT, status TEXT, data_cad TEXT)''')
    cursor.execute('''CREATE TABLE IF NOT EXISTS ordens 
        (id INTEGER PRIMARY KEY, ativo TEXT, titulo TEXT, custo REAL, status TEXT, data TEXT)''')
    conn.commit()
    conn.close()

class Api:
    def get_stats(self):
        conn = sqlite3.connect(DB_PATH)
        c = conn.cursor()
        total_ativos = c.execute("SELECT COUNT(*) FROM ativos").fetchone()[0]
        os_concluidas = c.execute("SELECT COUNT(*) FROM ordens WHERE status='Concluída'").fetchone()[0]
        custo = c.execute("SELECT SUM(custo) FROM ordens").fetchone()[0] or 0
        conn.close()
        return {"ativos": total_ativos, "os": os_concluidas, "custo": f"{custo:,.2f}"}

    def salvar_ativo(self, nome, tag, setor):
        conn = sqlite3.connect(DB_PATH)
        conn.cursor().execute("INSERT INTO ativos (nome, tag, setor, status, data_cad) VALUES (?,?,?,?,?)",
                             (nome, tag, setor, 'Operacional', datetime.now().strftime("%d/%m/%Y")))
        conn.commit()
        conn.close()
        return True

# HTML Integrado (Evita tela branca por falta de arquivo)
html_content = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>ManutençãoPro MHI</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
    <style>
        :root { --bg: #0e120e; --side: #080a08; --card: #151a15; --green: #4ade80; --text: #e2e8f0; }
        body { font-family: 'Segoe UI', sans-serif; background: var(--bg); color: var(--text); margin: 0; display: flex; height: 100vh; overflow: hidden; }
        .sidebar { width: 240px; background: var(--side); border-right: 1px solid #222; display: flex; flex-direction: column; }
        .logo { padding: 30px; color: var(--green); font-weight: bold; border-bottom: 1px solid #1a221a; }
        nav { flex: 1; padding: 20px 0; }
        .n-btn { width: 100%; padding: 15px 25px; background: none; border: none; color: #888; text-align: left; cursor: pointer; display: flex; align-items: center; gap: 12px; }
        .n-btn.active, .n-btn:hover { background: #1a221a; color: var(--green); border-left: 4px solid var(--green); }
        .main { flex: 1; padding: 30px; overflow-y: auto; }
        .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 30px; }
        .card { background: var(--card); padding: 20px; border-radius: 12px; border: 1px solid #222; }
        .chart-grid { display: grid; grid-template-columns: 1.5fr 1fr; gap: 20px; }
        .tab { display: none; } .tab.active { display: block; }
        .btn-add { background: var(--green); color: black; border: none; padding: 10px 20px; border-radius: 6px; font-weight: bold; cursor: pointer; }
    </style>
</head>
<body>
    <div class="sidebar">
        <div class="logo"><i class="fas fa-tools"></i> ManutençãoPro MHI</div>
        <nav>
            <button onclick="switchTab('dash')" id="l-dash" class="n-btn active"><i class="fas fa-th-large"></i> Painel</button>
            <button onclick="switchTab('ativos')" id="l-ativos" class="n-btn"><i class="fas fa-microchip"></i> Ativos</button>
            <button onclick="switchTab('os')" id="l-os" class="n-btn"><i class="fas fa-file-invoice"></i> Ordens</button>
            <button onclick="switchTab('rel')" id="l-rel" class="n-btn"><i class="fas fa-chart-bar"></i> Relatórios</button>
        </nav>
    </div>
    <div class="main">
        <div id="tab-dash" class="tab active">
            <h1>Visão Geral</h1>
            <div class="kpi-grid">
                <div class="card"><span>Ativos Totais</span><h2 id="v-ativos">0</h2></div>
                <div class="card"><span>Ordens Abertas</span><h2>0</h2></div>
                <div class="card" style="border-bottom: 3px solid var(--green)"><span>Custo Mensal</span><h2 id="v-custo">R$ 0,00</h2></div>
                <div class="card"><span>Concluídas</span><h2 id="v-os">0</h2></div>
            </div>
            <div class="chart-grid">
                <div class="card"><canvas id="c1"></canvas></div>
                <div class="card"><canvas id="c2"></canvas></div>
            </div>
        </div>
        <div id="tab-ativos" class="tab">
            <h1>Ativos <button class="btn-add" onclick="novoAtivo()">+ Novo Ativo</button></h1>
            <div id="lista-ativos"></div>
        </div>
    </div>
    <script>
        function switchTab(t) {
            document.querySelectorAll('.tab').forEach(x => x.classList.remove('active'));
            document.querySelectorAll('.n-btn').forEach(x => x.classList.remove('active'));
            document.getElementById('tab-'+t).classList.add('active');
            document.getElementById('l-'+t).classList.add('active');
        }
        async function load() {
            const s = await pywebview.api.get_stats();
            document.getElementById('v-ativos').innerText = s.ativos;
            document.getElementById('v-os').innerText = s.os;
            document.getElementById('v-custo').innerText = "R$ " + s.custo;
            
            new Chart(document.getElementById('c1'), {
                type: 'bar', data: { labels:['Concluídas'], datasets:[{data:[s.os], backgroundColor:'#4ade80'}] }
            });
            new Chart(document.getElementById('c2'), {
                type: 'doughnut', data: { labels:['Crítica','Alta','Média'], datasets:[{data:[1,2,1], backgroundColor:['#ef4444','#f97316','#3b82f6']}] }
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
    # Criando a janela com o Webview2 (Padrão Windows)
    window = webview.create_window('Manutenção MHI Pro', html=html_content, js_api=api, width=1280, height=800)
    webview.start()