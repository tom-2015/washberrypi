Imports System.Threading
Imports Washmachine
Imports WiringPiNet

''' <summary>
''' The main class that combines everything together
''' </summary>
Public Class WashingMachine

    'WiringPi defines for GPIO pins that drive relays
    Public Const MOTOR_POWER_GPIO As Integer = 1
    Public Const HEATING_POWER_GPIO As Integer = 2
    Public Const PUMP_OUT_GPIO As Integer = 3
    Public Const CIRCULATION_PUMP_GPIO As Integer = 4
    Public Const DOOR_LOCK_GPIO As Integer = 7
    Public Const WATER_VALVE1_GPIO As Integer = 6
    Public Const WATER_VALVE2_GPIO As Integer = 5
    Public Const OVERFLOW_DETECT As Integer = 27

    Public WithEvents Motor As MotorControl 'controls the motor
    Public WithEvents Heater As HeatControl 'controls the heating
    Public WithEvents Water As WaterControl 'controls the water level
    Public WithEvents Program As Program    'holds all programblocks that need to be executed
    Public WithEvents MMI As MMI            'controls display and buttons

    Public ProgramManager As ProgramManager 'class that creates programs based on wanted user settings

    Public GPIO As Gpio 'general GPIO instance
    Public PumpOut As GpioPin 'GPIO pins for pump, etc...
    Public CirculationPump As GpioPin
    Public DoorLock As GpioPin
    Public Heating As GpioPin
    Public Water1 As GpioPin
    Public Water2 As GpioPin
    Public OverFlowDetect As GpioPin

    ''' <summary>
    ''' Timeer and settings for auto saving the program state to be able to resume after power failure or program crash
    ''' </summary>
    Public SaveProgramTimer As Threading.Timer
    Public SaveProgramTimeout As Integer
    Public AutoSaveProgramState As Boolean = False
    Public SaveProgramFileName As String = "/dev/shm/program_state.xml"

    Public Event DebugEvent(ByVal Machine As WashingMachine, ByVal Message As String)

    ''' <summary>
    ''' Turns everything off and resets the class
    ''' </summary>
    Public Sub Reset()
        Motor.SetWantedSpeed(0)
        Motor.DisableMotorPower()
        Heater.StopTempControl()
        Water.StopControlWaterLevel()
        Heating.Write(0)
        PumpOut.Write(0)
        DoorLock.Write(0)
        Water1.Write(0)
        Water2.Write(0)
        CirculationPump.Write(0)
    End Sub

    Public Sub New()
        GPIO = New Gpio()

        PumpOut = New GpioPin(GPIO, PUMP_OUT_GPIO)
        PumpOut.SetMode(PinMode.Output)

        CirculationPump = New GpioPin(GPIO, CIRCULATION_PUMP_GPIO)
        CirculationPump.SetMode(PinMode.Output)

        DoorLock = New GpioPin(GPIO, DOOR_LOCK_GPIO)
        DoorLock.SetMode(PinMode.Output)

        Heating = New GpioPin(GPIO, HEATING_POWER_GPIO)
        Heating.SetMode(PinMode.Output)

        Water1 = New GpioPin(GPIO, WATER_VALVE1_GPIO)
        Water1.SetMode(PinMode.Output)

        Water2 = New GpioPin(GPIO, WATER_VALVE2_GPIO)
        Water2.SetMode(PinMode.Output)

        OverFlowDetect = New GpioPin(GPIO, OVERFLOW_DETECT)
        OverFlowDetect.SetMode(PinMode.Input)

        Motor = New MotorControl("/dev/ttyS0", New GpioPin(GPIO, MOTOR_POWER_GPIO))

        Water = New WaterControl(Me, Motor, Water1, Water2)
        Heater = New HeatControl(Me, Motor, Heating, Water)



        Reset()

        ProgramManager = New ProgramManager(Me)
        MMI = New MMI(Me)
        UpdateDisplay()

    End Sub

    ''' <summary>
    ''' Closes the doorlock
    ''' </summary>
    Public Sub CloseDoorLock()
        DoorLock.Write(1)
        RaiseEvent DebugEvent(Me, "Close doorlock")
    End Sub

    ''' <summary>
    ''' Adds water to the wanted level from Watersource
    ''' </summary>
    ''' <param name="WantedLevel"></param>
    ''' <param name="WaterSource">determines the soap type that will be added</param>
    Public Sub AddWater(ByVal WantedLevel As Integer, ByVal WaterSource As WaterControl.WaterSource)
        Water.StartWaterControl(WantedLevel, WaterSource)
        RaiseEvent DebugEvent(Me, "Add water " & WantedLevel & " source: " & WaterSource)
    End Sub

    ''' <summary>
    ''' Starts the pump that circulates the water
    ''' </summary>
    Public Sub StartCirculationPump()
        CirculationPump.Write(1)
        RaiseEvent DebugEvent(Me, "Start circulation pump")
    End Sub

    ''' <summary>
    ''' Starts washing, returns false is no motor power was detected
    ''' </summary>
    Public Function StartWashing(Optional ByVal CycleDelay As Integer = 2000) As Boolean
        Dim Res As Boolean = Motor.StartWashing(CycleDelay)
        RaiseEvent DebugEvent(Me, "Start washing")
        Return Res
    End Function

    ''' <summary>
    ''' starts heating
    ''' </summary>
    ''' <param name="WantedTemp"></param>
    Public Sub StartHeating(ByVal WantedTemp As Integer)
        Heater.StartTempControl(WantedTemp)
        RaiseEvent DebugEvent(Me, "Start heating to " & WantedTemp)
    End Sub

    ''' <summary>
    ''' Stops the heating
    ''' </summary>
    Public Sub StopHeating()
        Heater.StopTempControl()
        RaiseEvent DebugEvent(Me, "Stop heating")
    End Sub

    ''' <summary>
    ''' Stops the washing of motor
    ''' </summary>
    Public Sub StopWashing()
        Motor.StopMotor()
        RaiseEvent DebugEvent(Me, "Stop Washing")
    End Sub

    ''' <summary>
    ''' Stops circulation pump
    ''' </summary>
    Public Sub StopCirculationPump()
        CirculationPump.Write(0)
        RaiseEvent DebugEvent(Me, "Stop circulation pump")
    End Sub

    ''' <summary>
    ''' Starts centrifuge
    ''' automatically hangles pumps heat and water
    ''' Returns false if no motor power detected
    ''' </summary>
    Public Function StartCentrifuge(ByVal RPM As Integer) As Boolean

        Heater.StopTempControl()
        Water.StopControlWaterLevel()
        CirculationPump.Write(0)

        'start continues pumping
        PumpOut.Write(1)
        Thread.Sleep(100)
        Heating.Write(0)
        Thread.Sleep(100)

        Dim Res As Boolean = Motor.StartCentrifuge(RPM, False)
        RaiseEvent DebugEvent(Me, "Start centrifuge at " & RPM)
        Return Res
    End Function

    ''' <summary>
    ''' Stops motor and pump if centrifuging
    ''' </summary>
    Public Sub StopCentrifuge()
        PumpOut.Write(0)
        Heating.Write(0)
        Motor.StopMotor()
        RaiseEvent DebugEvent(Me, "Stop centrifuge")
    End Sub

    ''' <summary>
    ''' Opens the door (takes some time)
    ''' </summary>
    Public Sub OpenDoorlock()
        DoorLock.Write(0)
        RaiseEvent DebugEvent(Me, "Open doorlock")
    End Sub

    ''' <summary>
    ''' Pumps out water until Level is reached
    ''' waits until finished or timed out
    ''' </summary>
    ''' <param name="Level"></param>
    ''' <returns></returns>
    Public Function PumpOutWater(ByVal Level As Integer, Optional ByVal Timeout As Integer = 20000) As Boolean
        Dim Start As DateTime = Now()
        Water.StopControlWaterLevel()

        PumpOut.Write(1)
        RaiseEvent DebugEvent(Me, "Pumping water out to level " & Level)
        While Water.GetWaterLevel() > Level
            Thread.Sleep(100)
            If (Now() - Start).TotalMilliseconds > Timeout Then
                PumpOut.Write(0)
                Return False
            End If
        End While

        RaiseEvent DebugEvent(Me, "All water removed.")
        PumpOut.Write(0)

        Return True
    End Function

    Public Function toJSON() As String
        Dim Json As String = "{""rpm"": " & Motor.GetAvgRPM() & ", ""temp"": " & Heater.GetTemp() & ", ""water"": " & Water.GetWaterLevel() & ", ""heating"": " &
                                IIf(Heater.PowerEnabled, "1", "0") & ", ""motor"": " & IIf(Motor.PowerEnabled, "1", "0")

        'could be shorter but mono will crash...
        If PumpOut.CurrentValue = PinValue.High Then
            Json &= ", ""pump"": 1"
        Else
            Json &= ", ""pump"": 0"
        End If

        If CirculationPump.CurrentValue = PinValue.High Then
            Json &= ", ""circ_pump"": 1"
        Else
            Json &= ", ""circ_pump"": 0"
        End If

        If DoorLock.CurrentValue = PinValue.High Then
            Json &= ", ""lock"": 1"
        Else
            Json &= ", ""lock"": 0"
        End If

        Json &= ", ""water1"":" & IIf(Water.WaterSource1PowerEnabled, "1", "0") & ", ""water2"": " & IIf(Water.WaterSource2PowerEnabled, "1", "0") & ", "

        If Program IsNot Nothing Then
            Json &= " ""state"": " & Program.State & ", ""program"": " & Program.toJSON()
        Else
            Json &= " ""state"": 0, ""program"": {}"
        End If

        Return Json & "}"
    End Function

    Private Sub Motor_DebugPrint(Control As MotorControl, Text As String) Handles Motor.DebugPrint
        Console.WriteLine(Text)
        'RaiseEvent DebugEvent(Me, Text)
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        Try
            Reset()
        Catch e As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Sets the current program class
    ''' </summary>
    ''' <param name="Program"></param>
    Friend Sub SetProgram(Program As Program)
        If Me.Program IsNot Nothing Then
            Me.Program.StopExecute()
        End If
        Me.Program = Program
    End Sub

    ''' <summary>
    ''' Starts a new program
    ''' </summary>
    ''' <param name="pProgram"></param>
    Public Sub StartProgram(ByVal pProgram As Program)
        If Program IsNot Nothing Then
            StopProgram()
        End If
        Program = pProgram
        Program.Execute()
        UpdateDisplay()
        MMI.SetCentrifuge(Program.GeneralRPM)
        MMI.SetProgram(Program.GeneralProgramType)
        MMI.SetTemp(Program.GeneralTemp)
        MMI.DisableBuzzer()
        If SaveProgramTimer IsNot Nothing Then
            SaveProgramTimer.Change(Timeout.Infinite, Timeout.Infinite)
        End If
        SaveProgramTimeout = 60
        SaveProgramTimer = New Threading.Timer(New TimerCallback(AddressOf SaveProgramState), False, 1000, 1000)
        RaiseEvent DebugEvent(Me, "Starting program.")
    End Sub

    ''' <summary>
    ''' Called to save the current program state, this is to be able to continue the program after crash / power failure
    ''' Requires AutoSaveProgramState to be true, program is saved to SaveProgramFileName
    ''' </summary>
    ''' <param name="Force"></param>
    Private Sub SaveProgramState(ByVal Force As Boolean)
        If AutoSaveProgramState Then
            If Program IsNot Nothing Then
                If SaveProgramTimeout = 0 OrElse Force Then

                    Dim Xml As New Xml.XmlDocument()

                    If Not ProgramManager.SaveProgram(SaveProgramFileName, Program, True) Then
                        Console.WriteLine("Error saving program state!")
                    End If

                    SaveProgramTimeout = 60
                End If
                SaveProgramTimeout -= 1
            End If
        End If
        UpdateDisplay()
    End Sub

    ''' <summary>
    ''' Updates display if program is loaded (sets time left)
    ''' </summary>
    Public Sub UpdateDisplay()
        If Program IsNot Nothing Then
            If Program.State = Program.ProgramStates.Running Then
                Dim TimeLeft As Integer = Program.GetTimeLeft()
                Dim Hour As Integer = (TimeLeft \ 3600) Mod 20
                Dim Minute As Integer = (TimeLeft \ 60) Mod 60
                MMI.Set7Segment(0, Hour \ 10)
                MMI.Set7Segment(1, Hour Mod 10)
                MMI.Set7Segment(2, Minute \ 10)
                MMI.Set7Segment(3, Minute Mod 10)
                MMI.SetColon(True)
            End If
        End If
    End Sub

    Private Sub Motor_UnbalanceAlarm(Control As MotorControl, ForAxis As Integer, X As Single, Y As Single, Z As Single, TryCount As Integer) Handles Motor.UnbalanceAlarm
        RaiseEvent DebugEvent(Me, $"Unbalance detected on axis: {ForAxis} values: {X},{Y},{Z}, Peak: {Motor.PeakX},{Motor.PeakY},{Motor.PeakZ} nr tries left: {TryCount}")
    End Sub

    ''' <summary>
    ''' If an error occurs in the program this sub will send the error code to the display interface
    ''' </summary>
    ''' <param name="Program"></param>
    ''' <param name="Err"></param>
    ''' <param name="CurrentBlock"></param>
    ''' <param name="Message"></param>
    Private Sub Program_ExecuteError(Program As Program, Err As Integer, CurrentBlock As ProgramBlock, Message As String) Handles Program.ExecuteError
        MMI.Set7Segment(1, &HE)
        MMI.Set7Segment(2, Err / 10)
        MMI.Set7Segment(3, Err Mod 10)
        Dim BlockName As String = ""
        If CurrentBlock IsNot Nothing Then BlockName = CurrentBlock.Name
        RaiseEvent DebugEvent(Me, "Execute Error: " & Message & " in " & BlockName)
    End Sub

    ''' <summary>
    ''' when a block is finished
    ''' </summary>
    ''' <param name="Program"></param>
    ''' <param name="CurrentIndex"></param>
    ''' <param name="CurrentBlock"></param>
    ''' <param name="Message"></param>
    Private Sub Program_ExecuteProgress(Program As Program, CurrentIndex As Integer, CurrentBlock As ProgramBlock, Message As String) Handles Program.ExecuteProgress

        Dim TimeLeft As String = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTimeLeft()).Ticks), "HH:mm:ss")
        Dim TotalTime As String = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTotalTime()).Ticks), "HH:mm:ss")
        Dim TimeRunning As String = Format(New DateTime(TimeSpan.FromSeconds(Program.GetTimeRunning()).Ticks), "HH:mm:ss")

        RaiseEvent DebugEvent(Me, "Execute progress: " & CurrentBlock.Name & ", time left: " & TimeLeft & ", time running: " & TimeRunning & ", total time: " & TotalTime)

        SaveProgramState(True)
    End Sub

    Private Sub MMI_ButtonPressed(Button As MMI.Buttons, Val As Boolean) Handles MMI.ButtonPressed
        RaiseEvent DebugEvent(Me, "Button pressed: " & Val & "," & [Enum].GetName(GetType(MMI.Buttons), Button))
    End Sub

    Private Sub MMI_DebugEvent(MMI As MMI, Message As String) Handles MMI.DebugEvent
        RaiseEvent DebugEvent(Me, "MMI: " & Message)
    End Sub

    Private Sub Water_DebugEvent(WaterControl As WaterControl, Message As String) Handles Water.DebugEvent
        RaiseEvent DebugEvent(Me, "WaterControl: " & Message)
    End Sub

    ''' <summary>
    ''' Stops executing current program
    ''' </summary>
    Public Sub StopProgram()
        Program.StopExecute()
        Program = Nothing
        MMI.Set7Segment(0, 17)
        MMI.Set7Segment(1, 17)
        MMI.Set7Segment(2, 17)
        MMI.Set7Segment(3, 17)
        RaiseEvent DebugEvent(Me, "Program was stopped.")
    End Sub

    Private Sub Program_Finished(Program As Program) Handles Program.Finished
        If AutoSaveProgramState Then
            Try
                Kill(SaveProgramFileName)
            Catch e As Exception
                RaiseEvent DebugEvent(Me, "Error removing save program file: " & SaveProgramFileName)
            End Try
        End If

        Try
            Shell(My.Application.Info.DirectoryPath & "/finish.sh")
        Catch ex As Exception
            RaiseEvent DebugEvent(Me, "Execute finish.sh failed " & ex.Message)
        End Try

    End Sub

End Class
