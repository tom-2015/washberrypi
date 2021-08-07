Imports System.Threading
Imports System.Xml
Imports Washmachine

''' <summary>
''' Basic programblocks are defined here, each block will execute a basic function in the washingmachine
''' for example AddWater block will add water to the machine and maintain it filled to a certain level
''' ProgramBlock is a general class describing a few methods and functions each block should implement like timeleft, totaltime,...
''' </summary>
Public MustInherit Class ProgramBlock

    Protected m_Program As Program
    Protected m_Finished As Boolean
    Protected m_ErrorCode As Program.ProgramErrors
    Protected m_Type As ProgramBlockTypes

    Public Enum ProgramBlockTypes As Integer
        AddWater = 1
        CreateFoam = 2
        Wash = 3
        PumpOutWater = 4
        Centrifuge = 5
        CloseDoor = 6
        OpenDoor = 7
        WaitUserInput = 8
    End Enum

    Public Sub New(ByVal Prog As Program)
        m_Program = Prog
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        m_Program = Prog
        m_Type = Node.Attributes("type").Value
        If Node.Attributes("finished") IsNot Nothing Then m_Finished = Node.Attributes("finished").Value
    End Sub

    Public Overridable ReadOnly Property ErrorCode As Integer
        Get
            Return m_ErrorCode
        End Get
    End Property

    ''' <summary>
    ''' Execute the piece of program
    ''' Returns true if finished successfully
    ''' </summary>
    ''' <returns></returns>
    Public MustOverride Function Execute() As Boolean

    ''' <summary>
    ''' Returns identifier name
    ''' </summary>
    ''' <returns></returns>
    Public MustOverride ReadOnly Property Name() As String

    ''' <summary>
    ''' Returns time left to execute finished
    ''' </summary>
    ''' <returns>seconds</returns>
    Public MustOverride Function GetTimeLeft() As Integer

    ''' <summary>
    ''' Returns total time for execution
    ''' </summary>
    ''' <returns>seconds</returns>
    Public MustOverride Function TotalTime() As Integer

    ''' <summary>
    ''' Returns bloc, as JSON if closeobject is true an "}" will be added at the end
    ''' </summary>
    ''' <param name="CloseObject"></param>
    ''' <returns></returns>
    Public Overridable Function ToJSON(Optional ByVal CloseObject As Boolean = True) As String
        Return "{ ""type"": " & m_Type & ", ""name"": """ & Name() & """, ""time_left"": " & GetTimeLeft() & ", ""total_time"": " & TotalTime() & IIf(CloseObject, "}", "")
    End Function

    ''' <summary>
    ''' Returns the executing machine
    ''' </summary>
    ''' <returns></returns>
    Protected ReadOnly Property m_Machine() As WashingMachine
        Get
            Return m_Program.GetMachine()
        End Get
    End Property

    ''' <summary>
    ''' Saves the block to xml doc
    ''' </summary>
    ''' <param name="Doc"></param>
    ''' <param name="SaveState">if true also parameters about current execution state will be saved</param>
    ''' <returns></returns>
    Public Overridable Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = Doc.CreateElement("block")
        If SaveState Then
            BlockNode.Attributes.Append(Doc.CreateAttribute("finished")).Value = m_Finished.ToString()
        End If
        BlockNode.Attributes.Append(Doc.CreateAttribute("type")).Value = m_Type
        BlockNode.Attributes.Append(Doc.CreateAttribute("name")).Value = Name()
        Return BlockNode
    End Function

    ''' <summary>
    ''' Returns true if this block finished executing
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Finished() As Boolean
        Get
            Return m_Finished
        End Get
    End Property

    ''' <summary>
    ''' Returns summary string
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function ToString() As String
        Dim TimeLeft As Integer = GetTimeLeft()
        If TimeLeft < DateTime.MinValue.Ticks OrElse TimeLeft > DateTime.MaxValue.Ticks Then
            Return Name & ", " & TimeLeft
        End If
        Return Name & ", " & Format(New DateTime(TimeSpan.FromSeconds(TimeLeft).Ticks), "HH:mm:ss")
    End Function

End Class

''' <summary>
''' Adds water to the machine and waits for certain level to be reached within a timeout
''' </summary>
Public Class AddWater
    Inherits ProgramBlock

    Protected m_WantedWaterLevel As Integer
    Protected m_WaitForLevel As Integer
    Protected m_Source As WaterControl.WaterSource
    Protected m_TimeOut As Integer
    Protected m_Start As Date = Date.MinValue

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Prog"></param>
    ''' <param name="Source">Defines which soap must be added, which valve to open</param>
    ''' <param name="WantedWaterLevel">Defines the water level to maintain until water is pumped out (0-100%)</param>
    ''' <param name="WaitForLevel">Defines to wait for at least this level to be reached before executed successfully (0-100%)</param>
    ''' <param name="TimeOutSec">Wait for this timeout in Seconds to reach waitforlevel</param>
    Public Sub New(ByVal Prog As Program, ByVal Source As WaterControl.WaterSource, ByVal WantedWaterLevel As Integer, Optional ByVal WaitForLevel As Integer = 0, Optional ByVal TimeOutSec As Integer = 60)
        MyBase.New(Prog)
        m_Source = Source
        m_WantedWaterLevel = WantedWaterLevel
        m_WaitForLevel = WaitForLevel
        m_TimeOut = TimeOutSec
        m_Type = ProgramBlockTypes.AddWater
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
        m_WantedWaterLevel = Node.Attributes("wanted_level").Value
        m_WaitForLevel = Node.Attributes("wait_for_level").Value
        m_Source = Node.Attributes("source").Value
        If Node.Attributes("timeout") IsNot Nothing Then m_TimeOut = Node.Attributes("timeout").Value
    End Sub


    Public Overrides Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = MyBase.SaveBlock(Doc, SaveState)
        BlockNode.Attributes.Append(Doc.CreateAttribute("wanted_level")).Value = m_WantedWaterLevel
        BlockNode.Attributes.Append(Doc.CreateAttribute("wait_for_level")).Value = m_WaitForLevel
        BlockNode.Attributes.Append(Doc.CreateAttribute("source")).Value = m_Source
        BlockNode.Attributes.Append(Doc.CreateAttribute("timeout")).Value = m_TimeOut
        Return BlockNode
    End Function

    Public Overrides Function Execute() As Boolean
        m_Start = Now()
        m_Machine.AddWater(m_WantedWaterLevel, m_Source)

        While m_WantedWaterLevel > 0 AndAlso m_Machine.Water.GetWaterLevel() < m_WaitForLevel
            Thread.Sleep(1000)
            If m_Machine.Motor.ErrorCode And MotorControl.ErrorFlags.ErrorWaterSensor Then
                m_Finished = True
                m_ErrorCode = Program.ProgramErrors.ErrorWaterSensor
                Return False
            End If
            If m_TimeOut > 0 AndAlso (Now() - m_Start).TotalSeconds > m_TimeOut Then
                m_Finished = True
                m_ErrorCode = Program.ProgramErrors.ErrorNoWater
                Return False
            End If
        End While

        m_Finished = True
        Return True
    End Function

    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return TotalTime()
        Return Math.Max(m_TimeOut - (Now() - m_Start).TotalSeconds, 0)
    End Function

    Public Overrides Function TotalTime() As Integer
        Return m_TimeOut
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Add water"
        End Get
    End Property

