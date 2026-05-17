using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;

namespace SmartClassAI.DataAccess.Repository;

public class ClassroomRepository
    : Repository<Classroom>, IClassroomRepository
{
    private readonly ApplicationDbContext _db;

    public ClassroomRepository(ApplicationDbContext db)
        : base(db)
    {
        _db = db;
    }

    public void Update(Classroom obj)
    {
        _db.Classrooms.Update(obj);
    }
}