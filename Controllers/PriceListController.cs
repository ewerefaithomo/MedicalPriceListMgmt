using MedicalPriceListApp.Data;
using MedicalPriceListApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalPriceListApp.Controllers
{
    public class PriceListController : Controller
    {
        private readonly ApplicationDbContext _context;

        // =========================================================
        // ACCESS CODE
        // =========================================================
        private const string ManagementAccessCode = "1983";

        // Session key used after successful access-code verification
        private const string ManagementAuthorizedSession =
            "PriceListManagementAuthorized";

        public PriceListController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // GET: PriceList
        // DASHBOARD
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalItems =
                await _context.PriceLists.CountAsync();

            ViewBag.TotalDepartments =
                await _context.PriceLists
                    .Select(p => p.DEPARTMENT)
                    .Distinct()
                    .CountAsync();

            ViewBag.AvgPrice =
                await _context.PriceLists.AnyAsync()
                    ? await _context.PriceLists
                        .AverageAsync(p => p.CURRENT_PRICE)
                    : 0;

            return View();
        }


        // =========================================================
        // GET: PriceList/ViewList
        // DISPLAY AND SEARCH MEDICAL PRICE LIST
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ViewList(
            string? bankCode,
            string? service,
            string? department)
        {
            IQueryable<PriceList> query =
                _context.PriceLists.AsNoTracking();


            // -----------------------------------------------------
            // Search by Bank Code
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(bankCode))
            {
                bankCode = bankCode.Trim();

                query = query.Where(p =>
                    p.BANK_CODE.Contains(bankCode));
            }


            // -----------------------------------------------------
            // Search by Service
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(service))
            {
                service = service.Trim();

                query = query.Where(p =>
                    p.SERVICES.Contains(service));
            }


            // -----------------------------------------------------
            // Filter by Department
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(department))
            {
                department = department.Trim();

                query = query.Where(p =>
                    p.DEPARTMENT == department);
            }


            // -----------------------------------------------------
            // Department dropdown
            // -----------------------------------------------------
            ViewBag.Departments =
                await _context.PriceLists
                    .AsNoTracking()
                    .Select(p => p.DEPARTMENT)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();


            // -----------------------------------------------------
            // Preserve search values
            // -----------------------------------------------------
            ViewBag.CurrentBankCode = bankCode;
            ViewBag.CurrentService = service;
            ViewBag.CurrentDepartment = department;


            // -----------------------------------------------------
            // Retrieve records
            // -----------------------------------------------------
            var priceLists = await query
                .OrderBy(p => p.DEPARTMENT)
                .ThenBy(p => p.SERVICES)
                .ThenBy(p => p.BANK_CODE)
                .ToListAsync();


            ViewBag.ResultCount = priceLists.Count;

            return View(priceLists);
        }


        // =========================================================
        // MANAGEMENT ACCESS CODE
        // =========================================================


        // =========================================================
        // GET: PriceList/ModifyList
        //
        // This is the entry point for:
        // Manage
        // Manage Price List
        // =========================================================
        [HttpGet]
        public IActionResult ModifyList()
        {
            // Check whether user has already entered
            // the correct management access code.
            bool authorized =
                HttpContext.Session.GetString(
                    ManagementAuthorizedSession) == "YES";

            if (!authorized)
            {
                return View("ManagementAccessCode");
            }

            return RedirectToAction(nameof(ManageService));
        }


        // =========================================================
        // POST: PriceList/VerifyManagementAccessCode
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyManagementAccessCode(
            string accessCode)
        {
            if (string.IsNullOrWhiteSpace(accessCode))
            {
                ModelState.AddModelError(
                    "accessCode",
                    "Please enter the Access Code.");

                return View("ManagementAccessCode");
            }

            accessCode = accessCode.Trim();


            // -----------------------------------------------------
            // Verify Access Code
            // -----------------------------------------------------
            if (accessCode != ManagementAccessCode)
            {
                ModelState.AddModelError(
                    "accessCode",
                    "Invalid Access Code. Please enter the correct Access Code.");

                return View("ManagementAccessCode");
            }


            // -----------------------------------------------------
            // Authorize management session
            // -----------------------------------------------------
            HttpContext.Session.SetString(
                ManagementAuthorizedSession,
                "YES");


            // -----------------------------------------------------
            // Redirect to management form
            // -----------------------------------------------------
            return RedirectToAction(nameof(ManageService));
        }


        // =========================================================
        // GET: PriceList/ManageService
        //
        // Displays:
        // - Service combo box
        // - Bank Code
        // - Current Price
        // - Revenue Generating Fund
        // - Department
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManageService(
            string? bankCode)
        {
            if (!IsManagementAuthorized())
            {
                return RedirectToAction(nameof(ModifyList));
            }


            // -----------------------------------------------------
            // Load all services for combo box
            // -----------------------------------------------------
            var services = await _context.PriceLists
                .AsNoTracking()
                .OrderBy(p => p.SERVICES)
                .ThenBy(p => p.BANK_CODE)
                .ToListAsync();


            ViewBag.Services = services;


            // -----------------------------------------------------
            // No service selected yet
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(bankCode))
            {
                return View(new PriceList());
            }


            bankCode = bankCode.Trim();


            // -----------------------------------------------------
            // Find selected service
            // -----------------------------------------------------
            var selectedService =
                await _context.PriceLists
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == bankCode);


            if (selectedService == null)
            {
                TempData["Error"] =
                    "The selected service could not be found.";

                return View(new PriceList());
            }


            return View(selectedService);
        }


        // =========================================================
        // POST: PriceList/UpdateService
        //
        // Updates selected service.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateService(
            string originalBankCode,
            PriceList priceList)
        {
            if (!IsManagementAuthorized())
            {
                return RedirectToAction(nameof(ModifyList));
            }


            // -----------------------------------------------------
            // Clean values
            // -----------------------------------------------------
            originalBankCode =
                originalBankCode?.Trim() ?? string.Empty;

            priceList.BANK_CODE =
                priceList.BANK_CODE?.Trim() ?? string.Empty;

            priceList.SERVICES =
                priceList.SERVICES?.Trim() ?? string.Empty;

            priceList.REVENUE_GENERATING_FUND =
                priceList.REVENUE_GENERATING_FUND?.Trim()
                ?? string.Empty;

            priceList.DEPARTMENT =
                priceList.DEPARTMENT?.Trim() ?? string.Empty;


            // -----------------------------------------------------
            // Validate original Bank Code
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(originalBankCode))
            {
                TempData["Error"] =
                    "The original Bank Code was not supplied.";

                return RedirectToAction(nameof(ManageService));
            }


            // -----------------------------------------------------
            // Validate Bank Code
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.BANK_CODE))
            {
                ModelState.AddModelError(
                    "BANK_CODE",
                    "Bank Code is required.");
            }


            // -----------------------------------------------------
            // Validate Service
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.SERVICES))
            {
                ModelState.AddModelError(
                    "SERVICES",
                    "Service name is required.");
            }


            // -----------------------------------------------------
            // Validate Revenue Generating Fund
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(
                    priceList.REVENUE_GENERATING_FUND))
            {
                ModelState.AddModelError(
                    "REVENUE_GENERATING_FUND",
                    "Revenue Generating Fund is required.");
            }


            // -----------------------------------------------------
            // Validate Department
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.DEPARTMENT))
            {
                ModelState.AddModelError(
                    "DEPARTMENT",
                    "Department is required.");
            }


            // -----------------------------------------------------
            // Validate Price
            // -----------------------------------------------------
            if (priceList.CURRENT_PRICE < 0)
            {
                ModelState.AddModelError(
                    "CURRENT_PRICE",
                    "Current Price cannot be negative.");
            }


            // -----------------------------------------------------
            // Get existing record
            // -----------------------------------------------------
            var existingPriceList =
                await _context.PriceLists
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == originalBankCode);


            if (existingPriceList == null)
            {
                TempData["Error"] =
                    "The service record could not be found.";

                return RedirectToAction(nameof(ManageService));
            }


            // -----------------------------------------------------
            // Check if the new Bank Code already belongs
            // to another record.
            // -----------------------------------------------------
            if (!string.Equals(
                    originalBankCode,
                    priceList.BANK_CODE,
                    StringComparison.OrdinalIgnoreCase))
            {
                bool newBankCodeExists =
                    await _context.PriceLists
                        .AnyAsync(p =>
                            p.BANK_CODE == priceList.BANK_CODE);

                if (newBankCodeExists)
                {
                    ModelState.AddModelError(
                        "BANK_CODE",
                        "The new Bank Code already exists. Please enter a different Bank Code.");
                }
            }


            // -----------------------------------------------------
            // Validation failed
            // -----------------------------------------------------
            if (!ModelState.IsValid)
            {
                var services = await _context.PriceLists
                    .AsNoTracking()
                    .OrderBy(p => p.SERVICES)
                    .ThenBy(p => p.BANK_CODE)
                    .ToListAsync();

                ViewBag.Services = services;

                return View("ManageService", priceList);
            }


            // -----------------------------------------------------
            // CASE 1:
            // Bank Code has NOT changed.
            // Simply update the existing record.
            // -----------------------------------------------------
            if (string.Equals(
                    originalBankCode,
                    priceList.BANK_CODE,
                    StringComparison.OrdinalIgnoreCase))
            {
                existingPriceList.SERVICES =
                    priceList.SERVICES;

                existingPriceList.CURRENT_PRICE =
                    priceList.CURRENT_PRICE;

                existingPriceList.REVENUE_GENERATING_FUND =
                    priceList.REVENUE_GENERATING_FUND;

                existingPriceList.DEPARTMENT =
                    priceList.DEPARTMENT;

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Service updated successfully.";

                return RedirectToAction(
                    nameof(ManageService),
                    new
                    {
                        bankCode = priceList.BANK_CODE
                    });
            }


            // -----------------------------------------------------
            // CASE 2:
            // Bank Code has changed.
            //
            // Because BANK_CODE is the primary key, EF Core
            // should not modify it directly.
            //
            // Instead:
            // 1. Remove old record.
            // 2. Add new record with new Bank Code.
            // -----------------------------------------------------
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.PriceLists.Remove(existingPriceList);

                var newPriceList = new PriceList
                {
                    BANK_CODE =
                        priceList.BANK_CODE,

                    SERVICES =
                        priceList.SERVICES,

                    CURRENT_PRICE =
                        priceList.CURRENT_PRICE,

                    REVENUE_GENERATING_FUND =
                        priceList.REVENUE_GENERATING_FUND,

                    DEPARTMENT =
                        priceList.DEPARTMENT
                };

                _context.PriceLists.Add(newPriceList);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["Success"] =
                    "Service updated successfully, including the Bank Code.";

                return RedirectToAction(
                    nameof(ManageService),
                    new
                    {
                        bankCode = newPriceList.BANK_CODE
                    });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "The service could not be updated. No changes were saved.";

                return RedirectToAction(
                    nameof(ManageService),
                    new
                    {
                        bankCode = originalBankCode
                    });
            }
        }


        // =========================================================
        // POST: PriceList/DeleteService
        //
        // Deletes selected service.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(
            string bankCode)
        {
            if (!IsManagementAuthorized())
            {
                return RedirectToAction(nameof(ModifyList));
            }


            bankCode =
                bankCode?.Trim() ?? string.Empty;


            if (string.IsNullOrWhiteSpace(bankCode))
            {
                TempData["Error"] =
                    "No service was selected for deletion.";

                return RedirectToAction(nameof(ManageService));
            }


            // -----------------------------------------------------
            // Find record
            // -----------------------------------------------------
            var priceList =
                await _context.PriceLists
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == bankCode);


            if (priceList == null)
            {
                TempData["Error"] =
                    "The selected service could not be found.";

                return RedirectToAction(nameof(ManageService));
            }


            // Keep service name for completion message
            string serviceName =
                priceList.SERVICES;


            // -----------------------------------------------------
            // Delete
            // -----------------------------------------------------
            _context.PriceLists.Remove(priceList);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // Completion message
            // -----------------------------------------------------
            TempData["Success"] =
                $"Service \"{serviceName}\" was deleted successfully.";


            return RedirectToAction(
                nameof(ManageService));
        }


        // =========================================================
        // LOGOUT MANAGEMENT SESSION
        // =========================================================
        [HttpGet]
        public IActionResult ExitManagement()
        {
            HttpContext.Session.Remove(
                ManagementAuthorizedSession);

            TempData["Success"] =
                "You have exited Price List Management.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CHECK MANAGEMENT AUTHORIZATION
        // =========================================================
        private bool IsManagementAuthorized()
        {
            return HttpContext.Session.GetString(
                ManagementAuthorizedSession) == "YES";
        }


        // =========================================================
        // ADD NEW SERVICE ACCESS CODE
        // =========================================================

        // GET: PriceList/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View("AccessCode");
        }


        // POST: PriceList/VerifyAccessCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyAccessCode(
            string accessCode)
        {
            if (string.IsNullOrWhiteSpace(accessCode))
            {
                ModelState.AddModelError(
                    "accessCode",
                    "Please enter the Access Code.");

                return View("AccessCode");
            }

            accessCode = accessCode.Trim();

            if (accessCode != ManagementAccessCode)
            {
                ModelState.AddModelError(
                    "accessCode",
                    "Invalid Access Code. Please enter the correct Access Code.");

                return View("AccessCode");
            }

            return RedirectToAction(nameof(AddService));
        }


        // =========================================================
        // GET: PriceList/AddService
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AddService()
        {
            await LoadAddServiceLists();

            return View();
        }


        // =========================================================
        // POST: PriceList/AddService
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(
            PriceList priceList)
        {
            // -----------------------------------------------------
            // Clean values
            // -----------------------------------------------------
            priceList.BANK_CODE =
                priceList.BANK_CODE?.Trim() ?? string.Empty;

            priceList.SERVICES =
                priceList.SERVICES?.Trim() ?? string.Empty;

            priceList.REVENUE_GENERATING_FUND =
                priceList.REVENUE_GENERATING_FUND?.Trim()
                ?? string.Empty;

            priceList.DEPARTMENT =
                priceList.DEPARTMENT?.Trim() ?? string.Empty;


            // -----------------------------------------------------
            // Validate Bank Code
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.BANK_CODE))
            {
                ModelState.AddModelError(
                    "BANK_CODE",
                    "Bank Code is required.");
            }


            // -----------------------------------------------------
            // Validate Service
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.SERVICES))
            {
                ModelState.AddModelError(
                    "SERVICES",
                    "Service name is required.");
            }


            // -----------------------------------------------------
            // Validate Revenue Generating Fund
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(
                    priceList.REVENUE_GENERATING_FUND))
            {
                ModelState.AddModelError(
                    "REVENUE_GENERATING_FUND",
                    "Revenue Generating Fund is required.");
            }


            // -----------------------------------------------------
            // Validate Department
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(priceList.DEPARTMENT))
            {
                ModelState.AddModelError(
                    "DEPARTMENT",
                    "Department is required.");
            }


            // -----------------------------------------------------
            // Check duplicate Bank Code
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(priceList.BANK_CODE))
            {
                bool exists =
                    await _context.PriceLists
                        .AnyAsync(p =>
                            p.BANK_CODE ==
                            priceList.BANK_CODE);

                if (exists)
                {
                    ModelState.AddModelError(
                        "BANK_CODE",
                        "This Bank Code already exists. Please enter a different Bank Code.");
                }
            }


            // -----------------------------------------------------
            // Validation failed
            // -----------------------------------------------------
            if (!ModelState.IsValid)
            {
                await LoadAddServiceLists();

                return View(priceList);
            }


            // -----------------------------------------------------
            // Save
            // -----------------------------------------------------
            _context.PriceLists.Add(priceList);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "New Service added successfully.";


            return RedirectToAction(
                nameof(AddService));
        }


        // =========================================================
        // LOAD REVENUE FUNDS AND DEPARTMENTS
        // =========================================================
        private async Task LoadAddServiceLists()
        {
            ViewBag.RevenueGeneratingFunds =
                await _context.PriceLists
                    .AsNoTracking()
                    .Select(p =>
                        p.REVENUE_GENERATING_FUND)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();


            ViewBag.Departments =
                await _context.PriceLists
                    .AsNoTracking()
                    .Select(p => p.DEPARTMENT)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();
        }


        // =========================================================
        // GET: PriceList/Edit
        //
        // Kept for compatibility with any existing Edit view.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            string? bankCode)
        {
            if (string.IsNullOrWhiteSpace(bankCode))
            {
                return NotFound();
            }

            bankCode = bankCode.Trim();

            var priceList =
                await _context.PriceLists
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == bankCode);

            if (priceList == null)
            {
                return NotFound();
            }

            return View(priceList);
        }


        // =========================================================
        // POST: PriceList/Edit
        //
        // Kept for compatibility with existing Edit view.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string bankCode,
            PriceList priceList)
        {
            if (string.IsNullOrWhiteSpace(bankCode))
            {
                return NotFound();
            }

            bankCode = bankCode.Trim();


            if (!string.Equals(
                    bankCode,
                    priceList.BANK_CODE,
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                var existingPriceList =
                    await _context.PriceLists
                        .FirstOrDefaultAsync(
                            p => p.BANK_CODE == bankCode);

                if (existingPriceList == null)
                {
                    return NotFound();
                }


                existingPriceList.SERVICES =
                    priceList.SERVICES;

                existingPriceList.CURRENT_PRICE =
                    priceList.CURRENT_PRICE;

                existingPriceList.REVENUE_GENERATING_FUND =
                    priceList.REVENUE_GENERATING_FUND;

                existingPriceList.DEPARTMENT =
                    priceList.DEPARTMENT;


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Price item updated successfully!";

                return RedirectToAction(
                    nameof(ModifyList));
            }


            return View(priceList);
        }


        // =========================================================
        // GET: PriceList/Delete
        //
        // Kept for compatibility with existing Delete view.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(
            string? bankCode)
        {
            if (string.IsNullOrWhiteSpace(bankCode))
            {
                return NotFound();
            }

            bankCode = bankCode.Trim();

            var priceList =
                await _context.PriceLists
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == bankCode);

            if (priceList == null)
            {
                return NotFound();
            }

            return View(priceList);
        }


        // =========================================================
        // POST: PriceList/Delete
        //
        // Kept for compatibility with existing Delete view.
        // =========================================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            string bankCode)
        {
            if (string.IsNullOrWhiteSpace(bankCode))
            {
                return NotFound();
            }

            bankCode = bankCode.Trim();

            var priceList =
                await _context.PriceLists
                    .FirstOrDefaultAsync(
                        p => p.BANK_CODE == bankCode);

            if (priceList != null)
            {
                _context.PriceLists.Remove(priceList);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Price item removed successfully!";
            }

            return RedirectToAction(
                nameof(ModifyList));
        }
    }
}