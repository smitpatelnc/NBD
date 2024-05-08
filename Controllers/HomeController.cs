using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBD3.Data;
using NBD3.Models;

public class HomeController : Controller
{
    private readonly NBDContext _context;

    public HomeController(NBDContext context)
    {
        _context = context;
    }

    public IActionResult Index(DateTime? startDate, DateTime? endDate, string selectStatus)
    {
        // Fetch bid statistics
        var totalBids = _context.Bids.Count();
        var approvedBids = _context.Bids.Count(b => b.BidApprove);
        var pendingBids = _context.Bids.Count(b => !b.BidApprove);
        var rejectedBids = _context.Bids.Count(b => b.BidRejectedManager || b.BidRejectedClient);

        // Pass bid statistics to the view
        ViewBag.TotalBids = totalBids;
        ViewBag.ApprovedBids = approvedBids;
        ViewBag.PendingBids = pendingBids;
        ViewBag.RejectedBids = rejectedBids;

        // Query projects
        var projects = _context.Projects.AsQueryable();

        // Filter projects based on start date
        if (startDate != null)
        {
            var startDateOnly = new DateOnly(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day);
            projects = projects.Where(p => p.ProjectStartDate >= startDateOnly);
        }

        // Filter projects based on end date
        if (endDate != null)
        {
            var endDateOnly = new DateOnly(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day);
            projects = projects.Where(p => p.ProjectEndDate <= endDateOnly);
        }

        // Filter projects based on selected status
        if (!string.IsNullOrEmpty(selectStatus))
        {
            projects = projects.Where(p => p.ProjectStatus == selectStatus);
        }

        // Include related data (Location) and Bid amount
        projects = projects.Include(p => p.Location).Include(p => p.Bids);

        // Get distinct project statuses for dropdown list
        var distinctStatuses = _context.Projects.Select(p => p.ProjectStatus).Distinct().ToList();

        // Set selectStatus to null if it's empty or null
        if (string.IsNullOrEmpty(selectStatus))
        {
            selectStatus = null;
        }

        ViewBag.SelectedStatus = selectStatus;
        ViewBag.Statuses = distinctStatuses;

        // Materialize the query and pass the list of projects to the view
        var projectList = projects.ToList();
        ViewBag.Projects = projectList;

        return View(projectList);
    }

    public IActionResult Detail(int projectId)
    {
        var project = _context.Projects
            .Include(p => p.Location)
            .Include(p => p.Bids)
            .FirstOrDefault(p => p.ProjectId == projectId);

        if (project == null)
        {
            // Project not found, return a custom error view or redirect to a generic error page
            return View("ProjectNotFoundError");
            // or return RedirectToAction("Index", "Error"); // Example for redirecting to a generic error page
        }

        return View(project);
    }

}

//    // Action to approve a bid
//    public IActionResult ApproveBid(int id)
//    {
//        var bid = _context.Bids.FirstOrDefault(b => b.BidId == id);
//        if (bid != null)
//        {
//            bid.BidApprove = true;
//            bid.BidStatus = "Approved";
//            _context.SaveChanges();
//        }
//        return RedirectToAction("Index");
//    }

//    // Action to reject a bid
//    public IActionResult RejectBid(int id)
//    {
//        var bid = _context.Bids.FirstOrDefault(b => b.BidId == id);
//        if (bid != null)
//        {
//            bid.BidRejectedManager = true;
//            bid.BidStatus = "Rejected by Manager";
//            _context.SaveChanges();
//        }
//        return RedirectToAction("Index");
//    }

//    // Action to set bid cost
//    public IActionResult SetBidCost(int id, double cost)
//    {
//        var bid = _context.Bids.FirstOrDefault(b => b.BidId == id);
//        if (bid != null)
//        {
//            bid.BidCost = cost;
//            _context.SaveChanges();
//        }
//        return RedirectToAction("Index");
//    }

//    // Action to mark bid as pending
//    public IActionResult SetBidPending(int id)
//    {
//        var bid = _context.Bids.FirstOrDefault(b => b.BidId == id && !b.BidApprove);
//        if (bid != null)
//        {
//            bid.BidStatus = "Waiting Approval"; // Update bid status to "Waiting Approval"
//            _context.SaveChanges();
//        }
//        return RedirectToAction("Index");
//    }

//}
