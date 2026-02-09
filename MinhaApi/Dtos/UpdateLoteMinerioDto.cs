namespace MinhaApi.Dtos
{
    public class UpdateLoteMinerioDto
    {
        public string MinaOrigem { get; set; } = default!;
        public decimal TeorFe { get; set; }
        public decimal Umidade { get; set; }
        public decimal? SiO2 { get; set; }
        public decimal? P { get; set; }
        public decimal Toneladas { get; set; }
        public DateTime? DataProducao { get; set; }
        public int Status { get; set; }
        public string LocalizacaoAtual { get; set; } = default!;
    }
}