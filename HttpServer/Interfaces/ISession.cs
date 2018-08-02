﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Interfaces
{
    public interface ISession
    {
        void Close();
        Task SendExceptionAsync(Exception e);
    }
}
