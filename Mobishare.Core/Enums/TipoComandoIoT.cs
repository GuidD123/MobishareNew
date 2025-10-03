namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Tipi di comando inviabili ai dispositivi IoT
    /// </summary>
    public enum TipoComandoIoT
    {
        /// <summary>
        /// Sblocca fisicamente il mezzo
        /// </summary>
        Sblocca = 0,

        /// <summary>
        /// Blocca fisicamente il mezzo
        /// </summary>
        Blocca = 1,

        /// <summary>
        /// Cambia il colore della spia luminosa
        /// </summary>
        CambiaColoreSpia = 2,

        /// <summary>
        /// Richiedi il livello attuale della batteria
        /// </summary>
        RichiediBatteria = 3,

        /// <summary>
        /// Avvia un test del sistema
        /// </summary>
        AvviaTest = 4,

        /// <summary>
        /// Ferma il test in corso
        /// </summary>
        FermaTest = 5,

        /// <summary>
        /// Reset del sistema del mezzo
        /// </summary>
        Reset = 6,

        /// <summary>
        /// Metti il mezzo in modalità manutenzione
        /// </summary>
        ModalitaManutenzione = 7
    }
}