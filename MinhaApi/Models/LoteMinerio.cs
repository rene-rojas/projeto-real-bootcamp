namespace MinhaApi.Models
{
    public enum StatusLote
    {
        EmEstoque = 0,
        EmTransporte = 1,
        Embarcado = 2
    }

    public class LoteMinerio
    {
        public int Id { get; set; }

        // Identificação e rastreio
        public string CodigoLote { get; set; } = "";   // Ex.: "MNA-2026-000123"
        public string MinaOrigem { get; set; } = "";   // Ex.: "Carajás N4E"

        // Qualidade (simplificada)
        public decimal TeorFe { get; set; }            // % Ferro (0-100)
        public decimal Umidade { get; set; }           // % Umidade (0-100)
        public decimal? SiO2 { get; set; }             // opcional
        public decimal? P { get; set; }                // opcional (fósforo)

        // Logística básica
        public decimal Toneladas { get; set; }         // t
        public DateTime DataProducao { get; set; }     // quando foi gerado o lote
        public StatusLote Status { get; set; }         // estoque / transporte / embarcado
        public string LocalizacaoAtual { get; set; } = ""; // "Mina", "Pátio Carajás", "EFVM - Trem 123", "Porto Tubarão", etc.
    }
}