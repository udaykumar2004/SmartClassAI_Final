using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository;
using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartClassAI.DataAccess.Data.Repository
{
    public  class ClassroomMemberRepository : Repository<ClassroomMember>,
      IClassroomMemberRepository
    {
        private readonly ApplicationDbContext _db;

        public ClassroomMemberRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(ClassroomMember obj)
        {
            _db.ClassroomMembers.Update(obj);
        }
    }
}
