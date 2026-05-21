using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;

namespace ManutencaoMHI {
    static class Program {
        [STAThread]
        static void Main() {
            try {
                SQLitePCL.Batteries_V2.Init(); // Resolve erro de Banco de Dados
                ApplicationConfiguration.Initialize();
                Database.Init();
                Application.Run(new MainForm());
            } catch (Exception ex) {
                MessageBox.Show("Erro Crítico: " + ex.Message);
            }
        }
    }

    public static class Database {
        private static string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MHI_PRO_DATA", "mhi.db");
        public static void Init() {
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ativos (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, tag TEXT, categoria TEXT, localizacao TEXT, status TEXT, fabricante TEXT, modelo TEXT, serie TEXT, compra TEXT, garantia TEXT, obs TEXT);
                CREATE TABLE IF NOT EXISTS ordens (id INTEGER PRIMARY KEY AUTOINCREMENT, ativo TEXT, titulo TEXT, descricao TEXT, tipo TEXT, prioridade TEXT, responsavel TEXT, horas TEXT, prazo TEXT, obs TEXT, status_os TEXT DEFAULT 'Aberta');
                CREATE TABLE IF NOT EXISTS preventivas (id INTEGER PRIMARY KEY AUTOINCREMENT, ativo TEXT, titulo TEXT, descricao TEXT, frequencia TEXT, data TEXT, horas TEXT, checklist TEXT);
                CREATE TABLE IF NOT EXISTS estoque (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, codigo TEXT, categoria TEXT, qtd INTEGER, min INTEGER, unidade TEXT, custo REAL, localizacao TEXT, fornecedor TEXT, descricao TEXT);";
            cmd.ExecuteNonQuery();
        }
        public static SqliteConnection GetConn() => new SqliteConnection($"Data Source={dbPath}");
    }

    public class MainForm : Form {
        Panel sidebar = new Panel();
        TabControl tabs = new TabControl();
        Color bgDark = Color.FromArgb(14, 18, 14);
        Color mhiGreen = Color.FromArgb(74, 222, 128);

        public MainForm() {
            this.Text = "Manutenção MHI Pro - Industrial";
            this.Size = new Size(1300, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgDark;

            // Sidebar
            sidebar.Dock = DockStyle.Left; sidebar.Width = 220; sidebar.BackColor = Color.FromArgb(8, 10, 8);
            this.Controls.Add(sidebar);

            Label logo = new Label { Text = "MHI PRO", ForeColor = mhiGreen, Font = new Font("Segoe UI", 18, FontStyle.Bold), Dock = DockStyle.Top, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            sidebar.Controls.Add(logo);

            string[] menus = { "Painel", "Ativos", "Ordens de Serviço", "Preventiva", "Inventário", "Relatórios" };
            for (int i = menus.Length - 1; i >= 0; i--) {
                Button btn = new Button { Text = menus[i], Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0) };
                btn.FlatAppearance.BorderSize = 0;
                int idx = i; btn.Click += (s, e) => tabs.SelectedIndex = idx;
                sidebar.Controls.Add(btn);
            }

            // Tabs
            tabs.Dock = DockStyle.Fill;
            tabs.Appearance = TabAppearance.FlatButtons; tabs.ItemSize = new Size(0, 1); tabs.SizeMode = TabSizeMode.Fixed; // Esconde os cabeçalhos das abas
            foreach (var m in menus) {
                TabPage tp = new TabPage(m) { BackColor = bgDark };
                ConfigurarModulo(tp, m);
                tabs.TabPages.Add(tp);
            }
            this.Controls.Add(tabs);
        }

        private void ConfigurarModulo(TabPage tp, string nome) {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 60 };
            Label lbl = new Label { Text = nome, ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true };
            Button btnNovo = new Button { Text = "+ NOVO", Width = 120, Height = 35, Location = new Point(1000, 15), BackColor = mhiGreen, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            
            DataGridView grid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.FromArgb(20, 25, 20), ForeColor = Color.Black, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true };
            
            string tabela = nome == "Ordens de Serviço" ? "ordens" : nome == "Inventário" ? "estoque" : nome.ToLower();
            btnNovo.Click += (s, e) => AbrirForm(tabela, grid);
            grid.CellDoubleClick += (s, e) => { if(e.RowIndex >= 0) AbrirForm(tabela, grid, Convert.ToInt32(grid.Rows[e.RowIndex].Cells["id"].Value)); };

            header.Controls.Add(lbl); header.Controls.Add(btnNovo);
            tp.Controls.Add(grid); tp.Controls.Add(header);
            AtualizarGrid(grid, tabela);
        }

