namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Stati possibili di una corsa
    /// </summary>
    public enum StatoCorsa
    {
        /// <summary>
        /// Corsa attualmente in corso
        /// </summary>
        InCorso,

        /// <summary>
        /// Corsa completata regolarmente
        /// </summary>
        Completata,

        /// <summary>
        /// Corsa annullata dall'utente o dal sistema
        /// </summary>
        Annullata,

        /// <summary>
        /// Corsa sospesa per problemi tecnici
        /// </summary>
        Sospesa
    }
}