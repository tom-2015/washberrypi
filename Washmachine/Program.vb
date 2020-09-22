Imports System.IO
Imports System.Threading
Imports System.Xml

Public Class ProgramType
    Public ProgramType As ProgramManager.ProgramTypes

    Public Sub New(ByVal pType As ProgramManager.ProgramTypes)
        ProgramType = pType
    End Sub

    Public Overrides Function ToString() As String
        Select Case ProgramType
            Case ProgramManager.ProgramTypes.Centrifuge
                Return "Centrifugeren"
            Case ProgramManager.ProgramTypes.DailyWash
                Return "Dagelijkse Was"
            Case ProgramManager.ProgramTypes.ExtraDirty
                Return "Vlekken"
            Case ProgramManager.ProgramTypes.Fast
                Return "Snel"
            Case ProgramManager.ProgramTypes.Rinse
                Return "Spoelen"
        End Select
        Return ""
    End Function
End Class

''' <summary>
''' Program manager can create program class which contains programblocks
''' program makes an entire program of smaller components callec programblocks
''' </summary>
Public Class ProgramManager

    Dim m_Machine As WashingMachine

    ''' <summary>
    ''' Predefined program types (see also the washingmachine program select rotation button)
    ''' </summary>
    Public Enum ProgramTypes As Integer
        DailyWash = 1
        ExtraDirty = 2
        Fast = 3
        Rinse = 4
        Centrifuge = 5
        Delicate = 6
    End Enum

    ''' <summary>
    ''' Extra options that can be added
    ''' </summary>
    <Flags>
    Public Enum ProgramOptions As Integer
        None = 0
        WaitForFinalCentrifuge = 1
    End Enum

    Public Sub New(ByVal Machine As WashingMachine)
        m_Machine = Machine
    End Sub

    ''' <summary>
    ''' Creates a new program and adds all basic programblocks with configuration
    ''' </summary>
    ''' <param name="ProgramType">What kind of program</param>
    ''' <param name="WantedTemp">Wanted wash temperature</param>
    ''' <param name="RPM">Wanted final centrifuge speed</param>
    ''' <param name="Options">Adds extra options like wait for the final centrifuge</param>
    ''' <param name="ExtraRinse">add extra rinse blocks, default is 2</param>
    ''' <returns></returns>
    Public Function GenerateProgram(ByVal ProgramType As ProgramTypes, ByVal WantedTemp As Integer, ByVal RPM As Integer, Optional ByVal Options As ProgramOptions = ProgramOptions.None, Optional ByVal ExtraRinse As Integer = 2) As Program
        Dim Program As New Program(m_Machine, RPM, WantedTemp, Options, ProgramType)

        Program.AddBlock(New CloseDoorLock(Program))
        Console.WriteLine("Wash " & WantedTemp & "," & RPM & "," & ExtraRinse)
        Select Case ProgramType
            Case ProgramTypes.DailyWash
                Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 70, 35))
                Program.AddBlock(New CreateFoam(Program, 80))
                Program.AddBlock(New Wash(Program, WantedTemp, 10 * 60, 120 * 60))
                Program.AddBlock(New PumpOutWater(Program))
                Program.AddBlock(New Centrifuge(Program, RPM * 0.7, 3 * 60))

                For i As Integer = 0 To ExtraRinse - 1
                    Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
                    Program.AddBlock(New Wash(Program, 0, 2 * 60, 10 * 60))
                    Program.AddBlock(New PumpOutWater(Program))
                    Program.AddBlock(New Centrifuge(Program, RPM * 0.45, 1.5 * 60))
                Next
            Case ProgramTypes.ExtraDirty
                Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
                Program.AddBlock(New CreateFoam(Program, 80))
                Program.AddBlock(New Wash(Program, WantedTemp, 20 * 60, 120 * 60))
                Program.AddBlock(New PumpOutWater(Program))
                Program.AddBlock(New Centrifuge(Program, RPM * 0.7, 3 * 60))

                For i As Integer = 0 To ExtraRinse - 1
                    Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 75, 35))
                    Program.AddBlock(New Wash(Program, 0, 5 * 60, 10 * 60))
                    Program.AddBlock(New PumpOutWater(Program))
                    Program.AddBlock(New Centrifuge(Program, RPM * 0.45, 1.5 * 60))
                Next
            Case ProgramTypes.Fast
                Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 65, 35))
                Program.AddBlock(New CreateFoam(Program, 80))
                Program.AddBlock(New Wash(Program, WantedTemp, 5 * 60, 120 * 60))
                Program.AddBlock(New PumpOutWater(Program))
                Program.AddBlock(New Centrifuge(Program, RPM * 0.7, 2 * 60))
            Case ProgramTypes.Rinse
                Program.AddBlock(New PumpOutWater(Program))
            Case ProgramTypes.Delicate
                Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource2, 80, 35))
                Program.AddBlock(New CreateFoam(Program, 80))
                Program.AddBlock(New Wash(Program, WantedTemp, 6 * 60, 120 * 60))
                Program.AddBlock(New PumpOutWater(Program))
            Case ProgramTypes.Centrifuge
                Program.AddBlock(New PumpOutWater(Program))
                Program.AddBlock(New Centrifuge(Program, RPM, 4 * 60))
                Program.AddBlock(New OpenDoorLock(Program))
                Return Program
        End Select

        Program.AddBlock(New AddWater(Program, WaterControl.WaterSource.WaterSource3, 80, 40))
        Program.AddBlock(New Wash(Program, 0, 4 * 60, 10 * 60))

        If Options And ProgramOptions.WaitForFinalCentrifuge Then
            Program.AddBlock(New WaitForInput(Program))
        End If

        Program.AddBlock(New PumpOutWater(Program))
        Program.AddBlock(New Centrifuge(Program, RPM, 4 * 60))
        Program.AddBlock(New OpenDoorLock(Program))

        Return Program
    End Function

    ''' <summary>
    ''' Resume a program from saved file, can be used when power was interrupted to resume where machine was
    ''' Not yet tested
    ''' </summary>
    ''' <param name="File"></param>
    ''' <returns></returns>
    Public Function ResumeProgram(ByVal File As String) As Boolean
        Dim Xml As New XmlDocument()
        Xml.Load(File)
        Dim Node As XmlNode = Xml.SelectSingleNode("/machine/program")
        Dim Program As New Program(m_Machine, Node)
        m_Machine.Program = Program
        Program.Resume()
        Return True
    End Function

    ''' <summary>
    ''' Saves current program state to a file
    ''' </summary>
    ''' <param name="File"></param>
    ''' <returns></returns>
    Public Function SaveState(ByVal File As String) As Boolean
        Return SaveProgram(File, m_Machine.Program, True)
    End Function

    ''' <summary>
    ''' Saves a progral to a file
    ''' </summary>
    ''' <param name="File"></param>
    ''' <param name="Program"></param>
    ''' <param name="SaveState"></param>
    ''' <returns></returns>
    Public Shared Function SaveProgram(ByVal File As String, ByVal Program As Program, Optional ByVal SaveState As Boolean = False) As Boolean
        Dim Xml As New XmlDocument
        If Program IsNot Nothing Then
            Xml.AppendChild(Xml.CreateXmlDeclaration("1.0", "UTF-8", Nothing))
            Dim Node As XmlNode = Xml.AppendChild(Xml.CreateElement("machine"))
            Node.AppendChild(Program.SaveProgram(Xml, SaveState))
            Xml.Save(File)
        End If
        Return True
    End Function


End Class

''' <summary>
''' Holds all basic programblocks together and executes them
''' </summary>
Public Class Program

    Protected m_Machine As WashingMachine
    Protected m_Blocks As New List(Of ProgramBlock)
    Protected m_ExecuteThread As Thread
    Protected m_CurrentIndex As Integer
    Protected m_Start As DateTime = DateTime.MinValue
    Protected m_State As ProgramStates
    Protected m_ErrorCode As ProgramErrors

    Protected m_GeneralRPM As Integer
    Protected m_GeneralTemp As Integer
    Protected m_GeneralOptions As ProgramManager.ProgramOptions
    Protected m_GeneralType As ProgramManager.ProgramTypes

    ''' <summary>
    ''' Defined errors
    ''' </summary>
    Public Enum ProgramErrors As Integer
        ErrorNone = 0
        ErrorNoWater = 1
        ErrorNoMotorPower = 2
        ErrorBalanceFailed = 3
        ErrorTimeOut = 4
        ErrorClearWaterFailed = 5
        ErrorWaterSensor = 6
        ErrorWaterLeaking = 7
        ErrorMotorBlocked = 8
    End Enum

    ''' <summary>
    ''' States the program can have
    ''' </summary>
    Public Enum ProgramStates As Integer
        Idle = 0
        Running = 1
        Waiting = 2
        Finished = 3
        Aborted = 4
        [Error] = 5
    End Enum

    ''' <summary>
    ''' Raised when program is finished
    ''' </summary>
    ''' <param name="Program"></param>
    Public Event Finished(ByVal Program As Program)

    ''' <summary>
    ''' Raised when a block finished, progress
    ''' </summary>
    ''' <param name="Program"></param>
    ''' <param name="CurrentIndex"></param>
    ''' <param name="CurrentBlock"></param>
    ''' <param name="Message"></param>
    Public Event ExecuteProgress(ByVal Program As Program, ByVal CurrentIndex As Integer, ByVal CurrentBlock As ProgramBlock, ByVal Message As String)

    ''' <summary>
    ''' Execute error, no water, ...
    ''' </summary>
    ''' <param name="Program"></param>
    ''' <param name="Err"></param>
    ''' <param name="CurrentBlock"></param>
    ''' <param name="Message"></param>
    Public Event ExecuteError(ByVal Program As Program, ByVal Err As Integer, ByVal CurrentBlock As ProgramBlock, ByVal Message As String)

    Public Sub New(ByVal Machine As WashingMachine)
        m_Machine = Machine
    End Sub

    ''' <summary>
    ''' General values are passsed by ProgramManager.GenerateProgram and are used for updating the display
    ''' Some program blocks may use other RPM/Temp values but these are the general values displayed on the MMI
    ''' </summary>
    ''' <param name="Machine"></param>
    ''' <param name="GeneralRPM"></param>
    ''' <param name="GeneralTemp"></param>
    ''' <param name="GeneralOptions"></param>
    ''' <param name="GeneralType"></param>
    Public Sub New(ByVal Machine As WashingMachine, ByVal GeneralRPM As Integer, ByVal GeneralTemp As Integer, ByVal GeneralOptions As ProgramManager.ProgramOptions, ByVal GeneralType As ProgramManager.ProgramTypes)
        m_Machine = Machine
        m_State = ProgramStates.Idle
        m_GeneralRPM = GeneralRPM
        m_GeneralTemp = GeneralTemp
        m_GeneralOptions = GeneralOptions
        m_GeneralType = GeneralType
    End Sub

    Public Sub New(ByVal Machine As WashingMachine, ByVal Node As XmlNode)
        m_Machine = Machine
        m_State = ProgramStates.Idle
        LoadProgram(Node)
    End Sub

    ''' <summary>
    ''' Adds a block to the program
    ''' </summary>
    ''' <param name="Block"></param>
    Public Sub AddBlock(ByVal Block As ProgramBlock)
        m_Blocks.Add(Block)
    End Sub

    ''' <summary>
    ''' Starts executing asynchronously
    ''' </summary>
    Public Sub Execute()
        StopExecute()
        'm_GeneralRPM = Rpm
        'm_GeneralTemp = Temperature
        m_ExecuteThread = New Thread(AddressOf ExecuteThread)
        m_ExecuteThread.Start(False)
    End Sub

    ''' <summary>
    ''' Resumes after program as loaded
    ''' </summary>
    Public Sub [Resume]()
        StopExecute()
        m_ExecuteThread = New Thread(AddressOf ExecuteThread)
        m_ExecuteThread.Start(True)
    End Sub

    ''' <summary>
    ''' Execute thread procedure
    ''' </summary>
    Protected Sub ExecuteThread(ByVal Resuming As Boolean)
        Try
            If Not Resuming Then
                m_State = ProgramStates.Running
                m_Start = Now()
            End If
            For i As Integer = 0 To m_Blocks.Count - 1
                m_CurrentIndex = i
                Dim Block As ProgramBlock = m_Blocks(i)
                RaiseEvent ExecuteProgress(Me, i, Block, "")
                If Not Block.Finished Then
                    If Not Block.Execute() Then
                        m_State = ProgramStates.Error
                        m_ErrorCode = Block.ErrorCode
                        RaiseEvent ExecuteError(Me, m_ErrorCode, Block, "")
                        Exit For
                    End If
                End If
                Thread.Sleep(500)
            Next
            If m_State <> ProgramStates.Error Then
                m_State = ProgramStates.Finished
                RaiseEvent Finished(Me)
            End If
            m_Machine.Motor.DisableMotorPower()
        Catch ex As ThreadAbortException
            m_Machine.Reset()
        Finally
            m_Start = Date.MinValue
        End Try
        m_ExecuteThread = Nothing
    End Sub

    ''' <summary>
    ''' Stops executing the program
    ''' </summary>
    Public Sub StopExecute()
        If m_ExecuteThread IsNot Nothing Then
            m_State = ProgramStates.Aborted
            m_ExecuteThread.Abort()
            m_ExecuteThread = Nothing
            m_Machine.Motor.DisableMotorPower()
        End If
    End Sub

    ''' <summary>
    ''' Saves current execution state to a file
    ''' </summary>
    ''' <returns></returns>
    Public Function SaveProgram(ByVal Doc As XmlDocument, ByVal SaveState As Boolean) As XmlNode
        Dim ProgramNode As XmlNode = Doc.CreateElement("program")

        If SaveState Then
            With ProgramNode.Attributes
                .Append(Doc.CreateAttribute("execute_index")).Value = m_CurrentIndex
                .Append(Doc.CreateAttribute("rpm")).Value = m_GeneralRPM
                .Append(Doc.CreateAttribute("temp")).Value = m_GeneralTemp
                .Append(Doc.CreateAttribute("programtype")).Value = m_GeneralType
                .Append(Doc.CreateAttribute("options")).Value = m_GeneralOptions
                .Append(Doc.CreateAttribute("state")).Value = m_State
                .Append(Doc.CreateAttribute("start")).Value = (m_Start - New DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds
            End With
        End If

        Dim BlockNodes As XmlNode = ProgramNode.AppendChild(Doc.CreateElement("blocks"))

        For Each Block As ProgramBlock In m_Blocks
            BlockNodes.AppendChild(Block.SaveBlock(Doc, SaveState))
        Next
        If SaveState Then
            ProgramNode.AppendChild(m_Machine.Water.SaveState(Doc))
        End If
        Return ProgramNode
    End Function

    ''' <summary>
    ''' Loads program from file
    ''' </summary>
    ''' <param name="Node"></param>
    Public Sub LoadProgram(ByVal Node As XmlNode)
        m_Blocks = New List(Of ProgramBlock)
        If Node.Attributes("execute_index") IsNot Nothing Then m_CurrentIndex = Node.Attributes("execute_index").Value
        If Node.Attributes("rpm") IsNot Nothing Then m_GeneralRPM = Node.Attributes("rpm").Value
        If Node.Attributes("temp") IsNot Nothing Then m_GeneralTemp = Node.Attributes("temp").Value
        If Node.Attributes("programtype") IsNot Nothing Then m_GeneralType = Node.Attributes("programtype").Value
        If Node.Attributes("options") IsNot Nothing Then m_GeneralOptions = Node.Attributes("options").Value
        If Node.Attributes("state") IsNot Nothing Then m_State = Node.Attributes("state").Value
        If Node.Attributes("start") IsNot Nothing Then m_Start = (New DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(Node.Attributes("start").Value)

        Dim BlockNodes As XmlNodeList = Node.SelectNodes("blocks/block")
        For Each BlockNode As XmlNode In BlockNodes
            Select Case CType(Integer.Parse(BlockNode.Attributes("type").Value), ProgramBlock.ProgramBlockTypes)
                Case ProgramBlock.ProgramBlockTypes.AddWater
                    m_Blocks.Add(New AddWater(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.CreateFoam
                    m_Blocks.Add(New CreateFoam(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.Wash
                    m_Blocks.Add(New Wash(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.PumpOutWater
                    m_Blocks.Add(New PumpOutWater(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.Centrifuge
                    m_Blocks.Add(New Centrifuge(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.CloseDoor
                    m_Blocks.Add(New CloseDoorLock(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.OpenDoor
                    m_Blocks.Add(New OpenDoorLock(Me, BlockNode))
                Case ProgramBlock.ProgramBlockTypes.WaitUserInput
                    m_Blocks.Add(New WaitForInput(Me, BlockNode))
            End Select
        Next
        If Node.SelectSingleNode("WaterControl") IsNot Nothing Then
            m_Machine.Water.LoadState(Node.SelectSingleNode("WaterControl"))
        End If
    End Sub

    ''' <summary>
    ''' Gets/sets the current execution state of the program
    ''' </summary>
    ''' <returns></returns>
    Public Property State() As ProgramStates
        Get
            Return m_State
        End Get
        Set(value As ProgramStates)
            m_State = value
        End Set
    End Property

    ''' <summary>
    ''' Returns total estimated execution time in sec.
    ''' Depending on water input temperature it may take more time
    ''' </summary>
    ''' <returns></returns>
    Public Function GetTotalTime() As Integer
        Dim Time As Integer = 0
        For Each Block As ProgramBlock In m_Blocks
            Time += Block.TotalTime()
        Next
        Return Time
    End Function

    ''' <summary>
    ''' Returns time left before finish in sec.
    ''' </summary>
    ''' <returns></returns>
    Public Function GetTimeLeft() As Integer
        Dim TimeLeft As Integer = 0
        For i As Integer = m_CurrentIndex To m_Blocks.Count - 1
            TimeLeft += m_Blocks(i).GetTimeLeft()
        Next
        Return TimeLeft
    End Function

    ''' <summary>
    ''' Returns the washing machine
    ''' </summary>
    ''' <returns></returns>
    Public Function GetMachine() As WashingMachine
        Return m_Machine
    End Function

    ''' <summary>
    ''' Returns current execution index
    ''' </summary>
    ''' <returns></returns>
    Public Function GetCurrentBlockIndex() As Integer
        Return m_CurrentIndex
    End Function

    ''' <summary>
    ''' Returns all program blocks
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Blocks() As List(Of ProgramBlock)
        Get
            Return m_Blocks
        End Get
    End Property

    ''' <summary>
    ''' Returns time running
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GetTimeRunning() As Integer
        Get
            If m_Start = DateTime.MinValue Then Return 0
            Return (Now() - m_Start).TotalSeconds
        End Get
    End Property

    ''' <summary>
    ''' Returns the program type
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GeneralProgramType As ProgramManager.ProgramTypes
        Get
            Return m_GeneralType
        End Get
    End Property

    ''' <summary>
    ''' Returns current program options
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GeneralOptions As ProgramManager.ProgramOptions
        Get
            Return m_GeneralOptions
        End Get
    End Property

    ''' <summary>
    ''' Returns the genral temp used (some program blocks may use different temperature)
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GeneralTemp As Integer
        Get
            Return m_GeneralTemp
        End Get
    End Property

    ''' <summary>
    ''' Returns the end centrifuge RPM
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property GeneralRPM As Integer
        Get
            Return m_GeneralRPM
        End Get
    End Property

    ''' <summary>
    ''' Returns as json for display in webbrowser
    ''' </summary>
    ''' <returns></returns>
    Public Function toJSON() As String
        Dim JSON As String = "{ ""current_block"": " & m_CurrentIndex & ", ""time_left"": " & GetTimeLeft() & ", ""time_running"": " & GetTimeRunning() &
                              ", ""total_time"": " & GetTotalTime() & ", ""state"": " & m_State & ", ""err"": " & m_ErrorCode & ", ""blocks"": ["

        Dim BlockJSON As New List(Of String)
        For i As Integer = 0 To m_Blocks.Count - 1
            BlockJSON.Add(m_Blocks(i).ToJSON())
        Next

        JSON = JSON & Join(BlockJSON.ToArray(), ",") & "]}"

        Return JSON
    End Function

End Class