        private void AbrirForm(string tabela, DataGridView grid, int? id = null) {
            Form f = new Form { Text = "Gravar Informação", Size = new Size(600, 800), BackColor = Color.FromArgb(20, 25, 20), StartPosition = FormStartPosition.CenterParent };
            FlowLayoutPanel flp = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
            
            Dictionary<string, TextBox> campos = new Dictionary<string, TextBox>();
            string[] labels = tabela switch {
                "ativos" => new[] { "Nome do Ativo *", "Tag / Código *", "Categoria *", "Localização *", "Status *", "Fabricante", "Modelo", "Número de Série", "Data de Compra", "Vencimento da Garantia", "Observações" },
                "ordens" => new[] { "Máquina / Ativo *", "Título *", "Descrição *", "Tipo", "Prioridade", "Responsável", "Horas Estimadas", "Prazo", "Observações" },
                "preventivas" => new[] { "Ativo *", "Título *", "Descrição", "Frequência *", "Próxima Data *", "Horas Estimadas", "Checklist (um por linha)" },
                "estoque" => new[] { "Nome *", "Código / Referência", "Categoria", "Quantidade *", "Estoque Mínimo *", "Unidade", "Custo Unitário (R$)", "Localização", "Fornecedor", "Descrição" },
                _ => new string[0]
            };

            foreach (var l in labels) {
                flp.Controls.Add(new Label { Text = l, ForeColor = Color.White, Width = 520, Margin = new Padding(0, 10, 0, 0) });
                var t = new TextBox { Width = 520, BackColor = Color.Black, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                if (l.Contains("Descrição") || l.Contains("Observações") || l.Contains("Checklist")) { t.Multiline = true; t.Height = 80; }
                flp.Controls.Add(t); campos.Add(l, t);
            }

            Button btnSalvar = new Button { Text = "GRAVAR E SALVAR", Width = 520, Height = 50, BackColor = mhiGreen, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 20, 0, 0) };
            btnSalvar.Click += (s, e) => {
                using var conn = Database.GetConn(); conn.Open();
                var cmd = conn.CreateCommand();
                if (id == null) {
                    string cols = "", pars = "";
                    foreach (var k in campos.Keys) { 
                        string c = k.Replace(" *", "").Replace(" / ", "_").Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower();
                        cols += c + ","; pars += "@" + c + ",";
                    }
                    cmd.CommandText = $"INSERT INTO {tabela} ({cols.TrimEnd(',')}) VALUES ({pars.TrimEnd(',')})";
                } else {
                    string sets = "";
                    foreach (var k in campos.Keys) {
                        string c = k.Replace(" *", "").Replace(" / ", "_").Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower();
                        sets += $"{c}=@{c},";
                    }
                    cmd.CommandText = $"UPDATE {tabela} SET {sets.TrimEnd(',')} WHERE id={id}";
                }
                foreach (var kvp in campos) cmd.Parameters.AddWithValue("@" + kvp.Key.Replace(" *", "").Replace(" / ", "_").Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower(), kvp.Value.Text);
                cmd.ExecuteNonQuery(); f.Close(); AtualizarGrid(grid, tabela);
            };

            flp.Controls.Add(btnSalvar); f.Controls.Add(flp); f.ShowDialog();
        }

        private void AtualizarGrid(DataGridView g, string tabela) {
            try {
                using var conn = Database.GetConn(); conn.Open();
                var cmd = conn.CreateCommand(); cmd.CommandText = $"SELECT * FROM {tabela} ORDER BY id DESC";
                DataTable dt = new DataTable(); dt.Load(cmd.ExecuteReader()); g.DataSource = dt;
            } catch { }
        }
    }
}