End Class

''' <summary>
''' Starts a pump to circulate the water and soap, creating some foam
''' don't know what the purpose is though...
''' </summary>
Public Class CreateFoam
    Inherits ProgramBlock

    Protected m_Start As Date = Date.MinValue
    Protected m_Time As Integer

    Public Sub New(ByVal Prog As Program, ByVal Time As Integer)
        MyBase.New(Prog)
        m_Time = Time
        m_Type = ProgramBlockTypes.CreateFoam
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
        m_Time = Node.Attributes("time").Value
    End Sub

    Public Overrides Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = MyBase.SaveBlock(Doc, SaveState)
        BlockNode.Attributes.Append(Doc.CreateAttribute("time")).Value = m_Time
        Return BlockNode
    End Function

    Public Overrides Function Execute() As Boolean
        If m_Machine.Water.GetWaterLevel() > 20 Then
            m_Start = Now()
            m_Machine.StartCirculationPump()
            Thread.Sleep(m_Time * 1000)
            m_Machine.StopCirculationPump()
            m_Finished = True
            Return True
        Else
            m_ErrorCode = Program.ProgramErrors.ErrorNoWater
        End If
        m_Finished = True
        Return False
    End Function

    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return TotalTime()
        Return m_Time - (Now() - m_Start).TotalSeconds
    End Function

    Public Overrides Function TotalTime() As Integer
        Return m_Time
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Create foam"
        End Get
    End Property

