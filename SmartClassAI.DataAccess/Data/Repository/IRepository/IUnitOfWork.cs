// ============================================================
// IUnitOfWork.cs
// FIX: Added SaveAsync() — was missing, forcing sync saves everywhere
//      Removed IEnrollmentRepository from primary interface
//      (Enrollment is now legacy-only)
// ============================================================

using SmartClassAI.DataAccess.Data.Repository.IRepository;

namespace SmartClassAI.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    IClassroomRepository Classroom { get; }
    IClassroomMemberRepository ClassroomMember { get; }
    IAssignmentRepository Assignment { get; }
    IAssignmentSubmissionRepository AssignmentSubmission { get; }
    IMeetingRepository Meeting { get; }

    // Legacy — still available for migration scripts
    IEnrollmentRepository Enrollment { get; }

    // Synchronous save (legacy)
    void Save();

    // Async save (use this in all async controllers)
    Task SaveAsync();
}