import sys
import os
import sqlite3
from PyQt6.QtWidgets import (QApplication, QMainWindow, QWidget, QVBoxLayout, 
                             QHBoxLayout, QPushButton, QLabel, QStackedWidget, 
                             QTableWidget, QTableWidgetItem, QFrame, QGridLayout)
from PyQt6.QtCore import Qt, QSize
from PyQt6.QtGui import QFont, QColor

# CONFIGURAÇÃO DE BANCO DE DADOS EM LOCAL SEGURO
app_data = os.path.join(os.environ.get('APPDATA', os.getcwd()), 'MHI_Pro_v5')
if not os.path.exists(app_data): os.makedirs(app_data)
DB_PATH = os.path.join(app_data, "mhi_native.db")

class Database:
    def __init__(self):
        self.conn = sqlite3.connect(DB_PATH)
        self.create_tables()

    def create_tables(self):
        cursor = self.conn.cursor()
        cursor.execute('''CREATE TABLE IF NOT EXISTS ativos (id INTEGER PRIMARY KEY, nome TEXT, tag TEXT, setor TEXT, status TEXT)''')
        cursor.execute('''CREATE TABLE IF NOT EXISTS ordens (id INTEGER PRIMARY KEY, titulo TEXT, custo REAL, status TEXT, data TEXT)''')
        # Dados Iniciais
        if cursor.execute("SELECT COUNT(*) FROM ativos").fetchone()[0] == 0:
            cursor.execute("INSERT INTO ativos (nome, tag, setor, status) VALUES ('Laser Máquina', '001', 'Corte', 'Operacional')")
            cursor.execute("INSERT INTO ordens (titulo, custo, status, data) VALUES ('Motor', 230.0, 'Concluída', '12/05/2026')")
        self.conn.commit()

    def get_stats(self):
        cursor = self.conn.cursor()
        ativos = cursor.execute("SELECT COUNT(*) FROM ativos").fetchone()[0]
        concluidas = cursor.execute("SELECT COUNT(*) FROM ordens WHERE status='Concluída'").fetchone()[0]
        custo = cursor.execute("SELECT SUM(custo) FROM ordens").fetchone()[0] or 0
        return ativos, concluidas, custo

class MHIApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self.db = Database()
        self.setWindowTitle("Manutenção MHI Pro - Industrial")
        self.resize(1200, 800)
        self.setStyleSheet("""
            QMainWindow { background-color: #0e120e; }
            QLabel { color: #e2e8f0; font-family: 'Segoe UI'; }
            QPushButton { background-color: transparent; color: #888; border: none; text-align: left; padding: 15px; font-size: 14px; }
            QPushButton:hover { background-color: #1a221a; color: #4ade80; }
            QPushButton#active_menu { background-color: #1a221a; color: #4ade80; border-left: 4px solid #4ade80; }
            QFrame#card { background-color: #151a15; border: 1px solid #222; border-radius: 10px; }
        """)

        # LAYOUT PRINCIPAL
        main_layout = QHBoxLayout()
        container = QWidget()
        container.setLayout(main_layout)
        self.setCentralWidget(container)

        # SIDEBAR
        self.sidebar = QFrame()
        self.sidebar.setFixedWidth(250)
        self.sidebar.setStyleSheet("background-color: #080a08; border-right: 1px solid #222;")
        sidebar_layout = QVBoxLayout(self.sidebar)
        
        logo = QLabel("🛠 Manutenção MHI")
        logo.setStyleSheet("font-size: 18px; font-weight: bold; color: #4ade80; padding: 20px 0;")
        sidebar_layout.addWidget(logo)

        self.btn_dash = QPushButton("📊 Painel")
        self.btn_ativos = QPushButton("⚙ Ativos")
        self.btn_os = QPushButton("📝 Ordens de Serviço")
        
        for btn in [self.btn_dash, self.btn_ativos, self.btn_os]:
            sidebar_layout.addWidget(btn)
            btn.clicked.connect(self.change_page)

        sidebar_layout.addStretch()
        main_layout.addWidget(self.sidebar)

        # ÁREA DE CONTEÚDO (StackedWidget - Onde as abas trocam)
        self.pages = QStackedWidget()
        main_layout.addWidget(self.pages)

        # INICIALIZAR PÁGINAS
        self.ui_dashboard()
        self.ui_ativos()

    def change_page(self):
        btn = self.sender()
        if btn == self.btn_dash: self.pages.setCurrentIndex(0)
        elif btn == self.btn_ativos: self.pages.setCurrentIndex(1)

    def ui_dashboard(self):
        page = QWidget()
        layout = QVBoxLayout(page)
        
        title = QLabel("Painel de Controle")
        title.setStyleSheet("font-size: 24px; font-weight: bold; margin-bottom: 20px;")
        layout.addWidget(title)

        # KPI GRID
        kpi_layout = QGridLayout()
        ativos, os, custo = self.db.get_stats()

        def create_kpi(label, val):
            card = QFrame(); card.setObjectName("card")
            card.setFixedSize(220, 120)
            l = QVBoxLayout(card)
            t = QLabel(label); t.setStyleSheet("color: #888; font-size: 12px;")
            v = QLabel(str(val)); v.setStyleSheet("font-size: 28px; font-weight: bold;")
            l.addWidget(t); l.addWidget(v)
            return card

        kpi_layout.addWidget(create_kpi("Ativos Totais", ativos), 0, 0)
        kpi_layout.addWidget(create_kpi("OS Concluídas", os), 0, 1)
        kpi_layout.addWidget(create_kpi("Custo Total", f"R$ {custo:,.2f}"), 0, 2)
        
        layout.addLayout(kpi_layout)
        layout.addStretch()
        self.pages.addWidget(page)

    def ui_ativos(self):
        page = QWidget()
        layout = QVBoxLayout(page)
        title = QLabel("Inventário de Ativos")
        title.setStyleSheet("font-size: 24px; font-weight: bold;")
        layout.addWidget(title)

        table = QTableWidget(5, 4)
        table.setHorizontalHeaderLabels(["ID", "Ativo", "Setor", "Status"])
        table.setStyleSheet("background-color: #151a15; color: white; gridline-color: #222;")
        layout.addWidget(table)
        self.pages.addWidget(page)

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = MHIApp()
    window.show()
    sys.exit(app.exec())