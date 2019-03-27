//  NOTE:  I am excluding this functionality from rocketscience because I want to have minimal (zero)
// interaction with Azure

//using Microsoft.Azure.KeyVault;
//using Microsoft.IdentityModel.Clients.ActiveDirectory;
//using System;
//using System.Security.Cryptography.X509Certificates;

//namespace Microsoft.Azure.Functions.AFRocketScience
//{
//    // ----------------------------------------------------------------------------------------------
//    /// <summary>
//    /// Simplified access to Azure Keyvault.
//    /// Note: to get this to work locally, you will need to install the access certificates locally on your 
//    /// machine.
//    /// </summary>
//    // ----------------------------------------------------------------------------------------------
//    public class KeyVaultAccessor
//    {
//        /// <summary>
//        /// The certificate used to authenticate this app to Azure
//        /// </summary>
//        public X509Certificate2 ApplicationCertificate { get; set; }

//        private string _keyVaultUrl { get; set; }
//        private KeyVaultClient _keyVaultClient { get; set; }

//        // ----------------------------------------------------------------------------------------------
//        /// <summary>
//        /// ctor
//        /// </summary>
//        // ----------------------------------------------------------------------------------------------
//        public KeyVaultAccessor(string keyVaultUrl, string appId, string thumbprint)
//        {
//            this._keyVaultUrl = keyVaultUrl;
//            ApplicationCertificate = FindCertificateByThumbprint(thumbprint);
//            if(ApplicationCertificate == null)
//            {
//                throw new ApplicationException("Could not find certificate with Thumbprint: " + thumbprint);
//            }

//           _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
//                    async (string authority, string resource, string scope) =>
//                    {
//                        var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
//                        var assertionCert = new ClientAssertionCertificate(appId, ApplicationCertificate);
//                        var result = await context.AcquireTokenAsync(resource, assertionCert);
//                        return result.AccessToken;
//                    }),
//                    new System.Net.Http.HttpClient());
//        }

//        // ----------------------------------------------------------------------------------------------
//        /// <summary>
//        /// Locate a local certificate via thumbprint
//        /// </summary>
//        // ----------------------------------------------------------------------------------------------
//        public static X509Certificate2 FindCertificateByThumbprint(string thumbprint, StoreLocation startCertLocation = StoreLocation.CurrentUser)
//        {
//            X509Certificate2 cert = FindCertificate(X509FindType.FindByThumbprint, thumbprint, startCertLocation);
//            if (cert == null)
//            {
//                startCertLocation = startCertLocation == StoreLocation.LocalMachine ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
//                cert = FindCertificate(X509FindType.FindByThumbprint, thumbprint, startCertLocation);
//            }

//#if DEBUG
//            if (cert == null)
//            {
//                throw new ApplicationException($"Could not find cert from thrumbprint '{thumbprint}'.  Make sure you are running VS as admin.");
//            }
//#endif

//            return cert;
//        }

//        // ----------------------------------------------------------------------------------------------
//        /// <summary>
//        /// FindCertificates helper
//        /// </summary>
//        // ----------------------------------------------------------------------------------------------
//        public static X509Certificate2 FindCertificate(X509FindType findType, string findValue, StoreLocation certLocation = StoreLocation.CurrentUser, StoreName certStore = StoreName.My)
//        {
//            if (String.IsNullOrWhiteSpace(findValue)) throw new ArgumentNullException("findValue");

//            X509Store store = null;
//            try
//            {
//                store = new X509Store(certStore, certLocation);
//                store.Open(OpenFlags.ReadOnly);

//                X509Certificate2Collection certs = store.Certificates.Find(findType, findValue, false);
//                return (certs == null || certs.Count < 1) ? null : certs[0];
//            }
//            finally
//            {
//                store?.Close();
//            }
//        }

//        // ----------------------------------------------------------------------------------------------
//        /// <summary>
//        /// Get the secret value identified by the name
//        /// </summary>
//        // ----------------------------------------------------------------------------------------------
//        public string GetSecret(string secretName)
//        {
//            var secret = _keyVaultClient.GetSecretAsync(_keyVaultUrl, secretName).Result;
//            return secret.Value;
//        }
//    }
//}
