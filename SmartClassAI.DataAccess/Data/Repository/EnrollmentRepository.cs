using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository;
using SmartClassAI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartClassAI.DataAccess.Data.Repository
{
    public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
    {
        private readonly ApplicationDbContext _db;
        public EnrollmentRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public void Update(Enrollment obj)
        {
            _db.Enrollments.Update(obj);
        }
    }
}
