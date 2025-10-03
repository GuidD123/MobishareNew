namespace Mobishare.Core.Enums
{
    /// <summary>
    /// Ruoli disponibili per gli utenti del sistema
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Utente normale che può noleggiare mezzi
        /// </summary>
        Utente = 0,

        /// <summary>
        /// Gestore che può amministrare mezzi e parcheggi
        /// </summary>
        Gestore = 1,

        /// <summary>
        /// Super amministratore con accesso completo al sistema
        /// </summary>
        SuperAdmin = 99
    }
}