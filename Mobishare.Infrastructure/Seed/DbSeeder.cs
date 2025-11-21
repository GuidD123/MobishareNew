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
                    Password = passwordService.Hash("MarioRTest1"),
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
                },
                new Mezzo
                {
                    Matricola = "BE002",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "ME003",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM004",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
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
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "ME006",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM007",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                },

                new Mezzo
                {
                    Matricola = "BE008",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = 100
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
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "ME010",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM011",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                },
                new Mezzo
                {
                    Matricola = "ME012",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 3,
                    LivelloBatteria = 100
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
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "ME014",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = 100
                },
                new Mezzo
                {
                    Matricola = "BM015",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                },
                new Mezzo
                {
                    Matricola = "BE016",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 4,
                    LivelloBatteria = 100
                },

                new Mezzo
                {
                    Matricola = "BE017",
                    Tipo = TipoMezzo.BiciElettrica,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 1,
                    LivelloBatteria = 100
                },

                new Mezzo
                {
                    Matricola = "ME018",
                    Tipo = TipoMezzo.MonopattinoElettrico,
                    Stato = StatoMezzo.Disponibile,
                    IdParcheggioCorrente = 2,
                    LivelloBatteria = 100
                }
            ]);

            context.Mezzi.AddRange(mezzi);
        }
    }
}
