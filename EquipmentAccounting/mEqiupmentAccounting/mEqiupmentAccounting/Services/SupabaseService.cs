using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using mEquipmentAccounting.Models;
using Postgrest;
using Supabase;

namespace mEquipmentAccounting.Services
{
    public static class SupabaseService
    {
        private static Supabase.Client _client;

       
        private const string SupabaseUrl = "https://dapkqisliqyzdejdywbw.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImRhcGtxaXNsaXF5emRlamR5d2J3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDUwMTA3MjksImV4cCI6MjA2MDU4NjcyOX0.oNFqOzJlj4xYAJttWIkDZJAMm4SEx95F1vycqbpFvUI";
        

        public static bool IsInitialized { get; private set; } = false;

        public static async Task InitializeAsync()
        {
            if (_client != null && IsInitialized) return;

            var options = new SupabaseOptions { AutoRefreshToken = true };

            try
            {
                Debug.WriteLine("Initializing Supabase client...");
                _client = new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
                await _client.InitializeAsync();
                IsInitialized = true;
                Debug.WriteLine("Supabase client initialized successfully.");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Debug.WriteLine($"!!!!!!!!!! SUPABASE INITIALIZATION FAILED: {ex.Message} !!!!!!!!!!");
                throw new Exception($"Failed to initialize Supabase: {ex.Message}", ex);
            }
        }

        
        public static async Task<List<Equipment>> GetEquipmentListAsync()
        {
            if (!IsInitialized || _client == null)
            {
                Debug.WriteLine("!!!!!!!!!! Error: Supabase client not initialized before calling GetEquipmentList. !!!!!!!!!!");
                throw new InvalidOperationException("Supabase client is not initialized.");
            }

            try
            {
                Debug.WriteLine("Fetching equipment list...");
              
                var response = await _client.From<Equipment>()
                    .Select("*, categories(category_name), statuses(status_name), locations(location_name), users!left(user_id, first_name, last_name, username)")
                    .Order("name", Constants.Ordering.Ascending)
                    .Get();

                Debug.WriteLine($"Equipment list fetched. Status: {response.ResponseMessage.StatusCode}. Models count: {response.Models?.Count ?? 0}");
                if (!response.ResponseMessage.IsSuccessStatusCode)
                {
                    var errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    Debug.WriteLine($"!!!!!!!!!! Error content: {errorContent} !!!!!!!!!!");
                    throw new Exception($"API Error: {response.ResponseMessage.ReasonPhrase}");
                }
                return response.Models ?? new List<Equipment>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!!!!!!!!! Exception during GetEquipmentListAsync: {ex.Message} !!!!!!!!!!");
                throw new Exception("Failed to GetEquipmentList. Check logs for details.", ex);
            }
        }
    }
}