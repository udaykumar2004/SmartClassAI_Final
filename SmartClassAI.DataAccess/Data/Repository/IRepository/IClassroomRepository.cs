using SmartClassAI.Models;

namespace SmartClassAI.DataAccess.Repository.IRepository;

public interface IClassroomRepository : IRepository<Classroom>
{
    void Update(Classroom obj);
}