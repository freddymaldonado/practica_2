using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace PatientManager
{
    public class Patient
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string CI { get; set; }
        public string BloodGroup { get; set; }
        [BindNever] // Nos aseguramos que esta propiedad no esté vinculada al request
        [SwaggerSchema(ReadOnly = true, Description = "Auto-generated code for the patient. Do not supply this in requests.")] //Explicamos el código en swagger
        public string Code { get; set; }
    }
}