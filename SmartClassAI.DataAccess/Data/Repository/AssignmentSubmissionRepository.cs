using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;

namespace SmartClassAI.DataAccess.Repository;

public class AssignmentSubmissionRepository
    : Repository<AssignmentSubmission>,
      IAssignmentSubmissionRepository
{
    private readonly ApplicationDbContext _db;

    public AssignmentSubmissionRepository(
        ApplicationDbContext db)
        : base(db)
    {
        _db = db;
    }

    public void Update(
        AssignmentSubmission obj)
    {
        _db.AssignmentSubmissions
            .Update(obj);
    }
}