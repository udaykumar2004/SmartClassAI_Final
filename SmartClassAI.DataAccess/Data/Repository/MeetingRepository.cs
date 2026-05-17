using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository;
using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartClassAI.DataAccess.Data.Repository
{
    public class MeetingRepository : Repository<Meeting>,
      IMeetingRepository
    {
        private readonly ApplicationDbContext _db;

        public MeetingRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(Meeting obj)
        {
            _db.Meetings.Update(obj);
        }
    }
}
