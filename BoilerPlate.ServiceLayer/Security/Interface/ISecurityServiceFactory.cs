using System;
using System.Collections.Generic;
using System.Text;

namespace BoilerPlate.ServiceLayer.Security.Interface
{
    public interface ISecurityServiceFactory
    {
        ISecurityBaseService GetService(Type type);

    }
}
