using Microsoft.AspNetCore.Http;

namespace Mobishare.WebApp.Helpers
{
    /// <summary>
    /// Helper per gestire la sessione utente in modo centralizzato
    /// </summary>
    public static class SessionHelper
    {
        // Chiavi per la sessione
        private const string TOKEN_KEY = "Token";
        private const string USER_ID_KEY = "UserId";
        private const string USER_NAME_KEY = "UserName";
        private const string USER_EMAIL_KEY = "UserEmail";
        private const string USER_ROLE_KEY = "UserRole";
        private const string CREDITO_KEY = "Credito";
        private const string SOSPESO_KEY = "Sospeso";

        /// <summary>
        /// Verifica se l'utente è autenticato
        /// </summary>
        public static bool IsAuthenticated(this ISession session)
        {
            return !string.IsNullOrEmpty(session.GetString(TOKEN_KEY));
        }

        /// <summary>
        /// Salva il token JWT nella sessione
        /// </summary>
        public static void SetToken(this ISession session, string token)
        {
            session.SetString(TOKEN_KEY, token);
        }

        /// <summary>
        /// Ottiene il token JWT dalla sessione
        /// </summary>
        public static string? GetToken(this ISession session)
        {
            return session.GetString(TOKEN_KEY);
        }

        /// <summary>
        /// Salva l'ID utente nella sessione
        /// </summary>
        public static void SetUserId(this ISession session, int userId)
        {
            session.SetInt32(USER_ID_KEY, userId);
        }

        /// <summary>
        /// Ottiene l'ID utente dalla sessione
        /// </summary>
        public static int? GetUserId(this ISession session)
        {
            return session.GetInt32(USER_ID_KEY);
        }

        /// <summary>
        /// Salva il nome utente nella sessione
        /// </summary>
        public static void SetUserName(this ISession session, string userName)
        {
            session.SetString(USER_NAME_KEY, userName);
        }

        /// <summary>
        /// Ottiene il nome utente dalla sessione
        /// </summary>
        public static string? GetUserName(this ISession session)
        {
            return session.GetString(USER_NAME_KEY);
        }

        /// <summary>
        /// Salva l'email utente nella sessione
        /// </summary>
        public static void SetUserEmail(this ISession session, string email)
        {
            session.SetString(USER_EMAIL_KEY, email);
        }

        /// <summary>
        /// Ottiene l'email utente dalla sessione
        /// </summary>
        public static string? GetUserEmail(this ISession session)
        {
            return session.GetString(USER_EMAIL_KEY);
        }

        /// <summary>
        /// Salva il ruolo utente nella sessione
        /// </summary>
        public static void SetUserRole(this ISession session, string role)
        {
            session.SetString(USER_ROLE_KEY, role);
        }

        /// <summary>
        /// Ottiene il ruolo utente dalla sessione
        /// </summary>
        public static string? GetUserRole(this ISession session)
        {
            return session.GetString(USER_ROLE_KEY);
        }

        /// <summary>
        /// Verifica se l'utente è un Gestore
        /// </summary>
        public static bool IsGestore(this ISession session)
        {
            return session.GetUserRole()?.Equals("Gestore", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Verifica se l'utente è un Utente normale
        /// </summary>
        public static bool IsUtente(this ISession session)
        {
            return session.GetUserRole()?.Equals("Utente", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Salva il credito nella sessione
        /// </summary>
        public static void SetCredito(this ISession session, decimal credito)
        {
            session.SetString(CREDITO_KEY, credito.ToString("F2"));
        }

        /// <summary>
        /// Ottiene il credito dalla sessione
        /// </summary>
        public static decimal? GetCredito(this ISession session)
        {
            var creditoStr = session.GetString(CREDITO_KEY);
            if (decimal.TryParse(creditoStr, out var credito))
            {
                return credito;
            }
            return null;
        }

        /// <summary>
        /// Salva lo stato sospeso nella sessione
        /// </summary>
        public static void SetSospeso(this ISession session, bool sospeso)
        {
            session.SetString(SOSPESO_KEY, sospeso.ToString());
        }

        /// <summary>
        /// Verifica se l'utente è sospeso
        /// </summary>
        public static bool IsSospeso(this ISession session)
        {
            var sospesoStr = session.GetString(SOSPESO_KEY);
            return bool.TryParse(sospesoStr, out var sospeso) && sospeso;
        }

        /// <summary>
        /// Salva tutti i dati dell'utente nella sessione
        /// </summary>
        public static void SetUserData(this ISession session,
            string token,
            int userId,
            string userName,
            string email,
            string role,
            decimal? credito = null,
            bool sospeso = false)
        {
            session.SetToken(token);
            session.SetUserId(userId);
            session.SetUserName(userName);
            session.SetUserEmail(email);
            session.SetUserRole(role);

            if (credito.HasValue)
                session.SetCredito(credito.Value);

            session.SetSospeso(sospeso);
        }

        /// <summary>
        /// Pulisce tutti i dati dalla sessione (logout)
        /// </summary>
        public static void ClearUserData(this ISession session)
        {
            session.Clear();
        }

        /// <summary>
        /// Ottiene un oggetto con tutti i dati utente
        /// </summary>
        public static UserSessionData? GetUserData(this ISession session)
        {
            if (!session.IsAuthenticated())
                return null;

            return new UserSessionData
            {
                Token = session.GetToken(),
                UserId = session.GetUserId(),
                UserName = session.GetUserName(),
                Email = session.GetUserEmail(),
                Role = session.GetUserRole(),
                Credito = session.GetCredito(),
                IsSospeso = session.IsSospeso()
            };
        }
    }

    /// <summary>
    /// Classe per contenere i dati dell'utente dalla sessione
    /// </summary>
    public class UserSessionData
    {
        public string? Token { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public decimal? Credito { get; set; }
        public bool IsSospeso { get; set; }
    }
}