using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Thinktecture.IdentityModel.Client;

namespace pt_ChkConfig
{
    public class IONAPIFile
    {
        public string ti { get; set; }
        public string cn { get; set; }
        public string dt { get; set; }
        public string ci { get; set; }
        public string cs { get; set; }
        public string iu { get; set; }
        public string pu { get; set; }
        public string oa { get; set; }
        public string ot { get; set; }
        public string or { get; set; }
        public string ru { get; set; } = "oob://localhost/m3dlt";
        public string ev { get; set; }
        public string v { get; set; }
        public string saak { get; set; }
        public string sask { get; set; }

        public string Error { get; }

        private OAuth2Client client;
        private TokenResponse token = null;
        private AuthorizeResponse authorizeResponse = null;
        private DateTime expiresAt;

        public delegate void RetrievedTokenHandler(string aToken);
        public event RetrievedTokenHandler RetrievedTokenEvent;

        //private const string RedirectUri = "oob://localhost/qhexportmi";
        //LoginWebView webView = new LoginWebView();

        //public MainWindow mainWindow = null;

        public IONAPIFile()
        {

        }

        //public IONAPIFile(string aPath)
        //{
        //    Error = LoadIONAPI(aPath);
        //}

        private string getServiceAccount()
        {
            return (saak);
        }

        private string getServiceAccountKey()
        {
            return (sask);
        }

        public string getAuthorisationUrl()
        {
            return (pu + oa);
        }

        public string getRevocationUrl()
        {
            return (pu + or);
        }

        public string getTokenUrl()
        {
            return (pu + ot);
        }

        public string getIONAPIUrl()
        {
            return (iu + "/" + ti + "/");
        }

        public string getIONAPIUrlForM3()
        {
            return (iu + "/" + ti + "/M3/");
        }

        public string getIONAPIUrlForMingle()
        {
            return (iu + "/" + ti + "/Mingle/");
        }

        public string getIONAPIUrlForION()
        {
            return (iu + "/" + ti + "/IONSERVICES/datacatalog/");
        }

        public string getTenant()
        {
            return (ti);
        }

        public string getContext()
        {
            return (cn);
        }

        public string getClientId()
        {
            return (ci);
        }
        public string getClientSecret()
        {
            return (cs);
        }

        public static IONAPIFile LoadIONAPI(string aPath)
        {
            IONAPIFile result = null;

            //string fileContents = File.ReadAllText(aPath);

            using (StreamReader sr = new StreamReader(aPath))
            {
                string fileContents = sr.ReadToEnd();

                if (null != fileContents && fileContents.Length > 0)
                {
                    try
                    {
                        result = JsonConvert.DeserializeObject<IONAPIFile>(fileContents);
                    }
                    catch (Exception)
                    {

                    }

                }
                sr.Close();
            }



            return (result);
        }

        public void RevokeToken()
        {
            if (DateTime.Now < expiresAt)
            {
                RevokeToken(GetToken(), OAuth2Constants.AccessToken);
            }
        }


        private void RevokeToken(string token, string tokenType)
        {
            var client = new HttpClient();
            client.SetBasicAuthentication(getClientId(), getClientSecret());

            var postBody = new Dictionary<string, string>
            {
                { "token", token },
                { "token_type_hint", tokenType }
            };

            var result = client.PostAsync(getRevocationUrl(), new FormUrlEncodedContent(postBody)).Result;

            if (result.IsSuccessStatusCode)
            {
                //Console.WriteLine("Succesfully revoked token.");
            }
            else
            {
                //Console.WriteLine("Error revoking token. (" + result.StatusCode + ")");
            }
        }

        public string GetToken()
        {
            string result = null;
            if (token != null)
            {
                if (token.ExpiresIn > 0)
                {
                    if (DateTime.Now < expiresAt)
                    {
                        result = token.AccessToken;
                    }
                }
                else
                {
                    result = token.AccessToken;
                }
            }

            if (true == string.IsNullOrEmpty(result))
            {
                result = getToken();
            }

            return (result);
        }

        private string getToken()
        {
            string result = null;
            authorizeResponse = null;

            if (!(false == string.IsNullOrEmpty(getServiceAccount()) && false == string.IsNullOrEmpty(getServiceAccountKey())))
            {
                //client = new OAuth2Client(new Uri(getAuthorisationUrl()));
                //var state = Guid.NewGuid().ToString("N");
                //var nonce = Guid.NewGuid().ToString("N");

                //string url = client.CreateCodeFlowUrl(clientId: getClientId(),
                //redirectUri: ru,
                //state: state,
                //nonce: nonce);

                ////webView.Owner = mainWindow.Dispatcher.;
                //webView.Done += WebView_Done;
                //webView.Dispatcher.Invoke((Action)delegate { webView.Owner = mainWindow; webView.Show(); webView.Start(new Uri(url), new Uri(ru)); });
                //webView.autoResetEventHandle.WaitOne();
                ////Console.WriteLine("Wait One time returned: " + DateTime.Now.ToString("HH:mm:ss:fffffff"));

                //if (null != authorizeResponse && false == string.IsNullOrEmpty(authorizeResponse.Code))
                //{
                //    client = new OAuth2Client(new Uri(getTokenUrl()), getClientId(), getClientSecret());
                //}
            }

            client = new OAuth2Client(new Uri(getTokenUrl()), getClientId(), getClientSecret());

            DateTime dateTime = DateTime.Now;

            if (null != authorizeResponse)
            {
                token = client.RequestAuthorizationCodeAsync(authorizeResponse.Code, ru).Result;
            }
            else
            {
                token = client.RequestResourceOwnerPasswordAsync(getServiceAccount(), getServiceAccountKey()).Result;
            }

            if (null != token.RefreshToken)
            {
                token = client.RequestRefreshTokenAsync(token.RefreshToken).Result;
            }

            if (token.IsError)
            {

                if (token.IsHttpError)
                {
                    //Console.WriteLine("HTTP error: " + token.HttpErrorStatusCode);
                    //Console.WriteLine("HTTP error reason: " + token.HttpErrorReason);
                }
                else
                {
                    //Console.WriteLine("Protocol error response: " + token.Json);
                }

                token = null;
            }
            expiresAt = dateTime.AddSeconds(token.ExpiresIn);
            //result = token.AccessToken;


            if (null != token)
            {
                result = token.AccessToken;
            }


            return (result);
        }

        //private void WebView_Done(object sender, AuthorizeResponse e)
        //{
        //    authorizeResponse = e;
        //    webView.autoResetEventHandle.Set();

        //    //Console.WriteLine("WebView Done: " + DateTime.Now.ToString("HH:mm:ss:fffffff"));
        //}
    }
}
