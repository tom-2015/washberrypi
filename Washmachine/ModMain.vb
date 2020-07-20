Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Windows.Forms
Imports Washmachine
Imports WiringPiNet

Module ModMain

    ''' <summary>
    ''' The small httpserver based on httplistener
    ''' </summary>
    Public WithEvents HttpServer As WebServer

    ''' <summary>
    ''' The machine object is main washing machine
    ''' </summary>
    Public WithEvents Machine As WashingMachine

    ''' <summary>
    ''' Directory where the local http files are stored, in case you want to run http server for controlling the washingmachine from webbrowser
    ''' </summary>
    Public WwwDir As String = IO.Path.GetDirectoryName(Application.ExecutablePath) & IO.Path.DirectorySeparatorChar & "www"

    ''' <summary>
    ''' The main sub
    ''' </summary>
    Sub Main()
        Dim CtrlCPressed As Boolean = False
        Dim Timeout As Integer = 120

        'wait for clock sync or the program may corrupt when time is changed when running
        While Not isClockSynced() AndAlso Timeout > 0
            Console.WriteLine("Waiting for clock sync...")
            Thread.Sleep(1000)
            Timeout -= 1
        End While

        'create a new washingmachine
        Machine = New WashingMachine()
        Machine.Reset()

        'create new webserver and listen on port 80
        HttpServer = New WebServer
        HttpServer.AddDirectory(WwwDir)
        HttpServer.Listen(80)

        'Ctrl-C will exit program but this does not always work properly
        AddHandler Console.CancelKeyPress, Sub()
                                               Volatile.Write(CtrlCPressed, True)
                                           End Sub

        While Not Volatile.Read(CtrlCPressed)
            Thread.Sleep(2000)
        End While

        Console.WriteLine("Exiting " & CtrlCPressed)

        'clean up
        HttpServer.StopListen()
        Console.WriteLine("Ended HTTP server")
        Machine.Reset()
        Console.WriteLine("Machine reset ok")
        PiGpio.GpioTerminate()
        Console.WriteLine("GPIO terminate OK")

        Throw New Exception()
        End
    End Sub

    ''' <summary>
    ''' Returns true if the raspberry time / clock is synced using timedatctl command
    ''' </summary>
    ''' <returns></returns>
    Public Function isClockSynced() As Boolean
        Dim StartInfo As New ProcessStartInfo("timedatectl")
        StartInfo.RedirectStandardOutput = True
        StartInfo.UseShellExecute = False

        Dim P As Process = Process.Start(StartInfo)

        P.WaitForExit()

        Dim Text As String = P.StandardOutput.ReadToEnd()

        If InStr(Text, "System clock synchronized: yes") > 0 Then
            Console.WriteLine("Time sync OK: " & Text)
            Return True
        End If


        Return False

    End Function

    ''' <summary>
    ''' Handles incoming http requests
    ''' </summary>
    ''' <param name="Server"></param>
    ''' <param name="Context"></param>
    ''' <param name="Params"></param>
    Private Sub HttpServer_HttpRequest(Server As WebServer, Context As HttpListenerContext, Params As WebServer.HttpRequestParameters) Handles HttpServer.HttpRequest
        Try
            Dim Url As String = Context.Request.Url.AbsolutePath
            Console.WriteLine("Incoming http request " & Url)

            'set params.isProcessed = true to prevent the webserver class for searching the file on SD card
            Select Case Url
                Case "/"
                    Context.Response.StatusCode = 200
                    Context.Response.ContentType = "text/html"
                    Context.Response.ContentLength64 = FileLen(WwwDir & "/index.htm")
                    Server.SendFile(Context, WwwDir & "/index.htm")
                    Params.IsProcessed = True
                Case "/status"
                    Params.IsProcessed = SendStatus(Server, Context, Url)
                    Params.IsProcessed = True
                Case "/enable_accel"
                    Machine.Motor.StartAcceleroMeter()
                    Server.SendResponseText(Context, "{""error"":0}", "application/json")
                    Params.IsProcessed = True
                Case "/disable_accel"
                    Machine.Motor.StopAcceleroMeter()
                    Server.SendResponseText(Context, "{""error"":0}", "application/json")
                    Params.IsProcessed = True
                Case "/debug_accel"
                    EnableDebuggingAccel(Context)
                    Server.SendResponseText(Context, "{""error"":0}", "application/json")
                    Params.IsProcessed = True
                Case "/stop" 'stop
                    If Machine.Program IsNot Nothing Then
                        Machine.Program.StopExecute()
                    End If
                    Server.SendResponseText(Context, "{""error"":0}", "application/json")
                    Params.IsProcessed = True
                Case "/continue"
                    Dim ErrorNr As Integer = 1
                    If Machine.Program IsNot Nothing Then
                        If Machine.Program.State = Program.ProgramStates.Waiting Then
                            Machine.Program.State = Program.ProgramStates.Running
                            ErrorNr = 0
                        End If
                    End If
                    Server.SendResponseText(Context, "{""error"":" & ErrorNr & "}", "application/json")
                    Params.IsProcessed = True
                Case "/start" 'start
                    If LoadProgram(Server, Context) Then
                        Server.SendResponseText(Context, "{""error"": 0}", "application/json")
                    Else
                        Server.SendResponseText(Context, "{""error"": 1}", "application/json")
                    End If
                    Params.IsProcessed = True
            End Select
        Catch e As Exception
            Console.WriteLine("Http process exception: " & e.Message & " " & e.StackTrace)
        End Try
    End Sub

    ''' <summary>
    ''' Enables debugging (sending UDP packets) with accelerometer values to the FrmVibration which should be running on faster (windows) computer
    ''' requires the ip variable in GET request, if ip="" disable debugging else send UDP packets to port 2005 at ip
    ''' </summary>
    ''' <param name="Context"></param>
    Private Sub EnableDebuggingAccel(ByVal Context As HttpListenerContext)
        Dim IP As String = Context.Request.QueryString.Item("ip")
        Dim IPAddr As New IPAddress(0)
        If IPAddress.TryParse(IP, IPAddr) Then
            Machine.Motor.EnableAcceleroMeterDebugging(IPAddr)
        Else
            Machine.Motor.DisableAcceleroMeterDebugging()
        End If

    End Sub

    ''' <summary>
    ''' Loads a washing program from webrequest
    ''' requires program_type, temp, rpm and options as post requests 
    ''' </summary>
    ''' <param name="Server"></param>
    ''' <param name="Context"></param>
    ''' <returns></returns>
    Private Function LoadProgram(ByVal Server As WebServer, ByVal Context As HttpListenerContext) As Boolean
        Dim Variables As NameValueCollection = WebServer.DecodePostBody(Context.Request)

        Dim ProgramType As ProgramManager.ProgramTypes = Integer.Parse(Variables.Item("program_type"))
        Dim WantedTemp As Integer = Integer.Parse(Variables.Item("temp"))
        Dim WantedRPM As Integer = Integer.Parse(Variables.Item("rpm"))

        Dim Options As ProgramManager.ProgramOptions = ProgramManager.ProgramOptions.None
        Dim OptionVariables() As String = Variables.GetValues("options")
        If OptionVariables IsNot Nothing Then
            For Each Value As String In OptionVariables
                If Value <> "" AndAlso IsNumeric(Value) Then
                    Options = Options Or Integer.Parse(Value)
                End If
            Next
        End If


        Dim Program As Program = Machine.ProgramManager.GenerateProgram(ProgramType, WantedTemp, WantedRPM, Options)
        Machine.StartProgram(Program)
        Program.Execute()

        Return True
    End Function

    ''' <summary>
    ''' Sends the status on /status request as json
    ''' </summary>
    ''' <param name="Server"></param>
    ''' <param name="Context"></param>
    ''' <param name="Url"></param>
    ''' <returns></returns>
    Private Function SendStatus(ByVal Server As WebServer, ByVal Context As HttpListenerContext, ByVal Url As String) As Boolean
        Dim Status As String = Machine.toJSON()

        Context.Response.ContentLength64 = Status.Length
        Context.Response.ContentEncoding = Text.Encoding.UTF8
        Context.Response.ContentType = "application/json"
        Context.Response.StatusCode = 200
        Server.SendText(Context, Status)

        Return True
    End Function

    ''' <summary>
    ''' If the washingmachine sends debugging information this sub writes it to the command line
    ''' </summary>
    ''' <param name="Machine"></param>
    ''' <param name="Message"></param>
    Private Sub Machine_DebugEvent(Machine As WashingMachine, Message As String) Handles Machine.DebugEvent
        Console.WriteLine(Message)
    End Sub


End Module
