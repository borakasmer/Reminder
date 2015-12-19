using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Reminder.Controllers
{
    class Images
    {
        public string ImageName { get; set; }
    }

    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {            
            return View();
        }

        public JsonResult GetImages()
        {
            DirectoryInfo directory = new DirectoryInfo(Server.MapPath("/Notes"));
            List<Images> ImageList = new List<Images>(); 
            foreach (FileInfo file in directory.GetFiles().OrderBy(img=>img.CreationTime))
            {
                Images img = new Images();
                img.ImageName = file.Name;
                ImageList.Add(img);
            }
            return Json(ImageList, JsonRequestBehavior.AllowGet);
        }

        public async Task<string> DeleteImage(string imgUrl,string ConnectionID)
        {
            string fileName = Path.GetFileName(imgUrl);
            FileInfo file = new FileInfo(Server.MapPath("/Notes/" + fileName));
            file.Delete();

            //Triger SignalR
            if (Session["hubProxy"] == null)
            {
                //HubConnection hubConnection = new HubConnection("http://localhost:34544/");
                HubConnection hubConnection = new HubConnection(" http://stickypaper.azurewebsites.net/");
                IHubProxy hubProxy = hubConnection.CreateHubProxy("Reciver");
                await hubConnection.Start(new LongPollingTransport());
                Session.Add("hubProxy", hubProxy);
                hubProxy.Invoke("DeleteImage", fileName, ConnectionID);
            }
            else
            {
                ((IHubProxy)Session["hubProxy"]).Invoke("DeleteImage", fileName, ConnectionID);
            }
            //----------------------------

            return fileName;
        }
        public async Task<string> ChangeImageName(string Name, string Cordinates,string ConnectionID)
        {
            string NewName = Path.GetFileNameWithoutExtension(Name);
            FileInfo fInfo = new FileInfo(Server.MapPath("/Notes/"+NewName+".png"));
            if (NewName.Contains('_'))
            {
                NewName = NewName.Split('_')[0] + "_" + Cordinates+".png";
            }
            else
            {
                NewName = NewName + "_" + Cordinates+".png";
            }
            fInfo.MoveTo(Server.MapPath("/Notes/" + NewName));

            //Triger SignalR
            if (Session["hubProxy"] == null)
            {
                //HubConnection hubConnection = new HubConnection("http://localhost:34544/");
                HubConnection hubConnection = new HubConnection(" http://stickypaper.azurewebsites.net/");               
                IHubProxy hubProxy = hubConnection.CreateHubProxy("Reciver");                
                await hubConnection.Start(new LongPollingTransport());
                Session.Add("hubProxy", hubProxy);
                hubProxy.Invoke("MovePaper", (NewName + "æ" + Path.GetFileNameWithoutExtension(Name)), Cordinates, ConnectionID);
            }
            else
            {
                ((IHubProxy)Session["hubProxy"]).Invoke("MovePaper", (NewName + "æ" + Path.GetFileNameWithoutExtension(Name)), Cordinates, ConnectionID);
            }
            //----------------------------
            return  NewName+"æ"+Path.GetFileNameWithoutExtension(Name);
        }
        public async Task<string> PostMessage(string Note, string color,string ConnectionID)
        {
            Bitmap bmp = new Bitmap(Server.MapPath("/papers/" + color + ".png"));
            RectangleF rectf;
            if (color != "yellow")
                rectf = new RectangleF(40, 40, 200, 300);
            else
                rectf = new RectangleF(50, 60, 300, 300);

            Graphics g = Graphics.FromImage(bmp);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (color == "red")
                g.DrawString(Note, new Font("Comic Sans MS", 18, FontStyle.Bold), Brushes.White, rectf);
            else if (color == "yellow")
                g.DrawString(Note, new Font("Comic Sans MS", 31, FontStyle.Bold), Brushes.Red, rectf);
            else if (color == "green")
                g.DrawString(Note, new Font("Comic Sans MS", 35, FontStyle.Bold), Brushes.Blue, rectf);
            else if (color == "blue")
                g.DrawString(Note, new Font("Comic Sans MS", 18, FontStyle.Bold), Brushes.Black, rectf);

            g.Flush();
            Guid imgName = Guid.NewGuid();
            bmp.Save(Server.MapPath("/notes/" + imgName + ".png"));

            //Triger SignalR
            if (Session["hubProxy"] == null)
            {
                //HubConnection hubConnection = new HubConnection("http://localhost:34544/");
                HubConnection hubConnection = new HubConnection(" http://stickypaper.azurewebsites.net/");               
                IHubProxy hubProxy = hubConnection.CreateHubProxy("Reciver");
                await hubConnection.Start(new LongPollingTransport());
                Session.Add("hubProxy", hubProxy);
                hubProxy.Invoke("AddNote", imgName.ToString(), ConnectionID);
            }
            else
            {
                ((IHubProxy)Session["hubProxy"]).Invoke("AddNote", imgName.ToString(), ConnectionID);
            }

            return imgName.ToString();
        }
    }

    public class Reciver:Hub
    {
        public override async Task OnConnected()
        {
            await Clients.Caller.GetConnectionID(Context.ConnectionId);
        }
        public async Task MovePaper(string newName,string Cordinates,string ConnectionID)
        {
            await Clients.AllExcept(ConnectionID).MovePaper(newName, Cordinates);
        }
        public async Task AddNote(string imgName,string ConnectionID)
        {
            await Clients.AllExcept(ConnectionID).AddNote(imgName);
        }
        public async Task DeleteImage(string imgName, string ConnectionID)
        {
            await Clients.AllExcept(ConnectionID).DeleteImage(imgName);
        }
        
    }
}