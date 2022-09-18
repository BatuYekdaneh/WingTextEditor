using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WingTextEditor.MVVM.Model;
using static System.Net.Mime.MediaTypeNames;

namespace WingTextEditor.Core
{
    public class Language
    {
        public ExecuteType executeType { get; set; }
        public List<string> primitiveTypes { get; set; }
        public List<string> keyWords { get; set; }
        public List<string> declaring { get; set; }


        public Language(ExecuteType executeType)
        {
            this.executeType = executeType;
            primitiveTypes = new List<string>()
            {
                "int","string","bool"
            };
            keyWords = new List<string>()
            {
                "if","else","const","let","array"
            };
            declaring = new List<string>()
            {
                "let {variableName} is {object}",
                "array {variableName} is {primitiveTypes} type",
                "{primitiveTypes} {variableName} is {object}",
            };

        }
        public string Execute(ObservableCollection<TabControlModel> executedCollection)
        {
            string data = executedCollection[0].PageText;
            StringBuilder sb = new StringBuilder();
            var datas = TokenStream(data);
            if (datas.errors != null)
            {
                return datas.errors[0].ToString();
            }
            var otherDatas = Parse(datas.tokens);
            if (otherDatas.errors != null)
            {
                return otherDatas.errors[0].ToString();
            }
            foreach (Token tokens in datas.tokens)
            {
                sb.AppendLine(tokens.ToString());
            }

            return sb.ToString();
        }

        public (List<Error> errors, StringBuilder builder) Parse(List<Token> tokens)
        {
            int length = tokens.Count;
            int pos = 0;
            List<Error> errors = new List<Error>();
            StringBuilder stringBuilder = new StringBuilder();
            List<Variable<object>> variables = new List<Variable<object>>();

            while (pos < length)
            {
                //   MessageBox.Show(tokens[pos].value);
                Token token = tokens[pos];

                if (token.type == "operator" && token.value == "+")
                {
                    if (length > 2 && tokens[pos + 1].type == "int" && tokens[pos - 1].type == "int")
                    {
                        /*stringBuilder.Append(int.Parse(tokens[pos + 1].value)+
                            int.Parse(tokens[pos - 1].value));*/
                        tokens[pos - 1].value = (int.Parse(tokens[pos + 1].value) +
                            int.Parse(tokens[pos - 1].value)).ToString();
                    }
                    else
                    {
                        errors.Add(new Error("Invalid Operation"));
                        return (errors, null);
                    }
                    pos += 2;


                }
                else if (token.type == "keyword" && token.value == "print")
                {

                    if (length != pos + 1 && length > 1 && (tokens[pos + 1].type == "str" ||
                        tokens[pos + 1].type == "int" || tokens[pos + 1].type == "customVariableName"))
                    {
                        if (tokens[pos + 1].type == "customVariableName")
                        {
                            int index = -1;
                            for (int i = 0; i < variables.Count; i++)
                            {
                                if (variables[i].name == tokens[pos + 1].value)
                                    index = i;
                            }
                            if (index == -1)
                            {
                                errors.Add(new Error("No variable found"));
                                return (errors, null);
                            }
                            stringBuilder.Append(variables[index].value);
                            pos += 2;
                            continue;
                        }



                        stringBuilder.Append(tokens[pos + 1].value);
                    }


                    else
                    {
                        errors.Add(new Error("Expected string or number"));
                        return (errors, null);
                    }


                    pos += 2;
                }
                else if (token.type == "keyword" && token.value == "printf")
                {
                    if (length > 1 && (tokens[pos + 1].type == "str" ||
                        tokens[pos + 1].type == "int"))
                        stringBuilder.Append(tokens[pos + 1].value + "\n");
                    else
                    {
                        errors.Add(new Error("Expected string or number"));
                        return (errors, null);
                    }
                    pos += 2;
                }
                else if (token.type == "variableName" && token.value == "let")
                {
                    bool isValid = pos + 3 < tokens.Count && tokens[pos + 1].type == "customVariableName" &&
                        tokens[pos + 2].type == "keyword" && tokens[pos + 2].value == "is" && (tokens[pos + 3].type == "str"
                        || tokens[pos + 3].type == "int");
                    if (isValid)
                    {
                        bool valid = true;
                        for (int i = 0; i < variables.Count; i++)
                        {
                            if (variables[i].name == tokens[pos + 1].value)
                                valid = false;

                        }
                        if (!valid)
                        {
                            errors.Add(new Error("Variable is already defined"));
                            return (errors, null);
                        }

                        if (tokens[pos + 3].type == "int")
                            variables.Add(new Variable<object>("int", int.Parse(tokens[pos + 3].value), tokens[pos + 1].value));
                        else
                            variables.Add(new Variable<object>("str", tokens[pos + 3].value, tokens[pos + 1].value));
                    }
                    else
                    {
                        errors.Add(new Error("Unexpected Error"));
                        return (errors, null);
                    }
                    pos += 4;
                }
                else
                {
                    pos++;
                }

            }

            return (null, stringBuilder);


        }



