using System;

namespace PatientManager
{
    public class EmptyPatientListException : Exception
    {
        public EmptyPatientListException(string message) : base(message)
        {
        }
    }
}
