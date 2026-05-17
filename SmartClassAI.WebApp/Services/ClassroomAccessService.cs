using Microsoft.AspNetCore.Identity;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Services
{
    public class ClassroomAccessService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassroomAccessService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Classroom?> GetClassroomAsync(int classroomId, string? includeProperties = null)
        {
            return await _unitOfWork.Classroom.GetAsync(
                x => x.Id == classroomId,
                includeProperties: includeProperties);
        }

        public async Task<bool> IsGlobalAdminAsync(ApplicationUser user) =>
            await _userManager.IsInRoleAsync(user, SD.Role_Admin);

        public async Task<bool> IsClassTeacherAsync(ApplicationUser user, Classroom classroom) =>
            classroom.TeacherId == user.Id;

        public async Task<bool> IsClassroomMemberAsync(string userId, int classroomId) =>
            await _unitOfWork.ClassroomMember.AnyAsync(
                x => x.ClassroomId == classroomId && x.UserId == userId);

        public async Task<bool> IsEnrolledStudentAsync(string userId, int classroomId) =>
            await _unitOfWork.ClassroomMember.AnyAsync(
                x => x.ClassroomId == classroomId
                     && x.UserId == userId
                     && x.Role == SD.ClassroomRole_Student);

        public async Task<bool> CanAccessClassroomAsync(ApplicationUser user, int classroomId)
        {
            if (!user.IsActive || user.IsSuspended)
                return false;

            if (await IsGlobalAdminAsync(user))
                return true;

            var classroom = await GetClassroomAsync(classroomId);
            if (classroom == null)
                return false;

            if (await IsClassTeacherAsync(user, classroom))
                return true;

            return await IsClassroomMemberAsync(user.Id, classroomId);
        }

        public async Task<bool> CanManageClassroomAsync(ApplicationUser user, int classroomId)
        {
            if (!user.IsActive || user.IsSuspended)
                return false;

            if (await IsGlobalAdminAsync(user))
                return true;

            var classroom = await GetClassroomAsync(classroomId);
            if (classroom == null)
                return false;

            return await IsClassTeacherAsync(user, classroom);
        }

        public async Task<bool> CanSubmitAssignmentAsync(ApplicationUser user, int classroomId)
        {
            if (!user.IsActive || user.IsSuspended)
                return false;

            if (await IsGlobalAdminAsync(user))
                return false;

            var classroom = await GetClassroomAsync(classroomId);
            if (classroom == null || classroom.IsArchived)
                return false;

            if (await IsClassTeacherAsync(user, classroom))
                return false;

            return await IsEnrolledStudentAsync(user.Id, classroomId);
        }

        public async Task<bool> CanGradeAssignmentAsync(ApplicationUser user, int classroomId)
        {
            return await CanManageClassroomAsync(user, classroomId);
        }
    }
}
