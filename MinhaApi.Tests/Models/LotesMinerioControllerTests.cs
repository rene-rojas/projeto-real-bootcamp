using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaApi.Controllers;
using MinhaApi.Data;
using MinhaApi.Dtos;
using MinhaApi.Models;
using Xunit;

namespace MinhaApi.Tests.Controllers
{
    public class LotesMinerioControllerTests
    {
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"db_tests_{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        private LotesMinerioController CreateController(AppDbContext ctx)
            => new LotesMinerioController(ctx);

        private static LoteMinerio NovoLote(
            string codigo = "L-001",
            string mina = "Carajás",
            decimal teorFe = 65m,
            decimal umidade = 7m,
            decimal toneladas = 1000m,
            StatusLote status = StatusLote.EmEstoque,
            string local = "Pátio Carajás",
            DateTime? data = null,
            decimal? sio2 = null,
            decimal? p = null
        )
            => new()
            {
                CodigoLote = codigo,
                MinaOrigem = mina,
                TeorFe = teorFe,
                Umidade = umidade,
                Toneladas = toneladas,
                Status = status,
                LocalizacaoAtual = local,
                DataProducao = data ?? new DateTime(2026, 2, 9, 10, 0, 0, DateTimeKind.Utc),
                SiO2 = sio2,
                P = p
            };

        // ------------------------------
        // POST /api/LotesMinerio (Create)
        // ------------------------------

        [Fact]
        public async Task Create_ComDadosValidos_DeveRetornarCreatedEPersistir()
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var input = new CreateLoteMinerioDto
            {
                CodigoLote = "MNA-2026-000123",
                MinaOrigem = "Carajás N4E",
                TeorFe = 66.5m,
                Umidade = 8.2m,
                SiO2 = 3.9m,
                P = 0.03m,
                Toneladas = 15000.750m,
                DataProducao = null, // deve assumir DateTime.UtcNow no controller
                Status = 1,          // EmTransporte
                LocalizacaoAtual = "EFVM - Trem 123"
            };

            var before = DateTime.UtcNow;
            var result = await controller.Create(input);
            var after = DateTime.UtcNow;

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(LotesMinerioController.GetById), created.ActionName);

            var entity = Assert.IsType<LoteMinerio>(created.Value);
            Assert.True(entity.Id > 0);
            Assert.Equal("MNA-2026-000123", entity.CodigoLote);
            Assert.Equal("Carajás N4E", entity.MinaOrigem);
            Assert.Equal(66.5m, entity.TeorFe);
            Assert.Equal(8.2m, entity.Umidade);
            Assert.Equal(3.9m, entity.SiO2);
            Assert.Equal(0.03m, entity.P);
            Assert.Equal(15000.750m, entity.Toneladas);
            Assert.Equal(StatusLote.EmTransporte, entity.Status);
            Assert.Equal("EFVM - Trem 123", entity.LocalizacaoAtual);

            // DataProducao veio null -> controller usa UtcNow; toleramos janela.
            Assert.True(entity.DataProducao >= before && entity.DataProducao <= after.AddSeconds(5));

