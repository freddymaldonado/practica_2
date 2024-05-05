using System;

namespace PatientManager
{
    public class ValidationException : Exception 
    {
        public ValidationException(string message) : base(message) {}
    }
}