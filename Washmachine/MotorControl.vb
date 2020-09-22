Imports System.IO.Ports
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports Washmachine
Imports WiringPiNet

''' <summary>
''' This class controls motor speed, it sends commands to the microcontroller that drives the TRIAC
''' Also processes incoming data from the microcontroller like rotation speed, temperature and water level
''' the microcontroller is connected with the Raspberry Pi serial port at 9600 baud
''' also reads accelerometer samples to determine the unbalance when starting centrifuge
''' </summary>
Public Class MotorControl
    Public Const CentrifugeUnbalanceTestSpeed As Integer = 250

    <Flags>
    Public Enum ErrorFlags As Integer
        ErrorNoMotorPower = 2
        ErrorNoMotorBlocked = 4
        ErrorWaterSensor = 8
    End Enum

    Protected WithEvents m_Comport As IO.Ports.SerialPort

    Protected m_MonoEventThread As Thread  'mono doens't support serial port events, so we create a new thread that handles incoming data
    Protected m_ControlThread As Thread    'a sperate thread used for speed control, when washing this tread will change the spinning direction of the motor every x seconds
    Protected m_UnabalanceAlarm As Boolean 'becomes true if unbalance was detected with the accelerometer

    Public PrintDebugData As Boolean = True 'set to false to disable printing debug information

    Public Event DebugPrint(ByVal Control As MotorControl, ByVal Text As String)

    ''' <summary>
    ''' Fired when unabalance is detected, motor will then spin a few seconds in both directions and start a new try to centrifuge
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="ForAxis"></param>
    ''' <param name="X"></param>
    ''' <param name="Y"></param>
    ''' <param name="Z"></param>
    ''' <param name="TryCount"></param>
    Public Event UnbalanceAlarm(ByVal Control As MotorControl, ByVal ForAxis As Integer, ByVal X As Single, ByVal Y As Single, ByVal Z As Single, ByVal TryCount As Integer)

    ''' <summary>
    ''' Fired when balance is detected and centrifuge speed will start going to max speed
    ''' </summary>
    ''' <param name="Control"></param>
    Public Event BalanceDetectionOK(ByVal Control As MotorControl)

    ''' <summary>
    ''' Failed to get wash in balance, what did they put in the drum??
    ''' </summary>
    ''' <param name="Control"></param>
    Public Event BalanceDetectionFailed(ByVal Control As MotorControl)

    ''' <summary>
    ''' Fired when the wanted centrifuge speed is reached, countdown timer will then start
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="Speed"></param>
    Public Event WantedCentrifugeSpeedReached(ByVal Control As MotorControl, ByVal Speed As Integer)

    ''' <summary>
    ''' Fired when an error is received from the motorcontroller
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="Err"></param>
    Public Event MotorError(ByVal Control As MotorControl, ByVal Err As ErrorFlags)

    ''' <summary>
    ''' Fired when serial data was received
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="Data"></param>
    Public Event SampleArrival(ByVal Control As MotorControl, ByVal Data As String)


    'last input values from motor microcontroller
    Public CurrentOutputPower As Integer
    Public TachoPeriod As Integer
    Public CurrentRPM As Integer
    Public Acceleration As Integer 'acceleration of rpm (= current_rot_speed_samp - p_rot_speedsample)
    Public CurrentTemp As Integer
    Public WantedSpeed As Integer
    Public WaterLevel As Integer
    Public ErrorCode As Integer

    'accelerometer values
    Public AcceleroMeter As ADXL345
    Public FilterCoef As Single = 0.6
    Public ThresholdX As Single = 1.3
    Public ThresholdY As Single = 2.4
    Public ThresholdZ As Single = 2.3
    Public PeakThresholdX As Single = 1.1
    Public PeakThresholdY As Single = 3.1
    Public PeakThresholdZ As Single = 11
    Public ThresholdCountX As Integer
    Public ThresholdCountY As Integer
    Public ThresholdCountZ As Integer
    Public PeakX As Single
    Public PeakY As Single
    Public PeakZ As Single
    Public AcceleroMeterDebugging As Boolean
    Public AcceleroMeterDebugIP As IPEndPoint 'send UDP packets
    Public Const AcceleroMeterDebugPort As Integer = 2005

    Protected m_DebugSocket As New UdpClient 'udp socket for accelerometer debugging, will send each accelerometer sample over UDP

    Protected m_SamplesReceived As Integer
    Protected m_SamplesThread As Thread
    Protected m_AcceleroMeterEnabled As Boolean 'true if the accelerometer SamplesThread is running and processing samples from accelerometer, this thread will detect unbalance and set m_UnabalnceAlarm
    Protected m_AcceleroMeterWorking As Boolean
    Protected m_AcceleroMeterDatagramBuffer As New Queue(Of Byte())
    Protected m_AcceleroMeterDebuggingThread As Thread

    Protected m_LastRPM(0 To 5) As Integer
    Protected m_AvgRPM As Integer
    Protected m_RPMLock As New Object
    Protected m_LastDUnbalance(0 To 5) As Integer
    Protected m_LastSUnbalance(0 To 5) As Integer
    Protected m_AvgDUnbalance As Integer
    Protected m_AvgSUnbalance As Integer
    Protected m_TryCentrifugeCount As Integer

    Protected Class CentrifugeParams
        Public Speed As Integer
        Public Direction As Boolean

        Public Sub New(ByVal Speed As Integer, ByVal Direction As Boolean)
            Me.Speed = Speed
            Me.Direction = Direction
        End Sub
    End Class

    Protected Enum MotorControlStates
        Idle
        Washing
        Centrifuge
    End Enum

    Protected m_State As MotorControlStates
    Protected m_PowerPin As GpioPin

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="CommPort">Port filename where microntroller is connected</param>
    ''' <param name="GpioPin">The GPIOPin used for turning on power to the motorcontrol circuit</param>
    Public Sub New(ByVal CommPort As String, ByVal GpioPin As GpioPin)
        m_PowerPin = GpioPin
        m_PowerPin.SetMode(PinMode.Output)
        m_PowerPin.Write(0)

        m_Comport = New IO.Ports.SerialPort(CommPort, 9600, Parity.None, 8, 1)
        m_Comport.Open()
        m_Comport.NewLine = vbLf

        If Environment.OSVersion.Platform = PlatformID.Unix Then
            m_MonoEventThread = New Thread(AddressOf MonoSerialPortMonitor)
            m_MonoEventThread.IsBackground = True
            m_MonoEventThread.Name = "Motorcontrol:monoSerialPortMonitor"
            m_MonoEventThread.Start()
        End If

        m_State = MotorControlStates.Idle

        AcceleroMeter = New ADXL345()
        AcceleroMeter.SetFifoMode(ADXL345.FIFOModes.FIFO_STREAM)
        'AcceleroMeter.EnableEvents()

    End Sub


    ''' <summary>
    ''' Starts washing (alternate turning both directions)
    ''' CycleDelay is the time in ms between changing rotation
    ''' In addition every 5 cycles the motor will wait 15 seconds to prevent overheating
    ''' </summary>
    Public Function StartWashing(Optional ByVal CycleDelay As Integer = 5000) As Boolean
        StopMotor()
        Dim Res As Boolean = EnableMotorPower()
        m_State = MotorControlStates.Washing
        m_ControlThread = New Thread(AddressOf WashingThread)
        m_ControlThread.Start(CycleDelay)
        Return Res
    End Function

    ''' <summary>
    ''' Starts centrifuging at given RPM
    ''' Direction can be changed with Direction
    ''' </summary>
    ''' <param name="RPM"></param>
    ''' <param name="Direction"></param>
    Public Function StartCentrifuge(ByVal RPM As Integer, ByVal Direction As Boolean) As Boolean
        StopMotor()
        Dim Res As Boolean = EnableMotorPower()
        m_State = MotorControlStates.Centrifuge
        m_ControlThread = New Thread(AddressOf CentrifugeThread)
        m_ControlThread.Start(New CentrifugeParams(RPM, Direction))
        Return Res
    End Function

    ''' <summary>
    ''' Stop the motor (set speed 0)
    ''' </summary>
    Public Sub StopMotor()
        SetWantedSpeed(0)
        Thread.Sleep(100)
        If m_ControlThread IsNot Nothing Then
            m_State = MotorControlStates.Idle
            If Not m_ControlThread.Join(5000) Then m_ControlThread.Abort()
            m_ControlThread = Nothing
        End If
        Thread.Sleep(100)
        SetWantedSpeed(0)
        Thread.Sleep(100)
        SetBoost(False)
        Thread.Sleep(100)
    End Sub

    ''' <summary>
    ''' Run by ControlThread to do washing cycles
    ''' </summary>
    ''' <param name="CycleDelay"></param>
    Public Sub WashingThread(ByVal CycleDelay As Integer)
        Try
            Dim i As Integer = 0
            SetBoost(False)
            Thread.Sleep(100)
            While m_State <> MotorControlStates.Idle
                WashCycle(CycleDelay)
                i += 1
                If i = 5 Then 'every 3min we wait 15 seconds to prevent overheating of motor
                    Dim WaitStart As Date = Now()
                    While m_State <> MotorControlStates.Idle AndAlso (Now() - WaitStart).TotalSeconds < 15
                        Thread.Sleep(500)
                    End While
                End If
            End While
        Catch tab As ThreadAbortException

        Catch e As Exception

        Finally
            SetWantedSpeed(0)
        End Try
    End Sub

    Protected Sub WashCycle(ByVal CycleDelay As Integer)
        Thread.Sleep(100)
        SetWantedSpeed(40)
        Thread.Sleep(20000)
        SetWantedSpeed(0)
        Thread.Sleep(CycleDelay)
        SetWantedSpeed(-40)
        Thread.Sleep(12000)
        SetWantedSpeed(0)
        Thread.Sleep(CycleDelay)
    End Sub

    ''' <summary>
    ''' Run by ControlThread to do centrifuge
    ''' </summary>
    ''' <param name="Params"></param>
    Protected Sub CentrifugeThread(ByVal Params As CentrifugeParams)
        Try
            Dim Direction As Integer = 1

            If Params.Direction Then Direction = -1
            SetWantedSpeed(0)
            Thread.Sleep(100)
            SetBoost(False)

            StartAcceleroMeter() 'starts a high priority thread that reads data from accelerometer, disable the display

            If Not m_AcceleroMeterWorking Then
                If PrintDebugData Then RaiseEvent DebugPrint(Me, "Accelerometer not working!! abort centrifuge")
                Return
            End If

            'first very slowly go to 250 and check for any unbalance using the accelerometer
            Dim TryCentrifugeTimeout As Integer = 25
            Do
                If PrintDebugData Then RaiseEvent DebugPrint(Me, "Try centrifuge " & TryCentrifugeTimeout)

                'first do a short wash cycle
                Dim NWashCycle As Integer = 1
                If TryCentrifugeTimeout <= 21 Then NWashCycle = Rnd() * 4 + 1
                For n As Integer = 0 To NWashCycle - 1
                    Thread.Sleep(100)
                    SetWantedSpeed(40 * Direction)
                    Thread.Sleep(15000)
                    SetWantedSpeed(0)
                    Thread.Sleep(2000)
                    SetWantedSpeed(-40 * Direction)
                    Thread.Sleep(10000)
                    SetWantedSpeed(0)
                    Thread.Sleep(100)
                    SetWantedSpeed(40 * Direction)
                    Thread.Sleep(5000)
                Next

                PeakX = 0
                PeakY = 0
                PeakZ = 0
                m_UnabalanceAlarm = False

                'slowly accelerate to the wanted test speed, first set initial power
                SetWantedPower(28)
                Dim k As Integer = 0

                While Volatile.Read(m_LastRPM(0)) < CentrifugeUnbalanceTestSpeed * 0.9 AndAlso Volatile.Read(m_UnabalanceAlarm) = False
                    SetWantedPower(28 + k) 'increase motor power while no unbalance was detected
                    If Not Volatile.Read(m_UnabalanceAlarm) Then Thread.Sleep(100)
                    If Not Volatile.Read(m_UnabalanceAlarm) Then Thread.Sleep(100)
                    If Not Volatile.Read(m_UnabalanceAlarm) Then Thread.Sleep(50)

                    Dim Timeout As Integer = 100
                    While GetDrumAcceleration() > 3 AndAlso Timeout > 0 AndAlso Volatile.Read(m_UnabalanceAlarm) = False
                        Thread.Sleep(100)
                        If PrintDebugData AndAlso (Timeout Mod 10) = 0 Then RaiseEvent DebugPrint(Me, "Drum acceleration: " & GetDrumAcceleration() & " timeout=" & Timeout & ", power=" & (28 + k))
                        Timeout -= 1
                    End While

                    If Volatile.Read(m_LastRPM(0)) >= 100 Then 'add some extra delay when at 100 rpm to allow water to go out
                        For delay As Integer = 0 To 20
                            Thread.Sleep(100)
                            If Volatile.Read(m_UnabalanceAlarm) Then Exit For
                        Next
                    End If

                    'check if something wrong with motor
                    If (28 + k) > 50 AndAlso m_LastRPM(0) = 0 Then
                        StopMotor()
                        DisableMotorPower()
                        RaiseEvent MotorError(Me, ErrorFlags.ErrorNoMotorBlocked)
                        RaiseEvent DebugPrint(Me, "Error motor not running at 50% power!")
                        Return
                    End If

                    k = k + 1
                End While

                If Not Volatile.Read(m_UnabalanceAlarm) Then
                    Thread.Sleep(100)
                    SetWantedSpeed(CentrifugeUnbalanceTestSpeed * Direction)

                    'wait 10 sec
                    k = 0
                    While k < 100 AndAlso m_UnabalanceAlarm = False
                        k += 1
                        Thread.Sleep(100)
                    End While
                End If

                If m_UnabalanceAlarm Then 'reset speed if unbalanced
                    SetWantedSpeed(0)
                End If

                TryCentrifugeTimeout -= 1
                Volatile.Write(m_TryCentrifugeCount, TryCentrifugeTimeout)
            Loop While Volatile.Read(m_UnabalanceAlarm) AndAlso TryCentrifugeTimeout > 0 'check if unbalance alarm was triggered, try again until we decide to spawn error because of try timeout

            If TryCentrifugeTimeout = 0 Then
                If PrintDebugData Then RaiseEvent DebugPrint(Me, "Error detecting balance!")
                StopAcceleroMeter()
                RaiseEvent BalanceDetectionFailed(Me)
                Console.WriteLine("Centrifuge start failed!")
                Exit Sub
            Else
                RaiseEvent BalanceDetectionOK(Me)
                If PrintDebugData Then RaiseEvent DebugPrint(Me, "Balance detected OK " & Volatile.Read(m_SamplesReceived) & " acc. samples received " & PeakX & "," & PeakY & ", " & PeakZ & ".")
            End If

            StopAcceleroMeter() 'stop the accelerometer getting data we have balance

            Thread.Sleep(5000)

            'now we change from power control to speed control
            SetWantedSpeed(0)
            Thread.Sleep(100)
            SetBoost(True)
            Thread.Sleep(100)

            Dim CurrentWantedSpeed As Integer = CentrifugeUnbalanceTestSpeed

            'slow increase the speed to the wanted centrifuge speed
            While CurrentWantedSpeed < Params.Speed
                SetWantedSpeed(CurrentWantedSpeed * Direction)
                Dim TimeOut As Integer = 200
                While m_AvgRPM < CurrentWantedSpeed * 0.9 AndAlso TimeOut > 0
                    Thread.Sleep(100)
                    TimeOut -= 1
                End While
                Thread.Sleep(1000)
                CurrentWantedSpeed += Params.Speed / 20
            End While

            RaiseEvent WantedCentrifugeSpeedReached(Me, Params.Speed) 'wanted speed reached
            SetWantedSpeed(Params.Speed * Direction)

            While m_State <> MotorControlStates.Idle
                Thread.Sleep(1000)
            End While

        Catch tab As ThreadAbortException

        Catch e As Exception

        Finally
            SetWantedSpeed(0)
            SetBoost(False)
        End Try
    End Sub

    ''' <summary>
    ''' Sets motor relay
    ''' Returns true if power is going to motor (50Hz is detected)
    ''' Returns false if door is left open or motor is overheated
    ''' </summary>
    Public Function EnableMotorPower() As Boolean
        Dim i As Integer = 0
        m_PowerPin.Write(1)

        While (ErrorCode And ErrorFlags.ErrorNoMotorPower) > 0 AndAlso i < 10
            i += 1
            Thread.Sleep(1000)
        End While
        Return (ErrorCode And ErrorFlags.ErrorNoMotorPower) = 0
    End Function

    ''' <summary>
    ''' Turn off power to motor
    ''' </summary>
    Public Sub DisableMotorPower()
        m_PowerPin.Write(0)
        m_State = MotorControlStates.Idle
        If m_ControlThread IsNot Nothing Then
            If Not m_ControlThread.Join(2000) Then m_ControlThread.Abort()
        End If
    End Sub

    ''' <summary>
    ''' returns true if power to the motor is on
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property PowerEnabled() As Boolean
        Get
            If m_PowerPin.CurrentValue = PinValue.High Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    ''' <summary>
    ''' processes received data from the serial port
    ''' </summary>
    ''' <param name="Data"></param>
    Protected Sub ReceiveData(ByVal Data As String)
        Static PErrorCode As Integer
        Static PMinute As Integer

        If Data.Length > 2 AndAlso Data(0) = "{" AndAlso Data(Data.Length - 1) = "}" Then
            Dim NameValues() As String = Data.TrimStart("{").TrimEnd("}").Split(",")
            For Each NameValue As String In NameValues
                Dim NameAndValue() As String = NameValue.Split(":")
                Select Case NameAndValue(0)
                    Case "pwr"
                        CurrentOutputPower = NameAndValue(1)
                    Case "tacho"
                        TachoPeriod = NameAndValue(1)
                    Case "rpm"
                        CurrentRPM = NameAndValue(1)
                        AddRPM(CurrentRPM)
                    Case "temp"
                        CurrentTemp = NameAndValue(1)
                    Case "spd"
                        WantedSpeed = NameAndValue(1)
                    Case "water"
                        WaterLevel = NameAndValue(1)
                    Case "err"
                        ErrorCode = NameAndValue(1)
                End Select
            Next
            If PrintDebugData AndAlso ErrorCode <> 0 Then
                If ErrorCode <> PErrorCode OrElse Now.Minute <> PMinute Then
                    RaiseEvent DebugPrint(Me, "Rx: " & TimeString() & "->" & Data)
                    PErrorCode = ErrorCode
                    PMinute = Now.Minute
                End If
            End If
            RaiseEvent SampleArrival(Me, Data)
        End If
    End Sub

    ''' <summary>
    ''' Calculates average rpm
    ''' </summary>
    ''' <param name="RPM"></param>
    Protected Sub AddRPM(ByVal RPM As Integer)
        For i As Integer = 0 To m_LastRPM.Length - 2
            m_LastRPM(i + 1) = m_LastRPM(i)
        Next

        If m_AvgRPM < 200 AndAlso m_AvgRPM > 0 Then
            If RPM > 200 AndAlso Math.Abs(m_AvgRPM - RPM) > 200 Then
                RPM = m_AvgRPM + 10
            End If
        End If

        Dim Tmp_Acceleration As Integer = RPM - m_LastRPM(0)

        m_LastRPM(0) = (RPM + m_LastRPM(1) + m_LastRPM(3)) / 3

        Dim Avg As Integer = 0
        For i As Integer = 0 To m_LastRPM.Length - 1
            Avg += m_LastRPM(i)
        Next

        SyncLock m_RPMLock
            Thread.VolatileWrite(m_AvgRPM, Avg / m_LastRPM.Length)
            Thread.VolatileWrite(Acceleration, Tmp_Acceleration)
        End SyncLock
    End Sub

    ''' <summary>
    ''' Returns RPM with less averaging 
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GetRPM() As Integer
        Get
            SyncLock m_RPMLock
                Return Thread.VolatileRead(m_AvgRPM)
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Returns averaged RPM
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GetAvgRPM() As Integer
        Get
            SyncLock m_RPMLock
                Return Thread.VolatileRead(m_AvgRPM)
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Returns drump speed acceleration, if 0 the drump is not increasing/decreasing speed
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GetDrumAcceleration() As Integer
        Get
            SyncLock m_RPMLock
                Return Thread.VolatileRead(Acceleration)
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Turns on high speed coil on/off by sending command
    ''' </summary>
    ''' <param name="Value"></param>
    Protected Sub SetBoost(ByVal Value As Boolean)
        SendData("B=" & IIf(Value, "1", "0") & m_Comport.NewLine)
    End Sub

    ''' <summary>
    ''' Sends a command to the speed controller with wanted speed
    ''' </summary>
    ''' <param name="SpeedRPM">Wanted speed in RPM</param>
    Public Sub SetWantedSpeed(ByVal SpeedRPM As String)
        SendData("S=" & SpeedRPM & m_Comport.NewLine)
    End Sub

    ''' <summary>
    ''' Sends a command with wanted output power
    ''' Note: there is a limitation in max increment from previous command to prevent motor damage by software bug
    ''' </summary>
    ''' <param name="Power"></param>
    Public Sub SetWantedPower(ByVal Power As String)
        SendData("P=" & Power & m_Comport.NewLine)
    End Sub

    ''' <summary>
    ''' Sends data to the speed controller
    ''' </summary>
    ''' <param name="Data"></param>
    Public Sub SendData(ByVal Data As String)
        m_Comport.Write(Data)
        If PrintDebugData Then RaiseEvent DebugPrint(Me, "TX: " & Data)
    End Sub

    ''' <summary>
    ''' Raised when data from commport is received
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub m_Comport_DataReceived(sender As Object, e As SerialDataReceivedEventArgs) Handles m_Comport.DataReceived
        Select Case e.EventType
            Case SerialData.Chars
                ReceiveData(m_Comport.ReadExisting)
        End Select
    End Sub

    ''' <summary>
    ''' Mono needs a thread to monitor serial port
    ''' </summary>
    Private Sub MonoSerialPortMonitor()
        Try
            Dim Line As String = ""
            While m_Comport.IsOpen()
                ReceiveData(m_Comport.ReadLine)
            End While
        Catch e As ThreadAbortException
        End Try
    End Sub

    Private Sub m_Comport_ErrorReceived(sender As Object, e As SerialErrorReceivedEventArgs) Handles m_Comport.ErrorReceived
        If PrintDebugData Then RaiseEvent DebugPrint(Me, "Comport error: " & e.EventType.ToString())
    End Sub

    ''' <summary>
    ''' Starts a thread that reads samples from the accelerometer
    ''' </summary>
    Public Sub StartAcceleroMeter()
        StopAcceleroMeter()
        Machine.MMI.SuspendDisplay()
        m_SamplesReceived = 0
        PeakX = 0
        PeakY = 0
        PeakZ = 0
        Volatile.Write(m_AcceleroMeterEnabled, True)
        Volatile.Write(m_AcceleroMeterWorking, False)
        If AcceleroMeter.SetFifoMode(ADXL345.FIFOModes.FIFO_STREAM) Then
            Volatile.Write(m_AcceleroMeterWorking, True)
            If PrintDebugData Then RaiseEvent DebugPrint(Me, "Accelerometer initialized OK")
        End If

        m_SamplesThread = New Thread(AddressOf ProcessAcceleroMeterSamples)
        m_SamplesThread.IsBackground = True
        m_SamplesThread.Name = "MotorControl::Accelerometer Sampling thread"
        m_SamplesThread.Priority = ThreadPriority.AboveNormal
        m_SamplesThread.Start()

    End Sub

    ''' <summary>
    ''' Stops thread that reads samples from accelerometer
    ''' </summary>
    Public Sub StopAcceleroMeter()
        Machine.MMI.ResumeDisplay()
        Volatile.Write(m_AcceleroMeterEnabled, False)
        If m_SamplesThread IsNot Nothing Then
            If Not m_SamplesThread.Join(100) Then m_SamplesThread.Abort()
        End If
        m_SamplesThread = Nothing
    End Sub

    ''' <summary>
    ''' a thread that reads samples from the accelerometer 
    ''' </summary>
    Private Sub ProcessAcceleroMeterSamples()
        Try
            Dim Sample As New ADXL345.GSample
            While Volatile.Read(m_AcceleroMeterEnabled)
                Dim SamplesAvailable As Integer = AcceleroMeter.GetFifoSamplesAvailable()
                Dim idx As Integer = 0
                While SamplesAvailable > 0 AndAlso SamplesAvailable < 33 AndAlso m_AcceleroMeterEnabled AndAlso idx < 100 'must handle samples very fast or buffer will overflow at 400 samples/sec!
                    If SamplesAvailable >= 32 Then Console.WriteLine("OV") 'overflow detected, this is not good!
                    Volatile.Write(m_SamplesReceived, Volatile.Read(m_SamplesReceived) + SamplesAvailable)
                    For i As Integer = 0 To SamplesAvailable - 1
                        If AcceleroMeter.GetSample(Sample) Then
                            ProcessAcceleroMeterSample(Sample)
                        End If
                    Next
                    SamplesAvailable = AcceleroMeter.GetFifoSamplesAvailable()
                    idx += 1
                End While
                Thread.Sleep(1)
            End While
        Catch ex As ThreadAbortException

        End Try
    End Sub

    Private Sub ProcessAcceleroMeterSample(Sample As ADXL345.GSample)
        Dim HighPassValueX As Single, HighPassValueY As Single, HighPassValueZ As Single
        Static PHighPassValueX As Single, PHighPassValueY As Single, PHighPassValueZ As Single
        Static PSample As ADXL345.GSample
        Static AlarmTime As DateTime = DateTime.MinValue
        Static UnbalancedAxis As Integer

        If Math.Abs(Sample.X) > PeakX Then
            PeakX = Math.Abs(Sample.X)
        End If

        If Math.Abs(Sample.Y) > PeakY Then
            PeakY = Math.Abs(Sample.Y)
        End If

        If Math.Abs(Sample.Z) > PeakZ Then
            PeakZ = Math.Abs(Sample.Z)
        End If

        HighPassValueX = FilterCoef * (PHighPassValueX + Sample.X - PSample.X)
        HighPassValueY = FilterCoef * (PHighPassValueY + Sample.Y - PSample.Y)
        HighPassValueZ = FilterCoef * (PHighPassValueZ + Sample.Z - PSample.Z)

        If Math.Abs(HighPassValueX) > ThresholdX OrElse PeakX > PeakThresholdX Then
            UnbalancedAxis = UnbalancedAxis Or 1
            ThresholdCountX += 1
            AlarmTime = Now()
        End If

        If Math.Abs(HighPassValueY) > ThresholdY OrElse PeakY > PeakThresholdY Then
            UnbalancedAxis = UnbalancedAxis Or 2
            ThresholdCountY += 1
            AlarmTime = Now()
        End If

        If Math.Abs(PHighPassValueZ) > ThresholdZ OrElse PeakZ > PeakThresholdZ Then
            UnbalancedAxis = UnbalancedAxis Or 4
            ThresholdCountZ += 1
            AlarmTime = Now()
        End If

        If ThresholdCountX >= 4 OrElse ThresholdCountY >= 4 OrElse ThresholdCountZ >= 4 Then 'must count at least 4 in 500ms before generating alarm
            ThresholdCountX = 4
            ThresholdCountY = 4
            ThresholdCountZ = 4
            Volatile.Write(m_UnabalanceAlarm, True) 'will be cleared in centrifuge thread
            RaiseEvent UnbalanceAlarm(Me, UnbalancedAxis, HighPassValueX, HighPassValueY, HighPassValueZ, Volatile.Read(m_TryCentrifugeCount))
        End If

        If AlarmTime <> DateTime.MinValue AndAlso (Now() - AlarmTime).TotalMilliseconds > 500 Then 'if last alarm > 500ms clear thresholds reset alarms
            UnbalancedAxis = 0
            ThresholdCountX = 0
            ThresholdCountY = 0
            ThresholdCountZ = 0
            AlarmTime = DateTime.MinValue
            'RaiseEvent UnbalanceAlarm(Me, 0, HighPassValueX, HighPassValueY, PHighPassValueZ, m_TryCentrifugeCount)
        End If

        'check if debugging is enabled and save the sample in a FIFO
        If AcceleroMeterDebugging Then
            Dim DataGram(0 To 39) As Byte
            Static SampleCounter As UInt32
            BitConverter.GetBytes(SampleCounter).CopyTo(DataGram, 0)
            BitConverter.GetBytes(Sample.X).CopyTo(DataGram, 4)
            BitConverter.GetBytes(Sample.Y).CopyTo(DataGram, 8)
            BitConverter.GetBytes(Sample.Z).CopyTo(DataGram, 12)
            BitConverter.GetBytes(HighPassValueX).CopyTo(DataGram, 16)
            BitConverter.GetBytes(HighPassValueY).CopyTo(DataGram, 20)
            BitConverter.GetBytes(HighPassValueZ).CopyTo(DataGram, 24)
            BitConverter.GetBytes(ThresholdCountX).CopyTo(DataGram, 28)
            BitConverter.GetBytes(ThresholdCountY).CopyTo(DataGram, 32)
            BitConverter.GetBytes(ThresholdCountZ).CopyTo(DataGram, 36)
            SyncLock m_AcceleroMeterDatagramBuffer
                m_AcceleroMeterDatagramBuffer.Enqueue(DataGram.Clone())
            End SyncLock
            If SampleCounter = UInt32.MaxValue Then SampleCounter = 0
            SampleCounter += 1
        End If

        PHighPassValueX = HighPassValueX
        PHighPassValueY = HighPassValueY
        PHighPassValueZ = HighPassValueZ

    End Sub

    ''' <summary>
    ''' Turn on debugging for accelerometer
    ''' </summary>
    ''' <param name="IP"></param>
    Public Sub EnableAcceleroMeterDebugging(ByVal IP As IPAddress)
        AcceleroMeterDebugIP = New IPEndPoint(IP, AcceleroMeterDebugPort)
        AcceleroMeterDebugging = True
        m_AcceleroMeterDebuggingThread = New Thread(AddressOf SendAcceleroMeterBuffer)
        m_AcceleroMeterDebuggingThread.IsBackground = True
        m_AcceleroMeterDebuggingThread.Start()
    End Sub

    ''' <summary>
    ''' Turns off debugging for accelerometer
    ''' </summary>
    Public Sub DisableAcceleroMeterDebugging()
        If m_AcceleroMeterDebuggingThread IsNot Nothing Then
            AcceleroMeterDebugging = False
            m_AcceleroMeterDebuggingThread.Abort()
            m_AcceleroMeterDebuggingThread = Nothing
        End If
    End Sub

    ''' <summary>
    ''' This is called in a sperate thread to send the accelerometer samples over UDP packet
    ''' </summary>
    Public Sub SendAcceleroMeterBuffer()
        Try
            While True
                Try
                    While m_AcceleroMeterDatagramBuffer.Count > 0
                        Dim DataGram() As Byte
                        SyncLock m_AcceleroMeterDatagramBuffer
                            DataGram = m_AcceleroMeterDatagramBuffer.Dequeue()
                        End SyncLock
                        If DataGram IsNot Nothing Then
                            m_DebugSocket.Send(DataGram, DataGram.Length, AcceleroMeterDebugIP) 'this is slow and must be done here or the accelerometer buffer will overflow
                        End If
                    End While
                Catch e As SocketException

                End Try

                Thread.Sleep(20)

            End While

        Catch e As ThreadAbortException

        End Try
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        If m_Comport IsNot Nothing Then m_Comport.Close()
        StopAcceleroMeter()
        DisableAcceleroMeterDebugging()

    End Sub
End Class
