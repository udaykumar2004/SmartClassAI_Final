using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartClassAI.DataAccess.Data.Repository.IRepository
{
    public  interface IAssignmentRepository : IRepository<Assignment>
    {
        void Update(Assignment obj);
    }
}
