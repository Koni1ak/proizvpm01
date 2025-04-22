using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EquipmentAccounting.Models; // Проверьте namespace
using Supabase;
using Postgrest.Responses;
using System.Linq;
using System.Diagnostics; // Для Debug.WriteLine

namespace EquipmentAccounting.Services // Проверьте namespace
{
    public static class SupabaseService
    {
        private static Supabase.Client _client;

        // ---> ВСТАВЬТЕ ВАШИ ДАННЫЕ SUPABASE <---
        private const string SupabaseUrl = "https://dapkqisliqyzdejdywbw.supabase.co"; // ЗАМЕНИТЕ!
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImRhcGtxaXNsaXF5emRlamR5d2J3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDUwMTA3MjksImV4cCI6MjA2MDU4NjcyOX0.oNFqOzJlj4xYAJttWIkDZJAMm4SEx95F1vycqbpFvUI"; // ЗАМЕНИТЕ!
        // ---> ----------------------------- <---

        public static async Task InitializeAsync()
        {
            if (_client == null)
            {
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true // Оставляем, если планировалось
                };

                Debug.WriteLine("SupabaseService: Initializing Supabase.Client...");
                _client = new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
                await _client.InitializeAsync();
                Debug.WriteLine("SupabaseService: Supabase.Client initialized.");
            }
            else
            {
                Debug.WriteLine("SupabaseService: Supabase.Client already initialized.");
            }
        }

        // Вспомогательный метод для проверки инициализации клиента
        private static void EnsureClientInitialized()
        {
            if (_client == null)
            {
                Debug.WriteLine("!!! SupabaseService Error: Client is null. InitializeAsync was not called or failed.");
                // Бросаем исключение, чтобы предотвратить NullReferenceException при вызове _client.From<T>()
                throw new InvalidOperationException("Supabase client is not initialized. Call InitializeAsync first.");
            }
        }

