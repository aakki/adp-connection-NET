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
using System.Runtime.Serialization;


namespace ADPClient
{
    [DataContract]
    public class AuthorizationCodeConfiguration : ConnectionConfiguration
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="JSONConfigObject"></param>
        /// <returns></returns>
        public override ConnectionConfiguration init(String JSONConfigObject)
        {
            AuthorizationCodeConfiguration ccfg =  JSONUtil.Deserialize<AuthorizationCodeConfiguration>(JSONConfigObject);

            this.authorizationCode = ccfg.authorizationCode;
            this.baseAuthorizationURL = ccfg.baseAuthorizationURL;
            this.redirectURL = ccfg.redirectURL;
            this.responseType = ccfg.responseType;
            this.scope = ccfg.scope;
            this.state = ccfg.state;

            this.clientID = ccfg.clientID;
            this.clientSecret = ccfg.clientSecret;
            this.sslCertPath = ccfg.sslCertPath;
            this.sslKeyPass = ccfg.sslKeyPass;
            this.tokenServerURL = ccfg.tokenServerURL;
            this.apiRequestURL = ccfg.apiRequestURL;
            // this.tokenExpiration = ccfg.tokenExpiration;
            this.accessScope = ccfg.accessScope;

            return ccfg;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string scope { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string responseType {get; set;}

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int state { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public String baseAuthorizationURL { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public String redirectURL { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public String authorizationCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        String authorizationURL {get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public override string grantType
        {
            get
            {
                return "authorization_code";
            }
            set { }
        }
    }
}

