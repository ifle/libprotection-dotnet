﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace LibProtection.Injections
{
    public sealed class Url : RegexLanguageProvider
    {
        protected override Enum ErrorTokenType { get; } = UrlTokenType.Error;

        protected override IEnumerable<RegexTokenDefinition> TokenDefinitions { get; } = new[]
        {
            new RegexTokenDefinition(@"[^:/?#]+:", UrlTokenType.Scheme),
            new RegexTokenDefinition(@"//[^/?#]*", UrlTokenType.AuthorityCtx),
            new RegexTokenDefinition(@"[^?#]*", UrlTokenType.PathCtx),
            new RegexTokenDefinition(@"\?[^#]*", UrlTokenType.QueryCtx),
            new RegexTokenDefinition(@"#.*", UrlTokenType.Fragment)
        };

        private Url() { }

        public override IEnumerable<Token> Tokenize(string text, int offset = 0)
        {
            foreach (var token in base.Tokenize(text, offset))
            {
                var tokenText = token.Text;
                var lowerBound = token.Range.LowerBound;

                switch ((UrlTokenType) token.Type)
                {
                    case UrlTokenType.AuthorityCtx:
                        tokenText = tokenText.Substring(2, tokenText.Length - 2);
                        lowerBound += 2;

                        foreach (var subToken in SplitToken(tokenText, lowerBound, ":@", UrlTokenType.AuthorityEntry))
                        {
                            yield return subToken;
                        }
                        break;

                    case UrlTokenType.PathCtx:
                        foreach (var subToken in SplitToken(tokenText, lowerBound, "\\/", UrlTokenType.PathEntry))
                        {
                            yield return subToken;
                        }
                        break;

                    case UrlTokenType.QueryCtx:
                        foreach (var subToken in SplitToken(tokenText, lowerBound, "&=", UrlTokenType.QueryEntry))
                        {
                            yield return subToken;
                        }
                        break;

                    default:
                        yield return token;
                        break;
                }
            }
        }

        public override bool TrySanitize(string text, Token context, out string sanitized)
        {
            switch (context.LanguageProvider)
            {
                case Url _:
                    if (TryUrlEncode(text, (UrlTokenType) context.Type, out sanitized))
                    {
                        return true;
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported URL island: {context}");
            }

            return base.TrySanitize(text, context, out sanitized);
        }

        protected override bool IsSafeToken(Enum type, string text)
        {
            switch ((UrlTokenType) type)
            {
                case UrlTokenType.QueryEntry:
                case UrlTokenType.Fragment:
                    return true;

                case UrlTokenType.PathEntry:
                    return !text.Contains("..");
            }

            return false;
        }

        private IEnumerable<Token> SplitToken(string text, int lowerBound, string splitChars, UrlTokenType tokenType)
        {
            if (string.IsNullOrEmpty(text)) { yield break; }
            var sb = new StringBuilder();

            foreach (var c in text)
            {
                if (splitChars.Contains(c.ToString()))
                {
                    if (sb.Length != 0)
                    {
                        var tokenText = sb.ToString();
                        sb.Clear();
                        var upperBound = lowerBound + tokenText.Length - 1;
                        yield return CreateToken(tokenType, lowerBound, upperBound, tokenText);
                        lowerBound = upperBound + 2;
                    }
                    else
                    {
                        lowerBound++;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length != 0)
            {
                var lastTokenText = sb.ToString();
                yield return CreateToken(tokenType, lowerBound, lowerBound + lastTokenText.Length - 1, lastTokenText);
            }
        }

        private static bool TryUrlEncode(string text, UrlTokenType tokenType, out string encoded)
        {
            encoded = null;

            switch (tokenType)
            {
                case UrlTokenType.PathEntry:
                case UrlTokenType.QueryEntry:
                case UrlTokenType.Fragment:
                    encoded = HttpUtility.UrlEncode(text);
                    return true;

            }

            return false;
        }
    }
}