        // --- Методы для работы со справочниками ---
        public static async Task<List<Category>> GetCategoriesAsync()
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine("SupabaseService: Getting Categories...");
            var response = await _client.From<Category>().Get();
            Debug.WriteLine($"SupabaseService: GetCategories Status: {response?.ResponseMessage?.StatusCode}");
            return response?.Models ?? new List<Category>();
        }

        public static async Task<List<Status>> GetStatusesAsync()
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine("SupabaseService: Getting Statuses...");
            var response = await _client.From<Status>().Get();
            Debug.WriteLine($"SupabaseService: GetStatuses Status: {response?.ResponseMessage?.StatusCode}");
            return response?.Models ?? new List<Status>();
        }

        public static async Task<List<Location>> GetLocationsAsync()
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine("SupabaseService: Getting Locations...");
            var response = await _client.From<Location>().Get();
            Debug.WriteLine($"SupabaseService: GetLocations Status: {response?.ResponseMessage?.StatusCode}");
            return response?.Models ?? new List<Location>();
        }

        public static async Task<List<User>> GetUsersAsync()
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine("SupabaseService: Getting Users...");
            // Загружаем только нужные поля для ComboBox
            var response = await _client.From<User>()
                                        .Select("user_id, username, first_name, last_name, is_active") // Добавили is_active для фильтрации
                                        .Get();
            Debug.WriteLine($"SupabaseService: GetUsers Status: {response?.ResponseMessage?.StatusCode}");
            return response?.Models ?? new List<User>();
        }


        // --- Методы для работы с Оборудованием (Equipment) ---
        public static async Task<List<Equipment>> GetEquipmentListAsync()
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine("SupabaseService: Getting Equipment List...");
            try
            {
                var response = await _client.From<Equipment>()
                                            // Явный Select для избежания конфликтов псевдонимов
                                            .Select("equipment_id, name, serial_number, inventory_number, description, purchase_date, warranty_expiry_date, created_at, updated_at, category_id, status_id, location_id, assigned_user_id, category:categories(category_id, category_name, description), status:statuses(status_id, status_name, description), location:locations(location_id, location_name, address, description), assigned_user:users!left(user_id, username, first_name, last_name, email, is_active)")
                                            .Order("equipment_id", Postgrest.Constants.Ordering.Descending)
                                            .Get();

                Debug.WriteLine($"SupabaseService: GetEquipmentList Status: {response?.ResponseMessage?.StatusCode}");
                if (response != null && response.ResponseMessage.IsSuccessStatusCode)
                {
                    return response.Models ?? new List<Equipment>();
                }
                else
                {
                    // Формируем информативное сообщение об ошибке
                    var errorReason = response?.ResponseMessage?.ReasonPhrase ?? "Unknown Reason";
                    var errorContent = response != null ? await response.ResponseMessage.Content.ReadAsStringAsync() : "Response is null";
                    string fullErrorMessage = $"Failed to fetch equipment. Status: {errorReason}. DB Error: {errorContent}";
                    Debug.WriteLine($"!!! SupabaseService: {fullErrorMessage}");
                    // Пробрасываем исключение с деталями ошибки
                    throw new Exception(fullErrorMessage);
                }
            }
            catch (Exception ex) // Ловим любые другие исключения (сеть и т.д.)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in GetEquipmentListAsync: {ex}");
                // Пробрасываем исходное исключение, чтобы сохранить stack trace
                throw;
            }
        }

        public static async Task<Equipment> AddEquipmentAsync(Equipment newEquipment, long changedByUserId, string notes = "Equipment created")
        {
            EnsureClientInitialized(); // Проверка инициализации
            if (newEquipment == null) throw new ArgumentNullException(nameof(newEquipment));
            Debug.WriteLine($"SupabaseService: Adding Equipment '{newEquipment.Name}' by UserID {changedByUserId}...");

            try
            {
                // 1. Добавляем оборудование
                // Устанавливаем UpdatedAt перед добавлением, если нужно (или БД сама это делает)
                // newEquipment.CreatedAt = DateTimeOffset.UtcNow; // Обычно устанавливается БД
                newEquipment.UpdatedAt = DateTimeOffset.UtcNow;

                var insertResponse = await _client.From<Equipment>().Insert(newEquipment);
                var addedEquipment = insertResponse?.Models?.FirstOrDefault();

                if (addedEquipment == null)
                {
                    var errorReason = insertResponse?.ResponseMessage?.ReasonPhrase ?? "Unknown Reason";
                    var errorContent = insertResponse != null ? await insertResponse.ResponseMessage.Content.ReadAsStringAsync() : "Response is null";
                    string fullErrorMessage = $"Failed to add equipment. Status: {errorReason}. DB Error: {errorContent}";
                    Debug.WriteLine($"!!! SupabaseService: {fullErrorMessage}");
                    throw new Exception(fullErrorMessage);
                }
                Debug.WriteLine($"SupabaseService: Equipment added with ID {addedEquipment.EquipmentId}.");

                // 2. Добавляем запись в историю
                var historyEntry = new AssignmentHistory
                {
                    EquipmentId = addedEquipment.EquipmentId,
                    ChangedByUserId = changedByUserId,
                    ChangeType = "Created",
                    NewStatusId = addedEquipment.StatusId,
                    NewLocationId = addedEquipment.LocationId,
                    NewAssignedUserId = addedEquipment.AssignedUserId,
                    Notes = notes // Используем переданные или стандартные заметки
                };
                await AddHistoryEntryAsync(historyEntry); // Вызываем метод добавления истории

                return addedEquipment; // Возвращаем добавленный объект
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in AddEquipmentAsync: {ex}");
                throw;
            }
        }

        public static async Task<Equipment> UpdateEquipmentAsync(Equipment updatedEquipment, Equipment originalEquipment, long changedByUserId, string notes)
        {
            EnsureClientInitialized(); // Проверка инициализации
            if (updatedEquipment == null) throw new ArgumentNullException(nameof(updatedEquipment));
            if (originalEquipment == null) throw new ArgumentNullException(nameof(originalEquipment));
            Debug.WriteLine($"SupabaseService: Updating Equipment ID {updatedEquipment.EquipmentId} by UserID {changedByUserId}...");

            try
            {
                // 1. Определяем, что изменилось для истории
                var historyEntry = new AssignmentHistory
                {
                    EquipmentId = updatedEquipment.EquipmentId,
                    ChangedByUserId = changedByUserId,
                    Notes = notes
                };

                bool changed = false; // Флаг, были ли ЗНАЧИМЫЕ изменения для истории
                string changeType = "";

                // Сравниваем ключевые поля
                if (originalEquipment.StatusId != updatedEquipment.StatusId)
                {
                    historyEntry.PreviousStatusId = originalEquipment.StatusId;
                    historyEntry.NewStatusId = updatedEquipment.StatusId;
                    changeType += "Status Changed; ";
                    changed = true;
                }
                if (originalEquipment.LocationId != updatedEquipment.LocationId)
                {
                    historyEntry.PreviousLocationId = originalEquipment.LocationId;
                    historyEntry.NewLocationId = updatedEquipment.LocationId;
                    changeType += "Moved; ";
                    changed = true;
                }
                if (originalEquipment.AssignedUserId != updatedEquipment.AssignedUserId)
                {
                    historyEntry.PreviousAssignedUserId = originalEquipment.AssignedUserId;
                    historyEntry.NewAssignedUserId = updatedEquipment.AssignedUserId;
                    changeType += "Assigned/Returned; ";
                    changed = true;
                }

                // Проверяем, нужно ли записывать историю, если изменились другие поля или есть заметка
                bool otherFieldsChanged = originalEquipment.Name != updatedEquipment.Name ||
                                          originalEquipment.SerialNumber != updatedEquipment.SerialNumber ||
                                          originalEquipment.InventoryNumber != updatedEquipment.InventoryNumber ||
                                          originalEquipment.Description != updatedEquipment.Description ||
                                          originalEquipment.PurchaseDate != updatedEquipment.PurchaseDate ||
                                          originalEquipment.WarrantyExpiryDate != updatedEquipment.WarrantyExpiryDate ||
                                          originalEquipment.CategoryId != updatedEquipment.CategoryId;

                if (!changed && (otherFieldsChanged || !string.IsNullOrWhiteSpace(notes)))
                {
                    // Если ключевые поля не менялись, но изменились другие или есть заметка
                    changeType = "Updated"; // Общий тип изменения
                    changed = true; // Ставим флаг, чтобы история записалась
                }

                historyEntry.ChangeType = changeType.Trim().TrimEnd(';');


                // 2. Обновляем запись оборудования в БД
                // Устанавливаем UpdatedAt вручную перед отправкой
                updatedEquipment.UpdatedAt = DateTimeOffset.UtcNow;

                // Отправляем обновленный объект целиком
                var updateResponse = await _client.From<Equipment>()
                                                 .Update(updatedEquipment);

                var updatedResult = updateResponse?.Models?.FirstOrDefault();

                if (updatedResult == null)
                {
                    var errorReason = updateResponse?.ResponseMessage?.ReasonPhrase ?? "Unknown Reason";
                    var errorContent = updateResponse != null ? await updateResponse.ResponseMessage.Content.ReadAsStringAsync() : "Response is null";
                    string fullErrorMessage = $"Failed to update equipment. Status: {errorReason}. DB Error: {errorContent}";
                    Debug.WriteLine($"!!! SupabaseService: {fullErrorMessage}");
                    throw new Exception(fullErrorMessage);
                }
                Debug.WriteLine($"SupabaseService: Equipment ID {updatedResult.EquipmentId} updated.");


                // 3. Добавляем запись в историю, если были изменения (флаг changed)
                if (changed)
                {
                    await AddHistoryEntryAsync(historyEntry);
                }
                else
                {
                    Debug.WriteLine($"SupabaseService: No significant changes detected for Equipment ID {updatedEquipment.EquipmentId}, history skipped.");
                }


                return updatedResult; // Возвращаем обновленный объект
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in UpdateEquipmentAsync: {ex}");
                throw;
            }
        }

        public static async Task DeleteEquipmentAsync(long equipmentId, long changedByUserId, string notes = "Equipment deleted")
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine($"SupabaseService: Deleting Equipment ID {equipmentId} by UserID {changedByUserId}...");

            try
            {
                // 1. Сначала получаем данные об оборудовании для истории
                var getResponse = await _client.From<Equipment>()
                                           .Filter("equipment_id", Postgrest.Constants.Operator.Equals, equipmentId.ToString())
                                           .Limit(1)
                                           .Get();
                var equipmentToDelete = getResponse?.Models?.FirstOrDefault();

                if (equipmentToDelete == null)
                {
                    string message = $"Equipment with ID {equipmentId} not found for deletion.";
                    Debug.WriteLine($"!!! SupabaseService: {message}");
                    throw new KeyNotFoundException(message);
                }

                // 2. Добавляем запись в историю ПЕРЕД удалением (если хотим сохранить историю)
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
                await AddHistoryEntryAsync(historyEntry);

                // 3. Удаляем само оборудование
                await _client.From<Equipment>()
                             .Filter("equipment_id", Postgrest.Constants.Operator.Equals, equipmentId.ToString())
                             .Delete();

                Debug.WriteLine($"SupabaseService: Equipment with ID {equipmentId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in DeleteEquipmentAsync for ID {equipmentId}: {ex}");
                throw; // Пробрасываем исключение
            }
        }


        // --- Метод для работы с Историей ---
        public static async Task AddHistoryEntryAsync(AssignmentHistory historyEntry)
        {
            EnsureClientInitialized(); // Проверка инициализации
            if (historyEntry == null) throw new ArgumentNullException(nameof(historyEntry));

            historyEntry.ChangeTimestamp = DateTimeOffset.UtcNow; // Устанавливаем время прямо перед записью
            Debug.WriteLine($"SupabaseService: Adding History for Equipment ID {historyEntry.EquipmentId}, Type: {historyEntry.ChangeType ?? "N/A"}...");
            try
            {
                // Отправляем объект истории на вставку
                var response = await _client.From<AssignmentHistory>().Insert(historyEntry);

                // Проверяем ответ (опционально, Insert обычно бросает исключение при ошибке)
                if (response?.ResponseMessage != null && !response.ResponseMessage.IsSuccessStatusCode)
                {
                    var errorContent = await response.ResponseMessage.Content.ReadAsStringAsync();
                    Debug.WriteLine($"!!! SupabaseService: Failed to add history entry. Status: {response.ResponseMessage.ReasonPhrase}. DB Error: {errorContent}");
                    // Можно решить, стоит ли падать из-за ошибки истории
                    // throw new Exception($"Failed to add history entry: {response.ResponseMessage.ReasonPhrase}");
                }
                else
                {
                    Debug.WriteLine($"SupabaseService: History entry added.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in AddHistoryEntryAsync: {ex}");
                // Можно проигнорировать или пробросить дальше
                // throw;
            }
        }

        public static async Task<List<AssignmentHistory>> GetEquipmentHistoryAsync(long equipmentId)
        {
            EnsureClientInitialized(); // Проверка инициализации
            Debug.WriteLine($"SupabaseService: Getting History for Equipment ID {equipmentId}...");
            try
            {
                var response = await _client.From<AssignmentHistory>()
                                           // Загружаем данные пользователя, внесшего изменения
                                           .Select("*, changed_by_user:users!changed_by_user_id(user_id, first_name, last_name, username)")
                                           .Filter("equipment_id", Postgrest.Constants.Operator.Equals, equipmentId.ToString())
                                           .Order("change_timestamp", Postgrest.Constants.Ordering.Descending)
                                           .Get();

                Debug.WriteLine($"SupabaseService: GetHistory Status: {response?.ResponseMessage?.StatusCode}");

                if (response != null && response.ResponseMessage.IsSuccessStatusCode)
                {
                    return response.Models ?? new List<AssignmentHistory>();
                }
                else
                {
                    var errorReason = response?.ResponseMessage?.ReasonPhrase ?? "Unknown Reason";
                    var errorContent = response != null ? await response.ResponseMessage.Content.ReadAsStringAsync() : "Response is null";
                    string fullErrorMessage = $"Failed to fetch history. Status: {errorReason}. DB Error: {errorContent}";
                    Debug.WriteLine($"!!! SupabaseService: {fullErrorMessage}");
                    throw new Exception(fullErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! SupabaseService: Exception in GetEquipmentHistoryAsync for ID {equipmentId}: {ex}");
                throw;
            }
        }


        // --- Временная заглушка для ID пользователя ---
        public static long GetCurrentUserIdPlaceholder()
        {
            // Возвращаем ID пользователя (например, администратора или системного пользователя)
            // который будет использоваться для записи истории по умолчанию.
            long defaultUserId = 1; // ID пользователя 'admin' из вашего скрипта
            Debug.WriteLine($"SupabaseService: Using placeholder User ID: {defaultUserId}");
            return defaultUserId;
        }
    } // Конец класса SupabaseService
} // Конец namespace