End Class

''' <summary>
''' This block will start washing
''' </summary>
Public Class Wash
    Inherits ProgramBlock

    Protected m_Start As Date = Date.MinValue
    Protected m_WantedTemp As Integer
    Protected m_TimeAtTemp As Integer
    Protected m_MaxTime As Integer
    Protected m_StartAtTemp As Date = Date.MinValue
    Protected m_startTemp As Integer
    Protected m_TimeToReachTemp As Integer = 0
    Protected m_SavedTimeWashing As Integer

    Protected Const AVERAGE_HEATING_TIME As Integer = 50 'take average 50 sec for 1 degree temp rise
    Protected Const AVERAGE_WATER_TEMP As Integer = 15   'take average input water temp

    ''' <summary>
    ''' Starts a wash for TimeAtTemp and WantedTemperature
    ''' </summary>
    ''' <param name="Prog"></param>
    ''' <param name="WantedTemperature">if -1 use the general temperature selected by the user in Program.temp. If 0 do not use any heating</param>
    ''' <param name="TimeAtTemp">Time in seconds to keep washing when the WantedTemperature was reached</param>
    ''' <param name="MaxTime">Max time to wait to reach WantedTemperature or fail</param>
    Public Sub New(ByVal Prog As Program, ByVal WantedTemperature As Integer, ByVal TimeAtTemp As Integer, ByVal MaxTime As Integer)
        MyBase.New(Prog)
        m_WantedTemp = WantedTemperature
        m_TimeAtTemp = TimeAtTemp
        m_MaxTime = MaxTime
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
        m_WantedTemp = Node.Attributes("wanted_temp").Value
        m_TimeAtTemp = Node.Attributes("time_at_temp").Value
        m_MaxTime = Node.Attributes("max_time").Value
        If Node.Attributes("time_washing") IsNot Nothing Then
            m_SavedTimeWashing = Node.Attributes("time_washing").Value
        End If
    End Sub

    Public Overrides Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = MyBase.SaveBlock(Doc, SaveState)
        BlockNode.Attributes.Append(Doc.CreateAttribute("wanted_temp")).Value = m_WantedTemp
        BlockNode.Attributes.Append(Doc.CreateAttribute("time_at_temp")).Value = m_TimeAtTemp
        BlockNode.Attributes.Append(Doc.CreateAttribute("max_time")).Value = m_MaxTime
        If SaveState AndAlso Not m_Finished Then
            If m_StartAtTemp <> Date.MinValue Then
                BlockNode.Attributes.Append(Doc.CreateAttribute("time_washing")).Value = (Now - m_StartAtTemp).TotalSeconds
            End If
        End If
        Return BlockNode
    End Function

    Public Overrides Function Execute() As Boolean
        Dim Res As Boolean = True

        m_Start = Now()
        m_TimeToReachTemp = 0

        If m_Machine.Water.GetWaterLevel() < 30 Then
            m_ErrorCode = Program.ProgramErrors.ErrorNoWater
            Return False
        End If

        m_startTemp = m_Machine.Heater.GetTemp()

        If Not m_Machine.StartWashing(IIf(m_WantedTemp > 60, 10000, 5000)) Then
            m_Finished = True
            m_ErrorCode = Program.ProgramErrors.ErrorNoMotorPower
            Return False
        End If

        If m_WantedTemp > 0 Then
            m_Machine.StartHeating(m_WantedTemp)
        Else
            m_Machine.StopHeating()
        End If

        'wait for heating
        Dim SecondsAtTemp As Integer = 0
        While SecondsAtTemp < 5 'must be at temp for at least 5 samples 

            Thread.Sleep(1000)

            If (Now - m_Start).TotalSeconds > 180 Then
                If (m_Machine.Heater.GetTemp() - m_startTemp) > 0 Then
                    m_TimeToReachTemp = (Now - m_Start).TotalSeconds / (m_Machine.Heater.GetTemp() - m_startTemp) * (m_WantedTemp - m_startTemp) - (Now - m_Start).TotalSeconds
                    Console.WriteLine("Time for temp: " & m_TimeToReachTemp & "," & ((Now - m_Start).TotalSeconds / (m_Machine.Heater.GetTemp() - m_startTemp)))
                End If
            Else
                m_TimeToReachTemp = (m_WantedTemp - m_startTemp) * AVERAGE_HEATING_TIME - (Now - m_Start).TotalSeconds 'take average of 50 sec for 1 degree
            End If

            If m_TimeToReachTemp > 9000 Then
                m_Machine.StopHeating()
                m_Machine.Program.GenerateError(Program.ProgramErrors.ErrorTempSensor, "Error with temperature sensor, takes too long to heat > 9000s.")
                m_Machine.StopWashing()
                Return False
            End If

            If (Now() - m_Start).TotalSeconds > m_MaxTime AndAlso m_MaxTime > 0 Then
                Res = False
                Exit While
            End If

            If m_Machine.Heater.GetTemp() >= (m_WantedTemp - m_WantedTemp * 0.04) Then
                SecondsAtTemp += 1
            Else
                SecondsAtTemp = 0
            End If
        End While

        m_Machine.StartWashing(2500)

        Console.WriteLine("Temp " & m_WantedTemp & " reached " & m_Machine.Heater.GetTemp())


        If m_SavedTimeWashing > 0 Then 'resume from saved state
            m_StartAtTemp = DateAdd(DateInterval.Second, m_SavedTimeWashing * -1, Now())
        Else
            m_StartAtTemp = Now()
        End If

        'now wash at temperature
        While (Now() - m_StartAtTemp).TotalSeconds < m_TimeAtTemp
            Thread.Sleep(1000)
            If (Now() - m_Start).TotalSeconds > m_MaxTime AndAlso m_MaxTime > 0 Then
                m_ErrorCode = Program.ProgramErrors.ErrorTimeOut
                Res = False
                Exit While
            End If
        End While

        m_Machine.StopHeating()
        Thread.Sleep(1000)
        m_Machine.StopWashing()
        Thread.Sleep(1000)
        m_Finished = True
        Return Res
    End Function

    ''' <summary>
    ''' Returns estimated time left based on heating speed of the water
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return TotalTime()
        If m_StartAtTemp <> Date.MinValue Then
            Return Math.Max(Math.Min(m_TimeAtTemp - (Now() - m_StartAtTemp).TotalSeconds, m_MaxTime - (Now() - m_Start).TotalSeconds), 0)
        Else
            If m_WantedTemp > 0 Then
                If m_TimeToReachTemp <= 0 Then
                    m_TimeToReachTemp = (m_WantedTemp - AVERAGE_WATER_TEMP) * AVERAGE_HEATING_TIME - (Now - m_Start).TotalSeconds 'take average heating temperature
                End If
                If m_TimeToReachTemp > 0 Then
                    Return Math.Max(Math.Min(m_TimeToReachTemp + m_TimeAtTemp - IIf(m_StartAtTemp <> Date.MinValue, (Now - m_StartAtTemp).TotalSeconds, 0), m_MaxTime - (Now() - m_Start).TotalSeconds), 0)
                Else
                    Return Math.Max(Math.Min(m_MaxTime - (Now() - m_Start).TotalSeconds, m_TimeAtTemp - (Now() - m_Start).TotalSeconds), 0)
                End If
            Else
                Return Math.Max(m_TimeAtTemp - (Now() - m_Start).TotalSeconds, 0)
            End If
        End If
    End Function

    Public Overrides Function TotalTime() As Integer
        If m_WantedTemp > AVERAGE_WATER_TEMP Then
            Return Math.Min(m_MaxTime, (m_WantedTemp - AVERAGE_WATER_TEMP) * AVERAGE_HEATING_TIME) 'take average heating temperature
        Else
            Return Math.Min(m_TimeAtTemp, m_MaxTime)
        End If
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Washing"
        End Get
    End Property

