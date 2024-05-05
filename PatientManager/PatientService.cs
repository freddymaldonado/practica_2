using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PatientManager
{
    public class PatientService
    {
        private readonly string _filePath;

        public PatientService(string filePath)
        {
            _filePath = filePath;
            EnsureDataFileExists();
        }

        private void EnsureDataFileExists()
        {
            if (!File.Exists(_filePath))
            {
                using (var stream = File.Create(_filePath)) { }
            }
        }

        public void CreatePatient(Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.Name) || string.IsNullOrWhiteSpace(patient.LastName) || string.IsNullOrWhiteSpace(patient.CI) || string.IsNullOrWhiteSpace(patient.BloodGroup))
                throw new ValidationException("Patient name, last name, CI or bloodgroup must not be empty.");

            var record = $"{patient.Name},{patient.LastName},{patient.CI},{patient.BloodGroup}";
            File.AppendAllLines(_filePath, new[] { record });
        }

        public List<Patient> GetAllPatients()
        {
            var lines = File.ReadAllLines(_filePath);
            if (lines.Length == 0)
            {
                throw new EmptyPatientListException("The operation was succesful, however, there are no patients recorded. The list is empty.");
            }

            return lines.Select(line => line.Split(','))
                        .Select(parts => new Patient {
                            Name = parts[0], 
                            LastName = parts[1], 
                            CI = parts[2], 
                            BloodGroup = parts[3] 
                        }).ToList();
        }

        public Patient GetPatientByCI(string ci)
        {
            var patient = GetAllPatients().FirstOrDefault(p => p.CI == ci);
            if (patient == null)
                throw new NotFoundException($"Patient with CI {ci} not found.");

            return patient;
        }

        public void UpdatePatient(string ci, string newName, string newLastName)
        {
            var patients = GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.CI == ci);
            if (patient == null)
                throw new NotFoundException("Patient not found");

            patient.Name = newName;
            patient.LastName = newLastName;
            File.WriteAllLines(_filePath, patients.Select(p => $"{p.Name},{p.LastName},{p.CI},{p.BloodGroup}"));
        }

        public void DeletePatient(string ci)
        {
            var patients = GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.CI == ci);
            if (patient == null)
                throw new NotFoundException("Patient not found");

            var updatedPatients = patients.Where(p => p.CI != ci);
            File.WriteAllLines(_filePath, updatedPatients.Select(p => $"{p.Name},{p.LastName},{p.CI},{p.BloodGroup}"));
        }
    }
}
