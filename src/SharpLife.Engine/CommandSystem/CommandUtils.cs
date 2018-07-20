﻿/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using SharpLife.Engine.CommandSystem.Commands;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Engine.CommandSystem
{
    public static class CommandUtils
    {
        /// <summary>
        /// Parses a string into the commands that it contains
        /// </summary>
        /// <param name="commandSource"></param>
        /// <param name="commands"></param>
        public static List<ICommand> ParseCommands(CommandSource commandSource, string commands)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            //A command is delimited by either newlines, semicolons or the end of the string, quoted semicolons don't delimit
            var tokensList = commands.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(Tokenizer.GetTokens).ToList();

            var list = new List<ICommand>();

            foreach (var tokens in tokensList)
            {
                foreach (var commandList in SplitCommandList(tokens))
                {
                    list.Add(new Command(commandSource, commandList[0], commandList.Skip(1).ToList()));
                }
            }

            return list;
        }

        public static IList<IList<string>> SplitCommandList(IList<string> list)
        {
            var result = new List<IList<string>>();

            var currentList = 0;

            //Make sure that multiple semicolons don't create empty lists
            foreach (var token in list)
            {
                if (token == ";")
                {
                    ++currentList;
                }
                else
                {
                    if (result.Count <= currentList)
                    {
                        result.Add(new List<string>());
                    }

                    result[currentList].Add(token);
                }
            }

            return result;
        }

        /// <summary>
        /// When it's a whole number, strip trailing zeroes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FloatToConVarString(float value)
        {
            return value.ToString();
        }
    }
}