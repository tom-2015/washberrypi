Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports Washmachine

Public Class FrmTest

    Public WithEvents HttpServer As WebServer

    Public WithEvents Machine As WashingMachine
    Public WithEvents Program As Program
    Public WithEvents Motor As MotorControl

    'Public WithEvents Accel As ADXL345

    'Dim m_XMax As Single, m_YMax As Single, m_ZMax As Single
    'Dim m_XAvg(0 To 400) As Single
    'Dim m_YAvg(0 To 400) As Single
    'Dim m_ZAvg(0 To 400) As Single

    Dim Frm As FrmVibration
    Dim wwwDir As String = IO.Path.GetDirectoryName(Application.ExecutablePath) & IO.Path.DirectorySeparatorChar & "www"

    Private Sub FrmTest_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        CmbProgramType.Items.Clear()

        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.Fast))
        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.DailyWash))
        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.ExtraDirty))
        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.Rinse))
        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.Centrifuge))
        CmbProgramType.Items.Add(New ProgramType(ProgramManager.ProgramTypes.Delicate))

        Machine = New WashingMachine(IO.Path.GetDirectoryName(Application.ExecutablePath))
        Machine.Reset()
        Motor = Machine.Motor

        HttpServer = New HttpWebServer(90)

        HttpServer.AddDirectory(wwwDir, "/")
        TmrRefresh.Enabled = True

        'Accel = Machine.Motor.AcceleroMeter



    End Sub

    Private Sub Machine_DebugEvent(Machine As WashingMachine, Message As String) Handles Machine.DebugEvent
        'Console.WriteLine(Message)
        If TxtInfo.InvokeRequired Then
            TxtInfo.Invoke(Sub()
                               TxtInfo.AppendText(Message & vbCrLf)
                           End Sub)
        Else
            TxtInfo.AppendText(Message & vbCrLf)
        End If
    End Sub

    Private Sub TmrRefresh_Tick(sender As Object, e As EventArgs) Handles TmrRefresh.Tick
        LblMotorSpeed.Text = Machine.Motor.GetAvgRPM()
        LblTemp.Text = Machine.Heater.GetTemp()
        LblWater.Text = Machine.Water.GetWaterLevel()
        LblLoad.Text = Machine.Motor.CurrentRPM * (Math.Cos(Machine.Motor.CurrentOutputPower * Math.PI / 100) + 1)
    End Sub

    Public Sub StartProgram(ByVal Program As Program)
        Me.Program = Program
        SyncLock LstProgress
            LstProgress.Items.Clear()
            For i As Integer = 0 To Program.Blocks.Count - 1
                LstProgress.Items.Add(Program.Blocks(i))
            Next
        End SyncLock
        Program.Execute()
    End Sub

    Private Sub SavePrograms()
        'Program = New Program(Machine)

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, -1, 10 * 60, 120 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, 0, 3 * 60, 0.7))

        'For i As Integer = 0 To 1
        '    Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        '    Program.AddBlock(New Wash(Program, 0, 2 * 60, 10 * 60))
        '    Program.AddBlock(New PumpOutWater(Program))
        '    Program.AddBlock(New Centrifuge(Program, 0, 1.5 * 60, 0.45))
        'Next

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 4 * 60, 10 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, 0, 4 * 60, 1))
        'Program.AddBlock(New OpenDoorLock(Program))

        'ProgramManager.SaveProgram(IO.Path.GetDirectoryName(Application.ExecutablePath) & IO.Path.DirectorySeparatorChar & "daily_wash.xml", Program)

        '''''''


        'Program = New Program(Machine, "Gordijnen")

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 80, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, -1, 10 * 60, 30 * 60))

        'Program.AddBlock(New PumpOutWater(Program))

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 4 * 60, 10 * 60))
        'Program.AddBlock(New WaitForInput(Program))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New OpenDoorLock(Program))

        'ProgramManager.SaveProgram(IO.Path.GetDirectoryName(Application.ExecutablePath) & IO.Path.DirectorySeparatorChar & "curtains.xml", Program)





    End Sub

    Private Sub CmdStartWash_Click(sender As Object, e As EventArgs) Handles CmdStartWash.Click
        If CmbProgramType.SelectedItem Is Nothing Then
            MsgBox("Select program type")
            Exit Sub
        End If

        If Program IsNot Nothing Then
            Program.StopExecute()
        End If

        Dim Options As ProgramManager.ProgramOptions

        If ChWaitFinalCentrifuge.Checked Then
            Options = Options Or ProgramManager.ProgramOptions.WaitForFinalCentrifuge
        End If

        StartProgram(Machine.ProgramManager.GenerateProgram(CmbProgramType.SelectedItem.programtype, TxtWantedTemp.Text, TxtRPM.Text, Options))

        'If Program IsNot Nothing Then
        '    Program.StopExecute()
        'End If

        'Program = New Program(Machine)

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, -1, 10 * 60, 120 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, 0, 3 * 60, 0.7))

        'For i As Integer = 0 To 1
        '    Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        '    Program.AddBlock(New Wash(Program, 0, 2 * 60, 10 * 60))
        '    Program.AddBlock(New PumpOutWater(Program))
        '    Program.AddBlock(New Centrifuge(Program, 0, 1.5 * 60, 0.45))
        'Next

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 4 * 60, 10 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, 0, 4 * 60, 1))
        'Program.AddBlock(New OpenDoorLock(Program))

        ' ProgramManager.SaveProgram(Machine.ProgramManager.ProgramDirectory & IO.Path.DirectorySeparatorChar & "daily_wash.xml", Program)




    End Sub

    'Private Sub Centrifuge()
    '    Machine.PumpOutWater(20, 5000)
    '    Thread.Sleep(100)
    '    Machine.StartCentrifuge(1450)
    '    Thread.Sleep(5 * 60 * 1000)
    '    Machine.StopCentrifuge()
    '    Thread.Sleep(30 * 1000)
    'End Sub

    Private Sub CmdStartCentrifuge_Click(sender As Object, e As EventArgs) Handles CmdStartCentrifuge.Click
        If Program IsNot Nothing Then
            Program.StopExecute()
        End If

        Program = New Program(Machine)

        Program.AddBlock(New CloseDoorLock(Program))
        Program.AddBlock(New PumpOutWater(Program))
        Program.AddBlock(New Centrifuge(Program, TxtRPM.Text, 7 * 60))
        Program.AddBlock(New OpenDoorLock(Program))

        StartProgram(Program)
    End Sub

    Private Sub CmdReset_Click(sender As Object, e As EventArgs) Handles CmdReset.Click
        If Program IsNot Nothing Then Program.StopExecute()
        Machine.Reset()
    End Sub

    Private Sub Program_ExecuteProgress(Program As Program, ByVal CurrentIndex As Integer, CurrentBlock As ProgramBlock, Message As String) Handles Program.ExecuteProgress
        SyncLock LstProgress
            LstProgress.SelectedIndex = CurrentIndex
        End SyncLock
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Machine.StartWashing()
    End Sub

    Private Sub TmrTime_Tick(sender As Object, e As EventArgs) Handles TmrTime.Tick
        If Program IsNot Nothing Then
            Try
                LblTimeLeft.Text = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTimeLeft()).Ticks), "HH:mm:ss")
                LblTotalTime.Text = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTotalTime()).Ticks), "HH:mm:ss")
                LblTimeRunning.Text = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTimeRunning()).Ticks), "HH:mm:ss")
            Catch ex As ArgumentOutOfRangeException
            End Try
            SyncLock LstProgress
                LstProgress.Refresh()
            End SyncLock
        End If
    End Sub

    Private Sub FrmTest_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        HttpServer.StopListen()
        If Program IsNot Nothing Then
            Program.StopExecute()
        End If
        If Machine IsNot Nothing Then
            Machine.Reset()
        End If
        End
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If Machine.DoorLock.CurrentValue = 0 Then
            Machine.CloseDoorLock()
        End If
        Machine.Motor.EnableMotorPower()
        Machine.Motor.SetWantedPower(TxtPower.Text)

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        'If Program IsNot Nothing Then
        '    Program.StopExecute()
        'End If

        'Program = New Program(Machine)

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 80, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, 30, 10 * 60, 120 * 60))

        'Program.AddBlock(New PumpOutWater(Program))

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 5 * 60, 10 * 60))
        'Program.AddBlock(New WaitForInput(Program))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New OpenDoorLock(Program))
        ''MsgBox("start")
        'StartProgram(Program)
    End Sub

    Private Sub HttpServer_HttpRequest(Server As HttpWebServer, Connection As HttpWebServerConnection, Url As String, ByRef isProcessed As Boolean) Handles HttpServer.HttpRequest
        Console.WriteLine("Incoming http request " & Url)
        Select Case Url
            Case "/"
                SendFile("index.htm", Connection)
            Case "/status"
                Console.WriteLine("send status")
                isProcessed = SendStatus(Server, Connection, Url)
                Console.WriteLine("send status end " & isProcessed)
            Case "/load" 'loads a program
               ' isProcessed =
            Case "/pause" 'pause

            Case "/stop" 'stop
                If Machine.Program IsNot Nothing Then
                    Machine.Program.StopExecute()
                End If
            Case "/start" 'start
                If LoadProgram(Connection) Then
                    StartProgram(Machine.Program)
                    Console.WriteLine("Start by webserver OK")
                    SendFile("started.htm", Connection)
                    isProcessed = True
                End If
        End Select
    End Sub

    Protected Sub SendFile(ByVal File As String, ByVal Connection As HttpWebServerConnection)
        Dim FileStream As New FileStream(wwwDir & Path.DirectorySeparatorChar & File, FileMode.Open)
        Connection.AddHeader(NETWebServer.HttpWebServerConnection.HTTP_CONTENT_LENGTH_HEADER, FileLen(wwwDir & Path.DirectorySeparatorChar & File))
        Connection.SendHttpCode(HttpWebServerCodes.HTTP_OK)
        Connection.SendStream(FileStream)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Frm = New FrmVibration

        Frm.Show()
        Frm.SetObject(Machine.Motor.AcceleroMeter, Machine.Motor)
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        'm_XMax = 0
        'm_YMax = 0
        'm_ZMax = 0

    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        'If Program IsNot Nothing Then
        '    Program.StopExecute()
        'End If

        'Program = New Program(Machine)

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, TxtWantedTemp.Text, 5 * 60, 120 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, TxtRPM.Text * 0.7, 3 * 60))

        ''For i As Integer = 0 To 1
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 80, 35))
        'Program.AddBlock(New Wash(Program, 0, 2.5 * 60, 10 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, 600, 1.5 * 60))
        ''Next
        '' Program.AddBlock(New Centrifuge(Program, 600, 4 * 60))

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 2.5 * 60, 10 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, TxtRPM.Text, 4 * 60))
        'Program.AddBlock(New OpenDoorLock(Program))
        ''MsgBox("start")
        'StartProgram(Program)
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click



        'If Program IsNot Nothing Then
        '    Program.StopExecute()
        'End If
        'TxtWantedTemp.Text = 90
        'TxtRPM.Text = 1400

        'Program = New Program(Machine)

        'Program.AddBlock(New CloseDoorLock(Program))
        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        'Program.AddBlock(New CreateFoam(Program, 80))
        'Program.AddBlock(New Wash(Program, TxtWantedTemp.Text, 10 * 60, 120 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, TxtRPM.Text * 0.7, 2 * 60))

        'For i As Integer = 0 To 1
        '    Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
        '    Program.AddBlock(New Wash(Program, 0, 2 * 60, 10 * 60))
        '    Program.AddBlock(New PumpOutWater(Program))
        '    Program.AddBlock(New Centrifuge(Program, 600, 1.5 * 60))
        'Next
        '' Program.AddBlock(New Centrifuge(Program, 600, 4 * 60))

        'Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        'Program.AddBlock(New Wash(Program, 0, 4 * 60, 10 * 60))
        'Program.AddBlock(New PumpOutWater(Program))
        'Program.AddBlock(New Centrifuge(Program, TxtRPM.Text, 4 * 60))
        'Program.AddBlock(New OpenDoorLock(Program))
        ''MsgBox("start")
        'StartProgram(Program)
    End Sub

    Private Function LoadProgram(ByVal Connection As HttpWebServerConnection) As Boolean
        Dim ProgramType As ProgramManager.ProgramTypes = Integer.Parse(Connection.Variables("program_type").Text)
        Dim WantedTemp As Integer = Integer.Parse(Connection.Variables("temp").Text)
        Dim WantedRPM As Integer = Integer.Parse(Connection.Variables("rpm").Text)

        Dim Options As ProgramManager.ProgramOptions = ProgramManager.ProgramOptions.None
        For Each Variable As HttpWebServerParameter In Connection.VariablesList
            If Variable.Name = "options" Then
                Options = Options Or Integer.Parse(Variable.Text)
            End If
        Next

        Dim Program As Program = Machine.ProgramManager.GenerateProgram(ProgramType, WantedTemp, WantedRPM, Options)
        Machine.SetProgram(Program)
        Program.Execute()
        Connection.SendData("OK")
        Connection.SendHttpCode(HttpWebServerCodes.HTTP_OK)

        Return True
    End Function

    Private Function SendStatus(ByVal Server As HttpWebServer, ByVal Connection As HttpWebServerConnection, ByVal Url As String) As Boolean
        Dim Status As String = Machine.toJSON()

        Console.WriteLine("add headers")
        Connection.AddHeader(HttpWebServerConnection.HTTP_CONTENT_TYPE_HEADER, "application/json")
        Connection.AddHeader(NETWebServer.HttpWebServerConnection.HTTP_CONTENT_LENGTH_HEADER, Status.Length)
        Connection.SendHttpCode(HttpWebServerCodes.HTTP_OK)
        Console.WriteLine("send status")
        Connection.SendData(Status)
        'Connection.Writer.Flush()
        Console.WriteLine("finish")
        Return True
    End Function

    Private Sub Motor_UnbalanceAlarm(Control As MotorControl, ForAxis As Integer, X As Single, Y As Single, Z As Single, TryCount As Integer) Handles Motor.UnbalanceAlarm
        IO.File.AppendAllText("/home/pi/unbalance.txt", "unbalance detected: " & ForAxis & ",x:" & X & ",y:" & Y & ",z:" & Z & ", cnt=" & TryCount)
    End Sub

    'Private Sub Accel_NewSample(Sample As ADXL345.GSample) Handles Accel.NewSample
    '    Static i As Integer
    '    i = i + 1

    '    If Math.Abs(Sample.X) > m_XMax Then m_XMax = Math.Abs(Sample.X)
    '    If Math.Abs(Sample.Y) > m_YMax Then m_YMax = Math.Abs(Sample.Y)
    '    If Math.Abs(Sample.Z) > m_ZMax Then m_ZMax = Math.Abs(Sample.Z)

    '    For j As Integer = 0 To m_XAvg.Count - 2
    '        m_XAvg(j) = m_XAvg(j + 1)
    '        m_YAvg(j) = m_YAvg(j + 1)
    '        m_ZAvg(j) = m_ZAvg(j + 1)
    '    Next

    '    m_XAvg(m_XAvg.Count - 1) = Sample.X
    '    m_YAvg(m_YAvg.Count - 1) = Sample.Y
    '    m_ZAvg(m_ZAvg.Count - 1) = Sample.Z



    '    If (i Mod 200) = 0 Then
    '        LblAccelX.Invoke(Sub()
    '                             LblAccelX.Text = Sample.X
    '                             LblAccelY.Text = Sample.Y
    '                             LblAccelZ.Text = Sample.Z

    '                             LblXMax.Text = m_XMax
    '                             LblYMax.Text = m_YMax
    '                             LblZMax.Text = m_ZMax

    '                             Dim AvgX As Single, AvgY As Single, AvgZ As Single
    '                             For k As Integer = 0 To m_XAvg.Count - 1
    '                                 AvgX += m_XAvg(k)
    '                                 AvgY += m_YAvg(k)
    '                                 AvgZ += m_ZAvg(k)
    '                             Next

    '                             AvgX = AvgX / m_XAvg.Count
    '                             AvgY = AvgY / m_YAvg.Count
    '                             AvgZ = AvgZ / m_ZAvg.Count

    '                             LblXAvg.Text = AvgX
    '                             LblYAvg.Text = AvgY
    '                             LblZAvg.Text = AvgZ
    '                         End Sub)
    '    End If
    'End Sub
End Class