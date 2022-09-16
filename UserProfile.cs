﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace CleanPlateBot
{
    /// <summary>
    /// This is our application state. Just a regular serializable .NET class.
    /// </summary>
    public class UserProfile
    {
       
        public string Name { get; set; }

        public int Score { get; set; }
        public string Image { get; set; }
    }
}
