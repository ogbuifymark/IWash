﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BoilerPlate.ServiceLayer.Security.Models
{
    public class PasswordResetTokenPayload
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ResetToken { get; set; }
    }
}
