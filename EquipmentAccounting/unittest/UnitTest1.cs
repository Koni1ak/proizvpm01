using Microsoft.VisualStudio.TestTools.UnitTesting;
using EquipmentAccounting.Services; // Используем сервис из основного проекта
using EquipmentAccounting.Models;   // Используем модели из основного проекта
using System;
using System.Threading.Tasks; // Для async Task
using EquipmentAccounting;

namespace EquipmentAccounting.Tests
{
    [TestClass]
    public class SupabaseServiceTests
    {
        // Важное примечание:
        // Из-за статической природы SupabaseService и прямого использования Supabase.Client,
        // эти тесты не могут полностью изолировать сервис от клиента без рефакторинга основного кода
        // (например, внедрения зависимостей через интерфейс).
        // Поэтому тесты здесь сосредоточены на:
        // 1. Проверке методов, не требующих инициализации клиента (GetCurrentUserIdPlaceholder).
        // 2. Проверке обработки невалидных входных данных (null аргументы).
        // Тесты, проверяющие реальную логику взаимодействия с БД (успешное добавление/удаление),
        // потребовали бы либо рефакторинга, либо интеграционного тестирования с mock-сервером Supabase.


        // --- Сценарии Успешного Выполнения (если код написан правильно) ---

        [TestMethod]
        public void GetCurrentUserIdPlaceholder_Always_ReturnsDefaultId()
        {
            // Arrange (Подготовка) - нет специфической подготовки для этого метода
            long expectedId = 1; // Ожидаемый ID по умолчанию

            // Act (Действие)
            long actualId = SupabaseService.GetCurrentUserIdPlaceholder();

            // Assert (Проверка)
            Assert.AreEqual(expectedId, actualId, "Метод GetCurrentUserIdPlaceholder должен возвращать предопределенный ID.");
            Console.WriteLine("Test Passed: GetCurrentUserIdPlaceholder_Always_ReturnsDefaultId");
        }

        // Этот тест проверяет, что код НЕ падает при передаче НЕ-null аргументов
        // Он НЕ проверяет успешность взаимодействия с БД (из-за ограничений статического клиента)
        // Он должен ПРОЙТИ, если метод может обработать вызов без NullReferenceException
        [TestMethod]
        public void UpdateEquipmentAsync_HandlesValidNonNullArguments_WithoutCrashing()
        {
            // Arrange
            var updatedEquipment = new Equipment { EquipmentId = 1, Name = "New Name" };
            var originalEquipment = new Equipment { EquipmentId = 1, Name = "Old Name" };
            long userId = 1;
            string notes = "Test update";

            // Act & Assert
            // Мы ожидаем, что *первым* упадет EnsureClientInitialized, если сервис не инициализирован.
            // Если бы сервис был инициализирован, вызов не должен был бы упасть из-за null аргументов.
            // Поэтому мы проверяем на ArgumentNullException - ее быть НЕ должно.
            try
            {
                // Попытка вызова (скорее всего, упадет на EnsureClientInitialized)
                // Если бы мы могли мокировать, мы бы проверили успешный путь.
                // В данном случае, мы лишь косвенно проверяем, что сам МЕТОД готов принять не-null аргументы.
                Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                    await SupabaseService.UpdateEquipmentAsync(updatedEquipment, originalEquipment, userId, notes)
                ).Wait(); // Ждем завершения асинхронной проверки исключения
                Console.WriteLine("Test Passed (Expected InvalidOperationException): UpdateEquipmentAsync_HandlesValidNonNullArguments");

            }
            catch (ArgumentNullException ex) // Этот тип исключения НЕ ожидается
            {
                Assert.Fail($"UpdateEquipmentAsync не должен бросать ArgumentNullException при валидных аргументах. Исключение: {ex.Message}");
            }
            catch (InvalidOperationException)
            {
                // Ожидаемое исключение, если клиент не инициализирован. Тест проходит.
                Console.WriteLine("Test Passed (Caught expected InvalidOperationException): UpdateEquipmentAsync_HandlesValidNonNullArguments");
            }
            catch (Exception ex) // Другие неожиданные исключения
            {
                Assert.Fail($"UpdateEquipmentAsync бросил неожиданное исключение: {ex.Message}");
            }
        }

        // Аналогично предыдущему, проверяем AddEquipmentAsync на готовность принять не-null аргумент
        [TestMethod]
        public void AddEquipmentAsync_HandlesValidNonNullArguments_WithoutCrashing()
        {
            // Arrange
            var newEquipment = new Equipment { Name = "New Equipment" };
            long userId = 1;
            string notes = "Test add";

            // Act & Assert
            try
            {
                Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                   await SupabaseService.AddEquipmentAsync(newEquipment, userId, notes)
                ).Wait();
                Console.WriteLine("Test Passed (Expected InvalidOperationException): AddEquipmentAsync_HandlesValidNonNullArguments");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Fail($"AddEquipmentAsync не должен бросать ArgumentNullException при валидных аргументах. Исключение: {ex.Message}");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Test Passed (Caught expected InvalidOperationException): AddEquipmentAsync_HandlesValidNonNullArguments");
            }
            catch (Exception ex)
            {
                Assert.Fail($"AddEquipmentAsync бросил неожиданное исключение: {ex.Message}");
            }
        }

        // --- Сценарии Проверки Обработки Ошибок (Эти тесты должны ПРОЙТИ, если ошибки обрабатываются правильно) ---

        [TestMethod]
        public async Task AddEquipmentAsync_ThrowsArgumentNullException_WhenEquipmentIsNull()
        {
            // Arrange
            Equipment newEquipment = null; // Невалидный ввод
            long userId = 1;
            string notes = "Test add";

            // Act & Assert
            // Проверяем, что метод бросает ArgumentNullException при передаче null
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await SupabaseService.AddEquipmentAsync(newEquipment, userId, notes)
            );
            Console.WriteLine("Test Passed: AddEquipmentAsync_ThrowsArgumentNullException_WhenEquipmentIsNull");
        }

        [TestMethod]
        public async Task UpdateEquipmentAsync_ThrowsArgumentNullException_WhenOriginalEquipmentIsNull()
        {
            // Arrange
            var updatedEquipment = new Equipment { EquipmentId = 1, Name = "New Name" };
            Equipment originalEquipment = null; // Невалидный ввод
            long userId = 1;
            string notes = "Test update";

            // Act & Assert
            // Проверяем, что метод бросает ArgumentNullException при передаче null
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await SupabaseService.UpdateEquipmentAsync(updatedEquipment, originalEquipment, userId, notes)
            );
            Console.WriteLine("Test Passed: UpdateEquipmentAsync_ThrowsArgumentNullException_WhenOriginalEquipmentIsNull");
        }
    }
}