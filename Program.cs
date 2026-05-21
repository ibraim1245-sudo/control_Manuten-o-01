using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;

namespace ManutencaoMHI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try 
            {
                ApplicationConfiguration.Initialize();
                Database.Init();
                Application.Run(new MainForm());
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Erro ao iniciar aplicativo: " + ex.Message, "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public static class Database
    {
        private static string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MHI_PRO_DATA", "mhi_v2.db");
        public static void Init() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ativos (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, tag TEXT, categoria TEXT, localizacao TEXT, status TEXT, fabricante TEXT, modelo TEXT, serie TEXT, compra TEXT, garantia TEXT, obs TEXT);
                    CREATE TABLE IF NOT EXISTS ordens (id INTEGER PRIMARY KEY AUTOINCREMENT, ativo TEXT, titulo TEXT, descricao TEXT, tipo TEXT, prioridade TEXT, responsavel TEXT, horas TEXT, prazo TEXT, obs TEXT, status_os TEXT DEFAULT 'Aberta');
                    CREATE TABLE IF NOT EXISTS preventivas (id INTEGER PRIMARY KEY AUTOINCREMENT, ativo TEXT, titulo TEXT, descricao TEXT, frequencia TEXT, proxima TEXT, horas TEXT, checklist TEXT);
                    CREATE TABLE IF NOT EXISTS estoque (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, codigo TEXT, categoria TEXT, qtd INTEGER, min INTEGER, unidade TEXT, custo REAL, localizacao TEXT, fornecedor TEXT, descricao TEXT);";
                cmd.ExecuteNonQuery();
            } catch (Exception ex) { throw new Exception("Falha no Banco de Dados: " + ex.Message); }
        }
        public static SqliteConnection GetConn() => new SqliteConnection($"Data Source={dbPath}");
    }

    public class MainForm : Form
    {
        TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        Color darkBg = Color.FromArgb(20, 30, 20); // Dark Green Theme
        Color mhiGreen = Color.FromArgb(74, 222, 128);

        public MainForm() {
            this.Text = "Manutenção MHI Pro - Industrial Edition";
            this.Size = new Size(1300, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = darkBg;

            string[] abas = { "Painel", "Ativos", "Ordens de Serviço", "Preventiva", "Inventário", "Relatórios" };
            foreach (var a in abas) {
                var tp = new TabPage(a) { BackColor = darkBg };
                if (a == "Ativos") ConfigurarAtivos(tp);
                tabs.TabPages.Add(tp);
            }
            this.Controls.Add(tabs);
        }

        private void ConfigurarAtivos(TabPage tp) {
            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            Button btn = new Button { Text = "+ Novo Ativo", Width = 150, Dock = DockStyle.Left, BackColor = mhiGreen, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            
            DataGridView grid = new DataGridView { 
                Dock = DockStyle.Fill, 
                BackgroundColor = Color.FromArgb(30, 40, 30), 
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };

            btn.Click += (s, e) => AbrirModalAtivo(grid);
            pnl.Controls.Add(btn);
            tp.Controls.Add(grid);
            tp.Controls.Add(pnl);
            
            AtualizarGrid(grid, "SELECT id, tag, nome, localizacao, fabricante, modelo FROM ativos");
        }

        private void AbrirModalAtivo(DataGridView grid) {
            Form f = new Form { Text = "Novo Ativo", Size = new Size(600, 800), BackColor = Color.FromArgb(25, 35, 25), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            FlowLayoutPanel flp = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(25), AutoScroll = true };
            
            var campos = new Dictionary<string, Control>();
            string[] labels = { "Nome do Ativo *", "Tag / Código *", "Categoria *", "Localização *", "Status *", "Fabricante", "Modelo", "Número de Série", "Data de Compra", "Vencimento Garantia", "Observações" };

            foreach (var l in labels) {
                flp.Controls.Add(new Label { Text = l, ForeColor = Color.White, Width = 520, Margin = new Padding(0, 10, 0, 0) });
                var txt = new TextBox { Width = 520, BackColor = Color.Black, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                if (l.Contains("Observações")) { txt.Multiline = true; txt.Height = 80; }
                flp.Controls.Add(txt);
                campos.Add(l, txt);
            }

            Button btnSalvar = new Button { Text = "Cadastrar Ativo", Height = 50, Width = 520, BackColor = mhiGreen, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 20, 0, 0) };
            btnSalvar.Click += (s, e) => {
                using var conn = Database.GetConn(); conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO ativos (nome, tag, categoria, localizacao, status, fabricante, modelo, serie, compra, garantia, obs) VALUES (@n,@t,@c,@l,@s,@f,@m,@se,@co,@g,@o)";
                cmd.Parameters.AddWithValue("@n", campos["Nome do Ativo *"].Text);
                cmd.Parameters.AddWithValue("@t", campos["Tag / Código *"].Text);
                cmd.Parameters.AddWithValue("@c", campos["Categoria *"].Text);
                cmd.Parameters.AddWithValue("@l", campos["Localização *"].Text);
                cmd.Parameters.AddWithValue("@s", campos["Status *"].Text);
                cmd.Parameters.AddWithValue("@f", campos["Fabricante"].Text);
                cmd.Parameters.AddWithValue("@m", campos["Modelo"].Text);
                cmd.Parameters.AddWithValue("@se", campos["Número de Série"].Text);
                cmd.Parameters.AddWithValue("@co", campos["Data de Compra"].Text);
                cmd.Parameters.AddWithValue("@g", campos["Vencimento Garantia"].Text);
                cmd.Parameters.AddWithValue("@o", campos["Observações"].Text);
                cmd.ExecuteNonQuery();
                f.Close();
                AtualizarGrid(grid, "SELECT id, tag, nome, localizacao, fabricante, modelo FROM ativos");
            };

            flp.Controls.Add(btnSalvar);
            f.Controls.Add(flp);
            f.ShowDialog();
        }

        private void AtualizarGrid(DataGridView g, string query) {
            try {
                using var conn = Database.GetConn(); conn.Open();
                var cmd = conn.CreateCommand(); cmd.CommandText = query;
                DataTable dt = new DataTable(); dt.Load(cmd.ExecuteReader());
                g.DataSource = dt;
            } catch { }
        }
    }
}
