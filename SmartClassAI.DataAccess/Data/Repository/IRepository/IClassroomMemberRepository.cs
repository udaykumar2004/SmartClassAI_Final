using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartClassAI.DataAccess.Data.Repository.IRepository
{
   public  interface IClassroomMemberRepository : IRepository<ClassroomMember>
    {
        void Update(ClassroomMember obj);
    
    }
}
