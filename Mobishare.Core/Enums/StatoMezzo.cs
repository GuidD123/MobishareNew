namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Stati operativi di un mezzo nel sistema
    /// </summary>
    public enum StatoMezzo
    {
        /// <summary>
        /// Mezzo disponibile per il noleggio
        /// </summary>
        Disponibile,

        /// <summary>
        /// Mezzo attualmente in uso da un utente
        /// </summary>
        InUso,

        /// <summary>
        /// Mezzo non prelevabile (batteria scarica, danneggiato, etc.)
        /// </summary>
        NonPrelevabile,

        /// <summary>
        /// Mezzo in manutenzione programmata
        /// </summary>
        Manutenzione
    }
}