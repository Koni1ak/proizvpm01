
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EquipmentAccounting.Models; 
using Supabase;                   
using Supabase.Gotrue;            
using Supabase.Gotrue.Exceptions; 
using AuthUser = Supabase.Gotrue.User; 
using Postgrest.Responses;        
using static Postgrest.Constants;        
using System.Linq;                
using System.Text.Json;           
using Postgrest;                  


namespace EquipmentAccounting.Services
{
    
    public static class SupabaseService
    {
        private static Supabase.Client _client;

       
        private const string SupabaseUrl = "https://dapkqisliqyzdejdywbw.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImRhcGtxaXNsaXF5emRlamR5d2J3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDUwMTA3MjksImV4cCI6MjA2MDU4NjcyOX0.oNFqOzJlj4xYAJttWIkDZJAMm4SEx95F1vycqbpFvUI";
       
        
        private static AuthUser _currentUser;

        
        public static async Task InitializeAsync()
        {
            if (_client == null)
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                };

                _client = new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
                await _client.InitializeAsync();
                Console.WriteLine("Supabase client initialized successfully.");
            }
        }

        
        public static async Task<Session> SignInAsync(string email, string password)
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");

            try
            {
                var session = await _client.Auth.SignIn(email, password);

                if (session != null && session.User != null)
                {
                    _currentUser = session.User; 
                    Console.WriteLine($"User '{_currentUser.Email}' signed in successfully.");
                    
                }
                else
                {
                    Console.WriteLine("SignIn returned null session/user without throwing an exception.");
                    _currentUser = null;
                }
                return session;
            }
            catch (GotrueException ex)
            {
                Console.WriteLine($"Supabase Auth SignIn Error: Status={ex.StatusCode}, Reason={ex.Reason}, Message={ex.Message}");
                _currentUser = null;
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic SignIn Error: {ex}");
                _currentUser = null;
                throw new Exception("Ошибка сети или сервера при попытке входа.", ex);
            }
        }

        
        public static async Task SignOutAsync()
        {
            if (_client == null)
            {
                Console.WriteLine("SignOutAsync called but client is not initialized.");
                _currentUser = null;
                return;
            }

            try
            {
                if (_client.Auth.CurrentUser != null)
                {
                    Console.WriteLine($"Signing out user '{_client.Auth.CurrentUser.Email}' from Supabase session...");
                    
                    await _client.Auth.SignOut();
                    Console.WriteLine("User signed out from Supabase session successfully.");
                }
                else
                {
                    Console.WriteLine("No active Supabase session found to sign out.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supabase SignOut Error: {ex}");
            }
            finally
            {
                _currentUser = null;
                Console.WriteLine("Local current user data cleared.");
            }
        }

       
        public static AuthUser GetCurrentUser()
        {
            return _currentUser;
        }

        
        public static string GetCurrentUserId()
        {
            return _currentUser?.Id;
        }

        
        public static async Task<List<Category>> GetCategoriesAsync()
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                var response = await _client.From<Category>().Get();
                if (!response.ResponseMessage.IsSuccessStatusCode) { HandleErrorResponse("GetCategories", response.ResponseMessage, await response.ResponseMessage.Content.ReadAsStringAsync()); }
                
                return response.Models != null ? response.Models : new List<Category>();
            }
            catch (Exception ex) { HandleGenericError("GetCategories", ex); return new List<Category>(); }
        }

       
        public static async Task<List<Status>> GetStatusesAsync()
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                var response = await _client.From<Status>().Get();
                if (!response.ResponseMessage.IsSuccessStatusCode) { HandleErrorResponse("GetStatuses", response.ResponseMessage, await response.ResponseMessage.Content.ReadAsStringAsync()); }
                
                return response.Models != null ? response.Models : new List<Status>();
            }
            catch (Exception ex) { HandleGenericError("GetStatuses", ex); return new List<Status>(); }
        }

        
        public static async Task<List<Location>> GetLocationsAsync()
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                var response = await _client.From<Location>().Get();
                if (!response.ResponseMessage.IsSuccessStatusCode) { HandleErrorResponse("GetLocations", response.ResponseMessage, await response.ResponseMessage.Content.ReadAsStringAsync()); }
                
                return response.Models != null ? response.Models : new List<Location>();
            }
            catch (Exception ex) { HandleGenericError("GetLocations", ex); return new List<Location>(); }
        }

       
        public static async Task<List<Supabase.Gotrue.User>> GetUsersAsync()
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                
                string selectQuery = "user_id, username, first_name, last_name, email, role_id, created_at, is_active";
                
                Console.WriteLine($"DEBUG: Executing Supabase User (from 'users' table) Select: {selectQuery}");

                
                var response = await _client.From<EquipmentAccounting.Models.User>()
                                            .Select(selectQuery)
                                            .Get();

                if (response.ResponseMessage.IsSuccessStatusCode)
                {
                   
                    return response.Models != null ? response.Models : new List<Supabase.Gotrue.User>();
                }
                else
                {
                    string errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    HandleErrorResponse("GetUsers (dictionary)", response.ResponseMessage, errorContent);
                    return new List<Supabase.Gotrue.User>(); 
                }
            }
            catch (Exception ex)
            {
                HandleGenericError("GetUsers (dictionary)", ex);
                return new List<Supabase.Gotrue.User>(); 
            }
        }


        
        public static async Task<List<Equipment>> GetEquipmentListAsync()
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                string selectQuery = "equipment_id, name, serial_number, inventory_number, description, purchase_date, warranty_expiry_date, created_at, updated_at, " +
                                     "Category:categories(*), " +
                                     "Status:statuses(*), " +
                                     "Location:locations(*), " +
                                     "AssignedUser:users!left(*)";
                Console.WriteLine($"Executing Supabase Equipment Select: {selectQuery}");

                var response = await _client.From<Equipment>()
                                            .Select(selectQuery)
                                            .Order("equipment_id", Ordering.Ascending)
                                            .Get();

                if (response.ResponseMessage.IsSuccessStatusCode)
                {
                    
                    return response.Models != null ? response.Models : new List<Equipment>();
                }
                else
                {
                    string errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    HandleErrorResponse("GetEquipmentList", response.ResponseMessage, errorContent, true);
                    return null; 
                }
            }
            catch (Exception ex)
            {
                HandleGenericError("GetEquipmentList", ex, true);
                return null; 
            }
        }

        
        public static async Task<Equipment> AddEquipmentAsync(Equipment newEquipment, string changedByUserId, string notes = "Equipment created")
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            if (newEquipment == null) throw new ArgumentNullException(nameof(newEquipment));
            if (string.IsNullOrEmpty(changedByUserId)) throw new ArgumentNullException(nameof(changedByUserId), "User ID performing the change cannot be null or empty.");

            try
            {
                ModeledResponse<Equipment> insertResponse = await _client.From<Equipment>().Insert(newEquipment);
                var addedEquipment = insertResponse.Models.FirstOrDefault();

                if (addedEquipment == null || !insertResponse.ResponseMessage.IsSuccessStatusCode)
                {
                    string errorContent = await insertResponse.ResponseMessage.Content.ReadAsStringAsync();
                    HandleErrorResponse("AddEquipment (Insert)", insertResponse.ResponseMessage, errorContent, true);
                    return null; 
                }

                HandleHistoryEntry(addedEquipment.EquipmentId, changedByUserId, "Created", notes,
                                   null, addedEquipment.StatusId,
                                   null, addedEquipment.LocationId,
                                   null, addedEquipment.AssignedUserId);
                return addedEquipment;
            }
            catch (Exception ex)
            {
                HandleGenericError("AddEquipment", ex, true);
                return null; 
            }
        }

        
        public static async Task<Equipment> UpdateEquipmentAsync(Equipment updatedEquipment, Equipment originalEquipment, string changedByUserId, string notes)
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            if (updatedEquipment == null) throw new ArgumentNullException(nameof(updatedEquipment));
            if (originalEquipment == null) throw new ArgumentNullException(nameof(originalEquipment));
            if (string.IsNullOrEmpty(changedByUserId)) throw new ArgumentNullException(nameof(changedByUserId));

            try
            {
                string changeType = null; bool changed = false;
                if (originalEquipment.StatusId != updatedEquipment.StatusId) { changed = true; changeType = "Status Changed"; }
                if (originalEquipment.LocationId != updatedEquipment.LocationId) { changed = true; changeType = (changeType == null ? "Moved" : changeType + ", Moved"); }
                if (originalEquipment.AssignedUserId != updatedEquipment.AssignedUserId) { changed = true; changeType = (changeType == null ? "Assigned/Returned" : changeType + ", Assigned/Returned"); }
                if (!changed && !string.IsNullOrWhiteSpace(notes) && notes != (originalEquipment.Description ?? "")) { changed = true; changeType = "Updated"; }

                var updateResponse = await _client.From<Equipment>()
                                                 .Where(eq => eq.EquipmentId == updatedEquipment.EquipmentId)
                                                 .Set(eq => eq.Name, updatedEquipment.Name)
                                                 .Set(eq => eq.SerialNumber, updatedEquipment.SerialNumber)
                                                 .Set(eq => eq.InventoryNumber, updatedEquipment.InventoryNumber)
                                                 .Set(eq => eq.Description, updatedEquipment.Description)
                                                 .Set(eq => eq.PurchaseDate, updatedEquipment.PurchaseDate)
                                                 .Set(eq => eq.WarrantyExpiryDate, updatedEquipment.WarrantyExpiryDate)
                                                 .Set(eq => eq.CategoryId, updatedEquipment.CategoryId)
                                                 .Set(eq => eq.StatusId, updatedEquipment.StatusId)
                                                 .Set(eq => eq.LocationId, updatedEquipment.LocationId)
                                                 .Set(eq => eq.AssignedUserId, updatedEquipment.AssignedUserId)
                                                 .Set(eq => eq.UpdatedAt, DateTime.UtcNow)
                                                 .Update();

                var updatedResult = updateResponse.Models.FirstOrDefault();

                if (updatedResult == null || !updateResponse.ResponseMessage.IsSuccessStatusCode)
                {
                    string errorContent = await updateResponse.ResponseMessage.Content.ReadAsStringAsync();
                    HandleErrorResponse("UpdateEquipment", updateResponse.ResponseMessage, errorContent, true);
                    return null;

                    if (changed)
                    {
                        HandleHistoryEntry(updatedEquipment.EquipmentId, changedByUserId, changeType, notes ?? "Updated via application",
                                           originalEquipment.StatusId, updatedEquipment.StatusId,
                                           originalEquipment.LocationId, updatedEquipment.LocationId,
                                           originalEquipment.AssignedUserId, updatedEquipment.AssignedUserId);
                    }
                    return updatedResult;
                }
            }
            catch (Exception ex)
            {
                HandleGenericError("UpdateEquipment", ex, true);
                return null;
            }
        }

        
        public static async Task DeleteEquipmentAsync(long equipmentId, long changedByUserId, string notes = "Equipment deleted")
        {
            
            var getResponse = await _client.From<Equipment>()
                                       .Where(eq => eq.EquipmentId == equipmentId)
                                       .Limit(1)
                                       .Get();
            var equipmentToDelete = getResponse.Models.FirstOrDefault();

            if (equipmentToDelete == null)
            {
                
                Console.WriteLine($"Equipment with ID {equipmentId} not found for deletion.");
                
                throw new KeyNotFoundException($"Equipment with ID {equipmentId} not found.");
                
            }

           
            var historyEntry = new AssignmentHistory
            {
                EquipmentId = equipmentId,
                ChangedByUserId = changedByUserId,
                ChangeType = "Deleted",
                PreviousStatusId = equipmentToDelete.StatusId,
                PreviousLocationId = equipmentToDelete.LocationId,
                PreviousAssignedUserId = equipmentToDelete.AssignedUserId,
                Notes = notes
            };
           

            try 
            {
                
                await AddHistoryEntryAsync(historyEntry);

                
                await _client.From<Equipment>()
                             .Where(eq => eq.EquipmentId == equipmentId)
                             .Delete();

                Console.WriteLine($"Equipment with ID {equipmentId} deleted successfully.");
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error deleting equipment with ID {equipmentId}: {ex.Message}");
                
                throw new Exception($"Failed to delete equipment with ID {equipmentId}. See inner exception for details.", ex);
            }
        }



        private static void HandleHistoryEntry(long equipmentId, string changedByAuthUserId, string changeType, string notes,
                                                long? prevStatusId, long? newStatusId,
                                                long? prevLocationId, long? newLocationId,
                                                long? prevAssignedUserId, long? newAssignedUserId)
        {
            Console.WriteLine($"WARNING: History entry generation for Equipment ID {equipmentId}. " +
                              $"The provided 'changedByAuthUserId' ({changedByAuthUserId}) is a STRING (UUID) from Supabase Auth. " +
                              $"Your 'assignment_history.changed_by_user_id' column expects a BIGINT referencing 'users.user_id'. " +
                              "History entry will NOT be saved until this is resolved (see code comments).");

            var historyEntry = new AssignmentHistory
            {
                EquipmentId = equipmentId,

                ChangeType = changeType ?? "Updated",
                ChangeTimestamp = DateTimeOffset.UtcNow,
                Notes = notes,
                PreviousStatusId = newStatusId != prevStatusId ? prevStatusId : null,
                NewStatusId = newStatusId != prevStatusId ? newStatusId : null,
                PreviousLocationId = newLocationId != prevLocationId ? prevLocationId : null,
                NewLocationId = newLocationId != prevLocationId ? newLocationId : null,
                PreviousAssignedUserId = newAssignedUserId != prevAssignedUserId ? prevAssignedUserId : null,
                NewAssignedUserId = newAssignedUserId != prevAssignedUserId ? newAssignedUserId : null
            };

            Console.WriteLine($"History entry generated (but not saved due to ID issue): EqID={historyEntry.EquipmentId}, Type={historyEntry.ChangeType}");
        }
        
     
            


        
        public static async Task AddHistoryEntryAsync(AssignmentHistory historyEntry)
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            if (historyEntry == null) throw new ArgumentNullException(nameof(historyEntry));

            

            try
            {
                historyEntry.ChangeTimestamp = DateTimeOffset.UtcNow;
                var response = await _client.From<AssignmentHistory>().Insert(historyEntry);
                if (!response.ResponseMessage.IsSuccessStatusCode)
                {
                    string errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error adding history entry: Status={response.ResponseMessage.StatusCode}, Reason={response.ResponseMessage.ReasonPhrase}, Content={errorContent}");
                }
                else { Console.WriteLine($"History entry added successfully for equipment ID {historyEntry.EquipmentId}."); }
            }
            catch (Exception ex) { Console.WriteLine($"Generic error adding history entry: {ex}"); }
        }

        
        public static async Task<List<AssignmentHistory>> GetEquipmentHistoryAsync(long equipmentId)
        {
            if (_client == null) throw new InvalidOperationException("Supabase client not initialized.");
            try
            {
                var response = await _client.From<AssignmentHistory>()
                                            .Select("*")
                                            .Where(h => h.EquipmentId == equipmentId)
                                            .Order("change_timestamp", Ordering.Descending)
                                            .Get();

                if (!response.ResponseMessage.IsSuccessStatusCode)
                {
                    string errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    HandleErrorResponse("GetEquipmentHistory", response.ResponseMessage, errorContent);
                }
                
                return response.Models != null ? response.Models : new List<AssignmentHistory>();
            }
            catch (Exception ex)
            {
                HandleGenericError("GetEquipmentHistory", ex);
                return new List<AssignmentHistory>();
            }
        }

        
        private static void HandleErrorResponse(string operation, System.Net.Http.HttpResponseMessage response, string content, bool throwException = false)
        {
            string errorMessage = $"Error during operation '{operation}': Status={response.StatusCode}, Reason={response.ReasonPhrase}";
            string detailedMessage = errorMessage;
            try
            {
                using (var jsonDoc = JsonDocument.Parse(content))
                {
                    if (jsonDoc.RootElement.TryGetProperty("message", out var msg)) { detailedMessage += $", Message: {msg.GetString()}"; }
                    if (jsonDoc.RootElement.TryGetProperty("details", out var det)) { detailedMessage += $", Details: {det.GetString()}"; }
                    if (jsonDoc.RootElement.TryGetProperty("hint", out var hint)) { detailedMessage += $", Hint: {hint.GetString()}"; }
                }
            }
            catch { detailedMessage = $"{errorMessage}, Content: {content}"; }

            Console.WriteLine($"ERROR: {detailedMessage}");
            if (throwException) { throw new Exception($"Operation '{operation}' failed: {detailedMessage}"); }
        }

        
        private static void HandleGenericError(string operation, Exception ex, bool throwException = false)
        {
            Console.WriteLine($"ERROR: Generic exception during operation '{operation}': {ex}");
            if (throwException) { throw new Exception($"An internal error occurred during '{operation}': {ex.Message}", ex); }
        }


        
    }
}