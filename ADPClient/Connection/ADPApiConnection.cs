﻿/*# This file is part of adp-api-library.
# https://github.com/adp/adp-api-lib-net

# Copyright © 2015-2016 ADP, LLC.

# Licensed under the Apache License, Version 2.0 (the “License”);
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at

# http://www.apache.org/licenses/LICENSE-2.0

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an “AS IS” BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
# express or implied.  See the License for the specific language
# governing permissions and limitations under the License.
*/
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;
using ADPClient.ADPException;

namespace ADPClient
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class ADPApiConnection
    {

        /// <summary>
        /// 
        /// </summary>
        public ConnectionConfiguration connectionConfiguration { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        protected int tokenExpiration { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string state { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ADPAccessToken accessToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionConfiguration"></param>
        public ADPApiConnection(ConnectionConfiguration connectionConfiguration)
        {
            this.connectionConfiguration = connectionConfiguration;
        }

        /// <summary>
        /// 
        /// </summary>
        public void connect()
        {
            if (connectionConfiguration == null)
            {
                throw new Exception("Configuration not provided.");
            }

            this.accessToken = this.getAccessToken();
        }

        /// <summary>
        /// 
        /// </summary>
        public void disconnect()
        {
            this.accessToken = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool isConnectedIndicator()
        {
            if (accessToken != null)
            {
                // we have a valid token so check if it expired
                if (DateTime.Compare(DateTime.Now, accessToken.ExpiresOn.Value) > 0)
                {
                    // token expires so set to null
                    accessToken = null;
                }
            }

            return accessToken != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>valid ADPAccessToken</returns>
        protected virtual ADPAccessToken getAccessToken()
        {
            ADPAccessToken token = accessToken;
            Dictionary<string, string> data = null;
            AuthenticationHeaderValue credentials = null;

            if (!isConnectedIndicator())
            {
                if (String.IsNullOrEmpty(connectionConfiguration.grantType))
                {
                    throw new Exception("ADP Connection Exception: config option grantType cannot be null/empty");
                }

                if (String.IsNullOrEmpty(connectionConfiguration.tokenServerURL))
                {
                    throw new Exception("ADP Connection Exception: config option tokenServerURL cannot be null/empty");
                }
                
                data = new Dictionary<string, string>();

                data.Add("client_id", connectionConfiguration.clientID);
                data.Add("client_secret", connectionConfiguration.clientSecret);
                data.Add("grant_type", connectionConfiguration.grantType);

                // send the data to ADP server/s
                var result = Post(connectionConfiguration.tokenServerURL, data, credentials /* new AuthenticationHeaderValue("Basic", encodedCredentials) */);

                if (!String.IsNullOrEmpty(result))
                {
                    token = JSONUtil.Deserialize<ADPAccessToken>(result);
                }
            }

            // if valid token from session or 
            // token not expired return it.
            return token;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ADPProductURL"></param>
        /// <returns></returns>
        public string getADPData(string ADPProductURL)
        {
            string serverResponse = null;
            ADPAccessToken token = getAccessToken();
            Dictionary<string, string> data = null;

            if (isConnectedIndicator() && (token != null))
            {
                // send the data to ADP server/s
                // since we have a valid token
                serverResponse = Post(ADPProductURL, data, new AuthenticationHeaderValue(token.TokenType, token.AccessToken),  "application/json");
            }
            else {
                throw new Exception("Connection Exception: connection not established.");
            }
            return serverResponse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        static byte[] PEM(string type, byte[] data)
        {
            string pem = Encoding.ASCII.GetString(data);
            string header = String.Format("-----BEGIN {0}-----", type);
            string footer = String.Format("-----END {0}-----", type);
            int start = pem.IndexOf(header) + header.Length;
            int end = pem.IndexOf(footer, start);
            string base64 = pem.Substring(start, (end - start));
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        private X509Certificate2 LoadCertificateFile(string Filename, string SSLKeyPath, string Password = null)
        {
            X509Certificate2 x509 = null;

            if (Filename.EndsWith(".pfx"))
            {
                x509 = new X509Certificate2(Filename, Password);
            }
            else {
                using (FileStream fs = File.OpenRead(Filename))
                {
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    if (data[0] != 0x30)
                    {
                        // maybe it's ASCII PEM base64 encoded ? 
                        data = PEM("CERTIFICATE", data);
                    }
                    if (data != null)
                        x509 = new X509Certificate2(data);
                }
            }
            return x509;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="credentials"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        protected string Post(string url, Dictionary<string, string> data, AuthenticationHeaderValue authentication = null, string contentType = "application/x-www-form-urlencoded")
        {
            string responseString = null;
            FormUrlEncodedContent content = null;
            string certpath = (HttpContext.Current == null) ? connectionConfiguration.sslCertPath : HttpContext.Current.Server.MapPath(connectionConfiguration.sslCertPath);

            var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", connectionConfiguration.clientID, connectionConfiguration.clientSecret)));
            
            WebRequestHandler handler = new WebRequestHandler();
            X509Certificate2 certificate = LoadCertificateFile(certpath, connectionConfiguration.sslKeyPath, connectionConfiguration.sslKeyPass);
            handler.ClientCertificates.Add(certificate);

            using (var client = new HttpClient(handler))
            {
                // iat needs to support Basic Authentication
                // uncomment this when it does.
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
                // System.Net.Http.Headers.AuthenticationHeaderValue t = new AuthenticationHeaderValue("Basic", encodedCredentials);
                // var g = t.GetType();

                if (authentication != null)
                {
                    client.DefaultRequestHeaders.Authorization = authentication;
                    // client.DefaultRequestHeaders.Add("Authorization", authorization); //  "Bearer " + accessToken);
                }
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));

                if (data != null)
                {
                    content = new FormUrlEncodedContent(data);
                }

                var response = client.PostAsync(url, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    // by calling .Result you are performing a synchronous call
                    var responseContent = response.Content;

                    // by calling .Result you are synchronously reading the result
                    responseString = responseContent.ReadAsStringAsync().Result;
                } else
                {
                    throw new ADPConnectionException(String.Format("Connection Exception: {0}: {1}", response.StatusCode, response.ReasonPhrase), new JavaScriptSerializer().Serialize(response));
                }
            }

            return responseString;
        }
    }
}

