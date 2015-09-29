using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace BingAdsAppTest
{
    // Using access tokens requires that you register your application and that
    // the user gives consent to your application to access their data. This 
    // example uses a form and WebBrowser control to get the user's consent.
    // The control and form require a single-threaded apartment.

    partial class OAuthForm : Form
    {
        private static OAuthForm _form;
        private static WebBrowser _browser;

        private static string _code;
        private static string _error;

        // When you register your application, the Client ID is provisioned.

        private const string ClientId = "000000004816911A";

        // Request-related URIs that you use to get an authorization code, 
        // access token, and refresh token.

        private const string AuthorizeUri = "https://login.live.com/oauth20_authorize.srf";
        private const string TokenUri = "https://login.live.com/oauth20_token.srf";
        private const string DesktopUri = "https://login.live.com/oauth20_desktop.srf";
        private const string RedirectPath = "/oauth20_desktop.srf";
        private const string ConsentUriFormatter = "{0}?client_id={1}&scope=bingads.manage&response_type=code&redirect_uri={2}";
        private const string AccessUriFormatter = "{0}?client_id={1}&code={2}&grant_type=authorization_code&redirect_uri={3}";
        private const string RefreshUriFormatter = "{0}?client_id={1}&grant_type=refresh_token&redirect_uri={2}&refresh_token={3}";

        // Constructor

        public OAuthForm(string uri)
        {
            InitializeForm(uri);
        }

        [STAThread]
        static void Main()
        {
            // Create the URI to get user consent. Returns the authorization
            // code that is used to get an access token and refresh token.

            var uri = string.Format(ConsentUriFormatter, AuthorizeUri, ClientId, DesktopUri);
            Console.WriteLine("\n\n\nuse the following url to init OAuthForm: {0}\n\n\n", uri);
            _form = new OAuthForm(uri);
            _form.FormClosing += form_FormClosing;
            //  _form.Size = new System.Drawing.Size(420, 580);

            Application.EnableVisualStyles();

            // Launch the form and make an initial request for user consent.
            // For example POST /oauth20_authorize.srf?
            //                 client_id=<ClientId>
            //                 &scope=bingads.manage
            //                 &response_type=code
            //                 &redirect_uri=https://login.live.com/oauth20_desktop.srf HTTP/1.1

            Application.Run(_form);  // Blocks until the form closes

            // While the application is running, browser_Navigated filters traffic to identify
            // the redirect URI. The redirect's query string will contain either the authorization  
            // code if the user consented or an error if the user declined.
            // For example https://login.live.com/oauth20_desktop.srf?code=<code>


            // If the user did not give consent or the application was 
            // not registered, the authorization code will be null.

            if (string.IsNullOrEmpty(_code))
            {
                Console.WriteLine(_error);
                return;
            }

            // Use the authentication code to get the access token and refresh token. 
            // For example POST /oauth20_token.srf?
            //                 client_id=<ClientId>
            //                 &code=<code>
            //                 &grant_type=authorization_code
            //                 &redirect_uri=https://login.live.com/oauth20_desktop.srf HTTP/1.1

            uri = string.Format(AccessUriFormatter, TokenUri, ClientId, _code, DesktopUri);
            Console.WriteLine("\n\n\nuse the following url to get access token: {0}\n\n\n", uri);
            AccessTokens tokens = GetAccessTokens(uri);

            // You may use the access token to set the AuthenticationToken header in 
            // Bing Ads API calls.

            // You should securely store the refresh token for use
            // later when the access token expires. 

            Console.WriteLine("\n\n\nAccess token expires in {0} minutes: ", tokens.ExpiresIn / 60);
            Console.WriteLine("\nAccess token: " + tokens.AccessToken);
            Console.WriteLine("\nRefresh token: " + tokens.RefreshToken);

            // The access token is valid for a limited time per the 'expires_in' response field.

            // Use the refresh token to get a new access token without further user consent.
            // For example POST /oauth20_token.srf?
            //                 client_id=<ClientId>
            //                 &grant_type=refresh_token
            //                 &redirect_uri=https://login.live.com/oauth20_desktop.srf
            //                 &refresh_token=<tokens.RefreshToken>

            uri = string.Format(RefreshUriFormatter, TokenUri, ClientId, DesktopUri, tokens.RefreshToken);
            tokens = GetAccessTokens(uri);

            Console.WriteLine("\n\n\nAccess token expires in {0} minutes: ", tokens.ExpiresIn / 60);
            Console.WriteLine("\nAccess token: " + tokens.AccessToken);
            Console.WriteLine("\nRefresh token: " + tokens.RefreshToken);
        }

        // Initialize the form that contains the browser control.
        // The Navigate() method requests the access code, and the
        // Navigated delegate does the work (i.e. gets the query
        // string and parses it for the access code).

        private void InitializeForm(string uri)
        {
            _browser = new WebBrowser { Dock = DockStyle.Fill };

            _browser.Navigated += browser_Navigated;
            _browser.Navigate(uri);

            Controls.AddRange(new Control[] { _browser });
        }

        // The form gets closed by the user if they click the system exit button
        // or the by the browser_Navigated delegate.

        private static void form_FormClosing(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Captures all the consent traffic. Filter the traffic for the redirect URI. 

        public static void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            try
            {
                if (e.Url.AbsolutePath.Equals(RedirectPath))
                {
                    if (!string.IsNullOrEmpty(e.Url.Query))
                    {
                        Dictionary<string, string> parameters = ParseQueryString(e.Url.Query, new[] { '&', '?' });

                        if (parameters.ContainsKey("code"))
                        {
                            _code = parameters["code"];
                            Console.WriteLine("\nGet Authorization Code: {0}\n", _code);
                        }
                        else
                        {
                            parameters = ParseQueryString(e.Url.Fragment, new[] { '&', '?', '#' });
                            if (parameters.ContainsKey("error_description"))
                            {
                                _error = Uri.UnescapeDataString(parameters["error_description"]);
                            }
                            else
                            {
                                throw new System.Exception("Unexpect uri: " + e.Url.AbsoluteUri);
                            }
                        }

                        _form.Close();
                    }
                }
            }
            catch(System.Exception ex)
            {
                Console.WriteLine("\nGet exception when open webpage: {0}\n", ex.Data);
            }
        }

        // Parses the URI query string. The query string contains a list of name-value pairs 
        // following the '?'. Each name-value pair is separated by an '&'.

        private static Dictionary<string, string> ParseQueryString(string query, char[] delimiters)
        {
            var parameters = new Dictionary<string, string>();

            string[] pairs = query.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pair in pairs)
            {
                string[] nameValue = pair.Split(new[] { '=' });
                parameters.Add(nameValue[0], nameValue[1]);
            }

            return parameters;
        }

        // Gets an access token. Returns the access token, access token 
        // expiration, and refresh token.

        private static AccessTokens GetAccessTokens(string uri)
        {
            var responseSerializer = new DataContractJsonSerializer(typeof(AccessTokens));
            AccessTokens tokenResponse = null;

            try
            {
                var realUri = new Uri(uri, UriKind.Absolute);

                var addy = realUri.AbsoluteUri.Substring(0, realUri.AbsoluteUri.Length - realUri.Query.Length);
                var request = (HttpWebRequest)WebRequest.Create(addy);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(realUri.Query.Substring(1));
                }

                var response = (HttpWebResponse)request.GetResponse();

                // The response is JSON, for example: {
                //                                     "token_type":"bearer",
                //                                     "expires_in":3600,
                //                                     "scope":"bingads.manage",
                //                                     "access_token":"<AccessToken>",
                //                                     "refresh_token":"<RefreshToken>"
                //                                    }

                // Use the JSON serializer to serialize the response into the AccessTokens object.

                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                        tokenResponse = (AccessTokens)responseSerializer.ReadObject(responseStream);
                }
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)e.Response;

                Console.WriteLine("HTTP status code: " + response.StatusCode);
            }

            return tokenResponse;
        }
    }


    // The grant flow returns more fields than captured in this sample.
    // Additional fields are not relevant for calling Bing Ads APIs or refreshing the token.

    [DataContract]
    class AccessTokens
    {
        [DataMember]
        // Indicates the duration in seconds until the access token will expire.
        internal int expires_in = 0;

        [DataMember]
        // When calling Bing Ads service operations, the access token is used as  
        // the AuthenticationToken header element.
        internal string access_token = null;

        [DataMember]
        // May be used to get a new access token with a fresh expiration duration.
        internal string refresh_token = null;

        public string AccessToken { get { return access_token; } }
        public int ExpiresIn { get { return expires_in; } }
        public string RefreshToken { get { return refresh_token; } }
    }
}
