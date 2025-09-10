namespace backend.Model
{
    public class Appointment
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";

    }
}
