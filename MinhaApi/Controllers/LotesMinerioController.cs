using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaApi.Data;
using MinhaApi.Models;
using MinhaApi.Dtos;

namespace MinhaApi.Controllers
{
    [ApiController]
    [Route("api/LotesMinerio")]
    public class LotesMinerioController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LotesMinerioController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLoteMinerioDto input)
        {
            if (string.IsNullOrWhiteSpace(input.CodigoLote))
                return BadRequest("CodigoLote é obrigatório.");
            if (string.IsNullOrWhiteSpace(input.MinaOrigem))
                return BadRequest("MinaOrigem é obrigatória.");
            if (string.IsNullOrWhiteSpace(input.LocalizacaoAtual))
                return BadRequest("LocalizacaoAtual é obrigatória.");
            if (input.TeorFe is < 0 or > 100)
                return BadRequest("TeorFe deve estar entre 0 e 100 (%).");
            if (input.Umidade is < 0 or > 100)
                return BadRequest("Umidade deve estar entre 0 e 100 (%).");
            if (input.Toneladas <= 0)
                return BadRequest("Toneladas deve ser > 0.");
            if (input.Status is < 0 or > 2)
                return BadRequest("Status inválido (use 0, 1 ou 2).");

            var exists = await _db.LotesMinerio.AnyAsync(x => x.CodigoLote == input.CodigoLote);
            if (exists)
                return Conflict($"Já existe um lote com CodigoLote '{input.CodigoLote}'.");

            var lote = new LoteMinerio
            {
                CodigoLote = input.CodigoLote,
                MinaOrigem = input.MinaOrigem,
                TeorFe = input.TeorFe,
                Umidade = input.Umidade,
                SiO2 = input.SiO2,
                P = input.P,
                Toneladas = input.Toneladas,
                DataProducao = input.DataProducao ?? DateTime.UtcNow,
                Status = (StatusLote)input.Status,
                LocalizacaoAtual = input.LocalizacaoAtual
            };

            _db.LotesMinerio.Add(lote);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = lote.Id }, lote);
        }

        [HttpGet("{id:int}")]
        
        public async Task<IActionResult> GetById(int id)
        {
            var l = await _db.LotesMinerio.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (l is null) return NotFound();

            var dto = new LoteMinerioResponseDto(
                l.Id, l.CodigoLote, l.MinaOrigem, l.TeorFe, l.Umidade, l.SiO2, l.P,
                l.Toneladas, l.DataProducao, l.Status, l.LocalizacaoAtual
            );

            return Ok(dto);
        }

        
        [HttpGet("")]
        
        public async Task<IActionResult> GetAll()
        {
            var lotes = await _db.LotesMinerio.AsNoTracking().ToListAsync();
            return Ok(lotes);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLoteMinerioDto input)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(input.MinaOrigem))
                return BadRequest("MinaOrigem é obrigatória.");
            if (string.IsNullOrWhiteSpace(input.LocalizacaoAtual))
                return BadRequest("LocalizacaoAtual é obrigatória.");
            if (input.TeorFe is < 0 or > 100)
                return BadRequest("TeorFe deve estar entre 0 e 100 (%).");
            if (input.Umidade is < 0 or > 100)
                return BadRequest("Umidade deve estar entre 0 e 100 (%).");
            if (input.Toneladas <= 0)
                return BadRequest("Toneladas deve ser > 0.");
            if (input.Status is < 0 or > 2)
                return BadRequest("Status inválido (use 0, 1 ou 2).");

            var lote = await _db.LotesMinerio.FirstOrDefaultAsync(x => x.Id == id);
            if (lote is null) return NotFound();

            // Atualiza campos (CodigoLote não muda aqui)
            lote.MinaOrigem = input.MinaOrigem;
            lote.TeorFe = input.TeorFe;
            lote.Umidade = input.Umidade;
            lote.SiO2 = input.SiO2;
            lote.P = input.P;
            lote.Toneladas = input.Toneladas;
            lote.DataProducao = input.DataProducao ?? lote.DataProducao;
            lote.Status = (StatusLote)input.Status;
            lote.LocalizacaoAtual = input.LocalizacaoAtual;

            await _db.SaveChangesAsync();
            return NoContent(); // 204
        }
        
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lote = await _db.LotesMinerio.FirstOrDefaultAsync(x => x.Id == id);
            if (lote is null)
                return NotFound(); // 404

            _db.LotesMinerio.Remove(lote);

            try
            {
                await _db.SaveChangesAsync();
                return NoContent(); // 204
            }
            catch (DbUpdateException ex)
            {
                // Ex.: violação de FK se houver dependências (movimentações, notas, etc.)
                return Conflict("Não foi possível excluir o lote. Ele pode estar relacionado a outros registros.");
            }
        }

    }
}