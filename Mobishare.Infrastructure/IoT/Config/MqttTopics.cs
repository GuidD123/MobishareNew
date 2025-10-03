namespace Mobishare.Infrastructure.IoT.Config
{
    /// <summary>
    /// Topics MQTT utilizzati nel sistema - seguendo la specifica del progetto
    /// </summary>
    public static class MqttTopics
    {
        // === TOPICS PRINCIPALI ===

        /// <summary>
        /// Topic per invio informazioni mezzi dal Gateway al Backend
        /// Formato: Parking/{id_parking}/Mezzi
        /// </summary>
        public const string MezziStatus = "Parking/{0}/Mezzi";

        /// <summary>
        /// Topic per comandi dal Backend al Gateway per un mezzo specifico
        /// Formato: Parking/{id_parking}/StatoMezzi/{id_mezzo}
        /// </summary>
        public const string ComandoMezzo = "Parking/{0}/StatoMezzi/{1}";

        /// <summary>
        /// Topic per risposte ai comandi
        /// Formato: Parking/{id_parking}/RisposteComandi/{id_mezzo}
        /// </summary>
        public const string RispostaComando = "Parking/{0}/RisposteComandi/{1}";

        // === WILDCARD TOPICS PER SUBSCRIPTION ===

        /// <summary>
        /// Sottoscrizione a tutti i mezzi di tutti i parcheggi
        /// </summary>
        public const string TuttiMezzi = "Parking/+/Mezzi";

        /// <summary>
        /// Sottoscrizione a tutti i comandi per mezzi
        /// </summary>
        public const string TuttiComandi = "Parking/+/StatoMezzi/+";

        /// <summary>
        /// Sottoscrizione a tutte le risposte ai comandi
        /// </summary>
        public const string TutteRisposte = "Parking/+/RisposteComandi/+";

        /// <summary>
        /// Sottoscrizione a tutti gli eventi di un parcheggio specifico
        /// Formato: Parking/{id_parking}/#
        /// </summary>
        public const string TuttoUnParcheggio = "Parking/{0}/#";

        // === METODI HELPER ===

        /// <summary>
        /// Genera il topic per lo status dei mezzi di un parcheggio
        /// </summary>
        public static string GetMezziStatusTopic(int idParcheggio)
        {
            return string.Format(MezziStatus, idParcheggio);
        }

        /// <summary>
        /// Genera il topic per inviare un comando a un mezzo
        /// </summary>
        public static string GetComandoMezzoTopic(int idParcheggio, string idMezzo)
        {
            return string.Format(ComandoMezzo, idParcheggio, idMezzo);
        }

        /// <summary>
        /// Genera il topic per ricevere risposte ai comandi
        /// </summary>
        public static string GetRispostaComandoTopic(int idParcheggio, string idMezzo)
        {
            return string.Format(RispostaComando, idParcheggio, idMezzo);
        }

        /// <summary>
        /// Genera il topic wildcard per tutti gli eventi di un parcheggio
        /// </summary>
        public static string GetTuttoUnParcheggioTopic(int idParcheggio)
        {
            return string.Format(TuttoUnParcheggio, idParcheggio);
        }
    }
}