            // Confirma persistência no "banco"
            var persisted = await ctx.LotesMinerio.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id);
            Assert.NotNull(persisted);
        }

        [Theory]
        [InlineData(null, "Carajás", 10, 10, 100, 1, "Pátio", "CodigoLote é obrigatório.")]
        [InlineData("   ", "Carajás", 10, 10, 100, 1, "Pátio", "CodigoLote é obrigatório.")]
        [InlineData("COD", null, 10, 10, 100, 1, "Pátio", "MinaOrigem é obrigatória.")]
        [InlineData("COD", "   ", 10, 10, 100, 1, "Pátio", "MinaOrigem é obrigatória.")]
        [InlineData("COD", "Mina", 10, 10, 100, 1, null, "LocalizacaoAtual é obrigatória.")]
        [InlineData("COD", "Mina", 10, 10, 100, 1, "   ", "LocalizacaoAtual é obrigatória.")]
        [InlineData("COD", "Mina", -0.1, 10, 100, 1, "Loc", "TeorFe deve estar entre 0 e 100 (%).")]
        [InlineData("COD", "Mina", 100.1, 10, 100, 1, "Loc", "TeorFe deve estar entre 0 e 100 (%).")]
        [InlineData("COD", "Mina", 10, -0.1, 100, 1, "Loc", "Umidade deve estar entre 0 e 100 (%).")]
        [InlineData("COD", "Mina", 10, 100.1, 100, 1, "Loc", "Umidade deve estar entre 0 e 100 (%).")]
        [InlineData("COD", "Mina", 10, 10, 0, 1, "Loc", "Toneladas deve ser > 0.")]
        [InlineData("COD", "Mina", 10, 10, 100, -1, "Loc", "Status inválido (use 0, 1 ou 2).")]
        [InlineData("COD", "Mina", 10, 10, 100, 3, "Loc", "Status inválido (use 0, 1 ou 2).")]
        public async Task Create_ComDadosInvalidos_DeveRetornarBadRequest(
            string? codigo, string? mina, decimal teorFe, decimal umidade,
            decimal toneladas, int status, string? local, string mensagemEsperada)
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var input = new CreateLoteMinerioDto
            {
                CodigoLote = codigo ?? "",
                MinaOrigem = mina ?? "",
                TeorFe = teorFe,
                Umidade = umidade,
                Toneladas = toneladas,
                Status = status,
                LocalizacaoAtual = local ?? ""
            };

            var result = await controller.Create(input);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var msg = Assert.IsType<string>(bad.Value);
            Assert.Equal(mensagemEsperada, msg);
        }

        [Fact]
        public async Task Create_ComCodigoDuplicado_DeveRetornarConflict()
        {
            using var ctx = CreateInMemoryContext();
            // Seed com um lote existente
            ctx.LotesMinerio.Add(NovoLote(codigo: "DUPL-123"));
            await ctx.SaveChangesAsync();

            var controller = CreateController(ctx);

            var input = new CreateLoteMinerioDto
            {
                CodigoLote = "DUPL-123",
                MinaOrigem = "Carajás",
                TeorFe = 60m,
                Umidade = 5m,
                Toneladas = 100m,
                Status = 0,
                LocalizacaoAtual = "Pátio"
            };

            var result = await controller.Create(input);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var msg = Assert.IsType<string>(conflict.Value);
            Assert.Contains("Já existe um lote com CodigoLote", msg);
        }

        // ------------------------------
        // GET /api/LotesMinerio/{id}
        // ------------------------------

        [Fact]
        public async Task GetById_QuandoNaoExiste_DeveRetornarNotFound()
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var result = await controller.GetById(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_QuandoExiste_DeveRetornarOkComDto()
        {
            using var ctx = CreateInMemoryContext();
            var existente = NovoLote(codigo: "OK-001", status: StatusLote.EmTransporte);
            ctx.LotesMinerio.Add(existente);
            await ctx.SaveChangesAsync();

            var controller = CreateController(ctx);

            var result = await controller.GetById(existente.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var dto = Assert.IsType<LoteMinerioResponseDto>(ok.Value);
            Assert.Equal(existente.Id, dto.Id);
            Assert.Equal(existente.CodigoLote, dto.CodigoLote);
            Assert.Equal(existente.MinaOrigem, dto.MinaOrigem);
            Assert.Equal(existente.TeorFe, dto.TeorFe);
            Assert.Equal(existente.Umidade, dto.Umidade);
            Assert.Equal(existente.SiO2, dto.SiO2);
            Assert.Equal(existente.P, dto.P);
            Assert.Equal(existente.Toneladas, dto.Toneladas);
            Assert.Equal(existente.DataProducao, dto.DataProducao);
            Assert.Equal(existente.Status, dto.Status);
            Assert.Equal(existente.LocalizacaoAtual, dto.LocalizacaoAtual);
        }

        // ------------------------------
        // GET /api/LotesMinerio
        // ------------------------------

        [Fact]
        public async Task GetAll_DeveRetornarOkComLista()
        {
            using var ctx = CreateInMemoryContext();
            ctx.LotesMinerio.Add(NovoLote(codigo: "L-1"));
            ctx.LotesMinerio.Add(NovoLote(codigo: "L-2"));
            await ctx.SaveChangesAsync();

            var controller = CreateController(ctx);

            var result = await controller.GetAll();
            var ok = Assert.IsType<OkObjectResult>(result);
            var lista = Assert.IsType<List<LoteMinerio>>(ok.Value);

            Assert.Equal(2, lista.Count);
            Assert.Contains(lista, x => x.CodigoLote == "L-1");
            Assert.Contains(lista, x => x.CodigoLote == "L-2");
        }

        // ------------------------------
        // PUT /api/LotesMinerio/{id}
        // ------------------------------

        [Theory]
        [InlineData(null, "Loc", 10, 10, 100, 1, "MinaOrigem é obrigatória.")]
        [InlineData("   ", "Loc", 10, 10, 100, 1, "MinaOrigem é obrigatória.")]
        [InlineData("Mina", null, 10, 10, 100, 1, "LocalizacaoAtual é obrigatória.")]
        [InlineData("Mina", "   ", 10, 10, 100, 1, "LocalizacaoAtual é obrigatória.")]
        [InlineData("Mina", "Loc", -0.1, 10, 100, 1, "TeorFe deve estar entre 0 e 100 (%).")]
        [InlineData("Mina", "Loc", 100.1, 10, 100, 1, "TeorFe deve estar entre 0 e 100 (%).")]
        [InlineData("Mina", "Loc", 10, -0.1, 100, 1, "Umidade deve estar entre 0 e 100 (%).")]
        [InlineData("Mina", "Loc", 10, 100.1, 100, 1, "Umidade deve estar entre 0 e 100 (%).")]
        [InlineData("Mina", "Loc", 10, 10, 0, 1, "Toneladas deve ser > 0.")]
        [InlineData("Mina", "Loc", 10, 10, 100, -1, "Status inválido (use 0, 1 ou 2).")]
        [InlineData("Mina", "Loc", 10, 10, 100, 3, "Status inválido (use 0, 1 ou 2).")]
        public async Task Update_ComDadosInvalidos_DeveRetornarBadRequest(
            string? mina, string? loc, decimal teorFe, decimal umidade, decimal toneladas, int status, string mensagemEsperada)
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var input = new UpdateLoteMinerioDto
            {
                MinaOrigem = mina ?? "",
                LocalizacaoAtual = loc ?? "",
                TeorFe = teorFe,
                Umidade = umidade,
                Toneladas = toneladas,
                Status = status
            };

            var result = await controller.Update(1, input);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var msg = Assert.IsType<string>(bad.Value);
            Assert.Equal(mensagemEsperada, msg);
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNotFound()
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var input = new UpdateLoteMinerioDto
            {
                MinaOrigem = "Mina",
                LocalizacaoAtual = "Loc",
                TeorFe = 10,
                Umidade = 10,
                Toneladas = 1,
                Status = 0
            };

            var result = await controller.Update(999, input);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_ComDadosValidos_DeveRetornarNoContentEAtualizar()
        {
            using var ctx = CreateInMemoryContext();
            var existente = NovoLote(codigo: "UP-001", mina: "Antiga Mina", status: StatusLote.EmEstoque, local: "Antiga Loc");
            ctx.LotesMinerio.Add(existente);
            await ctx.SaveChangesAsync();

            var controller = CreateController(ctx);

            var input = new UpdateLoteMinerioDto
            {
                MinaOrigem = "Nova Mina",
                TeorFe = 70.5m,
                Umidade = 9.9m,
                SiO2 = 4.2m,
                P = 0.04m,
                Toneladas = 2000.5m,
                DataProducao = existente.DataProducao, // não muda de fato
                Status = 2, // Embarcado
                LocalizacaoAtual = "Nova Loc"
            };

            var result = await controller.Update(existente.Id, input);
            Assert.IsType<NoContentResult>(result);

            var atualizado = await ctx.LotesMinerio.AsNoTracking().SingleAsync(x => x.Id == existente.Id);
            // CodigoLote não muda via Update
            Assert.Equal("UP-001", atualizado.CodigoLote);
            Assert.Equal("Nova Mina", atualizado.MinaOrigem);
            Assert.Equal(70.5m, atualizado.TeorFe);
            Assert.Equal(9.9m, atualizado.Umidade);
            Assert.Equal(4.2m, atualizado.SiO2);
            Assert.Equal(0.04m, atualizado.P);
            Assert.Equal(2000.5m, atualizado.Toneladas);
            Assert.Equal(StatusLote.Embarcado, atualizado.Status);
            Assert.Equal("Nova Loc", atualizado.LocalizacaoAtual);
        }

        // ------------------------------
        // DELETE /api/LotesMinerio/{id}
        // ------------------------------

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarNotFound()
        {
            using var ctx = CreateInMemoryContext();
            var controller = CreateController(ctx);

            var result = await controller.Delete(12345);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_QuandoExiste_DeveRetornarNoContentERemover()
        {
            using var ctx = CreateInMemoryContext();
            var existente = NovoLote(codigo: "DEL-001");
            ctx.LotesMinerio.Add(existente);
            await ctx.SaveChangesAsync();

            var controller = CreateController(ctx);

            var result = await controller.Delete(existente.Id);
            Assert.IsType<NoContentResult>(result);

            var aindaExiste = await ctx.LotesMinerio.AnyAsync(x => x.Id == existente.Id);
            Assert.False(aindaExiste);
        }

        // Para simular DbUpdateException no Delete, criamos um DbContext que lança na hora do SaveChangesAsync.
        private sealed class ThrowingDbContext : AppDbContext
        {
            public ThrowingDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                throw new DbUpdateException("Simulated constraint failure.", innerException: null);
            }
        }

        [Fact]
        public async Task Delete_QuandoSaveChangesFalha_DeveRetornarConflict()
        {
            // Usamos o contexto "normal" para criar o registro...
            var baseOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"db_throw_{Guid.NewGuid()}")
                .Options;

            using (var seed = new AppDbContext(baseOptions))
            {
                seed.LotesMinerio.Add(NovoLote(codigo: "C-ERR"));
                await seed.SaveChangesAsync();
            }

            // ...e um contexto customizado que lança DbUpdateException para o controller.
            using var throwingCtx = new ThrowingDbContext(baseOptions);
            var controller = new LotesMinerioController(throwingCtx);

            // Precisamos pegar o Id do item sem tracking
            int id;
            using (var reader = new AppDbContext(baseOptions))
            {
                id = reader.LotesMinerio.AsNoTracking().Single(x => x.CodigoLote == "C-ERR").Id;
            }

            var result = await controller.Delete(id);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var msg = Assert.IsType<string>(conflict.Value);
            Assert.Contains("Não foi possível excluir o lote", msg);
        }
    }
}