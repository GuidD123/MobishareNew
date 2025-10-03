# Scenari di Test Manuali - Mobishare

## Setup Iniziale
Prerequisiti

Avvia Mobishare.API. Log ok, MQTT connesso.
MQTTX connesso a localhost:1883.
Seeder già eseguito (mezzi e utenti presenti).
Se endpoint protetti: fai login e imposta Bearer in Postman. Usa un utente seedato.

### Prerequisiti
1. Database SQLite creato e popolato con seeder
2. Mosquitto broker avviato (porta 1883)
3. Backend API in esecuzione (porta 5000)
4. Gateway IoT in esecuzione
5. MQTTX aperto per monitorare messaggi MQTT

### Credenziali Test (dal seeder)
- **Gestore**: admin@mobishare.com / AdminM123!
- **Utente 1**: mariorossi@email.com / MarioR123!
- **Utente 2**: laurabianchi@email.com / LauraB123!
- **Utente 3**: lucaverdi@email.com / LucaV123!

////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////


## CATEGORIA 1: AUTENTICAZIONE

### TEST-AUTH-01: Login Gestore con credenziali valide
**Obiettivo**: Verificare login amministratore

**Passi**:
1. Aprire Swagger: `http://localhost:5000/swagger`
2. Endpoint: `POST /api/utenti/login`
3. Body:
```json
{
  "email": "admin@mobishare.com",
  "password": "AdminM123!"
}

eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJHZXN0b3JlIiwiZXhwIjoxNzYwMDI5OTU5LCJpc3MiOiJtb2Jpc2hhcmUiLCJhdWQiOiJtb2Jpc2hhcmUtY2xpZW50In0.kVmvrXSmTxp_5w1NyE0mL3-uvUGTNkpbCryrZk_GbJ0

Cliccare "Execute"

Risultato Atteso:

Status Code: 200 OK
Response contiene:

token: stringa JWT valida
ruolo: "Gestore"
nome: "Admin"
email: "admin@mobishare.com"

Response:
{
  "messaggio": "Login effettuato",
  "dati": {
    "id": 1,
    "nome": "Admin",
    "ruolo": "Gestore",
    "credito": 0,
    "sospeso": false,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJHZXN0b3JlIiwiZXhwIjoxNzU5NDEzNjA2LCJpc3MiOiJtb2Jpc2hhcmUiLCJhdWQiOiJtb2Jpc2hhcmUtY2xpZW50In0.-1GwjEGEUZXQsV276vvZppzvNnOuFMFIeYJC_jHATQs"
  }
}

SERVER RESPONSE: 	TypeError: NetworkError when attempting to fetch resource. -> RISOLTO 

Risultato Ottenuto: [ ok ] PASS [ ] FAIL


//////////////////////////////////////////////////////

TEST-AUTH-02: Login con password errata
Obiettivo: Verificare rifiuto credenziali non valide
Passi:

POST /api/utenti/login
Body:

{
  "email": "admin@mobishare.com",
  "password": "PasswordSbagliata!"
}

Risultato Atteso:

Status Code: 401 Unauthorized
Messaggio errore: "Credenziali non valide"

Risultato Ottenuto: [ ok ] PASS [ ] FAIL
 
Response body
Download

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Password": [
      "La password deve avere almeno 8 caratteri, una maiuscola e un numero"
    ]
  },
  "traceId": "00-6ff12c26a2b981eb66483a8253fea45c-46a6617d4aa4701c-00"
}


///////////////////////////////////////////////////////

TEST-AUTH-03: Accesso endpoint protetto senza token
Obiettivo: Verificare protezione endpoint
Passi:

GET /api/utenti (senza Authorization header)

Risultato Atteso:

Status Code: 401 Unauthorized

Risultato Ottenuto: [ ok ] PASS [ ] FAIL
	
Error: response status is 401
Response headers

 content-length: 0  
 date: Thu,02 Oct 2025 13:09:36 GMT 
 referrer-policy: strict-origin-when-cross-origin  
 server: Kestrel  
 www-authenticate: Bearer  
 x-content-type-options: nosniff  
 x-firefox-spdy: h2  
 x-frame-options: DENY  
 x-xss-protection: 1; mode=block 


///////////////////////////////////////////////////////


TEST-AUTH-04: Accesso endpoint protetto con token valido
Obiettivo: Verificare autorizzazione con JWT
Passi:

Ottenere token da TEST-AUTH-01
In Swagger, cliccare lucchetto "Authorize"
Inserire: Bearer {token}
GET /api/utenti

Risultato Atteso:

Status Code: 200 OK
Lista utenti dal database

Risultato Ottenuto: [ ok ] PASS [  ] FAIL

Note: Mi da 401 Non Autorizzato nonostante gli abbia dato il token ottenuto in Test01
Risolto -> dovevo scrivere Bearer -token- e ora funziona 
Response Body:
[
  {
    "id": 1,
    "nome": "Admin",
    "cognome": "Mobishare",
    "email": "admin@mobishare.com",
    "password": "AQAAAAIAAYagAAAAEOBIaxQSe2IHqmcNPH6WTAQZvX873ZIhIaoIERVuJKa82Q0WynSOMggYaubZCzTmLw==",
    "ruolo": 1,
    "credito": 0,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  },
  {
    "id": 2,
    "nome": "Mario",
    "cognome": "Rossi",
    "email": "mariorossi@email.com",
    "password": "AQAAAAIAAYagAAAAEOpFDIEPofuCv/PmeCRofcDWYjxQxkQUHKQf/rDxOZb1SV5y/hanQy1ZMfyEMytxJg==",
    "ruolo": 0,
    "credito": 30,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  },
  {
    "id": 3,
    "nome": "Laura",
    "cognome": "Bianchi",
    "email": "laurabianchi@email.com",
    "password": "AQAAAAIAAYagAAAAEFS7EjdexPXyulauZPA42cQNZMaZfPIAorEZC1C5dFI67TFXJu7PcgUtylUmw0VCLg==",
    "ruolo": 0,
    "credito": 25,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  },
  {
    "id": 4,
    "nome": "Luca",
    "cognome": "Verdi",
    "email": "lucaverdi@email.com",
    "password": "AQAAAAIAAYagAAAAEJzUjgib5PRdlTpWML6ILtBSYqoDOpYYU1Qs51p/y5GMZ0JKmh3TXZ9M8+rZ3CGT2w==",
    "ruolo": 0,
    "credito": 40,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  },
  {
    "id": 5,
    "nome": "Matteo",
    "cognome": "Neri",
    "email": "matteoneri@email.com",
    "password": "AQAAAAIAAYagAAAAEFn04honkCbBx2i9pP2O55Ub5T+irf8yNTLAH4YHKrsvJhJWovOfmwm5752htivO3g==",
    "ruolo": 0,
    "credito": 15,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  },
  {
    "id": 6,
    "nome": "Giovanni",
    "cognome": "Sospeso",
    "email": "sospeso@email.com",
    "password": "AQAAAAIAAYagAAAAEAgdXv55W6aCEshrhUS9uYTeRkU84bWu67sQBvMlWe1xUT/2glRcSKDZkUV9vSeFmA==",
    "ruolo": 0,
    "credito": 0,
    "debitoResiduo": 0,
    "sospeso": false,
    "corse": [],
    "ricaricheUtente": []
  }
]



///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////

CATEGORIA 2: GESTIONE MEZZI


TEST-MEZZI-01: Lista tutti i mezzi
Obiettivo: Recuperare lista mezzi dal database
Passi:

Login come gestore (ottenere token)
GET /api/mezzi

Risultato Atteso:

Status Code: 200 OK
Array con almeno 18 mezzi (dal seeder)
Ogni mezzo contiene: id, matricola, tipo, stato, livelloBatteria, idParcheggioCorrente

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

RESPONSE: 200 ma non ritorna lista mezzi -> risolto mi ritorna lista mezzi 

Response body:
{
  "messaggio": "Lista completa mezzi",
  "dati": [
    {
      "id": 1,
      "matricola": "BM001",
      "tipo": "BiciMuscolare",
      "stato": "Disponibile",
      "livelloBatteria": 100,
      "idParcheggioCorrente": 1,
      "nomeParcheggio": "Parcheggio1"
    },
    {
      "id": 2,
      "matricola": "BE002",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 65,
      "idParcheggioCorrente": 1,
      "nomeParcheggio": "Parcheggio1"
    },
    {
      "id": 3,
      "matricola": "ME003",
      "tipo": "MonopattinoElettrico",
      "stato": "Disponibile",
      "livelloBatteria": 48,
      "idParcheggioCorrente": 1,
      "nomeParcheggio": "Parcheggio1"
    },
    {
      "id": 4,
      "matricola": "BM004",
      "tipo": "BiciMuscolare",
      "stato": "InUso",
      "livelloBatteria": 100,
      "idParcheggioCorrente": 1,
      "nomeParcheggio": "Parcheggio1"
    },
    {
      "id": 5,
      "matricola": "BE005",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 98,
      "idParcheggioCorrente": 2,
      "nomeParcheggio": "Parcheggio2"
    },
    {
      "id": 6,
      "matricola": "ME006",
      "tipo": "MonopattinoElettrico",
      "stato": "Disponibile",
      "livelloBatteria": 50,
      "idParcheggioCorrente": 2,
      "nomeParcheggio": "Parcheggio2"
    },
    {
      "id": 7,
      "matricola": "BM007",
      "tipo": "BiciMuscolare",
      "stato": "Manutenzione",
      "livelloBatteria": 100,
      "idParcheggioCorrente": 2,
      "nomeParcheggio": "Parcheggio2"
    },
    {
      "id": 8,
      "matricola": "BM008",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 33,
      "idParcheggioCorrente": 2,
      "nomeParcheggio": "Parcheggio2"
    },
    {
      "id": 9,
      "matricola": "BE009",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 79,
      "idParcheggioCorrente": 3,
      "nomeParcheggio": "Parcheggio3"
    },
    {
      "id": 10,
      "matricola": "ME010",
      "tipo": "MonopattinoElettrico",
      "stato": "NonPrelevabile",
      "livelloBatteria": 15,
      "idParcheggioCorrente": 3,
      "nomeParcheggio": "Parcheggio3"
    },
    {
      "id": 11,
      "matricola": "BM011",
      "tipo": "BiciMuscolare",
      "stato": "Disponibile",
      "livelloBatteria": 100,
      "idParcheggioCorrente": 3,
      "nomeParcheggio": "Parcheggio3"
    },
    {
      "id": 12,
      "matricola": "BM012",
      "tipo": "MonopattinoElettrico",
      "stato": "Disponibile",
      "livelloBatteria": 87,
      "idParcheggioCorrente": 3,
      "nomeParcheggio": "Parcheggio3"
    },
    {
      "id": 13,
      "matricola": "BE013",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 46,
      "idParcheggioCorrente": 4,
      "nomeParcheggio": "Parcheggio4"
    },
    {
      "id": 14,
      "matricola": "ME014",
      "tipo": "MonopattinoElettrico",
      "stato": "NonPrelevabile",
      "livelloBatteria": 10,
      "idParcheggioCorrente": 4,
      "nomeParcheggio": "Parcheggio4"
    },
    {
      "id": 15,
      "matricola": "BM015",
      "tipo": "BiciMuscolare",
      "stato": "Disponibile",
      "livelloBatteria": 100,
      "idParcheggioCorrente": 4,
      "nomeParcheggio": "Parcheggio4"
    },
    {
      "id": 16,
      "matricola": "BM016",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 78,
      "idParcheggioCorrente": 4,
      "nomeParcheggio": "Parcheggio4"
    },
    {
      "id": 17,
      "matricola": "BE017",
      "tipo": "BiciElettrica",
      "stato": "Disponibile",
      "livelloBatteria": 3,
      "idParcheggioCorrente": 1,
      "nomeParcheggio": "Parcheggio1"
    },
    {
      "id": 18,
      "matricola": "ME018",
      "tipo": "MonopattinoElettrico",
      "stato": "Disponibile",
      "livelloBatteria": 2,
      "idParcheggioCorrente": 2,
      "nomeParcheggio": "Parcheggio2"
    }
  ]
}

///////////////////////////////////////////////////////


TEST-MEZZI-02: Dettaglio mezzo singolo
Obiettivo: Recuperare info di un mezzo specifico
Passi:

GET /api/mezzi/9 (o qualsiasi ID valido dal seeder)

Risultato Atteso:

Status Code: 200 OK
Dati del mezzo con ID richiesto
Include relazioni (ParcheggioCorrente)

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

Test con id  = 9
Response body:
{
  "messaggio": "Dettaglio mezzo",
  "dati": {
    "id": 9,
    "matricola": "BE009",
    "tipo": "BiciElettrica",
    "stato": "Disponibile",
    "livelloBatteria": 79,
    "idParcheggioCorrente": 3,
    "nomeParcheggio": "Parcheggio3"
  }
}


///////////////////////////////////////////////////////

TEST-MEZZI-03: Filtra mezzi per parcheggio
Obiettivo: Ottenere solo mezzi di un parcheggio
Passi:

GET /api/mezzi/parcheggio/2

Risultato Atteso:

Status Code: 200 OK
Solo mezzi con idParcheggioCorrente = 1
Almeno 4 mezzi (BM001, BE002, ME003, BM004 dal seeder)

Risultato Ottenuto: [ok ] PASS [ ] FAIL

Response Body:
{
  "messaggio": "Mezzi nel parcheggio Parcheggio2",
  "dati": {
    "parcheggio": "Parcheggio2",
    "totaleMezzi": 5,
    "mezzi": [
      {
        "id": 5,
        "matricola": "BE005",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 98,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 6,
        "matricola": "ME006",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 50,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 7,
        "matricola": "BM007",
        "tipo": "BiciMuscolare",
        "stato": "Manutenzione",
        "livelloBatteria": 100,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 8,
        "matricola": "BM008",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 33,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 18,
        "matricola": "ME018",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 2,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      }
    ],
    "riepilogo": [
      {
        "tipo": "BiciElettrica",
        "quantità": 2,
        "disponibili": 2,
        "inUso": 0,
        "nonPrelevabili": 0
      },
      {
        "tipo": "BiciMuscolare",
        "quantità": 1,
        "disponibili": 0,
        "inUso": 0,
        "nonPrelevabili": 0
      },
      {
        "tipo": "MonopattinoElettrico",
        "quantità": 2,
        "disponibili": 2,
        "inUso": 0,
        "nonPrelevabili": 0
      }
    ]
  }
}


///////////////////////////////////////////////////////

TEST-MEZZI-04: Filtra mezzi disponibili
Obiettivo: Ottenere solo mezzi prelevabili
Passi:

GET /api/mezzi/disponibili

Risultato Atteso:

Status Code: 200 OK
Solo mezzi con stato = "Disponibile"
Nessun mezzo in "InUso", "Manutenzione", "NonPrelevabile"

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

Response Body: 
{
  "messaggio": "Lista mezzi disponibili",
  "dati": {
    "totale": 14,
    "mezzi": [
      {
        "id": 2,
        "matricola": "BE002",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 65,
        "idParcheggioCorrente": 1,
        "nomeParcheggio": "Parcheggio1"
      },
      {
        "id": 5,
        "matricola": "BE005",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 98,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 8,
        "matricola": "BM008",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 33,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 9,
        "matricola": "BE009",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 79,
        "idParcheggioCorrente": 3,
        "nomeParcheggio": "Parcheggio3"
      },
      {
        "id": 13,
        "matricola": "BE013",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 46,
        "idParcheggioCorrente": 4,
        "nomeParcheggio": "Parcheggio4"
      },
      {
        "id": 16,
        "matricola": "BM016",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 78,
        "idParcheggioCorrente": 4,
        "nomeParcheggio": "Parcheggio4"
      },
      {
        "id": 17,
        "matricola": "BE017",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 3,
        "idParcheggioCorrente": 1,
        "nomeParcheggio": "Parcheggio1"
      },
      {
        "id": 1,
        "matricola": "BM001",
        "tipo": "BiciMuscolare",
        "stato": "Disponibile",
        "livelloBatteria": 100,
        "idParcheggioCorrente": 1,
        "nomeParcheggio": "Parcheggio1"
      },
      {
        "id": 11,
        "matricola": "BM011",
        "tipo": "BiciMuscolare",
        "stato": "Disponibile",
        "livelloBatteria": 100,
        "idParcheggioCorrente": 3,
        "nomeParcheggio": "Parcheggio3"
      },
      {
        "id": 15,
        "matricola": "BM015",
        "tipo": "BiciMuscolare",
        "stato": "Disponibile",
        "livelloBatteria": 100,
        "idParcheggioCorrente": 4,
        "nomeParcheggio": "Parcheggio4"
      },
      {
        "id": 3,
        "matricola": "ME003",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 48,
        "idParcheggioCorrente": 1,
        "nomeParcheggio": "Parcheggio1"
      },
      {
        "id": 6,
        "matricola": "ME006",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 50,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 12,
        "matricola": "BM012",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 87,
        "idParcheggioCorrente": 3,
        "nomeParcheggio": "Parcheggio3"
      },
      {
        "id": 18,
        "matricola": "ME018",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 2,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      }
    ]
  }
}

///////////////////////////////////////////////////////

TEST-MEZZI-05: Crea nuovo mezzo
Obiettivo: Aggiungere mezzo al database
Passi:

Login come gestore
POST /api/mezzi
Body:

{
  "matricola": "TEST001",
  "tipo": "BiciElettrica",
  "stato": "Disponibile",
  "livelloBatteria": 100,
  "idParcheggioCorrente": 1
}

Risultato Atteso:

Status Code: 201 Created
Response contiene il mezzo creato con ID generato
Stato iniziale: "Disponibile"
LivelloBatteria: valore casuale 20-100

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

	
Response body

{
  "messaggio": "Mezzo creato correttamente",
  "dati": {
    "id": 19,
    "matricola": "TEST001",
    "tipo": "BiciElettrica",
    "stato": "Disponibile",
    "livelloBatteria": 100,
    "idParcheggioCorrente": 1,
    "nomeParcheggio": "Parcheggio1"
  }
}

///////////////////////////////////////////////////////

TEST-MEZZI-06: Aggiorna stato mezzo
Obiettivo: Modificare stato di un mezzo
Passi:

Login come gestore
PUT /api/mezzi/1/stato
Body:
{
  "nuovoStato": "Manutenzione"
}
	
Risultato Atteso:

Status Code: 200 OK
Mezzo con stato aggiornato a "Manutenzione"

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

	
Response body -> idMezzo: 5


{
  "messaggio": "Stato del mezzo aggiornato correttamente",
  "dati": {
    "nuovoStato": "Manutenzione",
    "livelloBatteria": 10,
    "parcheggio": "Parcheggio2"
  }
}


///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////


CATEGORIA 3: GESTIONE PARCHEGGI
TEST-PARK-01: Lista tutti i parcheggi
Passi:

GET /api/parcheggi

Risultato Atteso:

Status Code: 200 OK
5 parcheggi dal seeder (Centro, Stazione, Università, Ospedale, Periferia)

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

Response body

{
  "messaggio": "Lista parcheggi",
  "dati": [
    {
      "id": 1,
      "nome": "Parcheggio1",
      "zona": "Centro",
      "indirizzo": "Via Roma 1, Cleanair",
      "capienza": 20,
      "attivo": true,
      "mezzi": [
        {
          "id": 1,
          "matricola": "BM001",
          "tipo": "BiciMuscolare",
          "stato": "Disponibile",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        },
        {
          "id": 2,
          "matricola": "BE002",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 65,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        },
        {
          "id": 3,
          "matricola": "ME003",
          "tipo": "MonopattinoElettrico",
          "stato": "Disponibile",
          "livelloBatteria": 48,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        },
        {
          "id": 4,
          "matricola": "BM004",
          "tipo": "BiciMuscolare",
          "stato": "InUso",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        },
        {
          "id": 17,
          "matricola": "BE017",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 3,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        },
        {
          "id": 19,
          "matricola": "TEST001",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 1,
          "nomeParcheggio": "Parcheggio1"
        }
      ]
    },
    {
      "id": 2,
      "nome": "Parcheggio2",
      "zona": "Stazione Centrale",
      "indirizzo": "Piazza Garibaldi 5, Cleanair",
      "capienza": 20,
      "attivo": true,
      "mezzi": [
        {
          "id": 5,
          "matricola": "BE005",
          "tipo": "BiciElettrica",
          "stato": "Manutenzione",
          "livelloBatteria": 10,
          "idParcheggioCorrente": 2,
          "nomeParcheggio": "Parcheggio2"
        },
        {
          "id": 6,
          "matricola": "ME006",
          "tipo": "MonopattinoElettrico",
          "stato": "Disponibile",
          "livelloBatteria": 50,
          "idParcheggioCorrente": 2,
          "nomeParcheggio": "Parcheggio2"
        },
        {
          "id": 7,
          "matricola": "BM007",
          "tipo": "BiciMuscolare",
          "stato": "Manutenzione",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 2,
          "nomeParcheggio": "Parcheggio2"
        },
        {
          "id": 8,
          "matricola": "BM008",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 33,
          "idParcheggioCorrente": 2,
          "nomeParcheggio": "Parcheggio2"
        },
        {
          "id": 18,
          "matricola": "ME018",
          "tipo": "MonopattinoElettrico",
          "stato": "Disponibile",
          "livelloBatteria": 2,
          "idParcheggioCorrente": 2,
          "nomeParcheggio": "Parcheggio2"
        }
      ]
    },
    {
      "id": 3,
      "nome": "Parcheggio3",
      "zona": "Università",
      "indirizzo": "Via Perrone 18, Cleanair",
      "capienza": 20,
      "attivo": true,
      "mezzi": [
        {
          "id": 9,
          "matricola": "BE009",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 79,
          "idParcheggioCorrente": 3,
          "nomeParcheggio": "Parcheggio3"
        },
        {
          "id": 10,
          "matricola": "ME010",
          "tipo": "MonopattinoElettrico",
          "stato": "NonPrelevabile",
          "livelloBatteria": 15,
          "idParcheggioCorrente": 3,
          "nomeParcheggio": "Parcheggio3"
        },
        {
          "id": 11,
          "matricola": "BM011",
          "tipo": "BiciMuscolare",
          "stato": "Disponibile",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 3,
          "nomeParcheggio": "Parcheggio3"
        },
        {
          "id": 12,
          "matricola": "BM012",
          "tipo": "MonopattinoElettrico",
          "stato": "Disponibile",
          "livelloBatteria": 87,
          "idParcheggioCorrente": 3,
          "nomeParcheggio": "Parcheggio3"
        }
      ]
    },
    {
      "id": 4,
      "nome": "Parcheggio4",
      "zona": "Ospedale",
      "indirizzo": "Corso Mazzini 18, Cleanair",
      "capienza": 20,
      "attivo": true,
      "mezzi": [
        {
          "id": 13,
          "matricola": "BE013",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 46,
          "idParcheggioCorrente": 4,
          "nomeParcheggio": "Parcheggio4"
        },
        {
          "id": 14,
          "matricola": "ME014",
          "tipo": "MonopattinoElettrico",
          "stato": "NonPrelevabile",
          "livelloBatteria": 10,
          "idParcheggioCorrente": 4,
          "nomeParcheggio": "Parcheggio4"
        },
        {
          "id": 15,
          "matricola": "BM015",
          "tipo": "BiciMuscolare",
          "stato": "Disponibile",
          "livelloBatteria": 100,
          "idParcheggioCorrente": 4,
          "nomeParcheggio": "Parcheggio4"
        },
        {
          "id": 16,
          "matricola": "BM016",
          "tipo": "BiciElettrica",
          "stato": "Disponibile",
          "livelloBatteria": 78,
          "idParcheggioCorrente": 4,
          "nomeParcheggio": "Parcheggio4"
        }
      ]
    },
    {
      "id": 5,
      "nome": "Parcheggio5",
      "zona": "Periferia",
      "indirizzo": "Via Torino 100, Cleanair",
      "capienza": 15,
      "attivo": true,
      "mezzi": []
    }
  ]
}

///////////////////////////////////////////////////////

TEST-PARK-02: Dettaglio parcheggio con mezzi
Passi:

GET /api/parcheggi/2

Risultato Atteso:

Status Code: 200 OK
Dati parcheggio
Include lista mezzi presenti (MezziPresenti)

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

Response Body:
{
  "messaggio": "Dettaglio parcheggio",
  "dati": {
    "id": 2,
    "nome": "Parcheggio2",
    "zona": "Stazione Centrale",
    "indirizzo": "Piazza Garibaldi 5, Cleanair",
    "capienza": 20,
    "attivo": true,
    "mezzi": [
      {
        "id": 5,
        "matricola": "BE005",
        "tipo": "BiciElettrica",
        "stato": "Manutenzione",
        "livelloBatteria": 10,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 6,
        "matricola": "ME006",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 50,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 7,
        "matricola": "BM007",
        "tipo": "BiciMuscolare",
        "stato": "Manutenzione",
        "livelloBatteria": 100,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 8,
        "matricola": "BM008",
        "tipo": "BiciElettrica",
        "stato": "Disponibile",
        "livelloBatteria": 33,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      },
      {
        "id": 18,
        "matricola": "ME018",
        "tipo": "MonopattinoElettrico",
        "stato": "Disponibile",
        "livelloBatteria": 2,
        "idParcheggioCorrente": 2,
        "nomeParcheggio": "Parcheggio2"
      }
    ]
  }
}



///////////////////////////////////////////////////////

TEST-PARK-03: Disponibilità parcheggio
Obiettivo: Verificare calcolo posti liberi
Passi:

GET /api/parcheggi/1/disponibilita

Risultato Atteso:

Status Code: 200 OK
Response:

{
  "postiTotali": 20,
  "postiOccupati": 4,
  "postiLiberi": 16
}

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

	
Response body

{
  "messaggio": "Disponibilità parcheggio Parcheggio1",
  "dati": {
    "postiTotali": 20,
    "postiOccupati": 5,
    "postiLiberi": 15
  }
}

///////////////////////////////////////////////////////

TEST-PARK-04: Aggiunta nuovo parcheggio

{
  "nome": "Parcheggio6",
  "zona": "Zona Nord",
  "indirizzo": "Via Milano 10, Cleanair",
  "capienza": 25,
  "attivo": true
}

Ok PASS

REsponse:
{
  "messaggio": "Parcheggio creato correttamente",
  "dati": {
    "id": 6,
    "nome": "Parcheggio6",
    "zona": "Zona Nord",
    "indirizzo": "Via Milano 10, Cleanair",
    "capienza": 25,
    "attivo": true,
    "mezzi": []
  }
}

////////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////

CATEGORIA 4: GESTIONE CORSE
TEST-CORSA-01: Inizia nuova corsa
Obiettivo: Utente preleva un mezzo
Passi:

Login come utente (mariorossi@email.com)
Identificare un mezzo disponibile (es. BM001)
POST /api/corse/inizia
Body:

{
  "matricolaMezzo": "BM001",
  "idParcheggioPrelievo": 1
}

Risultato Atteso:

Status Code: 201 Created
Response contiene corsa con:

id: generato
stato: "InCorso"
dataOraInizio: timestamp corrente
dataOraFine: null


Mezzo BM001 ora ha stato "InUso"

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

Response body
{
  "messaggio": "Corsa avviata correttamente",
  "dati": {
    "id": 6,
    "idUtente": 2,
    "matricolaMezzo": "BM001",
    "idParcheggioPrelievo": 1,
    "idParcheggioRilascio": null,
    "dataOraInizio": "2025-10-02T17:34:58.3435721Z",
    "dataOraFine": null,
    "costoFinale": null
  }
}


///////////////////////////////////////////////////////

TEST-CORSA-02: Termina corsa
Obiettivo: Utente rilascia mezzo
Passi:

Ottenere ID corsa da TEST-CORSA-01 (es. ID=6)
PUT /api/corse/6/termina
Body:

{
  "idParcheggioRilascio": 2
}

Risultato Atteso:

Status Code: 200 OK
Response contiene:

stato: "Completata"
dataOraFine: timestamp corrente
costoFinale: calcolato in base a durata


Mezzo torna stato "Disponibile"
Credito utente decrementato di costoFinale

Risultato Ottenuto: [ ok ] PASS [ ] FAIL

	
Response body

{
  "messaggio": "Corsa terminata correttamente",
  "dati": {
    "id": 6,
    "idUtente": 2,
    "matricolaMezzo": "BM001",
    "idParcheggioPrelievo": 1,
    "idParcheggioRilascio": 2,
    "dataOraInizio": "2025-10-02T17:34:58.3435721",
    "dataOraFine": "2025-10-02T17:36:52.276Z",
    "costoFinale": 1
  }
}

///////////////////////////////////////////////////////

TEST-CORSA-03: Storico corse utente
Passi:

Login come utente
GET /api/corse/utente/{idUtente}

Risultato Atteso:

Status Code: 200 OK
Lista corse dell'utente (in corso + completate)

Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////

TEST-CORSA-04: Tentativo prelievo mezzo non disponibile
Obiettivo: Verificare validazione stato mezzo
Passi:

Identificare un mezzo in stato "InUso" o "Manutenzione"
POST /api/corse/inizia con quel mezzo

Risultato Atteso:

Status Code: 400 Bad Request
Messaggio: "Mezzo non disponibile"

Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////

CATEGORIA 5: GESTIONE CREDITO
TEST-CREDITO-01: Visualizza credito utente
Passi:

Login come utente
GET /api/utenti/{idUtente}

Risultato Atteso:

Status Code: 200 OK
Campo credito con valore dal seeder (es. 30.00 per Mario Rossi)

Risultato Ottenuto: [ ] PASS [ ] FAIL



///////////////////////////////////////////////////////

TEST-CREDITO-02: Ricarica credito
Passi:

Login come utente
POST /api/ricariche
Body:

{
  "idUtente": 2,
  "importoRicarica": 20.00,
  "tipo": "CartaDiCredito"
}

Risultato Atteso:

Status Code: 201 Created
Ricarica salvata con stato "Completato"
Credito utente aumentato di 20.00

Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////

TEST-CREDITO-03: Tentativo corsa con credito insufficiente
Obiettivo: Verificare blocco se credito < tariffa minima
Prerequisiti: Utente con credito < 1.00
Passi:

Login come utente con credito basso
POST /api/corse/inizia

Risultato Atteso:

Status Code: 400 Bad Request
Messaggio: "Credito insufficiente"

Risultato Ottenuto: [ ] PASS [ ] FAIL



///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////

CATEGORIA 6: COMUNICAZIONE MQTT
TEST-MQTT-01: Verifica connessione Gateway
Obiettivo: Gateway si connette al broker
Prerequisiti: Mosquitto avviato, Gateway avviato
Passi:

Controllare log del Gateway

Risultato Atteso:

Log contiene: "Gateway emulatore connesso al broker MQTT"
MQTTX mostra client "MobishareGatewayEmulator" connesso

Risultato Ottenuto: [ ] PASS [ ] FAIL

///////////////////////////////////////////////////////

TEST-MQTT-02: Gateway riceve comando sblocco
Obiettivo: Backend invia comando, Gateway lo riceve
Prerequisiti: MQTTX sottoscritto a Parking/#
Passi:

Backend invia comando sblocco mezzo (via API corse/inizia)
Monitorare MQTTX

Risultato Atteso:

Topic: Parking/1/StatoMezzi/BM001
Payload contiene:

{
  "comando": "Sblocca",
  "timestamp": "..."
}

Log Gateway: "Processando comando Sblocca per mezzo BM001"

Risultato Ottenuto: [ ] PASS [ ] FAIL

///////////////////////////////////////////////////////

TEST-MQTT-03: Gateway pubblica status mezzo
Obiettivo: Gateway invia status al Backend
Passi:

Gateway avvia e aggiunge mezzo emulato
Monitorare MQTTX su topic Parking/1/Mezzi

Risultato Atteso:

Messaggio pubblicato su Parking/1/Mezzi
Payload:

{
  "idMezzo": "...",
  "matricola": "...",
  "livelloBatteria": 85,
  "stato": "Disponibile",
  "tipo": "BiciElettrica",
  "timestamp": "...",
  "messaggio": "Status da Gateway - Spia: Verde"
}
Risultato Ottenuto: [ ] PASS [ ] FAIL

///////////////////////////////////////////////////////

TEST-MQTT-04: Gateway risponde a comando
Obiettivo: Backend riceve conferma dal Gateway
Passi:

Backend invia comando (es. sblocco mezzo)
Gateway processa comando
Monitorare topic Parking/1/RisposteComandi/BM001

Risultato Atteso:

Risposta su topic corretto
Payload:

{
  "idMezzo": "BM001",
  "comandoOriginale": "sblocca",
  "successo": true,
  "messaggio": "Mezzo sbloccato",
  "timestamp": "..."
}
Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////

CATEGORIA 7: VALIDAZIONI BUSINESS
TEST-VAL-01: Email duplicata
Passi:

POST /api/utenti/registrazione
Body con email già esistente

Risultato Atteso:

Status Code: 400 Bad Request
Messaggio: "Email già registrata"

Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////

TEST-VAL-02: Password debole
Passi:

POST /api/utenti/registrazione
Body:

{
  "email": "nuovo@test.com",
  "password": "123",
  "nome": "Test",
  "cognome": "User"
}

Risultato Atteso:

Status Code: 400 Bad Request
Messaggio: "Password non soddisfa i requisiti"

Risultato Ottenuto: [ ] PASS [ ] FAIL

///////////////////////////////////////////////////////

TEST-VAL-03: Matricola mezzo duplicata
Passi:

POST /api/mezzi
Body con matricola già esistente (es. "BM001")

Risultato Atteso:

Status Code: 400 Bad Request
Messaggio: "Matricola già esistente"

Risultato Ottenuto: [ ] PASS [ ] FAIL


///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////
///////////////////////////////////////////////////////