End Class

''' <summary>
''' This pumps out dirty water and disables heating/water control
''' </summary>
Public Class PumpOutWater
    Inherits ProgramBlock

    Protected m_Start As Date = Date.MinValue
    Protected m_MaxTime As Integer

    Public Sub New(ByVal Prog As Program, Optional ByVal MaxTime As Integer = 80)
        MyBase.New(Prog)
        m_MaxTime = MaxTime
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
        m_MaxTime = Node.Attributes("max_time").Value
    End Sub


    Public Overrides Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = MyBase.SaveBlock(Doc, SaveState)
        BlockNode.Attributes.Append(Doc.CreateAttribute("m_maxtime")).Value = m_MaxTime
        Return BlockNode
    End Function

    Public Overrides Function Execute() As Boolean
        m_Start = Now()
        m_Finished = False

        m_Machine.PumpOutWater(10, m_MaxTime * 1000)
        Thread.Sleep(100)

        'add extra pump time
        m_Machine.PumpOut.Write(1)

        Thread.Sleep(10000)

        m_Machine.PumpOut.Write(0)

        m_Finished = True
        If (m_Machine.Water.GetWaterLevel() < 20) Then
            m_ErrorCode = Program.ProgramErrors.ErrorNone
            Return True
        Else
            m_ErrorCode = Program.ProgramErrors.ErrorClearWaterFailed
            Return False
        End If

    End Function

    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return m_MaxTime
        Return Math.Max(TotalTime() - (Now() - m_Start).TotalSeconds, 0)
    End Function

    Public Overrides Function TotalTime() As Integer
        Return m_MaxTime + 10
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Pumping water"
        End Get
    End Property

End Class

''' <summary>
''' This block handles centrifuge
''' </summary>
Public Class Centrifuge
    Inherits ProgramBlock

    Protected m_Start As Date = Date.MinValue
    Protected m_Time As Integer
    Protected m_RPM As Integer
    Protected WithEvents m_Motor As MotorControl
    Protected m_BalanceDetected As Boolean
    Protected m_BalanceFailed As Boolean
    Protected m_Stopping As Boolean
    Protected m_MotorError As Boolean

    ''' <summary>
    ''' Creates a new centrifuge block
    ''' </summary>
    ''' <param name="Prog"></param>
    ''' <param name="RPM">wanted RPM speed</param>
    ''' <param name="Time">time centrifuge at RPM</param>
    Public Sub New(ByVal Prog As Program, ByVal RPM As Integer, ByVal Time As Integer)
        MyBase.New(Prog)
        m_Time = Time
        m_RPM = RPM
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
        m_Time = Node.Attributes("time").Value
    End Sub

    Public Overrides Function SaveBlock(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim BlockNode As XmlNode = MyBase.SaveBlock(Doc, SaveState)
        BlockNode.Attributes.Append(Doc.CreateAttribute("time")).Value = m_Time
        Return BlockNode
    End Function


    Public Overrides Function Execute() As Boolean
        m_Finished = False
        m_Stopping = False
        m_Start = Now()
        m_Motor = m_Machine.Motor

        If m_RPM < 100 Then
            m_Finished = True
            Return True
        End If

        Volatile.Write(m_BalanceDetected, False)
        Volatile.Write(m_BalanceFailed, False)

        If Not m_Machine.StartCentrifuge(m_RPM) Then
            m_ErrorCode = Program.ProgramErrors.ErrorNoMotorPower
            m_Finished = True
            Return False
        End If

        While Volatile.Read(m_BalanceDetected) = False AndAlso Volatile.Read(m_BalanceFailed) = False AndAlso Volatile.Read(m_MotorError) = False
            Thread.Sleep(1000)
        End While

        If Volatile.Read(m_BalanceFailed) Then
            m_Machine.StopCentrifuge()
            m_ErrorCode = Program.ProgramErrors.ErrorBalanceFailed
            Return False
        End If

        If Volatile.Read(m_MotorError) Then
            m_Machine.StopCentrifuge()
            m_ErrorCode = Program.ProgramErrors.ErrorMotorBlocked
            Return False
        End If

        m_Start = Now()
        While (Now - m_Start).TotalSeconds < m_Time
            Thread.Sleep(1000)
        End While

        m_Machine.StopCentrifuge()
        m_Stopping = True

        Dim i As Integer = 0
        While m_Machine.Motor.GetAvgRPM() > 100 AndAlso i < 120
            Thread.Sleep(1000)
            i += 1
        End While

        Thread.Sleep(500)
        m_Motor.StartWashing()
        Thread.Sleep(30)
        m_Motor.StopMotor()

        m_Finished = True
        If (i > 120) Then Return False

        Return True
    End Function

    ''' <summary>
    ''' Time depends on how fast the balance was found/detected
    ''' it may not be exact time left
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return TotalTime()
        If Volatile.Read(m_BalanceDetected) Then
            If m_Stopping Then Return 60 + m_Time - (Now - m_Start).TotalSeconds 'take at least 60 seconds to find the balance
            Return m_Time - (Now - m_Start).TotalSeconds
        Else
            Return TotalTime() ' Math.Max(TotalTime(), (Now - m_Start).TotalSeconds) + 120 + 60
        End If

    End Function

    Public Overrides Function TotalTime() As Integer
        If m_RPM <= 100 Then Return 0
        Return m_Time + 120 + 60 + 60
    End Function

    Private Sub m_Motor_BalanceDetectionFailed(Control As MotorControl) Handles m_Motor.BalanceDetectionFailed
        Volatile.Write(m_BalanceFailed, True)
    End Sub

    Private Sub m_Motor_BalanceDetectionOK(Control As MotorControl) Handles m_Motor.BalanceDetectionOK
        Volatile.Write(m_BalanceDetected, True)
    End Sub

    Private Sub m_Motor_MotorError(Control As MotorControl, Err As MotorControl.ErrorFlags) Handles m_Motor.MotorError
        Volatile.Write(m_MotorError, True)
    End Sub

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Centrifuge"
        End Get
    End Property

End Class

''' <summary>
''' Closes the door lock
''' </summary>
Public Class CloseDoorLock
    Inherits ProgramBlock

    Public Sub New(ByVal Prog As Program)
        MyBase.New(Prog)
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
    End Sub

    Public Overrides Function Execute() As Boolean
        m_Machine.CloseDoorLock()
        Thread.Sleep(1000)
        m_Finished = True
        Return True
    End Function

    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        Return 1
    End Function

    Public Overrides Function TotalTime() As Integer
        Return 1
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Close doorlock"
        End Get
    End Property

End Class

''' <summary>
''' Opens door lock relay
''' </summary>
Public Class OpenDoorLock
    Inherits ProgramBlock

    Protected m_Start As Date = Date.MinValue
    Const DOOR_UNLOCK_TIME As Integer = 60

    Public Sub New(ByVal Prog As Program)
        MyBase.New(Prog)
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)

    End Sub

    ''' <summary>
    ''' Waits for door lock to open and enables the beep
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function Execute() As Boolean
        m_Start = Now()
        m_Machine.OpenDoorlock()
        Thread.Sleep(DOOR_UNLOCK_TIME * 1000)
        m_Machine.MMI.EnableBuzzer()
        Thread.Sleep(1000)
        m_Machine.MMI.DisableBuzzer()
        Thread.Sleep(1000)
        m_Machine.MMI.EnableBuzzer()
        Thread.Sleep(1000)
        m_Machine.MMI.DisableBuzzer()
        m_Finished = True
        Return True
    End Function

    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        If m_Start = Date.MinValue Then Return TotalTime()
        Return Math.Max(DOOR_UNLOCK_TIME - (Now() - m_Start).TotalSeconds, 0)
    End Function

    Public Overrides Function TotalTime() As Integer
        Return DOOR_UNLOCK_TIME
    End Function

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Open Door lock"
        End Get
    End Property


End Class

''' <summary>
''' This block is added when the user wants the machine to wait for the final centrifuge until play button was pressed
''' </summary>
Public Class WaitForInput
    Inherits ProgramBlock

    Public Sub New(ByVal Prog As Program)
        MyBase.New(Prog)
    End Sub

    Public Sub New(ByVal Prog As Program, ByVal Node As XmlNode)
        MyBase.New(Prog, Node)
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Wait user input"
        End Get
    End Property

    Public Overrides Function Execute() As Boolean
        If Not m_Finished Then
            m_Program.State = Program.ProgramStates.Waiting
            While m_Program.State = Program.ProgramStates.Waiting
                m_Machine.MMI.SetColon(False) 'fast blink the colon to indicate the pause
                Thread.Sleep(100)
                m_Machine.MMI.SetColon(True)
                Thread.Sleep(100)
            End While
            Return True
        End If
        m_Finished = True
        Return False
    End Function

    ''' <summary>
    ''' Time left is unknown
    ''' </summary>
    ''' <returns></returns>
    Public Overrides Function GetTimeLeft() As Integer
        If m_Finished Then Return 0
        Return 1
    End Function

    Public Overrides Function TotalTime() As Integer
        Return 1
    End Function
End Class