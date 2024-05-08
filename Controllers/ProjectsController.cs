using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NBD3.Data;
using NBD3.Models;

namespace NBD3.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Sales Assoc, Designer")]
    public class ProjectsController : Controller
    {
        private readonly NBDContext _context;

        public ProjectsController(NBDContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin, General Manager, Sales Assoc, Designer")]
        public async Task<IActionResult> Index(string searchString, int? clientId, string sortOrder, DateOnly? startDateFilter, DateOnly? endDateFilter, int? page)
        {
            ViewBag.NameSortParam = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DescriptionSortParam = sortOrder == "description" ? "description_desc" : "description";
            ViewBag.StartDateSortParam = sortOrder == "start_date" ? "start_date_desc" : "start_date";
            ViewBag.EndDateSortParam = sortOrder == "end_date" ? "end_date_desc" : "end_date";
            ViewBag.ClientSortParam = sortOrder == "client" ? "client_desc" : "client";
            ViewBag.LocationSortParam = sortOrder == "location" ? "location_desc" : "location";

            // Query projects from the database
            var projectsQuery = _context.Projects
                .Include(p => p.Client)
                .Include(p => p.Location)
                .Where(project =>
                    (string.IsNullOrEmpty(searchString) || project.ProjectName.ToLower().Contains(searchString.ToLower())) &&
                    (!clientId.HasValue || project.ClientId == clientId.Value));

            if (startDateFilter.HasValue)
            {
                DateOnly startDate = startDateFilter.Value;
                projectsQuery = projectsQuery.Where(project => project.ProjectStartDate >= startDate);
            }

            if (endDateFilter.HasValue)
            {
                DateOnly endDate = endDateFilter.Value;
                projectsQuery = projectsQuery.Where(project =>
                    !project.ProjectEndDate.HasValue || project.ProjectEndDate.Value <= endDate);
            }

            // Apply sorting
            projectsQuery = sortOrder switch
            {
                "name_desc" => projectsQuery.OrderByDescending(p => p.ProjectName),
                "description" => projectsQuery.OrderBy(p => p.ProjectDescription),
                "description_desc" => projectsQuery.OrderByDescending(p => p.ProjectDescription),
                "start_date" => projectsQuery.OrderBy(p => p.ProjectStartDate),
                "start_date_desc" => projectsQuery.OrderByDescending(p => p.ProjectStartDate),
                "end_date" => projectsQuery.OrderBy(p => p.ProjectEndDate),
                "end_date_desc" => projectsQuery.OrderByDescending(p => p.ProjectEndDate),
                "client" => projectsQuery.OrderBy(p => p.Client.ClientFirstName),
                "client_desc" => projectsQuery.OrderByDescending(p => p.Client.ClientFirstName),
                "location" => projectsQuery.OrderBy(p => p.Location.LocationName),
                "location_desc" => projectsQuery.OrderByDescending(p => p.Location.LocationName),
                _ => projectsQuery.OrderBy(p => p.ProjectName),
            };

            int pageSize = 5; // Adjust as needed
            var pagedProjects = await PaginatedList<Project>.CreateAsync(projectsQuery.AsNoTracking(), page ?? 1, pageSize);

            ViewBag.Clients = await _context.Clients.ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.StartDateFilter = startDateFilter;
            ViewBag.EndDateFilter = endDateFilter;

            return View(pagedProjects);
        }



        [Authorize(Roles = "Admin, General Manager, Sales Assoc, Designer")]
        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Client)
                .Include(p => p.Location)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin, General Manager,Designer")]
        public IActionResult Create()
        {

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "ContactFullName");
            // ViewData["LocationId"] = new SelectList(_context.Locations, "LocationId", "LocationName");
            ViewData["StatusID"] = new SelectList(_context.Statuss, "StatusID", "StatusName");
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin, General Manager, Designer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,ProjectName,ProjectDescription,ProjectLocation,ProjectStartDate,ProjectEndDate,ClientId,LocationId,StatusID")] Project project)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "ContactFullName", project.ClientId);
            // ViewData["LocationId"] = new SelectList(_context.Locations, "LocationId", "LocationName", project.LocationId);
            ViewData["StatusID"] = new SelectList(_context.Statuss, "StatusID", "StatusName", project.ProjectStatus);
            return View(project);
        }


        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin, General Manager,Designer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "ContactFullName", project.ClientId);
            ViewData["LocationId"] = new SelectList(_context.Locations, "LocationId", "LocationName", project.LocationId);
            ViewData["StatusID"] = new SelectList(_context.Statuss, "StatusID", "StatusName", project.ProjectStatus);
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin, General Manager,Designer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ProjectName,ProjectDescription,ProjectLocation,ProjectStartDate,ProjectEndDate,ClientId,LocationId,StatusID")] Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Validation for start date greater than end date
                if (project.ProjectEndDate.HasValue && project.ProjectEndDate < project.ProjectStartDate)
                {
                    ModelState.AddModelError(nameof(project.ProjectEndDate), "End date cannot be before start date.");
                    return View(project);
                }

                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "ContactFullName", project.ClientId);
            ViewData["LocationId"] = new SelectList(_context.Locations, "LocationId", "LocationName", project.LocationId);
            ViewData["StatusID"] = new SelectList(_context.Statuss, "StatusID", "StatusName", project.ProjectStatus);
            return View(project);
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin, General Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
        .AsNoTracking()
        .Include(p => p.Client) // Include related client data if needed
        .Include(p => p.Location) // Include related location data
        .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [Authorize(Roles = "Admin, General Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("There are no Projects to delete.");
            }

            var project = await _context.Projects.FindAsync(id);
            try
            {
                if (project != null)
                {
                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                }
                // Redirect to the index action of the Projects controller after deletion
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Project. Remember, you cannot delete a Project that has associated bids or other dependent data.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(project);
        }

        [Authorize(Roles = "Admin, General Manager")]
        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }
        [Authorize(Roles = "Admin, General Manager, Sales Assoc, Designer")]

        public ActionResult ProjectsAndMaintenance()
        {
            return View();
        }
    }
}
