using System;
using MinhaApi.Models;
using Xunit;

namespace MinhaApi.Tests.Models
{
    public class LoteMinerioTests
    {
        [Fact]
        public void Construtor_Padrao_DeveInicializarStringsComoVazias()
        {
            // Arrange & Act
            var lote = new LoteMinerio();

            // Assert
            Assert.NotNull(lote.CodigoLote);
            Assert.Equal(string.Empty, lote.CodigoLote);

            Assert.NotNull(lote.MinaOrigem);
            Assert.Equal(string.Empty, lote.MinaOrigem);

            Assert.NotNull(lote.LocalizacaoAtual);
            Assert.Equal(string.Empty, lote.LocalizacaoAtual);
        }

        [Fact]
        public void Construtor_Padrao_DeveInicializarTiposValorComDefaults()
        {
            // Arrange & Act
            var lote = new LoteMinerio();

            // Assert
            Assert.Equal(0, lote.Id);
            Assert.Equal(0m, lote.TeorFe);
            Assert.Equal(0m, lote.Umidade);
            Assert.Equal(0m, lote.Toneladas);

            // DateTime default é 01/01/0001 00:00:00
            Assert.Equal(default(DateTime), lote.DataProducao);

            // Enum default deve ser 0 => EmEstoque
            Assert.Equal(StatusLote.EmEstoque, lote.Status);

            // Nullable devem iniciar como null
            Assert.Null(lote.SiO2);
            Assert.Null(lote.P);
        }

        [Fact]
        public void Set_Get_DeveAtribuirERecuperarValoresCorretamente()
        {
            // Arrange
            var dataProducao = new DateTime(2026, 2, 9, 10, 30, 0, DateTimeKind.Utc);
            var lote = new LoteMinerio
            {
                Id = 42,
                CodigoLote = "MNA-2026-000123",
                MinaOrigem = "Carajás N4E",
                TeorFe = 66.5m,
                Umidade = 8.25m,
                SiO2 = 4.1m,
                P = 0.03m,
                Toneladas = 15000.75m,
                DataProducao = dataProducao,
                Status = StatusLote.EmTransporte,
                LocalizacaoAtual = "EFVM - Trem 123"
            };

            // Act & Assert
            Assert.Equal(42, lote.Id);
            Assert.Equal("MNA-2026-000123", lote.CodigoLote);
            Assert.Equal("Carajás N4E", lote.MinaOrigem);
            Assert.Equal(66.5m, lote.TeorFe);
            Assert.Equal(8.25m, lote.Umidade);
            Assert.Equal(4.1m, lote.SiO2);
            Assert.Equal(0.03m, lote.P);
            Assert.Equal(15000.75m, lote.Toneladas);
            Assert.Equal(dataProducao, lote.DataProducao);
            Assert.Equal(StatusLote.EmTransporte, lote.Status);
            Assert.Equal("EFVM - Trem 123", lote.LocalizacaoAtual);
        }

        [Theory]
        [InlineData(StatusLote.EmEstoque, 0)]
        [InlineData(StatusLote.EmTransporte, 1)]
        [InlineData(StatusLote.Embarcado, 2)]
        public void StatusLote_Enum_DeveManterOsValores(StatusLote status, int esperado)
        {
            // Arrange & Act
            var valorInteiro = (int)status;

            // Assert
            Assert.Equal(esperado, valorInteiro);
        }

        [Fact]
        public void Pode_ModificarStatusAoLongoDoCicloDeVida()
        {
            // Arrange
            var lote = new LoteMinerio();

            // Act & Assert
            Assert.Equal(StatusLote.EmEstoque, lote.Status);

            lote.Status = StatusLote.EmTransporte;
            Assert.Equal(StatusLote.EmTransporte, lote.Status);

            lote.Status = StatusLote.Embarcado;
            Assert.Equal(StatusLote.Embarcado, lote.Status);
        }

        [Fact]
        public void CamposOpcionais_PodemSerDefinidosEResetadosParaNull()
        {
            // Arrange
            var lote = new LoteMinerio();

            // Act
            lote.SiO2 = 3.5m;
            lote.P = 0.05m;

            // Assert
            Assert.Equal(3.5m, lote.SiO2);
            Assert.Equal(0.05m, lote.P);

            // Act - reset para null
            lote.SiO2 = null;
            lote.P = null;

            // Assert
            Assert.Null(lote.SiO2);
            Assert.Null(lote.P);
        }

        // ================================
        // Opcional (mantém Skip até implementar as regras)
        // ================================

        [Theory(Skip = "Regra de negócio ainda não implementada")]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public void Toneladas_NaoPodeSerNegativo(decimal toneladas)
        {
            var lote = new LoteMinerio();
            Assert.Throws<ArgumentOutOfRangeException>(() => lote.Toneladas = toneladas);
        }

        [Theory(Skip = "Regra de negócio ainda não implementada")]
        [InlineData(-0.1)]
        [InlineData(100.1)]
        public void TeoresDevemEstarEntreZeroECem(decimal teor)
        {
            var lote = new LoteMinerio();
            // Ex:
            // Assert.Throws<ArgumentOutOfRangeException>(() => lote.TeorFe = teor);
            // Assert.Throws<ArgumentOutOfRangeException>(() => lote.Umidade = teor);
        }

        [Theory(Skip = "Regra de negócio ainda não implementada")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CodigoLote_DeveSerObrigatorioENaoVazio(string codigo)
        {
            var lote = new LoteMinerio();
            // Ex:
            // Assert.Throws<ArgumentException>(() => lote.CodigoLote = codigo);
        }
    }
}