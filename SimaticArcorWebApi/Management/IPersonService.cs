using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom.Person;

namespace SimaticArcorWebApi.Management
{
    public interface IPersonService
    {
        Task CreatePerson(CreatePersonRequest req, CancellationToken ct);
    }
}
