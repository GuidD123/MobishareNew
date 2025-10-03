namespace Mobishare.Infrastructure.IoT.Config
{
    /// <summary>
    /// Topics MQTT utilizzati nel sistema Mobishare IoT.
    /// Struttura: Parking/{id_parcheggio}/{tipo_messaggio}/{id_mezzo}
    /// </summary>
    public static class MqttTopics
    {
        // === TOPICS PRINCIPALI ===

        /// <summary>
        /// Topic per pubblicazione status dei mezzi (Gateway → Backend).
        /// Formato: Parking/{id_parcheggio}/Mezzi/{id_mezzo}
        /// </summary>
        public const string MezziStatus = "Parking/{0}/Mezzi/{1}";

        /// <summary>
        /// Topic per invio comandi ai mezzi (Backend → Gateway).
        /// Formato: Parking/{id_parcheggio}/Comandi/{id_mezzo}
        /// </summary>
        public const string ComandoMezzo = "Parking/{0}/Comandi/{1}";

        /// <summary>
        /// Topic per risposte ai comandi (Gateway → Backend).
        /// Formato: Parking/{id_parcheggio}/RisposteComandi/{id_mezzo}
        /// </summary>
        public const string RispostaComando = "Parking/{0}/RisposteComandi/{1}";

        // === WILDCARD TOPICS PER SUBSCRIPTION ===

        /// <summary>
        /// Sottoscrizione a tutti gli status dei mezzi di tutti i parcheggi.
        /// Pattern: Parking/+/Mezzi/#
        /// </summary>
        public const string TuttiMezzi = "Parking/+/Mezzi/#";

        /// <summary>
        /// Sottoscrizione a tutti i comandi per mezzi di tutti i parcheggi.
        /// Pattern: Parking/+/Comandi/#
        /// </summary>
        public const string TuttiComandi = "Parking/+/Comandi/#";

        /// <summary>
        /// Sottoscrizione a tutte le risposte ai comandi di tutti i parcheggi.
        /// Pattern: Parking/+/RisposteComandi/#
        /// </summary>
        public const string TutteRisposte = "Parking/+/RisposteComandi/#";

        /// <summary>
        /// Sottoscrizione a tutti i messaggi di un parcheggio specifico.
        /// Pattern: Parking/{id_parcheggio}/#
        /// </summary>
        public const string TuttoUnParcheggio = "Parking/{0}/#";

        // === METODI HELPER ===

        /// <summary>
        /// Genera il topic per lo status di un mezzo specifico.
        /// </summary>
        /// <param name="idParcheggio">ID del parcheggio</param>
        /// <param name="idMezzo">ID del mezzo</param>
        /// <returns>Topic formattato: Parking/{id}/Mezzi/{idMezzo}</returns>
        public static string GetMezziStatusTopic(int idParcheggio, string idMezzo)
        {
            return string.Format(MezziStatus, idParcheggio, idMezzo);
        }

        /// <summary>
        /// Genera il topic per inviare un comando a un mezzo.
        /// </summary>
        /// <param name="idParcheggio">ID del parcheggio</param>
        /// <param name="idMezzo">ID del mezzo</param>
        /// <returns>Topic formattato: Parking/{id}/Comandi/{idMezzo}</returns>
        public static string GetComandoMezzoTopic(int idParcheggio, string idMezzo)
        {
            return string.Format(ComandoMezzo, idParcheggio, idMezzo);
        }

        /// <summary>
        /// Genera il topic per ricevere risposte ai comandi da un mezzo.
        /// </summary>
        /// <param name="idParcheggio">ID del parcheggio</param>
        /// <param name="idMezzo">ID del mezzo</param>
        /// <returns>Topic formattato: Parking/{id}/RisposteComandi/{idMezzo}</returns>
        public static string GetRispostaComandoTopic(int idParcheggio, string idMezzo)
        {
            return string.Format(RispostaComando, idParcheggio, idMezzo);
        }

        /// <summary>
        /// Genera il topic wildcard per tutti i messaggi di un parcheggio.
        /// </summary>
        /// <param name="idParcheggio">ID del parcheggio</param>
        /// <returns>Topic formattato: Parking/{id}/#</returns>
        public static string GetTuttoUnParcheggioTopic(int idParcheggio)
        {
            return string.Format(TuttoUnParcheggio, idParcheggio);
        }
    }
}