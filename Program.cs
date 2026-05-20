using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.IO;

namespace ManutencaoMHI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Database.Init();
            Application.Run(new MainForm());
        }
    }

    public static class Database
    {
        private static string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MHI_PRO", "mhi.db");
        public static void Init() {
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ativos (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, tag TEXT, categoria TEXT, localizacao TEXT, status TEXT, fabricante TEXT, modelo TEXT, serie TEXT, compra TEXT, garantia TEXT, obs TEXT);
                CREATE TABLE IF NOT EXISTS ordens (id INTEGER PRIMARY KEY AUTOINCREMENT, ativo TEXT, titulo TEXT, descricao TEXT, tipo TEXT, prioridade TEXT, responsavel TEXT, horas TEXT, prazo TEXT, status_os TEXT DEFAULT 'Aberta');
                CREATE TABLE IF NOT EXISTS estoque (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, codigo TEXT, categoria TEXT, qtd INTEGER, min INTEGER, unidade TEXT, custo REAL, fornecedor TEXT);";
            cmd.ExecuteNonQuery();
        }
        public static SqliteConnection GetConn() => new SqliteConnection($"Data Source={dbPath}");
    }

    public class MainForm : Form
    {
        TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        Color darkBg = Color.FromArgb(14, 18, 14);
        Color mhiGreen = Color.FromArgb(74, 222, 128);

        public MainForm() {
            this.Text = "Manutenção MHI Pro"; this.Size = new Size(1300, 800); this.BackColor = darkBg;
            string[] nomes = { "Painel", "Ativos", "Ordens de Serviço", "Preventiva", "Inventário", "Relatórios" };
            foreach (var n in nomes) {
                var tp = new TabPage(n) { BackColor = darkBg };
                if (n == "Ativos") ConfigurarAtivos(tp);
                if (n == "Ordens de Serviço") ConfigurarOS(tp);
                tabs.TabPages.Add(tp);
            }
            this.Controls.Add(tabs);
        }

        private void ConfigurarAtivos(TabPage tp) {
            Button btn = new Button { Text = "+ Novo Ativo", Dock = DockStyle.Top, Height = 40, BackColor = mhiGreen, FlatStyle = FlatStyle.Flat };
            DataGridView grid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.FromArgb(21, 26, 21), ForeColor = Color.Black, Name = "gridAtivos" };
            btn.Click += (s, e) => FormAtivo(grid);
            tp.Controls.Add(grid); tp.Controls.Add(btn);
            LoadGrid("SELECT id, tag, nome, localizacao, status FROM ativos", grid);
        }

        private void FormAtivo(DataGridView grid, int? id = null) {
            Form f = new Form { Text = "Novo Ativo", Size = new Size(550, 750), BackColor = Color.FromArgb(19, 25, 19), StartPosition = FormStartPosition.CenterParent };
            FlowLayoutPanel p = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            var txtNome = AddInput(p, "Nome do Ativo *");
            var txtTag = AddInput(p, "Tag / Código *");
            var txtCat = AddInput(p, "Categoria *");
            var txtLoc = AddInput(p, "Localização *");
            var txtFab = AddInput(p, "Fabricante");
            var txtMod = AddInput(p, "Modelo");
            var txtSer = AddInput(p, "Número de Série");
            var txtObs = AddInput(p, "Observações", true);

            Button btn = new Button { Text = "Cadastrar Ativo", BackColor = mhiGreen, Width = 480, Height = 45, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => {
                using var conn = Database.GetConn(); conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO ativos (nome, tag, categoria, localizacao, fabricante, modelo, serie, obs) VALUES (@n,@t,@c,@l,@f,@m,@s,@o)";
                cmd.Parameters.AddWithValue("@n", txtNome.Text);
                cmd.Parameters.AddWithValue("@t", txtTag.Text);
                cmd.Parameters.AddWithValue("@c", txtCat.Text);
                cmd.Parameters.AddWithValue("@l", txtLoc.Text);
                cmd.Parameters.AddWithValue("@f", txtFab.Text);
                cmd.Parameters.AddWithValue("@m", txtMod.Text);
                cmd.Parameters.AddWithValue("@s", txtSer.Text);
                cmd.Parameters.AddWithValue("@o", txtObs.Text);
                cmd.ExecuteNonQuery(); f.Close(); LoadGrid("SELECT id, tag, nome, localizacao, status FROM ativos", grid);
            };
            p.Controls.Add(btn); f.Controls.Add(p); f.ShowDialog();
        }

        private void ConfigurarOS(TabPage tp) {
            Button btn = new Button { Text = "+ Nova Ordem de Serviço", Dock = DockStyle.Top, Height = 40, BackColor = Color.Orange, FlatStyle = FlatStyle.Flat };
            tp.Controls.Add(btn);
        }

        private TextBox AddInput(FlowLayoutPanel p, string label, bool multi = false) {
            p.Controls.Add(new Label { Text = label, ForeColor = Color.White, Width = 480, Margin = new Padding(0, 10, 0, 0) });
            var t = new TextBox { Width = 480, BackColor = Color.FromArgb(26, 34, 26), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Multiline = multi, Height = multi ? 80 : 30 };
            p.Controls.Add(t); return t;
        }

        private void LoadGrid(string sql, DataGridView g) {
            using var conn = Database.GetConn(); conn.Open();
            var cmd = conn.CreateCommand(); cmd.CommandText = sql;
            DataTable dt = new DataTable(); dt.Load(cmd.ExecuteReader()); g.DataSource = dt;
        }
    }
}