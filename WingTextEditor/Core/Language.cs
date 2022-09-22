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
            List<Variable<object>> variables = new List<Variable<object>>();
            var datas = TokenStream(data);
            if (datas.errors != null)
            {
                return datas.errors[0].ToString();
            }
            var otherDatas = Parse(datas.tokens,variables);
            if (otherDatas.errors != null)
            {
                return otherDatas.errors[0].ToString();
            }
            foreach (Token tokens in datas.tokens)
            {
                sb.AppendLine(tokens.ToString());
            }
            

            return otherDatas.builder.ToString();
        }

        public (List<Error> errors, StringBuilder builder) Parse(List<Token> tokens, List<Variable<object>> variables)
        {
            int length = tokens.Count;
            int pos = 0;
            List<Error> errors = new List<Error>();
            StringBuilder stringBuilder = new StringBuilder();

            List<Token> InBracketTokens(int temp , string bracketType)
            {
                string bracket = "";
                string bracketReverse = "";
                if (bracketType == "(")
                {
                    bracket = "(";
                    bracketReverse = ")";
                }
                if (bracketType == "{")
                {
                    bracket = "{";
                    bracketReverse = "}";
                }

                int tempNumber = temp + 2;
                int bracketAmount = 1;
                List<Token> tokenList = new List<Token>();
                while ((bracketAmount > 1 || tokens[tempNumber].value != bracketReverse))
                {
                    if (bracketAmount > 1 && tokens[tempNumber].value == bracketReverse)
                    {
                        bracketAmount--;
                    }

                    if (tokens[tempNumber].value == bracket)
                        bracketAmount++;
                    tokenList.Add(tokens[tempNumber]);
                    tempNumber++;
                }
                return tokenList;
            }
            List<Token> InBracketTokensReverse(int temp, string bracketType)
            {
                string bracket = "";
                string bracketReverse = "";
                if(bracketType == "(")
                {
                    bracket="(";
                    bracketReverse=")";
                }
                if (bracketType == "{")
                {
                    bracket = "{";
                    bracketReverse = "}";
                }


                int tempNumber = temp - 2;
                int bracketAmount = 1;
                List<Token> tokenList = new List<Token>();
                while ((bracketAmount > 1 || tokens[tempNumber].value != bracket))
                {
                    if (bracketAmount > 1 && tokens[tempNumber].value == bracket)
                    {
                        bracketAmount--;
                    }

                    if (tokens[tempNumber].value == bracketReverse)
                        bracketAmount++;
                    tokenList.Add(tokens[tempNumber]);
                    tempNumber--;
                }
                return tokenList;
            }


            Variable<object> getVariable(int index)
            {
                int indexArray = -1;
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i].name == tokens[index].value)
                        indexArray = i;
                }
                if (indexArray == -1)
                {
                    errors.Add(new Error("No variable found"));
                    return null;
                }
                return variables[indexArray];
            }
            Variable<object> getVariableWithoutError(int index)
            {
                int indexArray = -1;
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i].name == tokens[index].value)
                        indexArray = i;
                }
                if (indexArray == -1)
                {
                    return null;
                }
                return variables[indexArray];
            }

            while (pos < length)
            {
                length = tokens.Count;
                if (pos >= length)
                    break;
                Token token = tokens[pos];

                if (token.type == "keyword" && (token.value == "print" || token.value=="printf"))
                {
                    if (length != pos + 1 && length > 1 && (tokens[pos + 1].type == "str" ||
                        tokens[pos + 1].type == "int" || tokens[pos + 1].type == "customVariableName" ||
                        tokens[pos + 1].type == "bracket"))
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
                            if(token.value=="print")
                                stringBuilder.Append(variables[index].value);
                            else
                                stringBuilder.Append(variables[index].value+"\n");
                            pos += 2;
                            continue;
                        }
                        else if (tokens[pos + 1].type == "bracket")
                        {
                            List<Token> tokenList = InBracketTokens(pos,"(");
                            List<Token> tokenListCopy = new();
                            tokenListCopy.AddRange(tokenList);
                            var datas = Parse(tokenList, variables);
                            List<Error> errors1 = new List<Error>();
                            if (datas.errors is not null)
                                errors.AddRange(datas.errors);
                            int i = 1;
                            while(tokens[pos + i].type == "bracket")
                            {
                                i++;
                            }
                            tokenListCopy.Remove(tokens[pos + i]);
                            foreach (Token tokensArray in tokenListCopy)
                            {
                                tokens.Remove(tokensArray);
                            }
                            if(tokens[pos + 2].type == "customVariableName")
                            {
                                Variable<object> variable = getVariable(pos+i);
                                if (variable is null)
                                    return (errors, null);

                                if (token.value == "print")
                                    stringBuilder.Append(variable.value);
                                else
                                    stringBuilder.Append(variable.value + "\n");

                                if (variable.queue.Count != 0)
                                {
                                    variable.value = variable.getFirstQueueElement();
                                    variable.queue.Clear();
                                }
                            }
                            else
                            {
                                if (token.value == "print")
                                    stringBuilder.Append(tokens[pos + 2].value);
                                else
                                    stringBuilder.Append(tokens[pos + 2].value + "\n");

                            }
                        }
                        else
                        {
                            if (token.value == "print")
                                stringBuilder.Append(tokens[pos + 1].value);
                            else
                                stringBuilder.Append(tokens[pos + 1].value + "\n");
                            if(pos+2<length && tokens[pos + 2].type == "operator")
                            {
                                errors.Add(new Error("Syntax Error"));
                                return (errors, null);
                            }
                        }

                    }


                    else
                    {
                        errors.Add(new Error("Expected string or number"));
                        return (errors, null);
                    }


                    pos += 2;
                }
                else if(token.type == "operator" && (token.value == "+" || token.value == "-" ||
                    token.value == "*" || token.value == "/"))
                {
                    int index = pos;
                    string operatorName=tokens[index].value;

                    bool isLeftVariableValid = true;
                    bool isRightVariableValid = true;



                    Variable<object> leftVariable;
                    Variable<object> rightVariable;
                    


                    try
                    {
                    leftVariable = getVariableWithoutError(pos - 1);
                    rightVariable = getVariableWithoutError(pos + 1);
                    }
                    catch (Exception e)
                    {
                        errors.Add(new Error("Operator Error"));
                        return (errors, null);
                    }


                    if(leftVariable is null || leftVariable.type!="int")
                        isLeftVariableValid = false;
                    if(rightVariable is null || rightVariable.type != "int")
                        isRightVariableValid = false;


                    if(length>2 && (tokens[pos-1].type=="int" || isLeftVariableValid) 
                        && (tokens[pos + 1].type == "int" 
                        || isRightVariableValid))
                    {


                        if (tokens[pos + 1].type == "customVariableName" && tokens[pos - 1].type == "customVariableName")
                        {
                            Variable<object> variable1 = getVariable(pos - 1);
                            Variable<object> variable2 = getVariable(pos + 1);
                            variable1.AddQueueElement(Convert.ToInt32(variable1.value));
                            variable2.AddQueueElement(Convert.ToInt32(variable2.value));
                            switch (operatorName)
                            {
                                case "+":
                                    variable1.value = Convert.ToInt32(variable1.value) + Convert.ToInt32(variable2.queue.Peek()) + "";
                                    break;
                                    case "-":
                                    variable1.value = Convert.ToInt32(variable1.value) - Convert.ToInt32(variable2.queue.Peek()) + "";
                                    break ;
                                case "*":
                                    variable1.value = Convert.ToInt32(variable1.value) * Convert.ToInt32(variable2.queue.Peek()) + "";
                                    break;
                                case "/":
                                    variable1.value = Convert.ToInt32(variable1.value) / Convert.ToInt32(variable2.queue.Peek()) + "";
                                    break;
                            }
                        }
                        else if (tokens[pos - 1].type == "customVariableName")
                        {
                            Variable<object> variable = getVariable(pos - 1);
                            if (variable is null)
                                return (errors,null);

                            variable.AddQueueElement(Convert.ToInt32(variable.value));
                            switch (operatorName)
                            {
                                case "+":
                                    variable.value = Convert.ToInt32(variable.value) + int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "-":
                                    variable.value = Convert.ToInt32(variable.value) - int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "*":
                                    variable.value = Convert.ToInt32(variable.value) * int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "/":
                                    variable.value = Convert.ToInt32(variable.value) / int.Parse(tokens[pos + 1].value) + "";
                                    break;
                            }

                        }
                        else if(tokens[pos + 1].type == "customVariableName")
                        {
                            Variable<object> variable = getVariable(pos + 1);
                            if(variable is null)
                                return(errors,null); 

                            variable.AddQueueElement(Convert.ToInt32(variable.value));
                            switch (operatorName)
                            {
                                case "+":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) + Convert.ToInt32(variable.queue.Peek()) + "";
                                    break;
                                case "-":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) - Convert.ToInt32(variable.queue.Peek()) + "";
                                    break;
                                case "*":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) * Convert.ToInt32(variable.queue.Peek()) + "";
                                    break;
                                case "/":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) / Convert.ToInt32(variable.queue.Peek()) + "";
                                    break;
                            }
                        }
                        else
                        {
                            switch (operatorName)
                            {
                                case "+":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) + int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "-":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) - int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "*":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) * int.Parse(tokens[pos + 1].value) + "";
                                    break;
                                case "/":
                                    tokens[pos - 1].value = int.Parse(tokens[pos - 1].value) / int.Parse(tokens[pos + 1].value) + "";
                                    break;
                            }

                        }
                        tokens.Remove(tokens[index]);
                        tokens.Remove(tokens[index]);


                    }
                    else if(length > 2 && (tokens[pos + 1].type == "bracket" || tokens[pos - 1].type == "bracket"))
                    {
                        
                        if(tokens[pos + 1].type == "bracket")
                        {
                            List<Token> list = InBracketTokens(pos , "(");
                            Parse(list,variables);
                        }
                        if(tokens[pos - 1].type == "bracket")
                        {
                            List<Token> list1 = InBracketTokensReverse(pos , "(");
                            Parse(list1, variables);
                        }

                        int i = 1;
                        while (tokens[pos + i].type == "bracket")
                        {
                            i++;
                        }
                        isLeftVariableValid = true;
                        isRightVariableValid = true;
                        int backIndex = 1;
                        while (tokens[pos - backIndex].type == "bracket")
                        {
                            backIndex++;
                        }
                        leftVariable = getVariableWithoutError(pos - backIndex);
                        rightVariable = getVariableWithoutError(pos + i);
                        if (leftVariable is null || leftVariable.type != "int")
                            isLeftVariableValid = false;
                        else if (rightVariable is null || rightVariable.type != "int")
                            isRightVariableValid = false;



                        if (length > 2 && (tokens[pos - backIndex].type == "int" || isLeftVariableValid)
                            && (tokens[pos + i].type == "int"
                            || isRightVariableValid))
                        {
                            switch (operatorName)
                            {
                                case "+":
                                    if (tokens[pos + i].type == "customVariableName" && tokens[pos - backIndex].type == "customVariableName")
                                    {
                                        Variable<object> variable1 = getVariable(pos - backIndex);
                                        Variable<object> variable2 = getVariable(pos + i);
                                        variable1.AddQueueElement(Convert.ToInt32(variable1.value));
                                        variable2.AddQueueElement(Convert.ToInt32(variable2.value));
                                        variable1.value = Convert.ToInt32(variable1.value) + Convert.ToInt32(variable2.queue.Peek()) + "";
                                    }
                                    else if (tokens[pos - backIndex].type == "customVariableName" && isLeftVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos - backIndex);
                                        if (variable is null)
                                            return (errors, null);
                                        variable.AddQueueElement(Convert.ToInt32(variable.value));

                                        variable.value = Convert.ToInt32(variable.value) + int.Parse(tokens[pos + i].value) + "";
                                    }
                                    else if (tokens[pos + i].type == "customVariableName" && isRightVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos + i);
                                        if (variable is null)
                                            return (errors, null);

                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) + Convert.ToInt32(variable.value) + "";
                                    }
                                    else
                                    {
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) + int.Parse(tokens[pos + i].value) + "";
                                    }
                                    break;
                                case "-":
                                    if (tokens[pos + i].type == "customVariableName" && tokens[pos - backIndex].type == "customVariableName")
                                    {
                                        Variable<object> variable1 = getVariable(pos - backIndex);
                                        Variable<object> variable2 = getVariable(pos + i);
                                        variable1.AddQueueElement(Convert.ToInt32(variable1.value));
                                        variable2.AddQueueElement(Convert.ToInt32(variable2.value));
                                        variable1.value = Convert.ToInt32(variable1.value) - Convert.ToInt32(variable2.queue.Peek()) + "";
                                    }
                                    else if (tokens[pos - backIndex].type == "customVariableName" && isLeftVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos - backIndex);
                                        if (variable is null)
                                            return (errors, null);
                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        variable.value = Convert.ToInt32(variable.value) - int.Parse(tokens[pos + i].value) + "";
                                    }
                                    else if (tokens[pos + i].type == "customVariableName" && isRightVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos + i);
                                        if (variable is null)
                                            return (errors, null);

                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) - Convert.ToInt32(variable.value) + "";
                                    }
                                    else
                                    {
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) - int.Parse(tokens[pos + i].value) + "";
                                    }
                                    break;
                                case "*":
                                    if (tokens[pos + i].type == "customVariableName" && tokens[pos - backIndex].type == "customVariableName")
                                    {
                                        Variable<object> variable1 = getVariable(pos - backIndex);
                                        Variable<object> variable2 = getVariable(pos + i);
                                        variable1.AddQueueElement(Convert.ToInt32(variable1.value));
                                        variable2.AddQueueElement(Convert.ToInt32(variable2.value));
                                        variable1.value = Convert.ToInt32(variable1.value) * Convert.ToInt32(variable2.queue.Peek()) + "";
                                    }
                                    else if (tokens[pos - backIndex].type == "customVariableName" && isLeftVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos - backIndex);
                                        if (variable is null)
                                            return (errors, null);
                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        variable.value = Convert.ToInt32(variable.value) * int.Parse(tokens[pos + i].value) + "";
                                    }
                                    else if (tokens[pos + i].type == "customVariableName" && isRightVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos + i);
                                        if (variable is null)
                                            return (errors, null);

                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) * Convert.ToInt32(variable.value) + "";
                                    }
                                    else
                                    {
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) * int.Parse(tokens[pos + i].value) + "";
                                    }
                                    break;
                                case "/":
                                    if (tokens[pos + i].type == "customVariableName" && tokens[pos - backIndex].type == "customVariableName")
                                    {
                                        Variable<object> variable1 = getVariable(pos - backIndex);
                                        Variable<object> variable2 = getVariable(pos + i);
                                        variable1.AddQueueElement(Convert.ToInt32(variable1.value));
                                        variable2.AddQueueElement(Convert.ToInt32(variable2.value));
                                        variable1.value = Convert.ToInt32(variable1.value) / Convert.ToInt32(variable2.queue.Peek()) + "";
                                    }
                                    else if (tokens[pos - backIndex].type == "customVariableName" && isLeftVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos - backIndex);
                                        if (variable is null)
                                            return (errors, null);
                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        variable.value = Convert.ToInt32(variable.value) / int.Parse(tokens[pos + i].value) + "";
                                    }
                                    else if (tokens[pos + i].type == "customVariableName" && isRightVariableValid)
                                    {
                                        Variable<object> variable = getVariable(pos + i);
                                        if (variable is null)
                                            return (errors, null);

                                        variable.AddQueueElement(Convert.ToInt32(variable.value));
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) / Convert.ToInt32(variable.value) + "";
                                    }
                                    else
                                    {
                                        
                                        tokens[pos - backIndex].value = int.Parse(tokens[pos - backIndex].value) / int.Parse(tokens[pos + i].value) + "";
                                    }
                                    break;

                            }

                        }
                        int bracketAmount =1;
                        int tempNumber = index;
                        List<Token> tokenList = new();   
                        while (tempNumber<tokens.Count)
                        {
                            if (bracketAmount > 1 && tokens[tempNumber].value == ")")
                            {
                                bracketAmount--;
                            }
                            if(bracketAmount ==1 && tokens[tempNumber].value == ")")
                            {
                                break;
                            }


                            if (tokens[tempNumber].value == "(")
                                bracketAmount++;
                            tokenList.Add(tokens[tempNumber]);
                            tempNumber++;
                        }
                        foreach (Token token1 in tokenList)
                            tokens.Remove(token1);
                            

                    }
                    else
                    {
                        errors.Add(new Error("Operator Error"));
                        return (errors, null);
                    }
                }
                else if (token.type == "variableName")
                {
                    int index = 0;
                    object value=0;
                    bool isValueChange = false;
                    if (tokens[pos + 3].type == "bracket" || tokens[pos + 3].type == "customVariableName")
                    {
                        if(tokens[pos + 3].type == "bracket")
                        {
                            List<Token> tokenList = InBracketTokens(pos + 2 , "(");
                            List<Token> tokenListCopy = new();
                            tokenListCopy.AddRange(tokenList);
                            var datas = Parse(tokenList, variables);
                            if (datas.errors is not null)
                                errors.AddRange(datas.errors);
                            index = 1;
                            while (tokens[pos + 3 + index].type == "bracket")
                            {
                                index++;
                            }
                            tokenListCopy.Remove(tokens[pos + 3 + index]);
                            foreach (Token tokensArray in tokenListCopy)
                            {
                                tokens.Remove(tokensArray);
                            }
                        }
                        if (tokens[pos + 3 + index].type == "customVariableName")
                        {
                            Variable<object> variable = getVariable(pos + 3 + index);
                            if (variable is null)
                                return (errors, null);


                                value=variable.value;
                            isValueChange=true;
                            if (variable.queue.Count != 0)
                            {
                                variable.value = variable.getFirstQueueElement();
                                variable.queue.Clear();
                            }
                        }
                        else
                        {
                            isValueChange=true;
                            value =tokens[pos + 3 + index].value;
                        }
                    }

                    Variable<object> variable1 = getVariableWithoutError(pos + 3 + index);



                    bool isLet = token.value == "let" && (tokens[pos + 3 + index].type == "str"
    || tokens[pos + 3 + index].type == "int" || tokens[pos + 3 + index].type == "bool" ||
                        (tokens[pos + 3 + index].type == "customVariableName")) ;
                    bool isInt = token.value == "int" && (tokens[pos + 3 + index].type == "int" ||
                        (tokens[pos + 3 + index].type=="customVariableName" && variable1.type=="int"));
                    bool isBool=token.value == "bool" && (tokens[pos + 3 + index].type == "bool" ||
                        (tokens[pos + 3 + index].type == "customVariableName" && variable1.type == "bool"));
                    bool isArray = token.value == "array" && (tokens[pos + 3 + index].value == "int" ||
                        tokens[pos + 3 + index].value == "string" || tokens[pos + 3 + index].value == "bool");
                    bool isString = token.value == "string" && (tokens[pos + 3 + index].type == "str" ||
                        (tokens[pos + 3 + index].type == "customVariableName" && variable1.type == "str"));
                    bool isValid = pos + 3 + index < tokens.Count && tokens[pos + 1].type == "customVariableName" &&
    tokens[pos + 2].type == "keyword" && tokens[pos + 2].value == "is" &&  (isLet || isInt || isString || isBool || isArray);
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
                        if (tokens[pos + 3 + index].type == "int"
                            || (tokens[pos + 3 + index].type == "customVariableName" && variable1.type == "int"))
                            if(isValueChange)
                                variables.Add(new Variable<object>("int", value, tokens[pos + 1].value));
                            else
                                variables.Add(new Variable<object>("int", int.Parse(tokens[pos + 3 + index].value), tokens[pos + 1].value));
                        else if (tokens[pos + 3 + index].type == "bool"
                            || (tokens[pos + 3 + index].type == "customVariableName" && variable1.type == "bool"))
                            if (isValueChange)
                                variables.Add(new Variable<object>("bool", value, tokens[pos + 1].value));
                            else
                                variables.Add(new Variable<object>("bool", tokens[pos + 3 + index].value, tokens[pos + 1].value));
                        else if (token.value == "array")
                        {
                            if(tokens[pos + 3].value == "int" )
                                variables.Add(new Variable<object>("array", new List<int>(), tokens[pos + 1].value));
                            else if (tokens[pos + 3].value == "string")
                                variables.Add(new Variable<object>("array", new List<string>(), tokens[pos + 1].value));
                            else if (tokens[pos + 3].value == "bool")
                                variables.Add(new Variable<object>("array", new List<bool>(), tokens[pos + 1].value));
                        }
                        else if (tokens[pos + 3 + index].type == "str"
                            || (tokens[pos + 3 + index].type == "customVariableName" && variable1.type == "str"))
                            if(isValueChange)
                                variables.Add(new Variable<object>("str", value, tokens[pos + 1].value));
                            else
                                variables.Add(new Variable<object>("str", tokens[pos + 3 + index].value, tokens[pos + 1].value));

                        if(pos-1>=0 && tokens[pos-1].value == "final")
                        variables[variables.Count - 1].isImmutable = true;
                    }
                    else
                    {
                        if(token.value == "int" && !isInt)
                            errors.Add(new Error("Integer Expected"));
                        else if (token.value == "string" && !isString)
                            errors.Add(new Error("String Expected"));
                        else if (token.value == "bool" && !isBool)
                            errors.Add(new Error("Boolean Expected"));
                        else if (token.value == "array" && !isArray)
                            errors.Add(new Error("Variable Type Expected"));
                        else
                        errors.Add(new Error("Unexpected Error"));

                        return (errors, null);
                    }
                    pos += 4;
                }
                else if(pos+1<length && token.type == "customVariableName" && tokens[pos+1].value=="is")
                {
                    Variable<object> variable = getVariable(pos);
                    if (variable == null)
                        return (errors, null);
                    if(variable.type != "array")
                    {
                        if (tokens[pos + 1].value == "is")
                        {
                            if (variable.isImmutable)
                            {
                                errors.Add(new Error("Variable can't be changed since it is an immutable variable"));
                                return (errors, null);
                            }


                            if (variable.type == "int")
                            {
                                if (tokens[pos + 2].type == "int")
                                {
                                    variable.value = int.Parse(tokens[pos + 2].value);
                                }
                                else
                                {
                                    errors.Add(new Error("Integer Expected"));
                                    return (errors, null);
                                }
                            }
                            else if (variable.type == "str")
                            {
                                if (tokens[pos + 2].type == "string")
                                {
                                    variable.value = tokens[pos + 2].value;
                                }
                                else
                                {
                                    errors.Add(new Error("String Expected"));
                                    return (errors, null);
                                }
                            }
                            else if (variable.type == "bool")
                            {
                                if (tokens[pos + 2].type == "bool")
                                {
                                    variable.value = bool.Parse(tokens[pos + 2].value);
                                }
                                else
                                {
                                    errors.Add(new Error("Boolean Expected"));
                                    return (errors, null);
                                }
                            }
                        }
                        else
                        {
                            errors.Add(new Error("Wrong variable declaring"));
                            return (errors, null);
                        }
                    }
                    else
                    {
                        errors.Add(new Error("Arrays are cannot be changed"));
                        return (errors, null);
                    }


                    pos+=3;


                }
                else if(pos+1<length && token.type == "customVariableName"
                    && tokens[pos + 1].type=="keyword" && (tokens[pos+1].value=="get" || tokens[pos + 1].value == "add"))
                {
                    Variable<object> variable = getVariable(pos);
                    if (variable is null)
                        return (errors, null);

                    string type=variable.value.GetType().GetGenericArguments()[0].Name;

                    

                    if(tokens[pos + 1].value == "add")
                    {
                        bool isIntValid = type=="Int32" && tokens[pos + 2].type=="int";
                        bool isStringValid = type == "String" && tokens[pos + 2].type == "str";
                        bool isBooleanValid = type == "Boolean" && tokens[pos + 2].type == "bool";
                        bool isValid = isBooleanValid || isStringValid || isIntValid;
                        if (isValid)
                        {
                            if (isIntValid)
                            {
                                (variable.value as List<int>).Add(int.Parse(tokens[pos +2].value));
          
                            }
                            else if (isStringValid)
                            {
                                (variable.value as List<string>).Add(tokens[pos + 2].value);
                            }
                            else if (isBooleanValid)
                            {
                                (variable.value as List<bool>).Add(bool.Parse(tokens[pos + 2].value));
                            }
                        }
                        else
                        {
                            errors.Add(new Error("Array Syntax Error"));
                            return (errors, null);
                        }

                    }
                    else if(tokens[pos + 1].value == "get")
                    {
                        bool isIntValid = type == "Int32" ;
                        bool isStringValid = type == "String";
                        bool isBooleanValid = type == "Boolean";
                        bool isValid = isBooleanValid || isStringValid || isIntValid;
                        if (isValid)
                        {
                            if(tokens[pos + 2].value != "all" && tokens[pos + 2].type=="int")
                            {
                                int index = int.Parse(tokens[pos + 2].value);
                                if (isIntValid)
                                {
                                    List<int> list = (variable.value as List<int>);
                                    if (index < list.Count)
                                    {

                                        tokens[pos].type = "int";
                                        tokens[pos].value = list[index].ToString();

                                    }
                                    else
                                    {
                                        errors.Add(new Error("Array Index Out Of Bound"));
                                        return (errors, null);
                                    }
                                }
                                else if (isStringValid)
                                {
                                    List<string> list = (variable.value as List<string>);
                                    if (index < list.Count)
                                    {

                                        tokens[pos].type = "str";
                                        tokens[pos].value = list[index].ToString();

                                    }
                                    else
                                    {
                                        errors.Add(new Error("Array Index Out Of Bound"));
                                        return (errors, null);
                                    }
                                }
                                else if (isBooleanValid)
                                {
                                    List<bool> list = (variable.value as List<bool>);
                                    if (index < list.Count)
                                    {

                                        tokens[pos].type = "bool";
                                        tokens[pos].value = list[index].ToString();

                                    }
                                    else
                                    {
                                        errors.Add(new Error("Array Index Out Of Bound"));
                                        return (errors, null);
                                    }
                                }
                            }
                            else if(tokens[pos + 2].value == "all")
                            {

                                tokens[pos].type = "str";
                                if (isIntValid)
                                {
                                    List<int> list = (variable.value as List<int>);
                                    tokens[pos].type = "str";
                                    tokens[pos].value = getStringOfList<int>(list);

                                }
                                else if (isStringValid)
                                {
                                    List<string> list = (variable.value as List<string>);
                                    tokens[pos].type = "str";
                                    tokens[pos].value = getStringOfList<string>(list);
                                }
                                else if (isBooleanValid)
                                {
                                    List<bool> list = (variable.value as List<bool>);
                                    tokens[pos].type = "str";
                                    tokens[pos].value = getStringOfList<bool>(list);

                                }

                                string getStringOfList<T>(List<T> list)
                                {
                                    StringBuilder builder = new();
                                    builder.Append("[");
                                    for(int i = 0; i < list.Count; i++)
                                    {
                                        builder.Append(list[i]);
                                        if(i==list.Count-1)
                                            builder.Append("]");
                                        else
                                            builder.Append(" ,");
                                    }
                                    if (list.Count == 0)
                                        return "The Array is Empty";
                                    return builder.ToString();
                                }
                            }
                            else
                            {
                                errors.Add(new Error("Array Syntax Error"));
                                return (errors, null);
                            }
                        }
                        else
                        {
                            errors.Add(new Error("Array Syntax Error"));
                            return (errors, null);
                        }
                    }

                    pos += 3;
                }
                else if (token.type == "booleanOperator")
                {
                    int indexForth = 0;
                    int indexBack = 0;
                    object valueBack = 0;
                    object valueForth = 0;
                    bool isValueChangeBack = false;
                    bool isValueChangeForth = false;


                    if (tokens[pos - 1].type == "bracket" || tokens[pos - 1].type == "customVariableName")
                    {
                        while (tokens[pos - 1 - indexBack].type == "bracket")
                        {
                            indexBack++;
                        }
                        if (tokens[pos - 1 - indexBack].type == "customVariableName")
                        {
                            Variable<object> variable = getVariable(pos - 1 - indexBack);
                            if (variable is null)
                                return (errors, null);


                            valueBack = variable.value;
                            isValueChangeBack = true;
                            if (variable.queue.Count != 0)
                            {
                                variable.value = variable.getFirstQueueElement();
                                variable.queue.Clear();
                            }
                        }
                        else
                        {
                            isValueChangeBack = true;
                            valueBack = tokens[pos - 1 - indexBack].value;
                        }
                    }
                    if (tokens[pos + 1].type == "bracket" || tokens[pos + 1].type == "customVariableName")
                    {
                        if (tokens[pos + 1].type == "bracket")
                        {
                            List<Token> tokenList = InBracketTokens(pos , "(");
                            List<Token> tokenListCopy = new();
                            tokenListCopy.AddRange(tokenList);
                            var datas = Parse(tokenList, variables);
                            if (datas.errors is not null)
                                errors.AddRange(datas.errors);
                            indexForth = 1;
                            while (tokens[pos + 1 + indexForth].type == "bracket")
                            {
                                indexForth++;
                            }
                            tokenListCopy.Remove(tokens[pos + 1 + indexForth]);
                            foreach (Token tokensArray in tokenListCopy)
                            {
                                tokens.Remove(tokensArray);
                            }
                        }
                        if (tokens[pos + 1 + indexForth].type == "customVariableName")
                        {
                            Variable<object> variable = getVariable(pos + 1 + indexForth);
                            if (variable is null)
                                return (errors, null);

                            valueForth = variable.value;
                            isValueChangeForth = true;
                            if (variable.queue.Count != 0)
                            {
                                variable.value = variable.getFirstQueueElement();
                                variable.queue.Clear();
                            }
                        }
                        else
                        {
                            isValueChangeForth = true;
                            valueForth = tokens[pos + 1 + indexForth].value;
                        }
                    }
                    Variable<object> variable1 = getVariableWithoutError(pos + 1 +indexForth);
                    Variable<object> variable2 = getVariableWithoutError(pos - 1 - indexBack);
                    bool isRightValid = true;
                    bool isLeftValid= true;
                    if (variable1 is null)
                        isRightValid=false;
                    if(variable2 is null)
                        isLeftValid=false;

                    if ((tokens[pos+1+indexForth].type == "int" || (tokens[pos + 1 + indexForth].type == "customVariableName" && variable1.type=="int")) 
                        && (tokens[pos - 1 - indexBack].type == "int" || (tokens[pos - 1 - indexBack].type == "customVariableName" && variable2.type == "int")))
                      {
                        void operatorUse(Func<Tuple<int, int>, bool> func)
                        {
                            if (isValueChangeBack && isValueChangeForth)
                            {

                                tokens[pos - 1 - indexBack].value = func(new Tuple<int,int>(Convert.ToInt32(valueBack), Convert.ToInt32(valueForth))).ToString().ToLower();
                                tokens[pos - 1 - indexBack].type = "bool";


                            }
                            else if (isValueChangeBack)
                            {
                                tokens[pos - 1 - indexBack].value = func(new Tuple<int, int>(Convert.ToInt32(valueBack) , int.Parse(tokens[pos + 1].value))).ToString().ToLower();
                                tokens[pos - 1 - indexBack].type = "bool";
                            }
                            else if (isValueChangeForth)
                            {
                                tokens[pos - 1 - indexBack].value = func(new Tuple<int, int>(int.Parse(tokens[pos - 1].value) , Convert.ToInt32(valueForth))).ToString().ToLower();
                                tokens[pos - 1 - indexBack].type = "bool";
                            }

                            else
                            {
                                tokens[pos - 1 - indexBack].value = func(new Tuple<int, int>(int.Parse(tokens[pos - 1].value) , int.Parse(tokens[pos + 1].value))).ToString().ToLower();
                                tokens[pos - 1 - indexBack].type = "bool";
                            }
                        }
                        if (token.value == ">")
                            operatorUse(x => x.Item1 > x.Item2);
                        else if(token.value == "<")
                            operatorUse(x => x.Item1 < x.Item2);
                        else if(token.value == "==")
                            operatorUse(x => x.Item1 == x.Item2);
                        else if(token.value == ">=")
                            operatorUse(x => x.Item1 >= x.Item2);
                        else if (token.value == ">=")
                            operatorUse(x => x.Item1 >= x.Item2);
                        else if (token.value == "<=")
                            operatorUse(x => x.Item1 <= x.Item2);
                        else if (token.value == "!=")
                            operatorUse(x => x.Item1 != x.Item2);
                    }
                    pos+=10;
                }
                else if(token.type=="keyword" && token.value == "if")
                {
                    int i = 1;

                    if (tokens[pos + 1].type == "bracket")
                    {
                        List<Token> tokenList = InBracketTokens(pos , "(");
                        List<Token> tokenListCopy = new();
                        tokenListCopy.AddRange(tokenList);
                        int tokenListCount = tokenListCopy.Count;
                        var datas = Parse(tokenList, variables);
                        if (datas.errors is not null)
                            errors.AddRange(datas.errors);
                        stringBuilder.Append(datas.builder.ToString());
                        while (tokens[pos + i].type == "bracket")
                        {
                            i++;
                        }

                        tokenListCopy.Remove(tokens[pos + i]);
                        foreach (Token tokensArray in tokenListCopy)
                        {
                            tokens.Remove(tokensArray);
                        }
                    }
                    if (i == 1)
                        i = 0;
                    else
                        i = 2;


                    if ((tokens[pos + 1 + i/2].type=="bool") && 
                        (tokens[pos + 2 + i].value == "then" && tokens[pos + 2 + i].type=="keyword") &&
                        tokens[pos + 3 + i].type == "bracket")
                    {

                        bool isValidElseIf = false;
                        List<Token> tokenList = InBracketTokens(pos + 2 + i , "{");
                        List<Token> tokenListCopy = new();
                        tokenListCopy.AddRange(tokenList);
                        int tokenListCount=tokenListCopy.Count;
                        if(tokens[pos + 1 + i/2].value == "true")
                        {
                            var datas = Parse(tokenList, new List<Variable<object>>(variables));
                            if (datas.errors is not null)
                            {
                                errors.AddRange(datas.errors);
                                return (errors, null);
                            }
                                
                            stringBuilder.Append(datas.builder.ToString());
                            int index = 1;
                            while (tokens[pos + 3 + i + index].type == "bracket")
                            {
                                index++;
                            }
                            tokenListCopy.Remove(tokens[pos + 3 + i + index]);
                            foreach (Token tokensArray in tokenListCopy)
                            {
                                tokens.Remove(tokensArray);
                            }
                            pos += 4 + 2 + i;
                        }
                        else
                        {
                            pos += 4 + 2 + i + tokenListCount - 1;
                            isValidElseIf = true;
                        }
                            
                        while (pos < tokens.Count && tokens[pos].value == "else if")
                        {
                            i = 1;

                            if (tokens[pos + 2].type == "bracket")
                            {
                                tokenList = InBracketTokens(pos, "(");
                                tokenListCopy = new();
                                tokenListCopy.AddRange(tokenList);
                                tokenListCount = tokenListCopy.Count;
                                var datas = Parse(tokenList, variables);
                                if (datas.errors is not null)
                                    errors.AddRange(datas.errors);
                                stringBuilder.Append(datas.builder.ToString());
                                while (tokens[pos + 1 + i].type == "bracket")
                                {
                                    i++;
                                }

                                tokenListCopy.Remove(tokens[pos + 1 + i]);
                                foreach (Token tokensArray in tokenListCopy)
                                {
                                    tokens.Remove(tokensArray);
                                }
                            }
                            if (i == 1)
                                i = 0;
                            else
                                i = 2;


                            if ((tokens[pos + 2 + i / 2].type == "bool") &&
                                (tokens[pos + 3 + i].value == "then" && tokens[pos + 3 + i].type == "keyword") &&
                                tokens[pos + 4 + i].type == "bracket")
                            {
                                tokenList = InBracketTokens(pos + 3 + i, "{");
                                tokenListCopy = new();
                                tokenListCopy.AddRange(tokenList);
                                tokenListCount = tokenListCopy.Count;
                                if (tokens[pos + 2 + i / 2].value == "true" && isValidElseIf)
                                {
                                    isValidElseIf=false;
                                    var datas = Parse(tokenList, new List<Variable<object>>(variables));
                                    if (datas.errors is not null)
                                    {
                                        errors.AddRange(datas.errors);
                                        return (errors, null);
                                    }

                                    stringBuilder.Append(datas.builder.ToString());
                                    int index = 1;
                                    while (tokens[pos + 4 + i + index].type == "bracket")
                                    {
                                        index++;
                                    }
                                    tokenListCopy.Remove(tokens[pos + 4 + i + index]);
                                    foreach (Token tokensArray in tokenListCopy)
                                    {
                                        tokens.Remove(tokensArray);
                                    }
                                    pos += 5 + 2 + i;
                                }
                                else
                                    pos += 5 + 2 + i + tokenListCount - 1;
                            }
                            else
                            {
                                errors.Add(new Error("Wrong if statement"));
                                return (errors, null);
                            }
                        }
                        if (pos< tokens.Count && tokens[pos].value == "else")
                        {
                                tokenList = InBracketTokens(pos , "{");
                                tokenListCopy = new();
                                tokenListCopy.AddRange(tokenList);
                                tokenListCount = tokenListCopy.Count;
                            if(isValidElseIf)
                            {

                                var datas = Parse(tokenList, new List<Variable<object>>(variables));
                                if (datas.errors is not null)
                                {
                                    errors.AddRange(datas.errors);
                                    return (errors, null);
                                }

                                stringBuilder.Append(datas.builder.ToString());
                                int index = 1;
                                while (tokens[pos + 1 + index].type == "bracket")
                                {
                                    index++;
                                }
                                tokenListCopy.Remove(tokens[pos + 1 + index]);
                                foreach (Token tokensArray in tokenListCopy)
                                {
                                    tokens.Remove(tokensArray);
                                }
                                pos += 4;
                                
                            }
                            else
                                pos += 4 + i + tokenListCount - 1;
                        }
                        
                    }
                    else if(token.type == "keyword" && token.value == "loop")
                    {

                    }
                    else
                    {
                        errors.Add(new Error("Wrong if statement"));
                        return (errors, null);
                    }
                }

                else
                {
                    pos++;
                }

            }
            if(errors.Count > 0)
                return (errors, null);
            else
                return (null, stringBuilder);
        }



        public (List<Error> errors, List<Token> tokens) TokenStream(string data)
        {
            string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
            string numbers = "0123456789";
            int pos = 0;
            int length = data.Length;
            string[] validKeywords = { "print", "final", "printf", "is" ,"get" , "add","if","else" , "else if","loop","to","all","then"};
            char[] validOperators = { '+', '-', '=' ,'*','/'};
            string[] booleanOperators = { ">", "<", "==", ">=", "<=", "!=" };
            string[] variableNames = { "int", "string", "bool", "let" , "array"};
            char[] bracketTypes = { '(', ')' , '{' , '}'};
            string[] command = { "//"};

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
                        else if(res=="true" || res == "false")
                        {
                            pos++;
                            tokens.Add(new Token("bool", res));
                            continue;
                        }
                        tokens.Add(new Token("customVariableName", res));
                        continue;

                    }
                    if(res == "else" && data[pos+1]=='i' && data[pos + 2] == 'f')
                    {
                        tokens.Add(new Token("keyword", "else if"));
                        pos += 3;
                    }
                        

                    tokens.Add(new Token("keyword", res));

                }
                else if (data[pos] == '/' && data[pos + 1] == '/')
                {
                    pos += 2;
                    string res = "";
                    while (pos < length && data[pos] != '\n')
                    {
                        res += data[pos];
                        pos++;
                    }

                    tokens.Add(new Token("command", res));
                }
                else if (booleanOperators.Contains(data[pos].ToString()) ||
         ((data[pos] == '=' && data[pos + 1] == '=') || (data[pos] == '!' && data[pos + 1] == '=') ||
         (data[pos] == '>' && data[pos + 1] == '=') || (data[pos] == '<' && data[pos + 1] == '=')))
                {
                    if ((data[pos] == '=' && data[pos + 1] == '='))
                    {
                        tokens.Add(new Token("booleanOperator", "=="));
                    }
                    else if ((data[pos] == '!' && data[pos + 1] == '='))
                    {
                        tokens.Add(new Token("booleanOperator", "!="));
                    }
                    else if ((data[pos] == '>' && data[pos + 1] == '='))
                    {
                        tokens.Add(new Token("booleanOperator", ">="));
                    }
                    else if ((data[pos] == '<' && data[pos + 1] == '='))
                    {
                        tokens.Add(new Token("booleanOperator", "<="));
                    }
                    else if (booleanOperators.Contains(data[pos].ToString()))
                    {
                        tokens.Add(new Token("booleanOperator", data[pos].ToString()));
                        pos++;
                        continue;
                    }

                    pos += 2;

                }

                else if (validOperators.Contains(currentChar))
                {
                    pos++;

                    tokens.Add(new Token("operator", currentChar.ToString()));
                }
                else if (bracketTypes.Contains(currentChar))
                {
                    string res="";
                    pos++;
                    int bracketAmount = 1;
                    char bracket = '0';
                    char bracketReverse = '0';
                    if (currentChar == '(')
                    {
                        bracket = '(';
                        bracketReverse = ')';
                    }
                    if (currentChar == '{')
                    {
                        bracket = '{';
                        bracketReverse = '}';
                    }

                    while (pos < length && (data[pos] != bracketReverse || bracketAmount>1))
                    {
                        if (bracketAmount > 1 && data[pos] == bracketReverse)
                        {
                            bracketAmount--;
                        }
                    
                        if (data[pos] == bracket)
                            bracketAmount++;
                        res +=data[pos];
                        pos++;

                    }
                    if(length==pos)
                    {
                        errors.Add(new Error("Bracket Error"));
                        return (errors, null);
                    }
                    else if(data[pos] == ')')
                    {
                        var datas = TokenStream(res);
                        tokens.Add(new Token("bracket", "("));
                        tokens.AddRange(datas.tokens);
                        tokens.Add(new Token("bracket", ")"));
                        if (datas.errors is not null)
                        errors.AddRange(datas.errors);
                        pos++;
                        continue;
                    }
                    else if (data[pos] == '}')
                    {
                        var datas = TokenStream(res);
                        tokens.Add(new Token("bracket", "{"));
                        tokens.AddRange(datas.tokens);
                        tokens.Add(new Token("bracket", "}"));
                        if (datas.errors is not null)
                            errors.AddRange(datas.errors);
                        pos++;
                        continue;
                    }
                    pos++;
                    tokens.Add(new Token("bracket",res));

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
        public Queue<T> queue;
        public bool isImmutable { get; set; }

        public Variable(string type, T value, string name)
        {
            this.type = type;
            this.value = value;
            this.name = name;
            isImmutable = false;
            queue = new Queue<T>();
        }

        public T getFirstQueueElement()
        {
            return queue.Dequeue();
        }
        public void AddQueueElement(T data)
        {
             queue.Enqueue(data);
        }
    }

    public class Token
    {
        public string type;
        public string value;
        public string tempValue { get; set; }

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
