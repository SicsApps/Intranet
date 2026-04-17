using System.Web.Mvc;

namespace Intranet.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Novidades()
        {
            //PROCESSA

            return View();
        }
        public ActionResult Index()
        {
            return View();
        }

        //    public PartialViewResult Institucional()
        //    {
        //        ViewBag.Pagina = "Institucional";
        //        return PartialView();
        //    }

        //    public PartialViewResult Contatos()
        //    {
        //        ViewBag.Pagina = "Contatos";
        //        return PartialView();
        //    }

        //    public PartialViewResult Lancamentos()
        //    {
        //        ViewBag.Pagina = "Lancamentos";
        //        return PartialView();
        //    }
    }
}