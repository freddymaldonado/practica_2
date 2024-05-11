using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace PatientManager
{
    public class PatientService
    {
        private readonly string _filePath;
        private readonly HttpClient _httpClient;

        public PatientService(string filePath)
        {
            _filePath = filePath;
            _httpClient = new HttpClient();
            EnsureDataFileExists();
        }

        private void EnsureDataFileExists()
        {
            if (!File.Exists(_filePath))
            {
                using (var stream = File.Create(_filePath)) { }
            }
        }

        //Antiguo servicio CreatePatient
        // public void CreatePatient(Patient patient)
        // {
        //     if (string.IsNullOrWhiteSpace(patient.Name) || string.IsNullOrWhiteSpace(patient.LastName) || string.IsNullOrWhiteSpace(patient.CI) || string.IsNullOrWhiteSpace(patient.BloodGroup))
        //         throw new ValidationException("Patient name, last name, CI or bloodgroup must not be empty.");

        //     var record = $"{patient.Name},{patient.LastName},{patient.CI},{patient.BloodGroup}";
        //     File.AppendAllLines(_filePath, new[] { record });
        // }

    //Nuevo servicio CreatePatient que utiliza el backing service (async)
        public async Task CreatePatient(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.Name) || string.IsNullOrWhiteSpace(patient.LastName) || 
            string.IsNullOrWhiteSpace(patient.CI) || string.IsNullOrWhiteSpace(patient.BloodGroup))
            throw new ValidationException("Patient name, last name, CI, or blood group must not be empty.");

        var json = JsonSerializer.Serialize(new { name = patient.Name, lastName = patient.LastName, ci = patient.CI });
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("http://localhost:5128/patients", data);
        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to generate patient code.");

        var patientCode = await response.Content.ReadAsStringAsync();
        patient.Code = patientCode.Trim('"');

        // Update record with the new patient code
        var record = $"{patient.Name},{patient.LastName},{patient.CI},{patient.BloodGroup},{patient.Code}";
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
                            BloodGroup = parts[3],
                            Code = parts[4] 
                        }).ToList();
        }

        public Patient GetPatientByCI(string ci)
        {
            var patient = GetAllPatients().FirstOrDefault(p => p.CI == ci);
            if (patient == null)
                throw new NotFoundException($"Patient with CI {ci} not found.");

            return patient;
        }

        public Patient GetPatientByCode(string code)
        {
            var patient = GetAllPatients().FirstOrDefault(p => p.Code == code);
            if (patient == null)
                throw new NotFoundException($"Patient with Code {code} not found.");

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
