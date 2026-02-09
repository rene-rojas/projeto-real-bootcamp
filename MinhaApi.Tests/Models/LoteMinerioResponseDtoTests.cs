using System;
using MinhaApi.Dtos;
using MinhaApi.Models;
using Xunit;

namespace MinhaApi.Tests.Dtos
{
    public class LoteMinerioResponseDtoTests
    {
        [Fact]
        public void Construtor_DeveAtribuirTodosOsCamposCorretamente()
        {
            // Arrange
            var id = 101;
            var codigo = "MNA-2026-000321";
            var mina = "Carajás N4W";
            var teorFe = 66.2m;
            var umidade = 7.3m;
            decimal? siO2 = 4.0m;
            decimal? p = 0.02m;
            var toneladas = 18000.25m;
            var data = new DateTime(2026, 02, 10, 12, 0, 0, DateTimeKind.Utc);
            var status = StatusLote.EmTransporte;
            var localizacao = "EFVM - Trem 789";

            // Act
            var dto = new LoteMinerioResponseDto(
                id, codigo, mina, teorFe, umidade, siO2, p, toneladas, data, status, localizacao
            );

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(codigo, dto.CodigoLote);
            Assert.Equal(mina, dto.MinaOrigem);
            Assert.Equal(teorFe, dto.TeorFe);
            Assert.Equal(umidade, dto.Umidade);
            Assert.Equal(siO2, dto.SiO2);
            Assert.Equal(p, dto.P);
            Assert.Equal(toneladas, dto.Toneladas);
            Assert.Equal(data, dto.DataProducao);
            Assert.Equal(status, dto.Status);
            Assert.Equal(localizacao, dto.LocalizacaoAtual);
        }

        [Fact]
        public void Deconstruction_DeveRespeitarAOrdemPosicional()
        {
            // Arrange
            var dto = new LoteMinerioResponseDto(
                7, "COD-7", "Mina X", 64.1m, 6.2m, 3.1m, 0.01m, 10000m,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), StatusLote.EmEstoque, "Pátio A"
            );

            // Act
            var (
                id, codigo, mina, teorFe, umidade, siO2, p, toneladas, data, status, localizacao
            ) = dto;

            // Assert
            Assert.Equal(7, id);
            Assert.Equal("COD-7", codigo);
            Assert.Equal("Mina X", mina);
            Assert.Equal(64.1m, teorFe);
            Assert.Equal(6.2m, umidade);
            Assert.Equal(3.1m, siO2);
            Assert.Equal(0.01m, p);
            Assert.Equal(10000m, toneladas);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), data);
            Assert.Equal(StatusLote.EmEstoque, status);
            Assert.Equal("Pátio A", localizacao);
        }

        [Fact]
        public void With_DevePermitirCopiaImutavelAlterandoSomenteOsCamposDesejados()
        {
            // Arrange
            var original = new LoteMinerioResponseDto(
                1, "COD-1", "Mina A", 65.0m, 7.0m, null, null, 5000m,
                new DateTime(2026, 2, 9, 10, 0, 0, DateTimeKind.Utc), StatusLote.EmEstoque, "Mina"
            );

            // Act
            var modificado = original with
            {
                Status = StatusLote.EmTransporte,
                LocalizacaoAtual = "EFVM - Trem 123",
                SiO2 = 3.3m
            };

            // Assert — o original permanece igual (imutabilidade)
            Assert.Equal(StatusLote.EmEstoque, original.Status);
            Assert.Equal("Mina", original.LocalizacaoAtual);
            Assert.Null(original.SiO2);

            // Assert — o novo reflete apenas as mudanças
            Assert.Equal(StatusLote.EmTransporte, modificado.Status);
            Assert.Equal("EFVM - Trem 123", modificado.LocalizacaoAtual);
            Assert.Equal(3.3m, modificado.SiO2);

            // Assert — os demais campos são copiados
            Assert.Equal(original.Id, modificado.Id);
            Assert.Equal(original.CodigoLote, modificado.CodigoLote);
            Assert.Equal(original.MinaOrigem, modificado.MinaOrigem);
            Assert.Equal(original.TeorFe, modificado.TeorFe);
            Assert.Equal(original.Umidade, modificado.Umidade);
            Assert.Equal(original.P, modificado.P);
            Assert.Equal(original.Toneladas, modificado.Toneladas);
            Assert.Equal(original.DataProducao, modificado.DataProducao);
        }

        [Fact]
        public void Equality_SameValues_DeveSerIgual()
        {
            // Arrange
            var a = new LoteMinerioResponseDto(
                10, "COD-10", "Mina Z", 62.5m, 6.9m, 2.9m, 0.02m, 8000m,
                new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), StatusLote.Embarcado, "Porto Tubarão"
            );

            var b = new LoteMinerioResponseDto(
                10, "COD-10", "Mina Z", 62.5m, 6.9m, 2.9m, 0.02m, 8000m,
                new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), StatusLote.Embarcado, "Porto Tubarão"
            );

            // Act & Assert
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_DiffValues_DeveSerDiferente()
        {
            // Arrange
            var a = new LoteMinerioResponseDto(
                10, "COD-10", "Mina Z", 62.5m, 6.9m, 2.9m, 0.02m, 8000m,
                new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), StatusLote.Embarcado, "Porto Tubarão"
            );

            var b = a with { Status = StatusLote.EmTransporte }; // altera 1 campo

            // Act & Assert
            Assert.NotEqual(a, b);
            Assert.True(a != b);
            Assert.False(a == b);
        }

        [Fact]
        public void ToString_DeveConterNomeDoRecordEValoresPrincipais()
        {
            // Arrange
            var dto = new LoteMinerioResponseDto(
                2, "COD-2", "Mina B", 63.3m, 5.5m, null, 0.01m, 7000m,
                new DateTime(2026, 2, 5, 8, 0, 0, DateTimeKind.Utc), StatusLote.EmEstoque, "Pátio Central"
            );

            // Act
            var s = dto.ToString();

            // Assert
            Assert.Contains(nameof(LoteMinerioResponseDto), s);
            Assert.Contains("Id = 2", s);
            Assert.Contains("CodigoLote = COD-2", s);
            Assert.Contains("Status = EmEstoque", s);
        }

        [Fact]
        public void CamposOpcionais_PodemSerNull()
        {
            // Arrange
            var dto = new LoteMinerioResponseDto(
                3, "COD-3", "Mina C", 64.0m, 6.0m, null, null, 9000m,
                new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), StatusLote.EmEstoque, "Pátio Carajás"
            );

            // Assert
            Assert.Null(dto.SiO2);
            Assert.Null(dto.P);
        }

        [Theory]
        [InlineData(StatusLote.EmEstoque, 0)]
        [InlineData(StatusLote.EmTransporte, 1)]
        [InlineData(StatusLote.Embarcado, 2)]
        public void Status_Enum_DeveManterValores(StatusLote status, int esperado)
        {
            // Arrange
            var dto = new LoteMinerioResponseDto(
                4, "COD-4", "Mina D", 65m, 6m, null, null, 9500m,
                new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), status, "Local X"
            );

            // Act
            var valor = (int)dto.Status;

            // Assert
            Assert.Equal(esperado, valor);
        }
    }
}