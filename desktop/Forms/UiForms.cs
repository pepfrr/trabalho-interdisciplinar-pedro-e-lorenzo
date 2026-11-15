using BrasaBrio.Desktop.Data;
using BrasaBrio.Desktop.Services;
using MySqlConnector;

namespace BrasaBrio.Desktop;

internal static class Ui
{
    public static readonly Color Red = Color.FromArgb(210, 39, 48);
    public static void Style(Form form, string title)
    {
        form.Text = title; form.StartPosition = FormStartPosition.CenterScreen; form.Size = new Size(1100, 700);
        form.MinimumSize = new Size(800, 520); form.BackColor = Color.FromArgb(18, 18, 18); form.ForeColor = Color.White;
        form.Font = new Font("Segoe UI", 10); form.FormBorderStyle = FormBorderStyle.Sizable;
    }
    public static Button Button(string text, Action click)
    {
        var button = new Button { Text = text, AutoSize = true, BackColor = Red, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Padding = new Padding(12, 6, 12, 6), Margin = new Padding(6) };
        button.FlatAppearance.BorderSize = 0; button.Click += (_, _) => click(); return button;
    }
    public static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.FromArgb(30, 30, 30), ForeColor = Color.Black, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false };
    public static void Error(Exception e) => MessageBox.Show(e.Message, "Brasa & Brio", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

public sealed class LoginForm : Form
{
    private readonly TextBox email = new() { Width = 300, MaxLength = 120 };
    private readonly TextBox senha = new() { Width = 300, UseSystemPasswordChar = true, MaxLength = 128 };
    public LoginForm()
    {
        Ui.Style(this, "Brasa & Brio | Acesso"); Size = new Size(460, 350); MaximizeBox = false;
        var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Padding = new Padding(35), Anchor = AnchorStyles.None };
        panel.Controls.Add(new Label { Text = "BRASA & BRIO", Font = new Font("Georgia", 24, FontStyle.Bold), ForeColor = Ui.Red, AutoSize = true, Margin = new Padding(0, 0, 0, 22) });
        panel.Controls.Add(new Label { Text = "E-mail", AutoSize = true }); panel.Controls.Add(email);
        panel.Controls.Add(new Label { Text = "Senha", AutoSize = true, Margin = new Padding(0, 12, 0, 0) }); panel.Controls.Add(senha);
        panel.Controls.Add(Ui.Button("Entrar", Entrar)); Controls.Add(panel); Layout += (_, _) => panel.Location = new Point((ClientSize.Width-panel.Width)/2,(ClientSize.Height-panel.Height)/2);
        AcceptButton = panel.Controls.OfType<Button>().First();
    }
    private void Entrar()
    {
        try { if (!Conexao.ValidarLogin(email.Text, senha.Text)) { MessageBox.Show("E-mail ou senha inválidos."); return; } Hide(); using var form = new MainForm(); form.ShowDialog(); Close(); }
        catch (Exception ex) { Ui.Error(ex); }
    }
}

public sealed class MainForm : Form
{
    public MainForm()
    {
        Ui.Style(this, "Brasa & Brio | Gestão de delivery");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(38), RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "PAINEL DE GESTÃO", Font = new Font("Georgia", 25, FontStyle.Bold), ForeColor = Ui.Red, AutoSize = true }, 0, 0);
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 30, 0, 0) };
        menu.Controls.Add(Ui.Button("Pedidos e importação JSON", () => new FrmPedidos().ShowDialog()));
        menu.Controls.Add(Ui.Button("Produtos", () => new FrmProdutos().ShowDialog()));
        menu.Controls.Add(Ui.Button("Clientes (CRUD completo)", () => new FrmCadastro("Clientes").ShowDialog()));
        menu.Controls.Add(Ui.Button("Entregadores (CRUD completo)", () => new FrmCadastro("Entregadores").ShowDialog()));
        layout.Controls.Add(menu, 0, 1); layout.Controls.Add(new Label { Text = "Pedidos entram pelo arquivo JSON exportado no site. O desktop não cria pedidos ou produtos.", AutoSize = true, ForeColor = Color.Silver }, 0, 2); Controls.Add(layout);
    }
}

