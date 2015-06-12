using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Web.Script;
using System.Web.Script.Serialization;
using SocketIOClient.Messages;
using WebSocket4Net;
using SOD_CS_Library;
using System.Drawing;
using System.Timers;

namespace SoD_ProjectedPixels
{

    public class Result
    {
        public string connections;
        public int count;
        public string layouts;
        public string realSizes;
        public int surfaceNo;
        public double x;
        public double y;
        public double xStart;
        public double yStart;
        public double xEnd;
        public double yEnd;
        public double height;
        public double width;
        public double radius;
        public string coorSys;
        public string name;
        public int windowNo;
        public int elementNo;
        public string lineColor;
        public int lineWidth;
        public string fillColor;
        public int sides;
        public string textureData;
        public string extension;
        public string color;
        public double ptSize;
        public string font;
        public string text;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DealWithSoD();
        }


        #region Projector Parameters

        static string _url = "http://192.168.0.139:5002/api/loadDefinedSurfaces";

        static String server = "";
        static String port = "";

        static String fileName = "";
        static bool connected = false;

        public const double CONVERTER = (512 / 2);

        public int pathLinesID;

        public Timer timer;

        JavaScriptSerializer js = new JavaScriptSerializer();

        #endregion

        #region Projector Set Up

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            server = serverip.Text;
            port = port_num.Text;
            _url = "http://" + server + ":" + port + "";

