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

        public const double CONVERTER = (512 / 5);

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

                    // Save the room list of surfaces
                    SoD.SODProjector.room.Surfaces = new Dictionary<string, SOD.Surface>();
                    SoD.SODProjector.room.Windows = new Dictionary<string, SOD.Window>();
                    SoD.SODProjector.room.Elements = new Dictionary<string, SOD.Element>();

                    JavaScriptSerializer js = new JavaScriptSerializer();
                    Result res_obj = (Result)js.Deserialize(result, typeof(Result));

                    int count = Convert.ToInt32(res_obj.count);

                    for (int i = 1; i <= count; i++) {
                        // get the surface numbers and save them
                        SOD.Surface surface = new SOD.Surface();
                        surface.ID = i.ToString();
                        surface.windows = new Dictionary<string, SOD.Window>();
                        SoD.SODProjector.room.Surfaces.Add(surface.ID, surface);
                    }

                    SoD.SODProjector.data = SoD.SODProjector.room;
                    SoD.SODProjector.subscriber.subscriberType = "device";
                    SoD.SODProjector.ID = SoD.ownDevice.ID;
                    SoD.RegisterProjector();
                }

                Console.Write(result);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

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
            }
        }

        

        public void addWindowToRoom(string result, string jsondata)
        {
            // Get the response from the projector server and parse data
            
            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach(KeyValuePair<string, SOD.Surface>pair in SoD.SODProjector.room.Surfaces)
            {
                // Add the new window to the surface
                SOD.Surface surface = pair.Value;
                SOD.Window window = new SOD.Window();
                window.ID = result_obj.windowNo.ToString();
                window.elements = new Dictionary<string, SOD.Element>();
                window.owner = surface.ID;

                if (surface.ID.Equals(json.surfaceNo.ToString()))
                {
                    surface.windows.Add(window.ID, window);
                    // add to list of all windows
                    SoD.SODProjector.room.Windows.Add(window.ID, window);
                }

                
            }
        }

        public void addElementToWindow(string result, string jsondata, string name, string type)
        {
            // Get the response from the projector server and parse data

            Result result_obj = (Result)js.Deserialize(result, typeof(Result));

            // parse the jsondata to update room data
            Result json = (Result)js.Deserialize(jsondata, typeof(Result));

            foreach (KeyValuePair<string, SOD.Surface> pair in SoD.SODProjector.room.Surfaces)
            {
               
                SOD.Surface surface = pair.Value;

                foreach (KeyValuePair<string, SOD.Window> w in surface.windows)
                {
                    SOD.Window window = w.Value;
                    if (window.ID.Equals(json.windowNo.ToString()))
                    {
                        if (type == "line")
                        {
                            SOD.Line line = new SOD.Line();
                            line.ID = result_obj.elementNo.ToString();
                            line.name = name;
                            line.x = json.xStart;
                            line.y = json.yStart;
                            line.end_x = json.xEnd;
                            line.end_y = json.yEnd;
                            line.type = type;
                            line.owner = window.ID;

                            // add to windows list of elements 
                            window.elements.Add(line.ID, line);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(line.ID, line);
                        }
                        else if (type == "text")
                        {
                            SOD.Text element = new SOD.Text();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;
                            element.text = json.text;
                            element.font = json.font;
                            element.ptSize = json.ptSize;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);
                        }
                        else
                        {
                            SOD.Element element = new SOD.Element();
                            element.ID = result_obj.elementNo.ToString();
                            element.name = name;
                            element.x = json.x;
                            element.y = json.y;
                            element.type = type;
                            element.owner = window.ID;

                            // add to windows list of elements 
                            window.elements.Add(element.ID, element);
                            // add to list of all elements
                            SoD.SODProjector.room.Elements.Add(element.ID, element);
                        }

                        
                        
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

            foreach (KeyValuePair<string, SOD.Element> pair in SoD.SODProjector.room.Elements)
            {
                SOD.Element element = pair.Value;
                if (element.ID == json.elementNo.ToString())
                {
                    element.x = json.x;
                    element.y = json.y;
                }
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

            SOD.Element element = getElement(json.elementNo);

            double end_x = (double)json.x;
            double end_y = (double)json.y;

            double inc_x = ((end_x * CONVERTER) - (double)element.x) / intervals;
            double inc_y = ((end_y * CONVERTER) - (double)element.y) / intervals;


            while(intervals > 0) 
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
                }
            }
            
        }

        private SOD.Element getElement(int elementNo)
        {
            foreach (KeyValuePair<string, SOD.Element> pair in SoD.SODProjector.room.Elements)
            {
                SOD.Element element = pair.Value;
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
        // TODO: FILL THE FOLLOWING VARIABLES AND WITH POSSIBLE VALUES
        static int _deviceID = -1;                   // OPTIONAL. If it's not unique, it will be "randomly" assigned by locator.
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

        }

        



        #endregion

    }
}
