using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;

namespace SmartClassAI.DataAccess.Repository;

public class AssignmentRepository
    : Repository<Assignment>,
      IAssignmentRepository
{
    private readonly ApplicationDbContext _db;

    public AssignmentRepository(ApplicationDbContext db)
        : base(db)
    {
        _db = db;
    }

    public void Update(Assignment obj)
    {
        _db.Assignments.Update(obj);
    }
}