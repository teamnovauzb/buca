using System;
using System.Collections.Specialized;
using System.Web;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Luxodd.Game.Scripts.Network
{
    public class FetchUrlQueryString : MonoBehaviour
    {
        private const string TokenNameParameter = "token";
        private const string WSUrlParameter = "ws";

        public string Token { get; private set; }
        public string WSUrl { get; private set; }

        private string _launchQueryString;

        private NameValueCollection _queryString;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetURLFromQueryStr();
#else
        private static string GetURLFromQueryStr() => Application.absoluteURL;
#endif

        private void Awake()
        {
            _launchQueryString = ReadURLFromQueryString();
            Token = ParseTokenFromURL();
            WSUrl = ParseWebSocketUrlFromQueryString();
            LoggerHelper.Log($"[{GetType().Name}][{nameof(Awake)}] URL received; " +
                             $"token present: {!string.IsNullOrEmpty(Token)}, ws present: {!string.IsNullOrEmpty(WSUrl)}");
        }

        private string ReadURLFromQueryString()
        {
            return GetURLFromQueryStr();
        }

        private string ParseTokenFromURL()
        {
            var url = _launchQueryString;
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string queryString;
            if (url.StartsWith("?", StringComparison.Ordinal))
            {
                queryString = url;
            }
            else if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                queryString = uri.Query;
            }
            else
            {
                LoggerHelper.LogWarning($"[{GetType().Name}] Could not parse launch URL.");
                return null;
            }

            var parametersCollection = HttpUtility.ParseQueryString(queryString);
            _queryString = parametersCollection;
            //LoggerHelper.Log($"[{GetType().Name}][{nameof(ParseTokenFromURL)}] OK, URL: {uri}, Query: {queryString}");
            return parametersCollection.Get(TokenNameParameter);
        }

        private string ParseWebSocketUrlFromQueryString()
        {
            if (_queryString == null)
            {
                var token = ParseTokenFromURL();
            }

            return _queryString != null ? _queryString.Get(WSUrlParameter) : string.Empty;
        }
    }
}
