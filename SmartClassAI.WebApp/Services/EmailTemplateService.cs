using Microsoft.AspNetCore.Identity.UI.Services;

namespace SmartClassAI.WebApp.Services
{
    public class EmailTemplateService
    {
        private readonly IEmailSender _sender;

        public EmailTemplateService(IEmailSender sender)
        {
            _sender = sender;
        }

        // =========================================================
        // CLASSROOM CREATED
        // =========================================================

        public async Task SendClassroomCreatedAsync(
            string teacherEmail,
            string teacherName,
            string classroomName,
            string subject,
            string joinCode)
        {
            string html = BuildTemplate(
                "🎓 Classroom Created",
                $@"
                <p>Hello <strong>{teacherName}</strong>,</p>

                <p>
                    Your classroom has been created successfully.
                </p>

                <div style='background:#f3f4f6;padding:20px;border-radius:12px;margin:20px 0'>
                    <h2>{classroomName}</h2>

                    <p><strong>Subject:</strong> {subject}</p>

                    <p>
                        <strong>Join Code:</strong>
                    </p>

                    <div style='font-size:30px;font-weight:bold;color:#4f46e5'>
                        {joinCode}
                    </div>
                </div>

                <p>
                    Share this join code with students.
                </p>
                ");

            await _sender.SendEmailAsync(
                teacherEmail,
                $"🎓 Classroom Created - {classroomName}",
                html);
        }

        // =========================================================
        // STUDENT JOINED
        // =========================================================

        public async Task SendStudentJoinedAsync(
            string teacherEmail,
            string teacherName,
            string studentName,
            string studentEmail,
            string classroomName)
        {
            string html = BuildTemplate(
                "🎉 New Student Joined",
                $@"
                <p>Hello <strong>{teacherName}</strong>,</p>

                <p>
                    A new student joined your classroom.
                </p>

                <div style='background:#ecfdf5;padding:20px;border-radius:12px;margin:20px 0'>
                    <p><strong>Name:</strong> {studentName}</p>

                    <p><strong>Email:</strong> {studentEmail}</p>

                    <p><strong>Classroom:</strong> {classroomName}</p>
                </div>
                ");

            await _sender.SendEmailAsync(
                teacherEmail,
                $"🎉 {studentName} joined {classroomName}",
                html);
        }

        // =========================================================
        // ASSIGNMENT CREATED
        // =========================================================

        public async Task SendAssignmentCreatedAsync(
            string studentEmail,
            string studentName,
            string teacherName,
            string classroomName,
            string assignmentTitle,
            string description,
            DateTime dueDate,
            int totalMarks)
        {
            string html = BuildTemplate(
                "📋 New Assignment",
                $@"
                <p>Hello <strong>{studentName}</strong>,</p>

                <p>
                    Your teacher posted a new assignment.
                </p>

                <div style='background:#fffbeb;padding:20px;border-radius:12px;margin:20px 0'>
                    <h2>{assignmentTitle}</h2>

                    <p>{description}</p>

                    <p><strong>Teacher:</strong> {teacherName}</p>

                    <p><strong>Classroom:</strong> {classroomName}</p>

                    <p>
                        <strong>Due Date:</strong>
                        {dueDate:dd MMM yyyy hh:mm tt}
                    </p>

                    <p>
                        <strong>Total Marks:</strong>
                        {totalMarks}
                    </p>
                </div>
                ");

            await _sender.SendEmailAsync(
                studentEmail,
                $"📋 New Assignment - {assignmentTitle}",
                html);
        }

        // =========================================================
        // ASSIGNMENT GRADED
        // =========================================================

        public async Task SendAssignmentGradedAsync(
            string studentEmail,
            string studentName,
            string assignmentTitle,
            string classroomName,
            int marks,
            int totalMarks,
            string? feedback)
        {
            double percentage =
                Math.Round((marks * 100.0) / totalMarks, 1);

            string html = BuildTemplate(
                "⭐ Assignment Graded",
                $@"
                <p>Hello <strong>{studentName}</strong>,</p>

                <p>
                    Your assignment has been graded.
                </p>

                <div style='background:#eff6ff;padding:20px;border-radius:12px;margin:20px 0'>
                    <h2>{assignmentTitle}</h2>

                    <p><strong>Classroom:</strong> {classroomName}</p>

                    <p>
                        <strong>Marks:</strong>
                        {marks}/{totalMarks}
                    </p>

                    <p>
                        <strong>Percentage:</strong>
                        {percentage}%
                    </p>

                    <p>
                        <strong>Feedback:</strong><br>
                        {feedback ?? "No feedback provided"}
                    </p>
                </div>
                ");

            await _sender.SendEmailAsync(
                studentEmail,
                $"⭐ Assignment Graded - {assignmentTitle}",
                html);
        }

        // =========================================================
        // TEMPLATE BUILDER
        // =========================================================

        private static string BuildTemplate(
            string title,
            string body)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>

<body style='margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif'>

    <table width='100%' cellpadding='0' cellspacing='0'>
        <tr>
            <td align='center' style='padding:40px 20px'>

                <table width='600' cellpadding='0' cellspacing='0'
                       style='background:white;border-radius:16px;overflow:hidden'>

                    <tr>
                        <td style='background:#4f46e5;padding:30px;text-align:center;color:white'>
                            <h1 style='margin:0'>
                                SmartClass AI
                            </h1>
                        </td>
                    </tr>

                    <tr>
                        <td style='padding:40px;color:#374151'>
                            {body}
                        </td>
                    </tr>

                    <tr>
                        <td style='background:#f9fafb;padding:20px;text-align:center;color:#6b7280;font-size:13px'>
                            © SmartClass AI
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>";
        }
    }
}