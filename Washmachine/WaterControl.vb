Imports System.Threading
Imports System.Xml
Imports Washmachine
Imports WiringPiNet

''' <summary>
''' Class that controls water level
''' </summary>
Public Class WaterControl

    Protected WithEvents m_MotorControl As MotorControl 'required because the microcontroller that drives motor also drives and reads the water level sensor
    Protected m_Machine As WashingMachine

    Protected m_LastWaterLevels(0 To 10) As Integer
    Protected m_AvgWaterLevel As Integer
    Protected m_WaterLevelLock As New Object

    Protected m_WaterIn1 As GpioPin 'voorwas    |__ wasverzachter
    Protected m_WaterIn2 As GpioPin 'waspoeder  |
    Protected m_ControlWater As Boolean
    Protected m_WaterControlThread As Thread
    Protected m_WantedWaterLevel As Integer
    Protected m_WantedWaterSource As WaterSource


    Public Event DebugEvent(ByVal WaterControl As WaterControl, ByVal Message As String)
    Public Event WaterChange(ByVal WaterControl As WaterControl, ByVal Source As WaterSource, ByVal Status As Boolean)

    Public Enum WaterSource
        WaterSource1 'voorwas
        WaterSource2 'was
        WaterSource3 'spoelen
    End Enum

    Protected Class WaterLevelParam
        Public WantedLevel As Integer
        Public WaterSource As WaterSource

        Public Sub New(ByVal Level As Integer, ByVal Source As WaterSource)
            Me.WantedLevel = Level
            Me.WaterSource = Source
        End Sub
    End Class

    ''' <summary>
    ''' Starts a new class to control the water level
    ''' </summary>
    ''' <param name="Motor"></param>
    ''' <param name="WaterIn1"></param>
    ''' <param name="WaterIn2"></param>
    Public Sub New(ByVal Machine As WashingMachine, ByVal Motor As MotorControl, ByVal WaterIn1 As GpioPin, ByVal WaterIn2 As GpioPin)
        m_MotorControl = Motor
        m_Machine = Machine
        m_WaterIn1 = WaterIn1
        m_WaterIn2 = WaterIn2
    End Sub

    Public Sub LoadState(ByVal Node As XmlNode)
        m_ControlWater = Node.Attributes("state").Value
        m_WantedWaterLevel = Node.Attributes("level").Value
        m_WantedWaterSource = Node.Attributes("source").Value
        If m_ControlWater Then
            StartWaterControl(m_WantedWaterLevel, m_WantedWaterSource)
        End If
    End Sub

    Public Function SaveState(ByVal Doc As XmlDocument) As XmlNode
        Dim WaterNode As XmlNode = Doc.CreateElement("WaterControl")

        WaterNode.Attributes.Append(Doc.CreateAttribute("state")).Value = m_ControlWater
        WaterNode.Attributes.Append(Doc.CreateAttribute("level")).Value = m_WantedWaterLevel
        WaterNode.Attributes.Append(Doc.CreateAttribute("source")).Value = m_WantedWaterSource
        Return WaterNode
    End Function

    ''' <summary>
    ''' Gives command to open valves and regulate the water level to level %
    ''' Watersource determines which kind of soap will be added to the water
    ''' Call StopControlWaterLevel to stop the water being regulated to level
    ''' </summary>
    ''' <param name="Level">%</param>
    ''' <param name="WaterSource"></param>
    Public Sub StartWaterControl(ByVal Level As Integer, ByVal WaterSource As WaterSource)
        StopControlWaterLevel()
        m_ControlWater = True
        m_WantedWaterLevel = Level
        m_WantedWaterSource = WaterSource
        m_WaterControlThread = New Thread(AddressOf ControlWaterLevel)
        m_WaterControlThread.Start(New WaterLevelParam(Level, WaterSource))
    End Sub

    Protected Sub ControlWaterLevel(ByVal Params As WaterLevelParam)
        Dim OverFlowCount As Integer = 0
        Try
            Dim PLevel As Integer
            Dim PPLevel As Integer
            While m_ControlWater 'every 500ms check the water level and open/close valves
                Dim Level As Integer = GetWaterLevel()
                If m_MotorControl.ErrorCode And MotorControl.ErrorFlags.ErrorWaterSensor Then
                    CloseValves()
                    RaiseEvent DebugEvent(Me, "Error watersensor!")
                Else
                    If Level < Params.WantedLevel - Params.WantedLevel * 5 / 100 AndAlso
                        PLevel < Params.WantedLevel - Params.WantedLevel * 5 / 100 AndAlso
                        PPLevel < Params.WantedLevel - Params.WantedLevel * 5 / 100 Then
                        OpenValve(Params.WaterSource)
                    ElseIf Level > Params.WantedLevel Then
                        CloseValves()
                    End If
                End If

                If m_Machine.OverFlowDetect.CurrentValue = PinValue.Low Then
                    OverFlowCount += 1
                    If OverFlowCount = 10 Then
                        CloseValves()
                        RaiseEvent DebugEvent(Me, "Water overflow detected!")
                        If Machine.Program IsNot Nothing Then
                            Machine.Program.GenerateError(Program.ProgramErrors.ErrorWaterOverflow, "Overflow detected.")
                        End If
                    End If
                Else
                    OverFlowCount = 0
                End If

                PLevel = Level
                PPLevel = PLevel
                Thread.Sleep(500)
            End While
        Catch tab As ThreadAbortException
            CloseValves()
        End Try
    End Sub

    ''' <summary>
    ''' Stops all valves
    ''' </summary>
    Public Sub StopControlWaterLevel()
        m_ControlWater = False
        If m_WaterControlThread IsNot Nothing Then
            If Not m_WaterControlThread.Join(1000) Then m_WaterControlThread.Abort()
        End If
        CloseValves()
    End Sub

    ''' <summary>
    ''' Returns current water level 0-100 (or more)%
    ''' </summary>
    ''' <returns></returns>
    Public Function GetWaterLevel() As Integer
        Dim Level As Integer
        SyncLock m_WaterLevelLock
            Level = (m_AvgWaterLevel - 1140) / 125 * 100
        End SyncLock
        If Level < 0 Then Level = 0
        Return Level
    End Function

    ''' <summary>
    ''' Open given valve, close others
    ''' </summary>
    ''' <param name="Source"></param>
    Public Sub OpenValve(ByVal Source As WaterSource)
        CloseValves()
        Select Case Source
            Case WaterSource.WaterSource1
                m_WaterIn1.Write(1)
                RaiseEvent WaterChange(Me, WaterSource.WaterSource1, False)
            Case WaterSource.WaterSource2
                m_WaterIn2.Write(1)
                RaiseEvent WaterChange(Me, WaterSource.WaterSource2, False)
            Case WaterSource.WaterSource3
                m_WaterIn1.Write(1)
                m_WaterIn2.Write(1)
                RaiseEvent WaterChange(Me, WaterSource.WaterSource3, False)
        End Select
    End Sub

    ''' <summary>
    ''' Closes all valves immediately
    ''' </summary>
    Public Sub CloseValves()
        m_WaterIn1.Write(0)
        m_WaterIn2.Write(0)
        RaiseEvent WaterChange(Me, WaterSource.WaterSource1, False)
        RaiseEvent WaterChange(Me, WaterSource.WaterSource2, False)
        RaiseEvent WaterChange(Me, WaterSource.WaterSource3, False)
    End Sub

    ''' <summary>
    ''' Process average water level
    ''' </summary>
    ''' <param name="Control"></param>
    ''' <param name="Data"></param>
    Private Sub m_MotorControl_SampleArrival(Control As MotorControl, Data As String) Handles m_MotorControl.SampleArrival
        For i As Integer = 0 To m_LastWaterLevels.Length - 2
            m_LastWaterLevels(i + 1) = m_LastWaterLevels(i)
        Next
        m_LastWaterLevels(0) = Control.WaterLevel
        Dim Avg As Integer = 0
        For i As Integer = 0 To m_LastWaterLevels.Length - 1
            Avg += m_LastWaterLevels(i)
        Next
        SyncLock m_WaterLevelLock
            m_AvgWaterLevel = Avg / m_LastWaterLevels.Length
        End SyncLock
    End Sub

    ''' <summary>
    ''' Returns true if power to water source 1 is enabled
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property WaterSource1PowerEnabled() As Boolean
        Get
            If m_WaterIn1.CurrentValue = PinValue.High Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    ''' <summary>
    ''' Returns true if valve 2 is on
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property WaterSource2PowerEnabled() As Boolean
        Get
            If m_WaterIn2.CurrentValue = PinValue.High Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        StopControlWaterLevel()
    End Sub
End Class
