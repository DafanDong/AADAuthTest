using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// The following using statements were added for this sample.
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
//using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using System.Configuration;

using System.Threading;
using System.Net;
using System.Security.Claims;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

using System.IdentityModel.Tokens;
using System.IdentityModel.Selectors;
using System.IdentityModel.Metadata;
using Microsoft.IdentityModel.Protocols;
using System.ServiceModel.Security;

using System.Configuration;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Redirect URI is the URI where Azure AD will return OAuth responses.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aad_login = ConfigurationManager.AppSettings["ida:AADLogin"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string audience = ConfigurationManager.AppSettings["ida:Audience"];

        Uri redirectUri = new Uri(ConfigurationManager.AppSettings["ida:RedirectUri"]);


        private static string authority = String.Format(CultureInfo.InvariantCulture, aad_login, tenant);
        //
        // To authenticate to the To Do list service, the client needs to know the service's App ID URI.
        // To contact the To Do list service we need it's URL as well.
        //
        private static string resource_id = ConfigurationManager.AppSettings["ida:ResourceId"];

        private HttpClient httpClient = new HttpClient();
        private Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = null;
        private string m_access_token;
        private async void onVerifyClick(object sender, RoutedEventArgs e)
        {
            // do something
            string issuer = "";
            List<SecurityToken> signingTokens = null;
            try
            {
                string stsDiscoveryEndpoint = string.Format("{0}/.well-known/openid-configuration", authority);
                ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint);
                
                OpenIdConnectConfiguration config = await configManager.GetConfigurationAsync();
                issuer = config.Issuer;
                signingTokens = config.SigningTokens.ToList();
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    ValidAudience = audience,
                    ValidIssuer = issuer,
                    IssuerSigningTokens = signingTokens,
                    CertificateValidator = X509CertificateValidator.None
                };

                // Validate token.
                SecurityToken validatedToken = new JwtSecurityToken();
                ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(m_access_token, validationParameters, out validatedToken);

                var puid = claimsPrincipal.FindFirst(@"puid");
                MessageBox.Show("Verify Access token success! ");

            }
            catch(Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "Verifing Access token get Inner Exception : " + ex.InnerException.Message;
                }
                MessageBox.Show(message);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            //
            // As the application starts, try to get an access token without prompting the user.  If one exists, populate the To Do list.  If not, continue.
            //
            authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority);
            AuthenticationResult result = null;
            
            try
            {
                result = authContext.AcquireToken(resource_id, clientId, redirectUri);
                string text = "Access token: ";
                token.Text = (text + result.AccessToken);
                m_access_token = result.AccessToken;
            }

            catch (AdalException ex)
            {
                if (ex.ErrorCode == "user_interaction_required")
                {
                    // There are no tokens in the cache.  Proceed without calling the To Do list service.
                }
                else
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }
                    MessageBox.Show(message);
                }
                return;
            }
            
        }
    }
}
