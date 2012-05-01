using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionParser.Exceptions;

namespace OptionParser
{
    class Tokens
    {
        private Token[] tokens;
        private int tokenNum;
        public Tokens(Token[] tokens)
        {
            this.tokenNum = 0;
            this.tokens = tokens;
        }
        public Token ActualToken()
        {
            if (tokenNum == tokens.Length)
            {
                return null;
            }
            else
            {
                return tokens[tokenNum];
            }
        }
        public Token NextToken()
        {
            if (++tokenNum == tokens.Length)
            {
                return null;
            }
            else
            {
                return tokens[tokenNum];
            }
        }
    }

    class Parser
    {

        private List<Option> options;
        private Tokens tokens;
        private Option lastOption;

        public List<string> Parameters { get; private set; }

        public List<Option> ParsedOption { get; private set; }

        public Parser(Token[] tokens, List<Option> options)
        {
            this.tokens = new Tokens(tokens);
            this.options = options;
            Parameters = new List<string>();
            ParsedOption = new List<Option>();
        }

        public void Parse()
        {
            ParsedOption = new List<Option>();
            Token actualToken = tokens.ActualToken();
            while (actualToken != null)
            {
                if (actualToken is OptionToken)
                {
                    ParseOption(actualToken);
                }
                else if (actualToken is ArgumentToken)
                {
                    ParseParameters(actualToken);
                }
                else if (actualToken is TreatAsArgumentToken)
                {
                    ParseParameters(actualToken);
                }
                actualToken = tokens.ActualToken();
            }
        }

        void ParseParameters(Token token)
        {
            Parameters.Add(token.Value);
            // determine next way
            Token nextToken = tokens.NextToken();

            if (nextToken == null) 
            {
                return;
            }
            else if (nextToken is ArgumentToken) 
            {
                ParseParameters(nextToken);
            }
            else if (nextToken is TreatAsArgumentToken)
            {
                ParseParameters(tokens.NextToken());
            }
            else if (nextToken is OptionToken)
            {
                ParseOption(nextToken);
            }
        }

        void ParseOption(Token token)
        {
            lastOption = GetOptionByToken(token);
            Token nextToken = tokens.NextToken();

            if (nextToken == null)
            {
                CheckSaturation(lastOption);
                lastOption.AddArgumentValue("true");
                ParsedOption.Add(lastOption);
            }
            else if (nextToken is OptionToken)
            {
                CheckSaturation(lastOption);
                lastOption.AddArgumentValue("true");
                ParsedOption.Add(lastOption);
                ParseOption(nextToken);
            }
            else if (nextToken is ArgumentToken)
            {
                if (IsFullSaturated(lastOption))               
                {
                    ParseOptionArguments(nextToken);
                }
                else
                {
                    lastOption.AddArgumentValue("true");
                    ParsedOption.Add(lastOption);
                    ParseParameters(nextToken);
                }
            }
            else if (nextToken is TreatAsArgumentToken)
            {
                CheckSaturation(lastOption);
                lastOption.AddArgumentValue("true");
                ParsedOption.Add(lastOption);
                ParseParameters(tokens.NextToken());
            }      
        }

        void ParseOptionArguments(Token token)
        {
            if (!lastOption.ValueType.Validate(token.Value))
            {
                throw new ArgumentValidityException();
            }

            lastOption.AddArgumentValue(token.Value);
            Token nextToken = tokens.NextToken();
            if (nextToken == null)
            {
                CheckSaturation(lastOption);
                ParsedOption.Add(lastOption);
            } 
            else if (nextToken is OptionToken)
            {
                CheckSaturation(lastOption);
                ParsedOption.Add(lastOption);
                ParseOption(nextToken);
            }
            else if (nextToken is ArgumentToken)
            {
                if (IsFullSaturated(lastOption))
                {
                    ParseOptionArguments(nextToken);
                }
                else
                {
                    ParsedOption.Add(lastOption);
                    ParseParameters(nextToken);
                }
            }
            else if (nextToken is TreatAsArgumentToken)
            {
                CheckSaturation(lastOption);
                ParsedOption.Add(lastOption);
                ParseParameters(tokens.NextToken());
            }
        }

        Option GetOptionByToken(Token token)
        {
            return options.Where(opt => opt.Switches.Contains(token.Value)).FirstOrDefault();
        }

        void CheckSaturation(Option option)
        {
            bool minimalSaturation = option.ArgumentsCount >= option.Arity.MinimalOccurs;
            if (!minimalSaturation)
            {
                throw new RequiredArgumentIsMissingException(option.Switches.First());
            } 
        }

        bool IsFullSaturated(Option option)
        {
             bool minimalSaturation = lastOption.ArgumentsCount >= lastOption.Arity.MinimalOccurs;
             bool maximalSaturation = lastOption.ArgumentsCount >= lastOption.Arity.MaximalOccurs;
             return (!minimalSaturation || (minimalSaturation && !maximalSaturation));
        }



    }
}