            addLayoutsToList();
            setup_button.IsEnabled = true;
        }

        private void addLayoutsToList()
        {
            try {
                string path = _url + "/api/getSavedLayouts";

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/json; charset=UTF-8";


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    Result result_obj = (Result)js.Deserialize(result, typeof(Result));
                    //Object obj = js.Deserialize(result, typeof(Object));

                    dynamic obj = JsonConvert.DeserializeObject(result);

                    for (int i = 0; i < (int)obj.count; i++)
                    {
                        string layout = (string)obj[i.ToString()];

                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = layout;

                        layout_file_list.Items.Add(item);
                    }

                    Console.WriteLine("Request of layout list made");
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }


        private void SetUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = _url + "/api/loadDefinedSurfaces";
                fileName = (string)layout_file_list.SelectionBoxItem;

                // Check for selection
                if (fileName == null)
                    return;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    string obj = "{\"fileName\":\"" + fileName + "\"}";
                    writer.Write(obj);
                    writer.Flush();
                    writer.Close();
                }


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    // Create new lists of surfaces, windows and elements for the room object
                    SoD.SODProjector.room.Surfaces = new Dictionary<string, Projector.Surface>();
                    SoD.SODProjector.room.Windows = new Dictionary<string, Projector.Window>();
                    SoD.SODProjector.room.Elements = new Dictionary<string, Projector.Element>();

                    // get data from projector server
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    Result res_obj = (Result)js.Deserialize(result, typeof(Result));

                    // sizes of each surface from layout file
                    string[] sizes = res_obj.realSizes.Split(';');

                    int count = Convert.ToInt32(res_obj.count);

                    // Create a surface object for each surface in the layout file and save to the room
                    for (int i = 1; i <= count; i++) {

                        // get the surface numbers and save them
                        Projector.Surface surface = new Projector.Surface();
                        surface.ID = i.ToString();

                        // save the real height and width of the surface and convert to meters 
                        string[] heightwidth = sizes[i-1].Split(':');
                        surface.height = (Convert.ToDouble(heightwidth[0]) / 100);
                        surface.width = (Convert.ToDouble(heightwidth[1]) / 100);

                        // create a new list of windows for the surface 
                        surface.windows = new Dictionary<string, Projector.Window>();

                        // Add the surface to the room object
                        SoD.SODProjector.room.Surfaces.Add(surface.ID, surface);

                        if (i == 1)
                            grid_surface1.Visibility = System.Windows.Visibility.Visible;
                        else if (i == 2)
                            grid_surface2.Visibility = System.Windows.Visibility.Visible;
                        else if (i == 3)
                            grid_surface3.Visibility = System.Windows.Visibility.Visible;
                        else if (i == 4)
                            grid_surface4.Visibility = System.Windows.Visibility.Visible;
                    }

                    // register the projector and its room data in SoD
                    SoD.SODProjector.data = SoD.SODProjector.room;
                    SoD.SODProjector.subscriber.subscriberType = "device";
                    SoD.SODProjector.ID = SoD.ownDevice.ID;
                    SoD.RegisterProjector();

                    surface_grid.Visibility = System.Windows.Visibility.Visible;

                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        


        //
        private void Set_Surfaces_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<string, Projector.Surface> pair in SoD.SODProjector.room.Surfaces)
            {
                Projector.Surface surface = pair.Value;
                ComboBoxItem selectedItem;

                if (pair.Key == "1")
                {
                    selectedItem = (ComboBoxItem)surface1_type_box.SelectedItem;
                    surface.type = selectedItem.Content.ToString();
                    surface.xoffset = Convert.ToDouble(xoff_surface1);
                    surface.zoffset = Convert.ToDouble(zoff_surface1);
                }
                else if (pair.Key == "2") 
                {
                    selectedItem = (ComboBoxItem)surface2_type_box.SelectedItem;
                    surface.type = selectedItem.Content.ToString();
                    surface.xoffset = Convert.ToDouble(xoff_surface2);
                    surface.zoffset = Convert.ToDouble(zoff_surface2);
                }
                else if (pair.Key == "3")
                {
                    selectedItem = (ComboBoxItem)surface3_type_box.SelectedItem;
                    surface.type = selectedItem.Content.ToString();
                    surface.xoffset = Convert.ToDouble(xoff_surface3);
                    surface.zoffset = Convert.ToDouble(zoff_surface3);
                }
                else if (pair.Key == "4")
                {
                    selectedItem = (ComboBoxItem)surface4_type_box.SelectedItem;
                    surface.type = selectedItem.Content.ToString();
                    surface.xoffset = Convert.ToDouble(xoff_surface4);
                    surface.zoffset = Convert.ToDouble(zoff_surface4);
                }
            }
        }


        #endregion 

        #region Reconnect to Server

        private void ReSetUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = _url + "/api/loadDefinedSurfaces";
                fileName = (string)layout_file_list.SelectionBoxItem;

                // Check for selection
                if (fileName == null)
                    return;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    string obj = "{\"fileName\":\"" + fileName + "\"}";
                    writer.Write(obj);
                    writer.Flush();
                    writer.Close();
                }


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                    recoverLayout();
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }

        private void recoverLayout()
        {

            foreach (KeyValuePair<string, Projector.Surface> s in SoD.SODProjector.room.Surfaces)
            {
                Projector.Surface surface = s.Value;

                foreach (KeyValuePair<string, Projector.Window> w in surface.windows)
                {
                    Projector.Window window = w.Value;

                    // draw a new window on server
                    try
                    {
                        string path = _url + "/api/newWindow";

                        Projector.newWindowjsondata data = new Projector.newWindowjsondata();
                        data.surfaceNo = Convert.ToInt32(surface.ID);
                        data.x = window.x;
                        data.y = window.y;
                        data.height = window.height;
                        data.width = window.width;
                        data.coorSys = "pix";
                        data.name = window.name;

                        string obj = js.Serialize(data);

                        HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                        request.Method = "POST";
                        request.ContentType = "application/json; charset=UTF-8";

                        //Send the request
                        using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                        {
                            writer.Write(obj);
                            writer.Flush();
                            writer.Close();
                        }

                        // Get the response
                        string result = null;
                        HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            result = reader.ReadToEnd();
                            Console.Write(result);

                            // update the ID after recovery
                            Result result_obj = (Result)js.Deserialize(result, typeof(Result));
                            window.ID = result_obj.windowNo.ToString();

                            foreach (KeyValuePair<string, Projector.Element> e in window.elements)
                            {
                                Projector.Element element = e.Value;

                                path = _url +  getPathForType(element.type);
                                obj = getElementStringData(element, window.ID);

                                HttpWebRequest element_request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                                element_request.Method = "POST";
                                element_request.ContentType = "application/json; charset=UTF-8";

                                //Send the request
                                using (StreamWriter writer = new StreamWriter(element_request.GetRequestStream()))
                                {
                                    writer.Write(obj);
                                    writer.Flush();
                                    writer.Close();
                                }

                                // Get the response
                                string element_result = null;
                                HttpWebResponse element_response = element_request.GetResponse() as HttpWebResponse;
                                using (StreamReader element_reader = new StreamReader(element_response.GetResponseStream()))
                                {
                                    element_result = element_reader.ReadToEnd();
                                    Console.Write(element_result);

                                    // update the ID after recovery
                                    result_obj = (Result)js.Deserialize(result, typeof(Result));
                                    element.ID = result_obj.elementNo.ToString();
                                }
                            }

                            resetup_button.IsEnabled = false;
                        }

                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);

                        setup_button.IsEnabled = false;
                        resetup_button.IsEnabled = true;
                    }

                }
            }
        }

        private string getPathForType(string type)
        {
            string path = "";

            if ("circle" == type)
                path = "/api/newCircle";
            else if ("rectangle" == type)
                path = "/api/newRectangle";
            else if ("image" == type)
                path = "/api/newTexRectangle";
            else if ("line" == type)
                path = "/api/newLine";
            else if ("text" == type)
                path = "/api/newText";
            else if ("path" == type)
            {

            }

            return path;
            
        }

        private string getElementStringData(Projector.Element element, string windowID)
        {
            string jsondata = "";

            if ("circle" == element.type) {

                Projector.Circle circle = (Projector.Circle)element;

                Projector.newCirclejsondata data = new Projector.newCirclejsondata();
                data.windowNo = Convert.ToInt32(windowID);
                data.x = circle.x;
                data.y = circle.y;
                data.radius = circle.radius;
                data.coorSys = "pix";
                data.lineColor = circle.lineColor;
                data.lineWidth = circle.lineWidth;
                data.fillColor = circle.fillColor;
                data.sides = circle.sides;

                jsondata = js.Serialize(data);
            }
            else if ("rectangle" == element.type) {

                Projector.Rectangle rectangle = (Projector.Rectangle)element;

                Projector.newRectjsondata data = new Projector.newRectjsondata();
                data.windowNo = Convert.ToInt32(windowID);
                data.x = rectangle.x;
                data.y = rectangle.y;
                data.height = rectangle.height;
                data.width = rectangle.width;
                data.coorSys = "pix";
                data.lineColor = rectangle.lineColor;
                data.lineWidth = rectangle.lineWidth;
                data.fillColor = rectangle.fillColor;

                jsondata = js.Serialize(data);
            }
            else if ("image" == element.type) {

                Projector.Image image = (Projector.Image)element;

                Projector.newImagejsondata data = new Projector.newImagejsondata();
                data.windowNo = Convert.ToInt32(windowID);
                data.x = image.x;
                data.y = image.y;
                data.height = image.height;
                data.coorSys = "pix";
                data.textureData = image.image;
                data.extension = image.extension;

                jsondata = js.Serialize(data);
            }
            else if ("line" == element.type) {

                Projector.Line line = (Projector.Line)element;

                Projector.newLinejsondata data = new Projector.newLinejsondata();
                data.windowNo = Convert.ToInt32(windowID);
                data.xStart = line.x;
                data.yStart = line.y;
                data.xEnd = line.end_x;
                data.yEnd = line.end_y;
                data.coorSys = "pix";
                data.color = line.color;
                data.width = line.width;

                jsondata = js.Serialize(data);
            }
            else if ("text" == element.type) {

                Projector.Text text = (Projector.Text)element;

                Projector.newtextjsondata data = new Projector.newtextjsondata();
                data.windowNo = Convert.ToInt32(windowID);
                data.text = text.text;
                data.x = text.x;
                data.y = text.y;
                data.coorSys = "pix";
                data.ptSize = text.ptSize;
                data.font = text.font;
                data.color = text.color;

                jsondata = js.Serialize(data);
            }
            else if ("path" == element.type) {

            }

            return jsondata;
        }


        #endregion

        #region Calls to Projector Server


        private void callProjectorServer(string api_call, string jsondata, string method, int PID, string name)
        {
            try
            {
                string path = _url + api_call;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = method;
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsondata);
                    writer.Flush();
                    writer.Close();
                }


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    switch (api_call)
                    {
                        case "/api/newWindow":
                            Console.WriteLine("New Window");
                            addWindowToRoom(result, jsondata);
                            break;
                        case "/api/newCircle":
                            Console.WriteLine("New Circle");
                            addElementToWindow(result, jsondata, name, "circle");
                            break;
                        case "/api/newRectangle":
                            Console.WriteLine("New Rectangle");
                            addElementToWindow(result, jsondata, name, "rectangle");
                            break;
                        case "/api/setRectangleTopLeft":
                            Console.WriteLine("Move rectangle");
                            moveElementOnWindow(result, jsondata, name);
                            break;
                        case "/api/relocateCircle":
                            Console.WriteLine("Move Circle");
                            moveElementOnWindow(result, jsondata, name);
                            break;
                        case "/api/newTexRectangle":
                            Console.WriteLine("New Textured Rectangle");
                            addElementToWindow(result, jsondata, name, "image");
                            break;
                        case "/api/setTexRectangleTopLeft":
                            Console.WriteLine("Move Textured rectangle");
                            moveElementOnWindow(result, jsondata, name);
                            break;
                        case "/api/newLine":
                            Console.WriteLine("New Line");
                            addElementToWindow(result, jsondata, name, "line");
                            break;
                        case "/api/newText":
                            Console.WriteLine("New Text");
                            addElementToWindow(result, jsondata, name, "text");
                            break;
                        case "/api/removeElement":
                            Console.WriteLine("Remove Element");
                            removeElementFromWindow(result, jsondata, name, "text");
                            break;
                        default:
                            Console.WriteLine("Path not found");
                            break;
                    }

                    Console.WriteLine("Request of surface list made by: ");
                    //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                    if (PID != -1)
                    {
                        SoD.SendAcknowledgementWithPID(PID, SoD.SODProjector.room);
                    }
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }


        private void getElementsOnWindow(string api_call, string jsondata, string method, int PID)
        {
            try
            {
                Result json = (Result)js.Deserialize(jsondata, typeof(Result));
                string path = _url + api_call;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = method;
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsondata);
                    writer.Flush();
                    writer.Close();
                }
                
                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    Result result_obj = (Result)js.Deserialize(result, typeof(Result));
                    
                    Console.WriteLine("Request of surface list made by: ");
                    //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                    if (PID != -1)
                    {
                        SoD.SendAcknowledgementWithPID(PID, result);
                    }
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }
        

        class MoveElementData
        {
            public int elementNo;
            public double x;
            public double y;
            public string coorSys;
        }

        private void transferElement(string api_call, string jsondata, string method, int PID, string name, int intervals)
        {
            Console.WriteLine("TRANSFER");
            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            Projector.Element element = getElement(json.elementNo);

            double end_x = (double)json.x;
            double end_y = (double)json.y;

            double inc_x = (end_x - (double)element.x) / intervals;
            double inc_y = (end_y - (double)element.y) / intervals;


            while (intervals > 0)
            {
                try
                {
                    string path = _url + api_call;

                    HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                    request.Method = method;
                    request.ContentType = "application/json; charset=UTF-8";

                    MoveElementData data = new MoveElementData();
                    data.elementNo = json.elementNo;
                    data.x = element.x + inc_x;
                    data.y = element.y + inc_y;
                    data.coorSys = json.coorSys;

                    jsondata = js.Serialize(data);

                    //Send the request
                    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(jsondata);
                        writer.Flush();
                        writer.Close();
                    }


                    // Get the response
                    string result = null;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = reader.ReadToEnd();

                        moveElementOnWindow(result, jsondata, name);

                        // Last move, send response
                        if (intervals == 1)
                        {
                            Console.WriteLine("Request of surface list made by: ");
                            //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                            if (PID != -1)
                            {
                                SoD.SendAcknowledgementWithPID(PID, SoD.SODProjector.room);
                                return;
                            }
                        }
                    }

                    intervals--;
                    Console.Write(result);

                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);

                    setup_button.IsEnabled = false;
                    resetup_button.IsEnabled = true;
                }
            }

        }

        private void createNewPath(string api_call, string jsondata, string method, int PID, string name)
        {
            try
            {
                string path = _url + api_call;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = method;
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsondata);
                    writer.Flush();
                    writer.Close();
                }


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    createPathElement(result, jsondata, name, "path");

                    //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                    if (PID != -1)
                    {
                        SoD.SendAcknowledgementWithPID(PID, SoD.SODProjector.room);
                    }
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }

        private void addLineToPath(string api_call, string jsondata, string method, int PID, string name)
        {
            try
            {
                string path = _url + api_call;

                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                request.Method = method;
                request.ContentType = "application/json; charset=UTF-8";

                //Send the request
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(jsondata);
                    writer.Flush();
                    writer.Close();
                }


                // Get the response
                string result = null;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();

                    addLineToPathElement(result, jsondata, name, "line");

                    //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                    if (PID != -1)
                    {
                        SoD.SendAcknowledgementWithPID(PID, SoD.SODProjector.room);
                    }
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                setup_button.IsEnabled = false;
                resetup_button.IsEnabled = true;
            }
        }

        


        #endregion

        #region Update Room

        public void addWindowToRoom(string result, string jsondata)
        {
            // Get the response from the projector server and parse data
            
            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Surface> pair in SoD.SODProjector.room.Surfaces)
            {
                // Add the new window to the surface
                Projector.Surface surface = pair.Value;
                Projector.Window window = new Projector.Window();
                window.ID = result_obj.windowNo.ToString();
                window.elements = new Dictionary<string, Projector.Element>();
                window.owner = surface.ID;
                window.x = json.x;
                window.y = json.y;
                window.height = json.height;
                window.width = json.width;
                window.name = json.name;

                if (surface.ID.Equals(json.surfaceNo.ToString()))
                {
                    surface.windows.Add(window.ID, window);
                    // add to list of all windows
                    SoD.SODProjector.room.Windows.Add(window.ID, window);
                }

                
            }
        }

        private void removeElementFromWindow(string result, string jsondata, string name, string p)
        {
            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Surface> pair in SoD.SODProjector.room.Surfaces)
            {

                Projector.Surface surface = pair.Value;

                foreach (KeyValuePair<string, Projector.Window> w in surface.windows)
                {
                    Projector.Window window = w.Value;

                    if (window.ID.Equals(json.windowNo.ToString()))
                    {
                        foreach (KeyValuePair<string, Projector.Element> e in window.elements)
                        {
                            Projector.Element element = e.Value;
                            if (element.ID == json.elementNo.ToString())
                            {
                                // remove from window's list of elements 
                                window.elements.Remove(e.Key);
                                // add to list of all elements
                                SoD.SODProjector.room.Elements.Remove(e.Key);
                            }
                        }
                        
                    }
                }
            }
        }

        public void addElementToWindow(string result, string jsondata, string name, string type)
        {
            // Get the response from the projector server and parse data

            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Surface> pair in SoD.SODProjector.room.Surfaces)
            {

                Projector.Surface surface = pair.Value;

                foreach (KeyValuePair<string, Projector.Window> w in surface.windows)
                {
                    Projector.Window window = w.Value;
                    if (window.ID.Equals(json.windowNo.ToString()))
                    {
                        if (type == "circle")
                        {
                            Projector.Circle element = new Projector.Circle();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;
                            element.radius = json.radius;
                            element.lineColor = json.lineColor;
                            element.lineWidth = json.lineWidth;
                            element.fillColor = json.fillColor;
                            element.sides = json.sides;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);

                        }
                        else if (type == "rectangle")
                        {
                            Projector.Rectangle element = new Projector.Rectangle();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;
                            element.height = json.height;
                            element.width = json.width;
                            element.lineColor = json.lineColor;
                            element.lineWidth = json.lineWidth;
                            element.fillColor = json.fillColor;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);
                        }
                        else if (type == "image")
                        {
                            Projector.Image element = new Projector.Image();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;
                            element.height = json.height;
                            element.width = json.width;
                            //element.image = json.textureData;
                            element.extension = json.extension;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);
                        }
                        else if (type == "line")
                        {
                            Projector.Line line = new Projector.Line();
                            line.ID = result_obj.elementNo.ToString();
                            line.name = name;
                            line.x = json.xStart;
                            line.y = json.yStart;
                            line.end_x = json.xEnd;
                            line.end_y = json.yEnd;
                            line.type = type;
                            line.owner = window.ID;
                            line.color = json.color;
                            line.width = json.width;

                            // add to windows list of elements 
                            window.elements.Add(line.ID, line);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(line.ID, line);
                        }
                        else if (type == "text")
                        {
                            Projector.Text element = new Projector.Text();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;
                            element.text = json.text;
                            element.font = json.font;
                            element.ptSize = json.ptSize;
                            element.color = json.color;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);
                        }
                        //else
                        //{
                        //    Projector.Element element = new Projector.Element();
                        //    element.ID = result_obj.elementNo.ToString();
                        //    element.name = name;
                        //    element.x = json.x;
                        //    element.y = json.y;
                        //    element.type = type;
                        //    element.owner = window.ID;

                        //    // add to windows list of elements 
                        //    window.elements.Add(element.ID, element);
                        //    // add to list of all elements
                        //    SoD.SODProjector.room.Elements.Add(element.ID, element);
                        //}

                        
                        
                    }
                }
            }
        }

        public void moveElementOnWindow(string result, string jsondata, string name)
        {
            // Get the response from the projector server and parse data

            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Element> pair in SoD.SODProjector.room.Elements)
            {
                Projector.Element element = pair.Value;
                if (element.ID == json.elementNo.ToString())
                {
                    element.x = json.x;
                    element.y = json.y;
                }
            }
        }


        public void createPathElement(string result, string jsondata, string name, string type)
        {
            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Surface> pair in SoD.SODProjector.room.Surfaces)
            {

                Projector.Surface surface = pair.Value;

                foreach (KeyValuePair<string, Projector.Window> w in surface.windows)
                {
                    Projector.Window window = w.Value;
                    if (window.ID.Equals(json.windowNo.ToString()))
                    {
                        Projector.Path element = new Projector.Path();
                        element.ID = result_obj.elementNo.ToString();
                        element.name = name;
                        element.x = json.xStart;
                        element.y = json.yStart;
                        element.type = type;
                        element.owner = window.ID;

                        element.lines = new List<Projector.Line>();

                        pathLinesID = 0;

                        Projector.Line line = new Projector.Line();
                        line.ID = result_obj.elementNo.ToString() +"."+ pathLinesID;
                        line.name = name + "-line";
                        line.x = json.xStart;
                        line.y = json.yStart;
                        line.end_x = json.xEnd;
                        line.end_y = json.yEnd;
                        line.type = "line";
                        line.owner = element.ID;
                        line.color = json.color;
                        line.width = json.width;

                        element.lines.Add(line);

                        // add to windows list of elements 
                        window.elements.Add(element.ID, element);
                        // add to list of all elements
                        SoD.SODProjector.room.Elements.Add(element.ID, element);

                        pathLinesID++;
                    }
                }
            }
        }

        private void addLineToPathElement(string result, string jsondata, string name, string p)
        {
            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, Projector.Element> pair in SoD.SODProjector.room.Elements) 
            {
                Projector.Element element = pair.Value;
                if (element.name == name)
                {
                    Projector.Path path = (Projector.Path)element;

                    Projector.Line line = new Projector.Line();
                    line.ID = result_obj.elementNo.ToString() +"."+ pathLinesID;
                    line.name = name + "-line";
                    line.x = json.xStart;
                    line.y = json.yStart;
                    line.end_x = json.xEnd;
                    line.end_y = json.yEnd;
                    line.type = "line";
                    line.owner = element.ID;
                    line.color = json.color;
                    line.width = json.width;

                    path.lines.Add(line);
                }
            }
        }



        private Projector.Element getElement(int elementNo)
        {
            foreach (KeyValuePair<string, Projector.Element> pair in SoD.SODProjector.room.Elements)
            {
                Projector.Element element = pair.Value;
                if (element.ID == elementNo.ToString())
                {
                    return element;
                }
            }
            return null;
        }


        #endregion

        #region SoD Config

        private void DealWithSoD()
        {
            if (SoD == null)
            {
                configureSoD();
                configureDevice();
                registerSoDEvents();
                connectSoD();
                
            }
        }



        #region SOD parameters
        static SOD_CS_Library.SOD SoD;
        // Device parameters. Set 
        // TODO: FILL THE FOLLOWING VARIABLES WITH POSSIBLE VALUES
        static int _deviceID = 27;                   // OPTIONAL. If it's not unique, it will be "randomly" assigned by locator.
        static string _deviceName = "ProjectedPixelsController";   // You can name your device
        static string _deviceType = "ProjectorController";   // Cusomize device
        static bool _deviceIsStationary = true;     // If mobile device, assign false.
        static double _deviceWidthInM = 1          // Device width in metres
                        , _deviceHeightInM = 1.5   // Device height in metres
                        , _deviceLocationX = 1     // Distance in metres from the sensor which was first connected to the server
                        , _deviceLocationY = 1      // Distance in metres from the sensor which was first connected to the server
                        , _deviceLocationZ = 1      // Distance in metres from the sensor which was first connected to the server
                        , _deviceOrientation = 0    // Device orientation in Degrees, if mobile device, 0.
                        , _deviceFOV = 70;           // Device Field of View in degrees


        // observers can let device know who enters/leaves the observe area.
        static string _observerType = "rectangular";
        static double _observeHeight = 2;
        static double _observeWidth = 2;
        static double _observerDistance = 2;
        static double _observeRange = 2;
        /*
         * You can also do Radial type observer. Simply change _observerType to "radial": 
         *      static string _observerType = "radial";
         * Then observeRange will be taken as the radius of the observeRange.
        */

        // SOD connection parameters
        //static string _SODAddress = "beastwin.marinhomoreira.com"; // LOCATOR URL or IP
        static string _SODAddress = "192.168.0.144"; // Sydney's computer
        static int _SODPort = 3000; // Port of LOCATOR
        #endregion

        public static void configureSoD()
        {
            // Configure and instantiate SOD object
            string address = _SODAddress;
            int port = _SODPort;
            SoD = new SOD_CS_Library.SOD(address, port);

            // configure and connect
            configureDevice();
        }

        private static void configureDevice()
        {
            // This method takes all the parameters you specified above and set the properties accordingly in the SOD object.
            // Configure device with its dimensions (mm), location in physical space (X, Y, Z in meters, from sensor), orientation (degrees), Field Of View (FOV. degrees) and name
            SoD.ownDevice.SetDeviceInformation(_deviceWidthInM, _deviceHeightInM, _deviceLocationX, _deviceLocationY, _deviceLocationZ, _deviceType, _deviceIsStationary);
            //SoD.ownDevice.orientation = _deviceOrientation;
            SoD.ownDevice.FOV = _deviceFOV;
            if (_observerType == "rectangular")
            {
                SoD.ownDevice.observer = new SOD_CS_Library.observer(_observeWidth, _observeHeight, _observerDistance);
            }
            else if (_observerType == "radial")
            {
                SoD.ownDevice.observer = new SOD_CS_Library.observer(_observeRange);
            }

            // Name and ID of device - displayed in Locator
            SoD.ownDevice.ID = _deviceID;
            SoD.ownDevice.name = _deviceName;
        }

        /// <summary>
        /// Connect SOD to Server
        /// </summary>
        public static void connectSoD()
        {
            SoD.SocketConnect();
        }


        /// <summary>
        /// Disconnect SOD from locator.
        /// </summary>
        public static void disconnectSoD()
        {
            SoD.Close();
        }

        /// <summary>
        /// Reconnect SOD to the locator.
        /// </summary>
        public static void reconnectSoD()
        {
            SoD.ReconnectToServer();
        }


        #endregion

        #region SoD Events

        private void registerSoDEvents()
        {
            // register for 'connect' event with io server
            // SOD Default Events, 
            SoD.On("connect", (data) =>
            {
                Console.WriteLine("\r\nConnected...");
                Console.WriteLine("Registering with server...\r\n");
                SoD.RegisterDevice();  //register the device with server everytime it connects or re-connects
            });

            // Sample event handler for when any device connects to server
            SoD.On("someDeviceConnected", (msgReceived) =>
            {
                Console.WriteLine("Some device connected to server: " + msgReceived.data);
            });

            // listener for event a person walks into a device
            SoD.On("enterObserveRange", (msgReceived) =>
            {
                // Parse the message 
                Dictionary<String, String> payload = new Dictionary<string, string>();
                SoD.SendToDevices.All("EnterView", payload);
            });

            // listener for event a person grab in the observeRange of another instance.
            SoD.On("grabInObserveRange", (msgReceived) =>
            {
                Console.WriteLine(" person " + msgReceived.data["payload"]["invader"] + " perform Grab gesture in a " + msgReceived.data["payload"]["observer"]["type"] + ": " + msgReceived.data["payload"]["observer"]["ID"]);
            });

            // listener for event a person leaves a device.
            SoD.On("leaveObserveRange", (msgReceived) =>
            {
                Dictionary<String, String> payload = new Dictionary<string, string>();
                SoD.SendToDevices.All("LeaveView", payload);
            });

            // Sample event handler for when any device disconnects from server
            SoD.On("someDeviceDisconnected", (msgReceived) =>
            {
                Console.WriteLine("Some device disconnected from server : " + msgReceived.data["name"]);
            });
            // END SOD default events

            // Listen for event from Projection to get the room setup 
            SoD.On("getAllSurfacesController", (msgReceived) =>
            {
                int PID = msgReceived.PID;

                Console.WriteLine("Request of surface list made by: " + msgReceived.data["ID"]);
                Console.WriteLine("PID: " + PID);
                //if PID was parsed successfully from the received message, send back a reply/acknowledgement with the same PID
                if (PID != -1)
                {
                    SoD.SendAcknowledgementWithPID(PID, SoD.SODProjector.room);
                }

            });

            // make a create new window for surface call to the projector api
            SoD.On("addWindowController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];
                

                callProjectorServer(api_call, jsondata, method, PID, name);

            });


            // make a create new circle call to the projector api
            SoD.On("newCircleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a move circle call to the projector api
            SoD.On("moveCircleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];
                int intervals = msgReceived.data["intervals"];

                if (intervals > 0)
                    transferElement(api_call, jsondata, method, PID, name, intervals);
                else
                    callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a create new rectangle call to the projector api
            SoD.On("newRectangleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a move rectangle call to the projector api
            SoD.On("moveRectangleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];
                int intervals = msgReceived.data["intervals"];

                if (intervals > 0)
                    transferElement(api_call, jsondata, method, PID, name, intervals);
                else
                    callProjectorServer(api_call, jsondata, method, PID, name);
            });


            // make a create new textured rectangle image call to the projector api
            SoD.On("newTexRectangleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a move rectangle call to the projector api
            SoD.On("moveTexRectangleController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];
                int intervals = msgReceived.data["intervals"];

                if (intervals > 0)
                    transferElement(api_call, jsondata, method, PID, name, intervals);
                else
                    callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a create new line call to the projector api
            SoD.On("newLineController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a create new text call to the projector api
            SoD.On("newTextController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });

            // make a create new line call to the projector api for the start of a path
            SoD.On("newPathController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                createNewPath(api_call, jsondata, method, PID, name);
            });

            // make a create new line call to the projector api for the start of a path
            SoD.On("addLineToPathController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                addLineToPath(api_call, jsondata, method, PID, name);
            });

            // remove element from projector server 
            SoD.On("removeElementController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"];
                string name = msgReceived.data["name"];

                callProjectorServer(api_call, jsondata, method, PID, name);
            });


            // get elements on window from projector server 
            SoD.On("getElementsOnWindowController", (msgReceived) =>
            {
                // Call the API
                int PID = msgReceived.PID;
                string api_call = msgReceived.data["path"];
                string jsondata = msgReceived.data["jsondata"];
                string method = msgReceived.data["method"]; 

                getElementsOnWindow(api_call, jsondata, method, PID);
            });
        }

       

        

        



        #endregion

        // Disconnect SoD so that the device ID will be the same
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            disconnectSoD();
        }

    }
}
