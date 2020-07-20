Imports System.Threading
Imports Washmachine
Imports WiringPiNet

''' <summary>
''' This class controls the temperature in a sperate thread
''' </summary>
Public Class HeatControl

    Protected WithEvents m_MotorControl As MotorControl
    Protected m_WaterControl As WaterControl
    Protected m_IOPin As GpioPin

    Protected m_LastTemps(0 To 10) As Integer
    Protected m_AvgTemp As Single
    Protected m_TempLock As New Object
    Protected m_WantedTemp As Single
    Protected m_TempControlThread As Thread
    Protected m_ControllingTemperature As Boolean

    Const SteinHartCoefA As Single = -0.4855491127 * 10 ^ -3
    Const SteinHartCoefB As Single = 4.979419048 * 10 ^ -4
    Const SteinHartCoefC As Single = -10.02505151 * 10 ^ -7

    Public Event DebugEvent(ByVal HeatControl As HeatControl, ByVal Message As String)
    Public Event HeaterChange(ByVal HeatControl As HeatControl, ByVal Status As Boolean)

    ''' <summary>
    ''' construct
    ''' </summary>
    ''' <param name="MotorControl">is needed because it provides information about the temperature</param>
    ''' <param name="IOPin"></param>
    ''' <param name="WaterControl">if waterlevel drops heating will be disabled automatically to prevent damage</param>
    Public Sub New(ByVal MotorControl As MotorControl, ByVal IOPin As GpioPin, ByVal WaterControl As WaterControl)
        m_MotorControl = MotorControl
        m_IOPin = IOPin
        m_WaterControl = WaterControl
    End Sub

    ''' <summary>
    ''' Starts heating to the wanted temperature
    ''' </summary>
    ''' <param name="WantedTemp"></param>
    Public Sub StartTempControl(ByVal WantedTemp As Single)
        StopTempControl()
        m_ControllingTemperature = True
        m_WantedTemp = WantedTemp
        m_TempControlThread = New Thread(AddressOf TempControl)
        m_TempControlThread.Start()
    End Sub

    ''' <summary>
    ''' stops the heating
    ''' </summary>
    Public Sub StopTempControl()
        m_ControllingTemperature = False
        If m_TempControlThread IsNot Nothing Then
            If Not m_TempControlThread.Join(1000) Then m_TempControlThread.Abort()
        End If
        DisableHeater()
    End Sub


    ''' <summary>
    ''' Seperate thread to control temperature
    ''' </summary>
    Protected Sub TempControl()
        Try
            While m_ControllingTemperature
                If m_WaterControl.GetWaterLevel() < 30 Then
                    DisableHeater()
                Else
                    Dim Temp As Single = GetTemp()
                    If Temp < 95 Then
                        If Temp < m_WantedTemp - m_WantedTemp * 5 / 100 Then
                            EnableHeater()
                        ElseIf Temp >= m_WantedTemp Then
                            DisableHeater()
                        End If
                    Else
                        DisableHeater()
                    End If
                End If
                Thread.Sleep(500)
            End While
        Catch ex As ThreadAbortException
            DisableHeater()
        End Try
    End Sub

    ''' <summary>
    ''' Returns temperature in degrees
    ''' </summary>
    ''' <returns></returns>
    Public Function GetTemp() As Single
        SyncLock m_TempLock
            Return m_AvgTemp
        End SyncLock
    End Function

    Protected Sub EnableHeater()
        m_IOPin.Write(1)
        RaiseEvent HeaterChange(Me, True)
    End Sub

    Protected Sub DisableHeater()
        m_IOPin.Write(0)
        RaiseEvent HeaterChange(Me, False)
    End Sub

    ''' <summary>
    ''' Incoming samples from the temp-> ADC converter located on the µcontroller for driving the motor
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="Data"></param>
    Private Sub m_MotorControl_SampleArrival(Control As MotorControl, Data As String) Handles m_MotorControl.SampleArrival
        For i As Integer = 0 To m_LastTemps.Length - 2
            m_LastTemps(i + 1) = m_LastTemps(i)
        Next

        m_LastTemps(0) = Control.CurrentTemp

        Dim Avg As Integer = 0
        For i As Integer = 0 To m_LastTemps.Length - 1
            Avg += m_LastTemps(i)
        Next

        'Calculate the temperature using Steinhart 
        'https://www.thinksrs.com/downloads/programs/Therm%20Calc/NTCCalibrator/NTCcalculator.htm
        'requires you to measure resistance at 3 different temperatures like 20°, 40° and 90°
        Dim Voltage As Single = (3.3 / 1024 * (Avg / m_LastTemps.Length)) 'voltage accros NTC
        Dim Resistenace As Single = Voltage / ((3.3 - Voltage) / 1000) 'resistance is voltage / current through 1K resistor 

        SyncLock m_TempLock
            m_AvgTemp = Math.Round((1 / (SteinHartCoefA + SteinHartCoefB * Math.Log(Resistenace) + SteinHartCoefC * (Math.Log(Resistenace)) ^ 3)) - 273) 'calculates temperature
        End SyncLock
    End Sub

    ''' <summary>
    ''' Returns true if heating is enabled
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property PowerEnabled() As Boolean
        Get
            If m_IOPin.CurrentValue = PinValue.High Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

End Class
