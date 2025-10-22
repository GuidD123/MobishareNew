using Mobishare.Core.Enums;
using Mobishare.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mobishare.Infrastructure.Services;
using Mobishare.Core.Data;

namespace Mobishare.Infrastructure.Seed
{
    public class DbSeeder
    {
        public static void SeedDatabase(MobishareDbContext context, PasswordService passwordService)
        {
            //Se ho già dei dati non fa nulla 
            if (context.Utenti.Any()) return;

            Console.WriteLine("Seeding Database....");

            //utenti
            if (!context.Utenti.Any())
            {
                SeedUtenti(context, passwordService);
                context.SaveChanges();
            }

            //parcheggi
            if (!context.Parcheggi.Any())
            {
                SeedParcheggi(context);
                context.SaveChanges();
            }

            //mezzi
            if (!context.Mezzi.Any())
            {
                SeedMezzi(context);
                context.SaveChanges();
            }

            //corse
            if (!context.Corse.Any())
            {
                SeedCorse(context);
                context.SaveChanges();
            }

            //ricariche
            if (!context.Ricariche.Any())
            {
                SeedRicariche(context);
                context.SaveChanges();
            }

            //feedbacks
            if (!context.Feedbacks.Any())
            {
                SeedFeedback(context);
                context.SaveChanges();
            }

            Console.WriteLine("Database seeding completato");
        }

        //SeedUtenti
        private static void SeedUtenti(MobishareDbContext context, PasswordService passwordService)
        {
            var utenti = new List<Utente>
            {
                //Gestore Admin 
                new() {
                    Nome = "Admin", //1
                    Cognome = "Mobishare",
                    Email = "admin@mobishare.com",
                    Password = passwordService.Hash("AdminM123!"),
                    Ruolo = UserRole.Gestore,
                    Credito = 0
                },

                new() {
                    Nome = "Mario", //2
                    Cognome = "Rossi",
                    Email = "mariorossi@email.com",
                    Password = passwordService.Hash("MarioR123!"),
                    Ruolo = UserRole.Utente,
                    Credito = 30.00m
                },

                new() {
                    Nome = "Laura", //3
                    Cognome = "Bianchi",
                    Email = "laurabianchi@email.com",
                    Password = passwordService.Hash("LauraB123!"),
                    Ruolo = UserRole.Utente,
                    Credito = 25.00m
                },

                new() {
                    Nome = "Luca", //4
                    Cognome = "Verdi",
                    Email = "lucaverdi@email.com",
                    Password = passwordService.Hash("LucaV123!"),
                    Ruolo = UserRole.Utente,
                    Credito = 40.00m
                },

                new() {
                    Nome = "Matteo", //5
                    Cognome = "Neri",
                    Email = "matteoneri@email.com",
                    Password = passwordService.Hash("MetteoN123!"),
                    Ruolo = UserRole.Utente,
                    Credito = 15.00m
                },

                // Utente sospeso
                new() {
                    Nome = "Giovanni",
                    Cognome = "Sospeso",
                    Email = "sospeso@email.com",
                    Password = passwordService.Hash("Sospeso123!"),
                    Ruolo = UserRole.Utente,
                    Credito = 0, // account sospeso
                    Sospeso = true
                }
            }; 

            context.Utenti.AddRange(utenti);
        }




        //SeedParcheggi
        private static void SeedParcheggi(MobishareDbContext context)
        {
            var parcheggi = new List<Parcheggio>
            {
                new() {
                    Nome = "Parcheggio1",
                    Zona = "Centro",
                    Indirizzo = "Via Roma 1, Cleanair",
                    Capienza = 20,
                    Attivo = true
                },

                new() {
                    Nome = "Parcheggio2",
                    Zona = "Stazione Centrale",
                    Indirizzo = "Piazza Garibaldi 5, Cleanair",
                    Capienza = 20,
                    Attivo = true
                },

                new() {
                    Nome = "Parcheggio3",
                    Zona = "Università",
                    Indirizzo= "Via Perrone 18, Cleanair",
                    Capienza = 20,
                    Attivo = true
                },

                new() {
                    Nome = "Parcheggio4",
                    Zona = "Ospedale",
                    Indirizzo = "Corso Mazzini 18, Cleanair",
                    Capienza = 20,
                    Attivo = true
                },

                new() { 
                    Nome = "Parcheggio5", 
                    Zona = "Periferia", 
                    Indirizzo = "Via Torino 100, Cleanair", 
                    Capienza = 15, 
                    Attivo = true }
            };

            context.Parcheggi.AddRange(parcheggi);
        }