public sealed class FrmPedidos : Form
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly ComboBox status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    public FrmPedidos()
    {
        Ui.Style(this, "Pedidos"); status.Items.AddRange(["Recebido", "Em preparo", "Saiu para entrega", "Entregue", "Cancelado"]); status.SelectedIndex = 0;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58 };
        bar.Controls.Add(Ui.Button("Importar JSON do site", Importar)); bar.Controls.Add(status);
        bar.Controls.Add(Ui.Button("Atualizar status", Atualizar)); bar.Controls.Add(Ui.Button("Excluir / cancelar", Excluir)); bar.Controls.Add(Ui.Button("Atualizar lista", Carregar));
        Controls.Add(grid); Controls.Add(bar); Load += (_, _) => Carregar();
    }
    private int? Id => grid.CurrentRow is null ? null : Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
    private void Carregar() { try { grid.DataSource = Conexao.Consultar("SELECT Id, CodigoSite, ClienteEmail, DataPedido, Status, Total FROM Pedidos ORDER BY DataPedido DESC"); } catch (Exception ex) { Ui.Error(ex); } }
    private void Importar()
    {
        using var dialog = new OpenFileDialog { Filter = "Arquivo JSON|*.json", Title = "Escolha o JSON exportado pelo site" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { var count = PedidoJsonImporter.Importar(dialog.FileName); MessageBox.Show($"{count} pedido(s) novo(s) importado(s). Repetidos foram ignorados."); Carregar(); } catch (Exception ex) { Ui.Error(ex); }
    }
    private void Atualizar()
    {
        if (Id is not int id) return;
        try { Conexao.Executar("UPDATE Pedidos SET Status=@status WHERE Id=@id", new MySqlParameter("@status", status.Text), new MySqlParameter("@id", id)); Carregar(); } catch (Exception ex) { Ui.Error(ex); }
    }
    private void Excluir()
    {
        if (Id is not int id || MessageBox.Show("Excluir este pedido?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try { Conexao.Executar("DELETE FROM Pedidos WHERE Id=@id", new MySqlParameter("@id", id)); Carregar(); } catch (Exception ex) { Ui.Error(ex); }
    }
}

public sealed class FrmProdutos : Form
{
    private readonly DataGridView grid = Ui.Grid();
    public FrmProdutos()
    {
        Ui.Style(this, "Produtos"); var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58 };
        bar.Controls.Add(Ui.Button("Editar produto selecionado", Editar)); bar.Controls.Add(Ui.Button("Excluir produto", Excluir)); bar.Controls.Add(Ui.Button("Atualizar lista", Carregar));
        Controls.Add(grid); Controls.Add(bar); Load += (_, _) => Carregar();
    }
    private int? Id => grid.CurrentRow is null ? null : Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
    private void Carregar() { try { grid.DataSource = Conexao.Consultar("SELECT Id, Nome, Categoria, Descricao, Preco, ImagemUrl, Ativo FROM Produtos ORDER BY Nome"); } catch (Exception ex) { Ui.Error(ex); } }
    private void Editar()
    {
        if (grid.CurrentRow is null) return;
        using var form = new Form { Text = "Editar produto", Size = new Size(430, 360), StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(25,25,25), ForeColor = Color.White };
        var fields = new[] { "Nome", "Categoria", "Descricao", "Preco", "ImagemUrl" }; var boxes = new Dictionary<string, TextBox>();
        for (int i=0;i<fields.Length;i++) { form.Controls.Add(new Label { Text=fields[i], Left=15, Top=18+i*48, Width=105 }); var box=new TextBox { Left=125, Top=15+i*48, Width=260, Text=Convert.ToString(grid.CurrentRow.Cells[fields[i]].Value)??"", MaxLength=240 }; form.Controls.Add(box); boxes[fields[i]]=box; }
        var ativo = new CheckBox { Text="Ativo", Left=125, Top=255, Checked=Convert.ToBoolean(grid.CurrentRow.Cells["Ativo"].Value), ForeColor=Color.White }; form.Controls.Add(ativo);
        var ok=Ui.Button("Salvar",()=>form.DialogResult=DialogResult.OK); ok.Left=125; ok.Top=285; form.Controls.Add(ok);
        if(form.ShowDialog(this)!=DialogResult.OK)return;
        if(!decimal.TryParse(boxes["Preco"].Text,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),out var preco)){MessageBox.Show("Informe um preço válido.");return;}
        try { Conexao.Executar("UPDATE Produtos SET Nome=@nome,Categoria=@categoria,Descricao=@descricao,Preco=@preco,ImagemUrl=@imagem,Ativo=@ativo WHERE Id=@id", new MySqlParameter("@nome",boxes["Nome"].Text),new MySqlParameter("@categoria",boxes["Categoria"].Text),new MySqlParameter("@descricao",boxes["Descricao"].Text),new MySqlParameter("@preco",preco),new MySqlParameter("@imagem",boxes["ImagemUrl"].Text),new MySqlParameter("@ativo",ativo.Checked),new MySqlParameter("@id",Id)); Carregar(); } catch(Exception ex){Ui.Error(ex);}
    }
    private void Excluir(){if(Id is not int id||MessageBox.Show("Excluir o produto selecionado?", "Confirmar", MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{Conexao.Executar("DELETE FROM Produtos WHERE Id=@id",new MySqlParameter("@id",id));Carregar();}catch(Exception ex){Ui.Error(ex);}}
}

public sealed class FrmCadastro : Form
{
    private readonly string tabela;
    private readonly DataGridView grid=Ui.Grid();
    private readonly TextBox nome=new(){Width=210,MaxLength=100}; private readonly TextBox email=new(){Width=210,MaxLength=120}; private readonly TextBox telefone=new(){Width=150,MaxLength=20}; private readonly TextBox documento=new(){Width=160,MaxLength=20};
    public FrmCadastro(string tabela)
    {
        if(tabela is not ("Clientes" or "Entregadores")) throw new ArgumentException("Cadastro não permitido."); this.tabela=tabela;
        Ui.Style(this,tabela); var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=110};
        foreach(var pair in new[]{("Nome",nome),("E-mail",email),("Telefone",telefone),("CPF",documento)}){bar.Controls.Add(new Label{Text=pair.Item1,AutoSize=true,Padding=new Padding(6,10,0,0)});bar.Controls.Add(pair.Item2);}
        bar.Controls.Add(Ui.Button("Inserir",Inserir));bar.Controls.Add(Ui.Button("Atualizar selecionado",Atualizar));bar.Controls.Add(Ui.Button("Excluir selecionado",Excluir));bar.Controls.Add(Ui.Button("Limpar",Limpar));
        Controls.Add(grid);Controls.Add(bar);Load+=(_,_)=>Carregar();
        grid.SelectionChanged+=(_,_)=>{if(grid.CurrentRow is null)return;nome.Text=Convert.ToString(grid.CurrentRow.Cells["Nome"].Value)??"";email.Text=Convert.ToString(grid.CurrentRow.Cells["Email"].Value)??"";telefone.Text=Convert.ToString(grid.CurrentRow.Cells["Telefone"].Value)??"";documento.Text=Convert.ToString(grid.CurrentRow.Cells["Documento"].Value)??"";};
    }
    private int? Id=>grid.CurrentRow is null?null:Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
    private void Carregar(){try{grid.DataSource=Conexao.Consultar($"SELECT Id,Nome,Email,Telefone,Documento FROM {tabela} ORDER BY Nome");}catch(Exception ex){Ui.Error(ex);}}
    private MySqlParameter[] Params()=>[new("@nome",nome.Text.Trim()),new("@email",email.Text.Trim()),new("@telefone",telefone.Text.Trim()),new("@documento",documento.Text.Trim())];
    private bool Validar(){if(string.IsNullOrWhiteSpace(nome.Text)||string.IsNullOrWhiteSpace(email.Text)){MessageBox.Show("Nome e e-mail são obrigatórios.");return false;}if(!email.Text.Contains('@')){MessageBox.Show("Informe um e-mail válido.");return false;}return true;}
    private void Inserir(){if(!Validar())return;try{Conexao.Executar($"INSERT INTO {tabela}(Nome,Email,Telefone,Documento) VALUES(@nome,@email,@telefone,@documento)",Params());Carregar();Limpar();}catch(Exception ex){Ui.Error(ex);}}
    private void Atualizar(){if(Id is not int id||!Validar())return;try{Conexao.Executar($"UPDATE {tabela} SET Nome=@nome,Email=@email,Telefone=@telefone,Documento=@documento WHERE Id=@id",[..Params(),new("@id",id)]);Carregar();}catch(Exception ex){Ui.Error(ex);}}
    private void Excluir(){if(Id is not int id||MessageBox.Show("Excluir o registro selecionado?","Confirmar",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{Conexao.Executar($"DELETE FROM {tabela} WHERE Id=@id",new MySqlParameter("@id",id));Carregar();Limpar();}catch(Exception ex){Ui.Error(ex);}}
    private void Limpar(){nome.Clear();email.Clear();telefone.Clear();documento.Clear();grid.ClearSelection();}
}
