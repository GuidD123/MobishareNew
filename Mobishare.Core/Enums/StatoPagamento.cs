namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Stati di elaborazione di un pagamento
    /// </summary>
    public enum StatoPagamento
    {
        /// <summary>
        /// Pagamento in attesa di elaborazione
        /// </summary>
        InSospeso,

        /// <summary>
        /// Pagamento completato con successo
        /// </summary>
        Completato,

        /// <summary>
        /// Pagamento fallito o rifiutato
        /// </summary>
        Fallito,

        /// <summary>
        /// Pagamento rimborsato all'utente
        /// </summary>
        //Rimborso,

        /// <summary>
        /// Pagamento annullato prima del completamento
        /// </summary>
        Annullato
    }
}