namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Colori possibili per la spia luminosa dei mezzi IoT
    /// </summary>
    public enum ColoreSpia
    {
        /// <summary>
        /// Spia rossa - mezzo non prelevabile o in errore
        /// </summary>
        Rosso = 0,

        /// <summary>
        /// Spia verde - mezzo disponibile per il noleggio
        /// </summary>
        Verde = 1,

        /// <summary>
        /// Spia blu - mezzo attualmente in uso - attualmente non prelevabile
        /// </summary>
        Blu = 2,

        /// <summary>
        /// Spia gialla - mezzo in manutenzione o diagnostica - non prelevabile
        /// </summary>
        Giallo = 3,

        /// <summary>
        /// Spia spenta - mezzo offline o senza alimentazione
        /// </summary>
        Spenta = 4
    }
}