        public (List<Error> errors, List<Token> tokens) TokenStream(string data)
        {
            string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
            string numbers = "0123456789";
            int pos = 0;
            int length = data.Length;
            string[] validKeywords = { "print", "final", "printf", "is" };
            char[] validOperators = { '+', '-', '=' };
            string[] variableNames = { "int", "string", "bool", "let" };

            List<Error> errors = new List<Error>();
            List<Token> tokens = new List<Token>();


            while (pos < length)
            {
                char currentChar = data[pos];

                if (currentChar == ' ' || currentChar == '\n')
                {
                    pos++;
                    continue;
                }
                else if (currentChar == '\"')
                {
                    string res = "";
                    pos++;

                    while (pos < length && data[pos] != '\"' && data[pos] != '\n')
                    {
                        res += data[pos];
                        pos++;
                    }
                    if (pos >= length || data[pos] != '\"')
                    {
                        errors.Add(new Error("Not a valid string"));
                        return (errors, null);
                    }
                    pos++;
                    tokens.Add(new Token("str", res));
                }
                else if (numbers.Contains(currentChar))
                {
                    string number = currentChar.ToString();
                    pos++;

                    while (pos < length && data[pos] != '\n' && numbers.Contains(data[pos]) && data[pos] != ' ')
                    {
                        number += data[pos];
                        pos++;
                    }
                    if (pos < length && !(!validChars.Contains(data[pos]) || data[pos] == ' '))
                    {
                        errors.Add(new Error("Not a valid number"));
                        return (errors, null);
                    }


                    tokens.Add(new Token("int", number));
                }
                else if (validChars.Contains(currentChar))
                {
                    string res = currentChar.ToString();
                    pos++;

                    while (pos < length && data[pos] != '\n' && (validChars.Contains(data[pos]) || numbers.Contains(data[pos])) && data[pos] != ' ')
                    {
                        res += data[pos];
                        pos++;
                    }
                    if (!validKeywords.Contains(res))
                    {
                        if (variableNames.Contains(res))
                        {
                            pos++;
                            tokens.Add(new Token("variableName", res));
                            continue;
                        }

                        /*  errors.Add(new Error("Syntax Error"));
                          return (errors, null);*/
                        tokens.Add(new Token("customVariableName", res));
                        pos++;
                        continue;

                    }
                    pos++;

                    tokens.Add(new Token("keyword", res));

                }
                else if (validOperators.Contains(currentChar))
                {
                    pos++;

                    tokens.Add(new Token("operator", currentChar.ToString()));
                }
                else
                {
                    pos++;
                }
            }
            return (null, tokens);
        }

    }

    public class Variable<T>
    {
        public string type;
        public T value;
        public string name;

        public Variable(string type, T value, string name)
        {
            this.type = type;
            this.value = value;
            this.name = name;
        }
    }

    public class Token
    {
        public string type;
        public string value;

        public Token(string type, string value)
        {
            this.type = type;
            this.value = value;
        }

        public override string? ToString()
        {
            return $"{{type: \"{type}\" , value: \"{value}\"}}\n";
        }
    }
    public class Error
    {
        public string message;

        public Error(string message)
        {
            this.message = message;
        }

        public override string? ToString()
        {
            return message;
        }
    }


    public enum ExecuteType
    {
        CStyle,
        JavaStyle,
    }



}
