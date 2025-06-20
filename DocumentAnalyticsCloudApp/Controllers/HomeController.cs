using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DocumentAnalyticsCloudApp.Models;
using DocumentAnalyticsCloudApp.Services;
using DocumentAnalyticsCloudApp.ViewModels;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace DocumentAnalyticsCloudApp.Controllers
{
    /*
     * HomeController handles the main application actions for document management including uploading, searching, sorting, classifying, and displaying analytics.
     * 
     * -- Developed By: Ez-Aldeen Asqool (E.A Developer) --
     */
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AzureBlobService _blobService;
        private readonly MongoDocService _mongoService;
        private readonly TextExtractionService _textExtractionService;
        private readonly DocumentClassifierService _classifier;


        public HomeController(ILogger<HomeController> logger, AzureBlobService blobService, MongoDocService mongoService, TextExtractionService textExtractionService, DocumentClassifierService classifier)
        {
            _logger = logger;
            _blobService = blobService;
            _mongoService = mongoService;
            _textExtractionService = textExtractionService;
            _classifier = classifier;
        }


        /*
         * Index Action:
         * This Action responsible for:
         * 1- return Index.cshtml page, and show these informations in it:
         *      - Total documents uploaded                        -- OR --   -Total documents contains search value (when entering search value)
         *      - Total storage used for all documents uploaded   -- OR --   -Total storage used for all documents contains search value (when entering search value)
         *      - Search Time
         * 
         * 2- Enables search field, take search field value (q value) and return documents that has the search value in its content.
         * 
         * 2- Show Sort and Classify buttons.
         * 
         */
        [HttpGet]
        public async Task<IActionResult> Index(string q)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var stopwatch = Stopwatch.StartNew();

            //Get all documents uploaded by user by using MongoDocService if search value is null (q == null) --- OR --- if earch value has value (q != null) get documents that contains q value on its content.
            List<DocumentModel> results = string.IsNullOrWhiteSpace(q)
                ? await _mongoService.GetAllDocumentsByUserAsync(userId)
                : await _mongoService.SearchDocumentsAsync(q, userId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string pattern = Regex.Escape(q);
                foreach (var doc in results)
                {
                    //highlight matching keywords (q) in the document content.  
                    string highlighted = Regex.Replace(doc.Content, pattern, match =>
                        $"<mark>{match.Value}</mark>", RegexOptions.IgnoreCase);

                    doc.Content = highlighted;
                }
            }

            stopwatch.Stop();

            var model = new DashboardViewModel
            {
                TotalDocuments = results.Count,
                TotalSizeInMB = Math.Round(results.Sum(d => d.SizeInBytes) / 1024.0 / 1024.0, 2),
                SearchExecutionMs = stopwatch.Elapsed.TotalMilliseconds
            };

            ViewBag.DashboardStats = model;

            return View(results); 
        }


        /*
         * Classify Action:
         * This Action responsible for:
         * 1- return Classify.cshtml page, and show these informations in it:
         *      - Total Documents Uploaded
         *      - Total Storage Used
         *      - Total Classification Time
         *      - documents uploaded grouped by its classification (category)
         * 
         */


        [HttpGet]
        public async Task<IActionResult> Classify()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Get all documents uploaded by user by using MongoDocService and group it by its classification (category) .
            var docs = await _mongoService.GetAllDocumentsByUserAsync(userId);
            var grouped = docs
                .GroupBy(doc => doc.ClassificationPath ?? "Unclassified")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            var stats = new DashboardViewModel
            {
                TotalDocuments = docs.Count,
                TotalSizeInMB = Math.Round(docs.Sum(d => d.SizeInBytes) / 1024.0 / 1024.0, 2),
                TotalClassificationTimeMs = Math.Round(docs.Sum(d => d.ClassificationTime), 2),
                SearchExecutionMs = 0 
            };

            ViewBag.DashboardStats = stats;

            return View("Classify", grouped);
        }


        /*
         * Sort Action:
         * This Action responsible for:
         * 1- return Sort.cshtml page, and show these informations in it:
         *      - Total Documents Uploaded
         *      - Total Storage Used
         *      - Sort Time
         *      - All documents sorted A–Z by title.
         * 
         */

        [HttpGet]
        public async Task<IActionResult> Sort()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var stopwatch = Stopwatch.StartNew();

            //Get all documents uploaded by user by using MongoDocService and sort it A-Z by Title.
            var sortedDocs = (await _mongoService.GetAllDocumentsByUserAsync(userId))
                .OrderBy(d => d.Title)
                .ToList();

            stopwatch.Stop();

            var stats = new DashboardViewModel
            {
                TotalDocuments = sortedDocs.Count,
                TotalSizeInMB = Math.Round(sortedDocs.Sum(d => d.SizeInBytes) / 1024.0 / 1024.0, 2),
                SearchExecutionMs = stopwatch.Elapsed.TotalMilliseconds
            };

            ViewBag.DashboardStats = stats;

            return View("Sort", sortedDocs); 
        }


        /*
         * Upload (HttpGet) Action:
         * This Action responsible for return Upload.cshtml page to let user upload his files.
         * 
         */
        [HttpGet]
        public IActionResult Upload() => View();


        /*
         * Upload (HttpPost) Action:
         * This Action responsible for:
         *      - reciving and handeling the uploaded (.PDF or .DOCX) file from user:
         *      - extracts title and content
         *      - stores the file in Azure Blob Storage
         *      - saves metadata to MongoDB
         *      - uses TextExtractionService for clean separation of text extraction logic.
         * 
         */

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ViewBag.Error = "No file uploaded.!";
                return View(); 
            }

            var extension = Path.GetExtension(uploadedFile.FileName).ToLower();
            if (extension != ".pdf" && extension != ".docx")
            {
                ViewBag.Error = "Only PDF and Word (.docx) files are allowed.!";
                return View();
            }
            // Upload file to Azure Blob Storage using AzureBlobService.
            using var stream = uploadedFile.OpenReadStream();
            var fileName = Guid.NewGuid() + Path.GetExtension(uploadedFile.FileName);
            var blobUrl = await _blobService.UploadFileAsync(stream, fileName);

            // Extract document title and full text content using TextExtractionService.
            string title = _textExtractionService.ExtractTitle(uploadedFile);
            string content = _textExtractionService.ExtractFullText(uploadedFile);

            // Measure classification time using Stopwatch.
            var sw = Stopwatch.StartNew();
            string classification = _classifier.Classify(content);
            sw.Stop();

            //get user Id to link document with user uploaded it.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Save document metadata and content to MongoDB.
            var docModel = new DocumentModel
            {
                Title = title,
                Content = content,
                FileName = uploadedFile.FileName,
                Url = blobUrl,
                SizeInBytes = uploadedFile.Length,
                UploadedAt = DateTime.UtcNow,
                ClassificationPath = classification,
                ClassificationTime = sw.Elapsed.TotalMilliseconds,
                UserId = userId
            };

            await _mongoService.AddDocumentAsync(docModel);

            return RedirectToAction("UploadSuccess");
        }

        /*
         * Update (HttpGet) Action:
         * This Action responsible for return Update.cshtml page to let user update his uploaded file by choosing another file.
         * 
         */
        [HttpGet]
        public async Task<IActionResult> Update(string id)
        {
            var document = await _mongoService.GetDocumentByIdAsync(id);
            if (document == null) return NotFound();

            return View(document); 
        }


        /*
         * Update (HttpPost) Action:
         * This Action responsible for receiving and handling the uploaded (.PDF or .DOCX) file from user to updating the oldest one by it
         * 
         */

        [HttpPost]
        public async Task<IActionResult> Update(string id, IFormFile newFile)
        {
            if (newFile == null || newFile.Length == 0)
                return BadRequest("No file uploaded.");

            var existing = await _mongoService.GetDocumentByIdAsync(id);
            if (existing == null) return NotFound();

            // Delete old file from Azure Blob Storage using AzureBlobService.
            await _blobService.DeleteFileAsync(existing.FileName);

            // Upload new file to Azure Blob Storage using AzureBlobService.
            var fileName = Guid.NewGuid() + Path.GetExtension(newFile.FileName);
            var blobUrl = await _blobService.UploadFileAsync(newFile.OpenReadStream(), fileName);

            // Extract metadata.
            string title = _textExtractionService.ExtractTitle(newFile);
            string content = _textExtractionService.ExtractFullText(newFile);

            var stopwatch = Stopwatch.StartNew();
            string classification = _classifier.Classify(content);
            stopwatch.Stop();

            existing.Title = title;
            existing.Content = content;
            existing.FileName = fileName;
            existing.Url = blobUrl;
            existing.SizeInBytes = newFile.Length;
            existing.UploadedAt = DateTime.UtcNow;
            existing.ClassificationPath = classification;
            existing.ClassificationTime = stopwatch.Elapsed.TotalMilliseconds;

            await _mongoService.UpdateDocumentAsync(existing);

            return RedirectToAction("Index");
        }

        /*
         * Delete Action:
         * This Action responsible for deleting exists file
         */
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            //Get document that user wants to delete it using MongoDocService.
            var doc = await _mongoService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound();

            //Delete file from Azure Blob Storage Using AzureBlobService. 
            await _blobService.DeleteFileAsync(doc.FileName);

            //Delete file metadata from MongoDB using MongoDocService.
            await _mongoService.DeleteDocumentAsync(id);

            return RedirectToAction("Index");
        }

        /*
         * UploadSuccess Action:
         * This Action responsible for Show UploadSuccess.cshtml page that tells user that its file is uploaded successfully
         */
        public IActionResult UploadSuccess()
        {
            return View();
        }

    }
}
