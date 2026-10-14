using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Models.Wiki;
using Crm.Services.Wiki;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Crm.Controllers
{
    /// <summary>
    /// «База знаний»: разделы, страницы (дерево до 3 уровней), редактор, версии, вложения, поиск,
    /// актуальность и связь с задачами. Права — в WikiRepository/WikiAccess: страницы чужой компании
    /// и недоступных разделов отдают 404, попытка записи без прав редактора — 403.
    /// Все POST требуют antiforgery-токен (поле формы или заголовок RequestVerificationToken).
    /// </summary>
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [Route("Wiki")]
    public class WikiController : Controller
    {
        /// <summary>Лимит тела запроса загрузки: файл + запас на multipart-обвязку.</summary>
        private const long UploadRequestLimit = WikiFileStorage.DefaultMaxFileSizeBytes + 1024 * 1024;

        private readonly IWikiRepository _wiki;
        private readonly WikiFileStorage _files;
        private readonly ILogger<WikiController> _logger;
        private readonly Crm.Services.Notifications.INotificationQueue _notifications;

        public WikiController(IWikiRepository wiki, WikiFileStorage files, ILogger<WikiController> logger,
            Crm.Services.Notifications.INotificationQueue notifications)
        {
            _wiki = wiki;
            _files = files;
            _logger = logger;
            _notifications = notifications;
        }

        // =====================================================================
        // Страницы (HTML)
        // =====================================================================

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return View(new WikiHomeViewModel { NoCompany = true, Ctx = new WikiUserContext() });

            var model = new WikiHomeViewModel { Ctx = ctx };
            await FillBaseAsync(model, ctx);
            model.Pinned = await _wiki.GetPinnedAsync(ctx, null);
            model.Favorites = await _wiki.GetFavoritesAsync(ctx);
            model.Recent = await _wiki.GetRecentAsync(ctx, 8);
            return View(model);
        }

        [HttpGet("Section/{id:int}")]
        public async Task<IActionResult> Section(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var model = new WikiSectionViewModel { Ctx = ctx };
            await FillBaseAsync(model, ctx);
            var section = model.Sections.FirstOrDefault(s => s.Id == id);
            if (section == null)
                return NotFound();

            model.Section = section;
            model.Tree = new WikiTreeViewModel { Section = section, Items = await _wiki.GetTreeAsync(ctx, id), CanEdit = section.CanEdit };
            model.Pinned = await _wiki.GetPinnedAsync(ctx, id);
            return View(model);
        }

        [HttpGet("Page/{id:int}")]
        public async Task<IActionResult> Page(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var page = await _wiki.GetPageAsync(ctx, id);
            if (page == null)
                return NotFound();

            var model = new WikiPageViewModel { Ctx = ctx, Page = page };
            await FillBaseAsync(model, ctx);
            model.Section = model.Sections.First(s => s.Id == page.SectionId);
            model.CanEdit = model.Section.CanEdit;
            model.Tree = new WikiTreeViewModel
            {
                Section = model.Section,
                Items = await _wiki.GetTreeAsync(ctx, page.SectionId),
                CurrentPageId = id,
                CanEdit = model.CanEdit,
            };
            model.Breadcrumbs = BuildCrumbs(model.Section, model.Tree.Items, page.Id, includeSelf: false);
            model.Depth = model.Tree.Depth(id);
            model.Children = model.Tree.Children(id).ToList();
            model.IsFavorite = await _wiki.IsFavoriteAsync(ctx, id);

            var names = await _wiki.GetUserNamesAsync(new[] { page.ModifiedByUserId }.Concat(page.OwnerUserId.HasValue ? new[] { page.OwnerUserId.Value } : Array.Empty<int>()));
            model.ModifiedByName = names.GetValueOrDefault(page.ModifiedByUserId);
            model.OwnerName = page.OwnerUserId.HasValue ? names.GetValueOrDefault(page.OwnerUserId.Value) : null;
            model.IsStale = page.ReviewDueDate.HasValue && page.ReviewDueDate.Value.Date < DateTime.UtcNow.Date;
            model.CanConfirmReview = model.CanEdit || page.OwnerUserId == ctx.UserId;
            return View(model);
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var page = await _wiki.GetPageAsync(ctx, id);
            if (page == null)
                return NotFound();

            var model = new WikiEditViewModel { Ctx = ctx, Page = page };
            await FillBaseAsync(model, ctx);
            model.Section = model.Sections.First(s => s.Id == page.SectionId);
            if (!model.Section.CanEdit)
                return StatusCode(StatusCodes.Status403Forbidden, "Редактировать страницы этого раздела могут только руководители и назначенные редакторы.");

            var tree = await _wiki.GetTreeAsync(ctx, page.SectionId);
            model.Breadcrumbs = BuildCrumbs(model.Section, tree, page.Id, includeSelf: true);
            return View(model);
        }

        [HttpGet("History/{id:int}")]
        public async Task<IActionResult> History(int id, int? v, int? compare)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var page = await _wiki.GetPageAsync(ctx, id);
            if (page == null)
                return NotFound();

            var model = new WikiHistoryViewModel { Ctx = ctx, Page = page };
            await FillBaseAsync(model, ctx);
            model.Section = model.Sections.First(s => s.Id == page.SectionId);
            model.CanEdit = model.Section.CanEdit;
            var tree = await _wiki.GetTreeAsync(ctx, page.SectionId);
            model.Breadcrumbs = BuildCrumbs(model.Section, tree, page.Id, includeSelf: true);
            model.Versions = await _wiki.GetVersionsAsync(ctx, id);

            var selectedNumber = v ?? model.Versions.FirstOrDefault()?.VersionNumber;
            if (selectedNumber.HasValue)
            {
                model.Selected = await _wiki.GetVersionAsync(ctx, id, selectedNumber.Value);
                if (model.Selected == null)
                    return NotFound();
                model.SelectedAuthor = model.Versions.FirstOrDefault(x => x.VersionNumber == selectedNumber)?.AuthorName;

                // Сравнение: с явно указанной версией или с предыдущей
                var compareNumber = compare ?? model.Versions
                    .Where(x => x.VersionNumber < selectedNumber.Value)
                    .Select(x => (int?)x.VersionNumber)
                    .FirstOrDefault();
                if (compareNumber.HasValue && compareNumber.Value != selectedNumber.Value)
                {
                    var other = await _wiki.GetVersionAsync(ctx, id, compareNumber.Value);
                    if (other != null)
                    {
                        // Сравниваем «старую → новую» независимо от порядка параметров
                        var (older, newer) = other.VersionNumber < model.Selected.VersionNumber ? (other, model.Selected) : (model.Selected, other);
                        model.CompareWith = other.VersionNumber;
                        // Текст берём из HTML заново, чтобы старые и новые версии извлекались одинаково
                        model.Diff = WikiDiff.Compare(WikiHtml.ToPlainText(older.ContentHtml), WikiHtml.ToPlainText(newer.ContentHtml));
                        model.TitleChanged = older.Title != newer.Title;
                        model.CompareTitle = other.Title;
                    }
                }
            }

            return View(model);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string? q)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var model = new WikiSearchViewModel { Ctx = ctx, Query = q?.Trim() };
            await FillBaseAsync(model, ctx);
            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                var hits = await _wiki.SearchAsync(ctx, model.Query, 50);
                model.Results = hits.Select(h => new WikiSearchResultView
                {
                    Hit = h,
                    TitleHtml = WikiSnippet.Highlight(h.Title, model.Query),
                    ExcerptHtml = WikiSnippet.Excerpt(h.ContentText, model.Query),
                    FilesHtml = h.MatchedFiles.Select(f => WikiSnippet.Highlight(f, model.Query)).ToList(),
                }).ToList();
            }
            return View(model);
        }

        [HttpGet("Review")]
        public async Task<IActionResult> Review()
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var model = new WikiReviewViewModel { Ctx = ctx };
            await FillBaseAsync(model, ctx);
            model.Pages = await _wiki.GetStalePagesAsync(ctx, DateTime.UtcNow);
            return View(model);
        }

        [HttpGet("Settings")]
        public async Task<IActionResult> Settings()
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return StatusCode(StatusCodes.Status403Forbidden, "Настройки базы знаний доступны только руководителям компании.");

            var model = new WikiSettingsViewModel { Ctx = ctx };
            await FillBaseAsync(model, ctx);
            model.Members = await _wiki.GetMembersAsync(ctx);
            model.CompanyEditorIds = (await _wiki.GetCompanyEditorIdsAsync(ctx)).ToHashSet();
            return View(model);
        }

        /// <summary>
        /// Отдача файла вложения/картинки. Только после проверки доступа к странице.
        /// Картинки и PDF (с проверенной сигнатурой) — inline, всё остальное — attachment;
        /// HTML/SVG/JS никогда не отдаются с «родным» типом. Всегда nosniff.
        /// </summary>
        [HttpGet("File/{versionId:int}")]
        public async Task<IActionResult> GetFile(int versionId, bool download = false)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var file = await _wiki.GetFileAsync(ctx, versionId);
            var path = file == null ? null : _files.GetPath(file.CompanyId, file.StoredName);
            if (file == null || path == null)
                return NotFound();

            var contentType = WikiFileStorage.ResolveContentType(file.FileName);
            var header = new byte[16];
            await using (var probe = System.IO.File.OpenRead(path))
            {
                var read = await probe.ReadAsync(header);
                header = header[..read];
            }

            var inline = !download && WikiFileStorage.CanPreviewInline(contentType, header);
            if (!inline && contentType.StartsWith("image/", StringComparison.Ordinal) && !WikiFileStorage.CanPreviewInline(contentType, header))
                contentType = "application/octet-stream"; // расширение «картинки», а внутри не картинка

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.CacheControl = "private, max-age=3600";
            if (contentType != "application/pdf")
                Response.Headers["Content-Security-Policy"] = "default-src 'none'; img-src 'self'; style-src 'unsafe-inline'; sandbox";

            var disposition = new ContentDispositionHeaderValue(inline ? "inline" : "attachment");
            disposition.SetHttpFileName(file.FileName);
            Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        // =====================================================================
        // API: разделы и редакторы (только руководители)
        // =====================================================================

        public record SectionRequest(string? Name, string? Icon, string? Color, string? Description);

        [HttpPost("api/sections")]
        public async Task<IActionResult> CreateSection([FromBody] SectionRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied("Создавать разделы могут только руководители компании.");
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Укажите название раздела." });

            var section = await _wiki.CreateSectionAsync(ctx, request.Name, request.Icon, request.Color, request.Description);
            return section == null ? Denied() : Json(new { section.Id, Url = $"/Wiki/Section/{section.Id}" });
        }

        [HttpPost("api/sections/{id:int}")]
        public async Task<IActionResult> UpdateSection(int id, [FromBody] SectionRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied("Изменять разделы могут только руководители компании.");
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Укажите название раздела." });

            return await _wiki.UpdateSectionAsync(ctx, id, request.Name, request.Icon, request.Color, request.Description) ? Ok(new { ok = true }) : NotFound();
        }

        [HttpPost("api/sections/{id:int}/delete")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied("Удалять разделы могут только руководители компании.");

            var (ok, message, files) = await _wiki.DeleteSectionAsync(ctx, id);
            if (!ok)
                return message == null ? NotFound() : BadRequest(new { message });

            foreach (var f in files)
                TryDeleteFile(f.CompanyId, f.StoredName);
            return Ok(new { ok = true });
        }

        public record ShiftRequest(int Delta);

        [HttpPost("api/sections/{id:int}/shift")]
        public async Task<IActionResult> ShiftSection(int id, [FromBody] ShiftRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied();
            return await _wiki.ShiftSectionAsync(ctx, id, Math.Sign(request.Delta)) ? Ok(new { ok = true }) : NotFound();
        }

        [HttpGet("api/sections/{id:int}/settings")]
        public async Task<IActionResult> GetSectionSettings(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied();

            var settings = await _wiki.GetSectionSettingsAsync(ctx, id);
            if (settings == null)
                return NotFound();

            var section = await _wiki.GetSectionAsync(ctx, id);
            var members = await _wiki.GetMembersAsync(ctx);
            return Json(new
            {
                section!.Id,
                section.Name,
                section.Icon,
                section.Description,
                settings.Value.AccessMode,
                AccessUserIds = settings.Value.AccessUserIds,
                EditorUserIds = settings.Value.EditorUserIds,
                Members = members.Select(m => new { m.UserId, m.Name, m.Email, m.IsManager }),
            });
        }

        public record SectionSettingsRequest(string? AccessMode, List<int>? AccessUserIds, List<int>? EditorUserIds);

        [HttpPost("api/sections/{id:int}/settings")]
        public async Task<IActionResult> SetSectionSettings(int id, [FromBody] SectionSettingsRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied("Доступ к разделу настраивают только руководители компании.");

            var ok = await _wiki.SetSectionSettingsAsync(ctx, id, request.AccessMode ?? WikiSection.AccessAll,
                request.AccessUserIds ?? new List<int>(), request.EditorUserIds ?? new List<int>());
            return ok ? Ok(new { ok = true }) : NotFound();
        }

        public record EditorsRequest(List<int>? UserIds);

        [HttpPost("api/editors")]
        public async Task<IActionResult> SetCompanyEditors([FromBody] EditorsRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!WikiAccess.CanManage(ctx))
                return Denied("Назначать редакторов могут только руководители компании.");

            return await _wiki.SetCompanyEditorsAsync(ctx, request.UserIds ?? new List<int>()) ? Ok(new { ok = true }) : Denied();
        }

        [HttpGet("api/members")]
        public async Task<IActionResult> Members()
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var members = await _wiki.GetMembersAsync(ctx);
            return Json(members.Select(m => new { m.UserId, m.Name, m.Email }));
        }

        // =====================================================================
        // API: страницы
        // =====================================================================

        public record CreatePageRequest(int SectionId, int? ParentId, string? Title, string? Template);

        [HttpPost("api/pages")]
        public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var title = string.IsNullOrWhiteSpace(request.Title) ? "Новая страница" : request.Title.Trim();
            var template = WikiTemplates.Find(request.Template) ?? WikiTemplates.All[0];
            var html = WikiHtml.Sanitize(template.Html);
            var (status, pageId, message) = await _wiki.CreatePageAsync(ctx, request.SectionId, request.ParentId, title, html, WikiHtml.ToPlainText(html));
            return status switch
            {
                WikiSaveStatus.Ok => Json(new { Id = pageId, Url = $"/Wiki/Edit/{pageId}" }),
                WikiSaveStatus.Forbidden => Denied("Создавать страницы в этом разделе могут только руководители и редакторы."),
                WikiSaveStatus.Invalid => BadRequest(new { message }),
                _ => NotFound(),
            };
        }

        /// <param name="Notify">«Уведомить сотрудников»: по умолчанию включено для новой страницы, выключено для правок.</param>
        public record SavePageRequest(int BaseVersion, bool Force, string? Title, string? Html, JsonElement? Json, bool Notify = false);

        [HttpPost("api/pages/{id:int}/save")]
        [RequestSizeLimit(8 * 1024 * 1024)]
        public async Task<IActionResult> SavePage(int id, [FromBody] SavePageRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();

            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0)
                return BadRequest(new { message = "Заголовок страницы не может быть пустым." });

            var html = WikiHtml.Sanitize(request.Html);
            var json = request.Json is { ValueKind: JsonValueKind.Object } element ? element.GetRawText() : null;
            var result = await _wiki.SavePageAsync(ctx, id, request.BaseVersion, request.Force, title, json, html, WikiHtml.ToPlainText(html));
            if (result.Status == WikiSaveStatus.Ok && request.Notify && result.Version != request.BaseVersion)
            {
                // Рассылка читателям раздела — фоном, дайджестом (см. WikiDigestBuilder)
                try
                {
                    await _notifications.AddWikiEventAsync(ctx.CompanyId, id, ctx.UserId, isUpdate: request.BaseVersion > 1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось поставить уведомление о странице {PageId}", id);
                }
            }

            return result.Status switch
            {
                WikiSaveStatus.Ok => Json(new { ok = true, result.Version }),
                WikiSaveStatus.Conflict => StatusCode(StatusCodes.Status409Conflict, new
                {
                    message = "Страницу уже изменили, пока вы её редактировали.",
                    result.Version,
                    result.ModifiedByName,
                    ModifiedDate = result.ModifiedDate.HasValue ? WikiFormat.Date(result.ModifiedDate.Value) : null,
                }),
                WikiSaveStatus.Forbidden => Denied("Редактировать страницы этого раздела могут только руководители и назначенные редакторы."),
                _ => NotFound(),
            };
        }

        public record MovePageRequest(int? ParentId, int Index);

        [HttpPost("api/pages/{id:int}/move")]
        public async Task<IActionResult> MovePage(int id, [FromBody] MovePageRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, id);

            var (ok, message) = await _wiki.MovePageAsync(ctx, id, request.ParentId, request.Index);
            return ok ? Ok(new { ok = true }) : BadRequest(new { message = message ?? "Не удалось переместить страницу." });
        }

        [HttpPost("api/pages/{id:int}/shift")]
        public async Task<IActionResult> ShiftPage(int id, [FromBody] ShiftRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, id);
            return await _wiki.ShiftPageAsync(ctx, id, Math.Sign(request.Delta)) ? Ok(new { ok = true }) : NotFound();
        }

        [HttpPost("api/pages/{id:int}/delete")]
        public async Task<IActionResult> DeletePage(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var page = await _wiki.GetPageAsync(ctx, id);
            if (page == null)
                return NotFound();
            if (!await _wiki.DeletePageAsync(ctx, id))
                return Denied("Удалять страницы могут только руководители и редакторы раздела.");
            return Json(new { ok = true, Url = page.ParentId.HasValue ? $"/Wiki/Page/{page.ParentId}" : $"/Wiki/Section/{page.SectionId}" });
        }

        public record PinRequest(bool Pinned);

        [HttpPost("api/pages/{id:int}/pin")]
        public async Task<IActionResult> PinPage(int id, [FromBody] PinRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.SetPinnedAsync(ctx, id, request.Pinned))
                return await PageDeniedOrNotFound(ctx, id);
            return Ok(new { ok = true, request.Pinned });
        }

        [HttpPost("api/pages/{id:int}/favorite")]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var result = await _wiki.ToggleFavoriteAsync(ctx, id);
            return result.HasValue ? Ok(new { ok = true, Favorite = result.Value }) : NotFound();
        }

        [HttpPost("api/pages/{id:int}/restore/{version:int}")]
        public async Task<IActionResult> RestoreVersion(int id, int version)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, id);

            var result = await _wiki.RestoreVersionAsync(ctx, id, version);
            return result.Status == WikiSaveStatus.Ok ? Json(new { ok = true, result.Version, Url = $"/Wiki/Page/{id}" }) : NotFound();
        }

        public record ReviewRequest(int? OwnerUserId, DateTime? ReviewDueDate);

        [HttpPost("api/pages/{id:int}/review")]
        public async Task<IActionResult> SetReview(int id, [FromBody] ReviewRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, id);
            return await _wiki.SetReviewAsync(ctx, id, request.OwnerUserId, request.ReviewDueDate)
                ? Ok(new { ok = true })
                : BadRequest(new { message = "Ответственный должен быть сотрудником компании." });
        }

        public record ReviewedRequest(DateTime? NextReviewDueDate);

        [HttpPost("api/pages/{id:int}/reviewed")]
        public async Task<IActionResult> MarkReviewed(int id, [FromBody] ReviewedRequest? request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (await _wiki.GetPageAsync(ctx, id) == null)
                return NotFound();
            return await _wiki.MarkReviewedAsync(ctx, id, request?.NextReviewDueDate)
                ? Ok(new { ok = true })
                : Denied("Подтвердить актуальность может ответственный за страницу или редактор раздела.");
        }

        /// <summary>Поиск страниц для выбора (ссылка на страницу в редакторе, привязка к задаче).</summary>
        [HttpGet("api/lookup")]
        public async Task<IActionResult> Lookup(string? q)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return Json(Array.Empty<object>());

            List<WikiPageListItem> items;
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                items = await _wiki.GetRecentAsync(ctx, 10);
            }
            else
            {
                var hits = await _wiki.SearchAsync(ctx, q, 15);
                items = hits.Select(h => new WikiPageListItem { Id = h.PageId, Title = h.Title, SectionId = h.SectionId, SectionName = h.SectionName, SectionIcon = h.SectionIcon, SectionColor = h.SectionColor }).ToList();
            }

            return Json(items.Select(i => new { i.Id, i.Title, i.SectionName, Tile = WikiFormat.TileData(i.SectionIcon, i.SectionColor), Url = $"/Wiki/Page/{i.Id}" }));
        }

        // =====================================================================
        // API: вложения
        // =====================================================================

        [HttpGet("api/pages/{id:int}/attachments")]
        public async Task<IActionResult> Attachments(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null || await _wiki.GetPageAsync(ctx, id) == null)
                return NotFound();

            var list = await _wiki.GetAttachmentsAsync(ctx, id);
            return Json(list.Select(a => new
            {
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                Size = WikiFormat.Size(a.SizeBytes),
                a.VersionNumber,
                Uploaded = WikiFormat.Date(a.UploadedDate),
                a.UploadedByName,
                Url = $"/Wiki/File/{a.CurrentVersionId}",
                DownloadUrl = $"/Wiki/File/{a.CurrentVersionId}?download=true",
                Preview = WikiFileStorage.IsImage(a.ContentType) ? "image" : a.ContentType == "application/pdf" ? "pdf" : null,
                History = a.History.Select(h => new
                {
                    h.VersionNumber,
                    h.FileName,
                    Size = WikiFormat.Size(h.SizeBytes),
                    Uploaded = WikiFormat.Date(h.UploadedDate),
                    h.UploadedByName,
                    Url = $"/Wiki/File/{h.Id}?download=true",
                }),
            }));
        }

        [HttpPost("api/pages/{id:int}/attachments")]
        [RequestSizeLimit(UploadRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
        public async Task<IActionResult> Upload(int id, IFormFile? file, bool inline = false, CancellationToken cancellationToken = default)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, id);

            var (error, name, contentType) = ValidateUpload(file, inline);
            if (error != null)
                return BadRequest(new { message = error });

            string storedName;
            await using (var stream = file!.OpenReadStream())
                storedName = await _files.SaveAsync(ctx.CompanyId, name!, stream, cancellationToken);

            if (inline && !await HasImageSignatureAsync(ctx.CompanyId, storedName, contentType!))
            {
                TryDeleteFile(ctx.CompanyId, storedName);
                return BadRequest(new { message = "Файл не похож на изображение (PNG, JPG, GIF, WEBP)." });
            }

            var created = await _wiki.AddAttachmentAsync(ctx, id, inline, name!, storedName, contentType!, file.Length);
            if (created == null)
            {
                TryDeleteFile(ctx.CompanyId, storedName);
                return Denied();
            }

            return Json(new { ok = true, created.Value.AttachmentId, Url = $"/Wiki/File/{created.Value.VersionId}", FileName = name });
        }

        [HttpPost("api/attachments/{id:int}/replace")]
        [RequestSizeLimit(UploadRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
        public async Task<IActionResult> Replace(int id, IFormFile? file, CancellationToken cancellationToken = default)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var pageId = await _wiki.GetAttachmentPageIdAsync(ctx, id);
            if (pageId == null)
                return NotFound();
            if (!await _wiki.CanEditPageAsync(ctx, pageId.Value))
                return await PageDeniedOrNotFound(ctx, pageId.Value);

            var (error, name, contentType) = ValidateUpload(file, inline: false);
            if (error != null)
                return BadRequest(new { message = error });

            string storedName;
            await using (var stream = file!.OpenReadStream())
                storedName = await _files.SaveAsync(ctx.CompanyId, name!, stream, cancellationToken);

            var versionId = await _wiki.AddAttachmentVersionAsync(ctx, id, name!, storedName, contentType!, file.Length);
            if (versionId == null)
            {
                TryDeleteFile(ctx.CompanyId, storedName);
                return Denied();
            }
            return Json(new { ok = true, Url = $"/Wiki/File/{versionId}" });
        }

        [HttpPost("api/attachments/{id:int}/delete")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var pageId = await _wiki.GetAttachmentPageIdAsync(ctx, id);
            if (pageId == null)
                return NotFound();
            if (!await _wiki.DeleteAttachmentAsync(ctx, id))
                return await PageDeniedOrNotFound(ctx, pageId.Value);
            // Файлы на диске не удаляем: история версий страницы и вложений остаётся восстановимой.
            return Ok(new { ok = true });
        }

        // =====================================================================
        // API: связь с задачами
        // =====================================================================

        [HttpGet("api/pages/{id:int}/tasks")]
        public async Task<IActionResult> PageTasks(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null || await _wiki.GetPageAsync(ctx, id) == null)
                return NotFound();

            var (tasks, hidden) = await _wiki.GetLinkedTasksAsync(ctx, id);
            return Json(new
            {
                Tasks = tasks.Select(t => new
                {
                    t.TaskId,
                    t.Name,
                    t.StatusName,
                    t.Status,
                    Deadline = t.Deadline?.ToString("dd.MM.yyyy"),
                    t.AssigneeName,
                    Url = $"/Tasks?openTask={t.TaskId}",
                }),
                Hidden = hidden,
            });
        }

        [HttpGet("api/tasks/{taskId:int}/pages")]
        public async Task<IActionResult> TaskPages(int taskId)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return Json(Array.Empty<object>());
            var pages = await _wiki.GetLinkedPagesForTaskAsync(ctx, taskId);
            if (pages == null)
                return NotFound();
            return Json(pages.Select(p => new { p.Id, p.Title, p.SectionName, Tile = WikiFormat.TileData(p.SectionIcon, p.SectionColor), Url = $"/Wiki/Page/{p.Id}" }));
        }

        public record TaskLinkRequest(int PageId);

        [HttpPost("api/tasks/{taskId:int}/link")]
        public async Task<IActionResult> LinkTask(int taskId, [FromBody] TaskLinkRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            return await _wiki.LinkTaskAsync(ctx, request.PageId, taskId) ? Ok(new { ok = true }) : NotFound();
        }

        [HttpPost("api/tasks/{taskId:int}/unlink")]
        public async Task<IActionResult> UnlinkTask(int taskId, [FromBody] TaskLinkRequest request)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            return await _wiki.UnlinkTaskAsync(ctx, request.PageId, taskId) ? Ok(new { ok = true }) : NotFound();
        }

        /// <summary>Заготовка задачи по странице: название + описание со ссылкой и чек-листом страницы.</summary>
        [HttpGet("api/pages/{id:int}/task-draft")]
        public async Task<IActionResult> TaskDraft(int id)
        {
            var ctx = await GetContextAsync();
            if (ctx == null)
                return NotFound();
            var page = await _wiki.GetPageAsync(ctx, id);
            if (page == null)
                return NotFound();

            var url = $"{Request.Scheme}://{Request.Host}/Wiki/Page/{page.Id}";
            var sb = new StringBuilder();
            sb.Append("<p>По статье базы знаний: <a href=\"").Append(WebUtility.HtmlEncode(url)).Append("\">")
              .Append(WebUtility.HtmlEncode(page.Title)).Append("</a></p>");

            var checklist = WikiHtml.ExtractChecklist(page.ContentHtml);
            if (checklist.Count > 0)
            {
                sb.Append("<p><strong>Чек-лист:</strong></p><ul>");
                foreach (var (text, isChecked) in checklist.Take(50))
                    sb.Append("<li>").Append(isChecked ? "☑ " : "☐ ").Append(WebUtility.HtmlEncode(text)).Append("</li>");
                sb.Append("</ul>");
            }

            return Json(new { PageId = page.Id, Name = page.Title.Length > 200 ? page.Title[..200] : page.Title, Description = sb.ToString(), ChecklistCount = checklist.Count });
        }

        // =====================================================================
        // Вспомогательное
        // =====================================================================

        private async Task<WikiUserContext?> GetContextAsync()
        {
            if (HttpContext.Items.TryGetValue(nameof(WikiUserContext), out var cached))
                return cached as WikiUserContext;

            WikiUserContext? ctx = null;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                ctx = await _wiki.GetContextAsync(userId);
            HttpContext.Items[nameof(WikiUserContext)] = ctx;
            return ctx;
        }

        private async Task FillBaseAsync(WikiViewModelBase model, WikiUserContext ctx)
        {
            model.Sections = await _wiki.GetSectionsAsync(ctx);
            if (ctx.CanWriteAnywhere || ctx.IsManager)
                model.StaleCount = (await _wiki.GetStalePagesAsync(ctx, DateTime.UtcNow)).Count;
        }

        private static List<WikiCrumb> BuildCrumbs(WikiSectionInfo section, List<WikiTreeItem> tree, int pageId, bool includeSelf)
        {
            var crumbs = new List<WikiCrumb>
            {
                new("База знаний", "/Wiki"),
                new(section.Name, $"/Wiki/Section/{section.Id}"),
            };
            var byId = tree.ToDictionary(t => t.Id);
            var chain = new List<WikiTreeItem>();
            var cursor = byId.GetValueOrDefault(pageId)?.ParentId;
            var guard = 0;
            while (cursor.HasValue && byId.TryGetValue(cursor.Value, out var item) && guard++ < 10)
            {
                chain.Insert(0, item);
                cursor = item.ParentId;
            }
            crumbs.AddRange(chain.Select(c => new WikiCrumb(c.Title, $"/Wiki/Page/{c.Id}")));
            if (includeSelf && byId.TryGetValue(pageId, out var self))
                crumbs.Add(new WikiCrumb(self.Title, $"/Wiki/Page/{self.Id}"));
            return crumbs;
        }

        private (string? Error, string? Name, string? ContentType) ValidateUpload(IFormFile? file, bool inline)
        {
            if (file == null || file.Length == 0)
                return ("Файл не передан.", null, null);
            if (file.Length > _files.MaxFileSizeBytes)
                return ($"Файл больше {_files.MaxFileSizeBytes / 1024 / 1024} МБ.", null, null);

            var name = WikiFileStorage.CleanFileName(file.FileName);
            var contentType = WikiFileStorage.ResolveContentType(name);
            if (inline && !WikiFileStorage.IsImage(contentType))
                return ("В текст можно вставить только изображение (PNG, JPG, GIF, WEBP).", null, null);
            return (null, name, contentType);
        }

        private async Task<bool> HasImageSignatureAsync(int companyId, string storedName, string contentType)
        {
            var path = _files.GetPath(companyId, storedName);
            if (path == null)
                return false;
            var header = new byte[16];
            await using var stream = System.IO.File.OpenRead(path);
            var read = await stream.ReadAsync(header);
            return WikiFileStorage.CanPreviewInline(contentType, header.AsSpan(0, read));
        }

        private void TryDeleteFile(int companyId, string storedName)
        {
            try
            {
                _files.Delete(companyId, storedName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить файл базы знаний {StoredName}", storedName);
            }
        }

        /// <summary>Страница видна, но прав на запись нет — 403; не видна вовсе — 404.</summary>
        private async Task<IActionResult> PageDeniedOrNotFound(WikiUserContext ctx, int pageId)
            => await _wiki.GetPageAsync(ctx, pageId) == null
                ? NotFound()
                : Denied("Редактировать страницы этого раздела могут только руководители и назначенные редакторы.");

        private IActionResult Denied(string? message = null)
            => StatusCode(StatusCodes.Status403Forbidden, new { message = message ?? "Недостаточно прав." });
    }
}