        //SeedMezzi 
        private static void SeedMezzi(MobishareDbContext context)
        {
            var random = new Random();
            var mezzi = new List<Mezzo>();

            // Parcheggio Centro (ID: 1)
            mezzi.AddRange(
            [
                new Mezzo
                {
                    Matricola = "BM001",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 100 // Bici muscolare sempre 100%
                },
                new Mezzo
                {
                    Matricola = "BE002",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = random.Next(60, 100)
                },
                new Mezzo
                {
                    Matricola = "ME003",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = random.Next(40, 90)
                },
                new Mezzo
                {
                    Matricola = "BM004",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.InUso,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 100
                }
            ]);

            // Parcheggio Stazione Centrale (ID: 2)
            mezzi.AddRange(
            [
                new Mezzo
                {
                    Matricola = "BE005",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = random.Next(50, 100)
                },
                new Mezzo
                {
                    Matricola = "ME006",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = random.Next(30, 90)
                },
                new Mezzo
                {
                    Matricola = "BM007",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Manutenzione,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = 100
                },

                new Mezzo
                {
                    Matricola = "BM008",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = random.Next(30, 80)
                }
            ]);

            // Parcheggio Università (ID: 3)
            mezzi.AddRange(
            [
                new Mezzo
                {
                    Matricola = "BE009",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = random.Next(70, 100)
                },
                new Mezzo
                {
                    Matricola = "ME010",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.NonPrelevabile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = 15 // Batteria scarica
                },
                new Mezzo
                {
                    Matricola = "BM011",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM012",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = random.Next(70, 90)
                }
            ]);

            // Parcheggio Ospedale (ID: 4)
            mezzi.AddRange(
            [
                new Mezzo
                {
                    Matricola = "BE013",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = random.Next(40, 60)
                },
                new Mezzo
                {
                    Matricola = "ME014",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.NonPrelevabile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = 10
                },
                new Mezzo
                {
                    Matricola = "BM015",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM016",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = random.Next(70, 90)
                },

                new Mezzo
                {
                    Matricola = "BE017",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 3 // quasi scarico
                },

                new Mezzo
                {
                    Matricola = "ME018",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = 2 // quasi scarico
                }
            ]);

            context.Mezzi.AddRange(mezzi);
        }




