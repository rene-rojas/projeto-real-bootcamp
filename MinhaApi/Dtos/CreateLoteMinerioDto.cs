namespace MinhaApi.Dtos
{
    public class CreateLoteMinerioDto
    {
        public string CodigoLote { get; set; } = "";
        public string MinaOrigem { get; set; } = "";
        public decimal TeorFe { get; set; }
        public decimal Umidade { get; set; }
        public decimal? SiO2 { get; set; }
        public decimal? P { get; set; }
        public decimal Toneladas { get; set; }
        public DateTime? DataProducao { get; set; } // opcional; default = agora
        public int Status { get; set; }             // 0=EmEstoque, 1=EmTransporte, 2=Embarcado
        public string LocalizacaoAtual { get; set; } = "";
    }
}