// ============================================================
// UnitOfWork.cs
// FIX: Added SaveAsync() implementation
// ============================================================

using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Data.Repository;
using SmartClassAI.DataAccess.Data.Repository.IRepository;
using SmartClassAI.DataAccess.Repository.IRepository;

namespace SmartClassAI.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public IClassroomRepository Classroom { get; private set; }
    public IClassroomMemberRepository ClassroomMember { get; private set; }
    public IAssignmentRepository Assignment { get; private set; }
    public IAssignmentSubmissionRepository AssignmentSubmission { get; private set; }
    public IMeetingRepository Meeting { get; private set; }
    public IEnrollmentRepository Enrollment { get; private set; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;

        Classroom = new ClassroomRepository(_db);
        ClassroomMember = new ClassroomMemberRepository(_db);
        Assignment = new AssignmentRepository(_db);
        AssignmentSubmission = new AssignmentSubmissionRepository(_db);
        Meeting = new MeetingRepository(_db);
        Enrollment = new EnrollmentRepository(_db);
    }

    public void Save() => _db.SaveChanges();

    public async Task SaveAsync() => await _db.SaveChangesAsync();
}