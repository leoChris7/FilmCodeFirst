using Microsoft.VisualStudio.TestTools.UnitTesting;
using FilmCodeFirst.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FilmCodeFirst.Models.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Net;


namespace FilmCodeFirst.Controllers.Tests
{
    [TestClass()]
    public class UtilisateursControllerTests
    {
        public FilmRatingsDBContext Context;
        public UtilisateursController Controller;

        [TestInitialize()]
        public void Init()
        {
            this.Context = new FilmRatingsDBContext();
            this.Controller = new UtilisateursController(Context);
        }

        [TestMethod()]
        public void GetUtilisateursTest()
        {
            // Arrange
            var usersBD = this.Context.Utilisateurs.ToList().Count();
            var usersAPI = this.Controller.GetUtilisateurs().Result.Value.Count();

            // Act
            var result = this.Controller.GetUtilisateurs();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(usersBD, usersAPI);
        }

        [TestMethod()]
        public void GetUtilisateurTest_Success()
        {
            // Arrange
            // ON récupère un utilisateur dans la bd
            int userId = Context.Utilisateurs.FirstOrDefault(x => x.UtilisateurId == 1).UtilisateurId;

            // Act
            // On récupère l'utilisateur du même ID dans l'API
            var result = Controller.GetUtilisateur(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.Id);

        }

        [TestMethod()]
        public void GetUtilisateurTest_Failure()
        {
            // Arrange
            // On récupère le dernier élément en rajoutant 1 (le test ne devrait donc pas le trouver)
            int userId = Context.Utilisateurs.FirstOrDefault(x=>x.UtilisateurId == Context.Utilisateurs.Max(x=>x.UtilisateurId)).UtilisateurId+1;

            // Act
            var result = Controller.GetUtilisateur(userId);

            // Assert
            Assert.IsTrue(result.Result.Result is NotFoundResult);
        }


        [TestMethod()]
        public void GetUtilisateurByEmailTest_Success()
        {
            // Arrange
            string emailBD = Context.Utilisateurs.FirstOrDefault().Mail ; // Email d'un utilisateur existant dans la base de données

            // Act
            var resultAPI = Controller.GetUtilisateurByEmail(emailBD).Result.Value.Mail;

            // Assert
            Assert.IsNotNull(resultAPI);
            Assert.AreEqual(emailBD, resultAPI);
        }

        [TestMethod()]
        public void GetUtilisateurByEmailTest_Failure()
        {
            // Arrange
            Random r = new Random();
            int randomN = r.Next(1, 999999);
            string email = Context.Utilisateurs.FirstOrDefault().Mail+ randomN.ToString(); // Email d'un utilisateur inexistant dans la base de données

            // Act
            var result = Controller.GetUtilisateurByEmail(email);

            // Assert
            Assert.IsTrue(result.Result.Result is NotFoundResult);
        }



        [TestMethod]
        public void Postutilisateur_ModelValidated_CreationOK()
        {
            // Arrange
            Random rnd = new Random();
            int chiffre = rnd.Next(1, 1000000000);
            // Le mail doit être unique donc 2 possibilités :
            // 1. on s'arrange pour que le mail soit unique en concaténant un random ou un timestamp
            // 2. On supprime le user après l'avoir créé. Dans ce cas, nous avons besoin d'appeler la méthode DELETE de l’API ou remove du DbSet.
             Utilisateur userAtester = new Utilisateur()
             {
                 Nom = "MACHIN",
                 Prenom = "Luc",
                 Mobile = "0606070809",
                 Mail = "machin" + chiffre + "@gmail.com",
                 Pwd = "Toto1234!",
                 Rue = "Chemin de Bellevue",
                 CodePostal = "74940",
                 Ville = "Annecy-le-Vieux",
                 Pays = "France",
                 Latitude = null,
                 Longitude = null
             };
            
            // Act
            var result = Controller.PostUtilisateur(userAtester).Result; // .Result pour appeler la méthode async de manière synchrone, afin d'attendre l’ajout
            
            // Assert
            Utilisateur? userRecupere = Context.Utilisateurs.Where(u => u.Mail.ToUpper() ==
            userAtester.Mail.ToUpper()).FirstOrDefault(); // On récupère l'utilisateur créé directement dans la BD grace à son mail unique
            // On ne connait pas l'ID de l’utilisateur envoyé car numéro automatique.
            // Du coup, on récupère l'ID de celui récupéré et on compare ensuite les 2 users
            userAtester.UtilisateurId = userRecupere.UtilisateurId;
            Assert.AreEqual(userRecupere, userAtester, "Utilisateurs pas identiques");
        }
        //Rappel : n’utiliser.Result que dans les tests, et non dans l’application cliente sinon
        // l’appel asynchrone échouera.

        // SI UNE CONTRAINTE DE LA BD N'EST PAS RESPECTEE!
        [ExpectedException(typeof(System.AggregateException))]
        [TestMethod()]
        public void POSTMailDuplique()
        {
            // Arrange
            string email = Context.Utilisateurs.FirstOrDefault().Mail;
            Utilisateur userAtester = new Utilisateur()
            {
                Nom = "MACHINE",
                Prenom = "Lucas",
                Mobile = "0606070129",
                Mail = email,
                Pwd = "Toto1234!",
                Rue = "Chemin de Bellevas",
                CodePostal = "74941",
                Ville = "Annecy-la-Vieux",
                Pays = "France",
                Latitude = null,
                Longitude = null
            };

            // Act
            var result = Controller.PostUtilisateur(userAtester).Result; // .Result pour appeler la méthode async de manière synchrone, afin d'attendre l’ajout
        }

        [TestMethod()]
        public async Task DeleteUtilisateurTest()
        {
            // Arrange
            Random rnd = new Random();
            int chiffre = rnd.Next(1, 1000000000);
            Utilisateur userAtester = new Utilisateur()
            {
                Nom = "MACHINE",
                Prenom = "Lucas",
                Mobile = "0606070129",
                Mail = "123@23" + chiffre + "000.MAILimpossible",
                Pwd = "Toto1234!",
                Rue = "Chemin de Bellevas",
                CodePostal = "74941",
                Ville = "Annecy-la-Vieux",
                Pays = "France",
                Latitude = null,
                Longitude = null
            };

            // Act
            Context.Utilisateurs.Add(userAtester); // Ajouter un utilisateur au contexte
            Context.SaveChanges();

            int id = Context.Utilisateurs.FirstOrDefault(x => x.Mail == userAtester.Mail).UtilisateurId;
            await Controller.DeleteUtilisateur(id); // Attendre la suppression de l'utilisateur

            int? idTrouveBD = Context.Utilisateurs.FirstOrDefault(x => x.UtilisateurId == id)?.UtilisateurId;

            // Assert
            Assert.IsNull(idTrouveBD);
        }

    }
}