        //SeedCorse
        private static void SeedCorse(MobishareDbContext context)
        {
            var corse = new List<Corsa>
            {
                // Corsa completata
                new() {
                    IdUtente = 2,
                    MatricolaMezzo = "BM001",
                    IdParcheggioPrelievo = 1,
                    IdParcheggioRilascio = 2,
                    DataOraInizio = DateTime.Now.AddHours(-2),
                    DataOraFine = DateTime.Now.AddHours(-1),
                    Stato = StatoCorsa.Completata,
                    CostoFinale = 3.50m,
                    SegnalazioneProblema = false
                },
                
                // Corsa in corso
                new() {
                    IdUtente = 3,
                    MatricolaMezzo = "BM004",  // BM004 (InUso) 
                    IdParcheggioPrelievo = 1,
                    DataOraInizio = DateTime.Now.AddMinutes(-30),
                    Stato = StatoCorsa.InCorso,
                    //CostoFinale = 0 // Sarà calcolato alla fine
                    SegnalazioneProblema = false
                },
                
                // Corsa completata ieri
                new() {
                    IdUtente = 4,
                    MatricolaMezzo ="BE002",  // BE002
                    IdParcheggioPrelievo = 1,
                    IdParcheggioRilascio = 3,
                    DataOraInizio = DateTime.Now.AddDays(-1).AddHours(-3),
                    DataOraFine = DateTime.Now.AddDays(-1).AddHours(-2),
                    Stato = StatoCorsa.Completata,
                    CostoFinale  = 5.20m,
                    SegnalazioneProblema = false
                },

                // Corsa in corso
                new() {
                    IdUtente = 5,
                    MatricolaMezzo = "ME010",  // ME010 (InUso) 
                    IdParcheggioPrelievo = 1,
                    DataOraInizio = DateTime.Now.AddMinutes(-30),
                    Stato = StatoCorsa.InCorso,
                    //CostoFinale = 0 // Sarà calcolato alla fine
                    SegnalazioneProblema = false
                },

                new() {
                    IdUtente = 2,
                    MatricolaMezzo = "BE005",
                    IdParcheggioPrelievo = 2,
                    IdParcheggioRilascio = 3,
                    DataOraInizio = DateTime.Now.AddHours(-5),
                    DataOraFine = DateTime.Now.AddHours(-4),
                    Stato = StatoCorsa.Completata,
                    CostoFinale = 4.00m,
                    SegnalazioneProblema = true // guasto segnalato
                }
            };

            context.Corse.AddRange(corse);
        }



        //SeedPagamenti
        private static void SeedRicariche(MobishareDbContext context)
        {
            //credito utente = somma ricariche - costo corse 

            var ricariche = new List<Ricarica>
            {
                new() {
                    IdUtente = 3,
                    ImportoRicarica = 40.00m,
                    DataRicarica = DateTime.Now.AddHours(-4),
                    Tipo = TipoRicarica.CartaDiCredito,
                    Stato = StatoPagamento.Completato,
                },

                new() {
                    IdUtente = 5,
                    ImportoRicarica = 20.00m,
                    DataRicarica = DateTime.Now.AddDays(-1).AddHours(-2),
                    Tipo = TipoRicarica.CartaDiCredito,
                    Stato = StatoPagamento.Completato,
                },
                
                // Ricarica credito
                new() {
                    IdUtente = 4,
                    ImportoRicarica = 30.00m,
                    DataRicarica = DateTime.Now.AddDays(-3),
                    Tipo = TipoRicarica.PayPal,
                    Stato = StatoPagamento.InSospeso,
                }, 

                //SeedRicariche (aggiunta fallita)
                new() {
                    IdUtente = 2,
                    ImportoRicarica = 15.00m,
                    DataRicarica = DateTime.Now.AddDays(-2),
                    Tipo = TipoRicarica.CartaDiCredito,
                    Stato = StatoPagamento.Fallito
                }
            };

            context.Ricariche.AddRange(ricariche);
        }

        //SeedFeedback
        private static void SeedFeedback(MobishareDbContext context)
        {
            var feedback = new List<Feedback>
            {
                new() {
                    IdUtente = 3, // Mario Rossi
                    IdCorsa = 1,
                    Valutazione = ValutazioneFeedback.Buono,
                    Commento = "Bici in buone condizioni, percorso piacevole",
                    DataFeedback = DateTime.Now.AddHours(-1)
                },

                new() {
                    IdUtente = 5, // Luca Verdi
                    IdCorsa = 3,
                    Valutazione = ValutazioneFeedback.Ottimo,
                    Commento = "Servizio eccellente, bici elettrica molto comoda",
                    DataFeedback = DateTime.Now.AddDays(-1).AddHours(-2)
                },

                //SeedFeedback (aggiunta negativa)
                new() {
                    IdUtente = 4,
                    IdCorsa = 2,
                    Valutazione = ValutazioneFeedback.Scarso,
                    Commento = "Monopattino con freni difettosi",
                    DataFeedback = DateTime.Now.AddMinutes(-50)
                }
            };

            context.Feedbacks.AddRange(feedback);
        }
    }
}
