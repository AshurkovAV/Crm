using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers;

/// <summary>
/// Страница "Умный калькулятор": справочник шаблонов изделий (с составом BOM) + расчёт сметы.
/// </summary>
[Authorize]
public class CalculatorController : Controller
{
    public IActionResult Index() => View();
}
