using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace pt_ChkConfig
{
    public class APIService : IONAPIFile
    {
        HttpClient gHttpClient = null;
        //private IONAPIFile credentials = null;
        //private Uri uri = null;
        public string ErrorMessage { get; set; } = "";
        public string StatusCode { get; set; }

        public string callService(Uri aUri, string aBody, bool aSendChunked = true)
        {
            return (callServiceInternal(aUri, aBody));
        }

        protected string callService(Uri aUri)
        {
            return (callServiceInternal(aUri, null));
        }

        private string callServiceInternal(Uri aUri, string aBody, bool aSendChunked = false)
        {
            string result = null;
            using (HttpClient client = new HttpClient { BaseAddress = new Uri(aUri.Scheme + "://" + aUri.Host) })
            {
                if (null == gHttpClient)
                {
                    gHttpClient = new HttpClient
                    {
                        BaseAddress = new Uri(aUri.Scheme + "://" + aUri.Host)
                    };
                    //client = gHttpClient;
                }
                string token = GetToken();

                if (false == string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    HttpRequestMessage request = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = aUri,
                        Headers =
                    {
                        { HttpRequestHeader.Accept.ToString(), "application/json" }
                    }
                    };

                    request.Headers.TransferEncodingChunked = aSendChunked;

                    if (false == string.IsNullOrEmpty(aBody))
                    {
                        request.Method = HttpMethod.Post;
                        request.Content = new StringContent(aBody, Encoding.UTF8, "application/json");
                    }

                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (null != response)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            result = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            StatusCode = response.StatusCode.ToString();
                            ErrorMessage = response.StatusCode.ToString() + " " + response.ReasonPhrase;
                        }
                    }
                    response.Dispose();
                    response = null;
                }
                client.Dispose();
            }

            return (result);
        }
    }
}
