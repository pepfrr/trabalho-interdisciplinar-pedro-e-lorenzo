CREATE DATABASE IF NOT EXISTS DB_Delivery CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE DB_Delivery;

CREATE TABLE IF NOT EXISTS UsuariosAcesso (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Email VARCHAR(120) NOT NULL UNIQUE,
  SenhaHash CHAR(64) NOT NULL,
  Ativo BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE TABLE IF NOT EXISTS Produtos (
  Id INT PRIMARY KEY,
  Nome VARCHAR(120) NOT NULL,
  Categoria VARCHAR(60) NOT NULL,
  Descricao VARCHAR(500) NOT NULL,
  Preco DECIMAL(10,2) NOT NULL,
  ImagemUrl VARCHAR(500) NOT NULL DEFAULT '',
  Ativo BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE TABLE IF NOT EXISTS Clientes (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(100) NOT NULL,
  Email VARCHAR(120) NOT NULL UNIQUE,
  Telefone VARCHAR(20) NOT NULL DEFAULT '',
  Documento VARCHAR(20) NOT NULL DEFAULT ''
);
CREATE TABLE IF NOT EXISTS Entregadores (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(100) NOT NULL,
  Email VARCHAR(120) NOT NULL UNIQUE,
  Telefone VARCHAR(20) NOT NULL DEFAULT '',
  Documento VARCHAR(20) NOT NULL DEFAULT ''
);
CREATE TABLE IF NOT EXISTS Pedidos (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  CodigoSite VARCHAR(80) NOT NULL UNIQUE,
  ClienteEmail VARCHAR(120) NOT NULL DEFAULT '',
  DataPedido DATETIME NOT NULL,
  Status VARCHAR(40) NOT NULL DEFAULT 'Recebido',
  Total DECIMAL(10,2) NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS ItensPedido (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  PedidoId INT NOT NULL,
  ProdutoId INT NOT NULL,
  NomeProduto VARCHAR(120) NOT NULL,
  Quantidade INT NOT NULL,
  PrecoUnitario DECIMAL(10,2) NOT NULL,
  CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
  CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id) ON DELETE RESTRICT
);

INSERT INTO UsuariosAcesso (Email, SenhaHash, Ativo)
VALUES ('admin@brasabrio.com', UPPER(SHA2('Brasa@123', 256)), TRUE)
ON DUPLICATE KEY UPDATE SenhaHash=VALUES(SenhaHash), Ativo=TRUE;

-- Produtos iniciais alinhados aos IDs do cardápio do site. Faça alterações de catálogo também pelo site/SQL.
INSERT INTO Produtos (Id, Nome, Categoria, Descricao, Preco, ImagemUrl, Ativo) VALUES
(1,'Picanha na brasa','Cortes','Picanha fatiada, farofa da casa e vinagrete fresco.',49.90,'assets/picanha-na-brasa.jpg',TRUE),
(2,'Costela fogo de chão','Cortes','Costela assada lentamente, macia e defumada.',54.90,'assets/costela-fogo-de-chao.jpg',TRUE),
(3,'Combo Churrasco','Combos','Picanha, linguiça, pão de alho e acompanhamentos.',89.90,'assets/cortes-na-grelha.jpg',TRUE),
(4,'Espetinho misto','Espetinhos','Alcatra, linguiça artesanal e cebola tostada.',24.90,'assets/churrasco-espeto.jpg',TRUE),
(5,'Linguiça artesanal','Cortes','Linguiça suculenta na brasa com molho de pimenta.',26.90,'assets/cortes-na-grelha.jpg',TRUE),
(6,'Pão de alho tostado','Acompanhamentos','Pão crocante com manteiga de alho e ervas.',14.90,'assets/churrasco-espeto.jpg',TRUE),
(7,'Queijo coalho na brasa','Acompanhamentos','Queijo dourado, melado de cana e orégano.',19.90,'assets/picanha-na-brasa.jpg',TRUE),
(8,'Churrasco da família','Combos','Seleção de carnes e acompanhamentos para compartilhar.',139.90,'assets/costela-fogo-de-chao.jpg',TRUE)
ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),Categoria=VALUES(Categoria),Descricao=VALUES(Descricao),Preco=VALUES(Preco),ImagemUrl=VALUES(ImagemUrl),Ativo=VALUES(Ativo);
