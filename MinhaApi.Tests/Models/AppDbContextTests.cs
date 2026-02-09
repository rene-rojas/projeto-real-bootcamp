using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql; // NpgsqlConnection
using MinhaApi.Data;
using MinhaApi.Models;
using Xunit;

namespace MinhaApi.Tests.Data
{
    /// <summary>
    /// Testes de METADADOS do modelo usando provider RELACIONAL (Npgsql).
    /// Não abre conexão; apenas constrói o modelo com UseNpgsql para habilitar APIs relacionais.
    /// </summary>
    public class AppDbContext_ModelMetadataTests
    {
        private AppDbContext CreateNpgsqlModelContext()
        {
            // Não conecta no banco. Apenas indica provider relacional para que GetColumnType() etc. funcionem.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public void Model_DeveConfigurarSchemaETabela_Corretamente()
        {
            using var ctx = CreateNpgsqlModelContext();

            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio));
            Assert.NotNull(entity);

            Assert.Equal("public", entity!.GetSchema());
            Assert.Equal("lotes_minerio", entity.GetTableName());
        }

        [Fact]
        public void Model_DeveConfigurarChavePrimaria()
        {
            using var ctx = CreateNpgsqlModelContext();

            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio))!;
            var pk = entity.FindPrimaryKey();

            Assert.NotNull(pk);
            Assert.Single(pk!.Properties);
            Assert.Equal(nameof(LoteMinerio.Id), pk.Properties[0].Name);
        }

        [Fact]
        public void Model_DeveConfigurarPropriedades_DeComprimentoEObrigatoriedade()
        {
            using var ctx = CreateNpgsqlModelContext();
            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio))!;

            var codigo = entity.FindProperty(nameof(LoteMinerio.CodigoLote))!;
            Assert.False(codigo.IsNullable);
            Assert.Equal(50, codigo.GetMaxLength());

            var mina = entity.FindProperty(nameof(LoteMinerio.MinaOrigem))!;
            Assert.False(mina.IsNullable);
            Assert.Equal(120, mina.GetMaxLength());

            var local = entity.FindProperty(nameof(LoteMinerio.LocalizacaoAtual))!;
            Assert.False(local.IsNullable);
            Assert.Equal(200, local.GetMaxLength());
        }

        [Fact]
        public void Model_DeveConfigurarTiposDeColuna()
        {
            using var ctx = CreateNpgsqlModelContext();
            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio))!;

            Assert.Equal("numeric(5,2)", entity.FindProperty(nameof(LoteMinerio.TeorFe))!.GetColumnType());
            Assert.Equal("numeric(5,2)", entity.FindProperty(nameof(LoteMinerio.Umidade))!.GetColumnType());
            Assert.Equal("numeric(5,2)", entity.FindProperty(nameof(LoteMinerio.SiO2))!.GetColumnType());
            Assert.Equal("numeric(5,3)", entity.FindProperty(nameof(LoteMinerio.P))!.GetColumnType());
            Assert.Equal("numeric(12,3)", entity.FindProperty(nameof(LoteMinerio.Toneladas))!.GetColumnType());
        }

        [Fact]
        public void Model_DeveTerIndiceUnicoEmCodigoLote()
        {
            using var ctx = CreateNpgsqlModelContext();
            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio))!;

            var idxCodigo = entity.GetIndexes().FirstOrDefault(i =>
                i.Properties.Count == 1 && i.Properties[0].Name == nameof(LoteMinerio.CodigoLote));

            Assert.NotNull(idxCodigo);
            Assert.True(idxCodigo!.IsUnique, "O índice de CodigoLote deve ser único.");
        }

        [Fact]
        public void Model_DeveConverterStatusParaInt()
        {
            using var ctx = CreateNpgsqlModelContext();
            var entity = ctx.Model.FindEntityType(typeof(LoteMinerio))!;
            var statusProp = entity.FindProperty(nameof(LoteMinerio.Status))!;

            // Forma estável: obter o type mapping e verificar o converter
            var typeMapping = statusProp.GetTypeMapping();
            Assert.NotNull(typeMapping);

            var converter = typeMapping!.Converter;
            Assert.NotNull(converter);
            Assert.Equal(typeof(int), converter!.ProviderClrType); // enum <-> int
        }

        [Fact]
        public void DbSet_DeveEstarDisponivel()
        {
            using var ctx = CreateNpgsqlModelContext();
            Assert.NotNull(ctx.LotesMinerio);
        }
    }

    /// <summary>
    /// Testes de EXECUÇÃO REAL contra PostgreSQL (Npgsql).
    /// Necessita de uma connection string em PG_CONNECTION_STRING apontando para um BANCO DE TESTES.
    /// Estes testes usam EnsureDeleted/EnsureCreated para isolar o schema a cada execução.
    /// </summary>
    public class AppDbContext_PostgresRuntimeTests
    {
        private (AppDbContext Ctx, NpgsqlConnection Conn)? TryCreatePostgresContext()
        {
            var connStr = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                Console.WriteLine("[SKIP-RUNTIME] PG_CONNECTION_STRING não definida — testes de runtime no PostgreSQL foram ignorados.");
                return null; // "pula" sem falhar
            }

            var conn = new NpgsqlConnection(connStr);
            conn.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conn)
                .Options;

            var ctx = new AppDbContext(options);

            // Isola os testes recriando o schema (CUIDADO: use somente em BANCO DE TESTES!)
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();

            return (ctx, conn);
        }

        [Fact]
        public void Inserir_DoisLotesComMesmoCodigo_DeveViolarUnicidade()
        {
            var result = TryCreatePostgresContext();
            if (result is null) return; // "pula" sem erro

            var (ctx, conn) = result.Value;
            using var _ = conn;

            var a = new LoteMinerio
            {
                CodigoLote = "DUP-001",
                MinaOrigem = "Carajás",
                TeorFe = 65.0m,
                Umidade = 7.0m,
                Toneladas = 1000m,
                DataProducao = new DateTime(2026, 2, 1),
                Status = StatusLote.EmEstoque,
                LocalizacaoAtual = "Pátio"
            };

            var b = new LoteMinerio
            {
                CodigoLote = "DUP-001", // duplicado
                MinaOrigem = "Carajás",
                TeorFe = 64.0m,
                Umidade = 6.5m,
                Toneladas = 900m,
                DataProducao = new DateTime(2026, 2, 2),
                Status = StatusLote.EmTransporte,
                LocalizacaoAtual = "EFVM"
            };

            ctx.LotesMinerio.Add(a);
            ctx.SaveChanges(); // primeiro ok

            ctx.LotesMinerio.Add(b);
            Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
        }

        [Fact]
        public void Inserir_Lote_ComCampoObrigatorioNulo_DeveFalhar()
        {
            var result = TryCreatePostgresContext();
            if (result is null) return;

            var (ctx, conn) = result.Value;
            using var _ = conn;

            var lote = new LoteMinerio
            {
                CodigoLote = null!, // Required (IsRequired)
                MinaOrigem = "Carajás",
                TeorFe = 60m,
                Umidade = 5m,
                Toneladas = 500m,
                DataProducao = DateTime.UtcNow,
                Status = StatusLote.EmEstoque,
                LocalizacaoAtual = "Pátio"
            };

            ctx.LotesMinerio.Add(lote);
            Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
        }

        [Fact]
        public void Crud_Completo_DeveFuncionar()
        {
            var result = TryCreatePostgresContext();
            if (result is null) return;

            var (ctx, conn) = result.Value;
            using var _ = conn;

            // CREATE
            var novo = new LoteMinerio
            {
                CodigoLote = "CRUD-001",
                MinaOrigem = "Mina X",
                TeorFe = 62.3m,
                Umidade = 6.7m,
                SiO2 = 3.1m,
                P = 0.02m,
                Toneladas = 1500.750m,
                DataProducao = new DateTime(2026, 2, 9, 10, 0, 0, DateTimeKind.Utc),
                Status = StatusLote.EmEstoque,
                LocalizacaoAtual = "Pátio Central"
            };

            ctx.LotesMinerio.Add(novo);
            ctx.SaveChanges();

            Assert.True(novo.Id > 0);

            // READ
            var carregado = ctx.LotesMinerio.Single(x => x.CodigoLote == "CRUD-001");
            Assert.Equal("Mina X", carregado.MinaOrigem);
            Assert.Equal(62.3m, carregado.TeorFe);
            Assert.Equal(6.7m, carregado.Umidade);
            Assert.Equal(3.1m, carregado.SiO2);
            Assert.Equal(0.02m, carregado.P);
            Assert.Equal(1500.750m, carregado.Toneladas);
            Assert.Equal(StatusLote.EmEstoque, carregado.Status);
            Assert.Equal("Pátio Central", carregado.LocalizacaoAtual);

            // UPDATE
            carregado.Status = StatusLote.EmTransporte;
            carregado.LocalizacaoAtual = "EFVM - Trem 999";
            ctx.SaveChanges();

            var aposUpdate = ctx.LotesMinerio.Single(x => x.Id == carregado.Id);
            Assert.Equal(StatusLote.EmTransporte, aposUpdate.Status);
            Assert.Equal("EFVM - Trem 999", aposUpdate.LocalizacaoAtual);

            // DELETE
            ctx.LotesMinerio.Remove(aposUpdate);
            ctx.SaveChanges();

            var existeAinda = ctx.LotesMinerio.Any(x => x.Id == carregado.Id);
            Assert.False(existeAinda);
        }
    }
}