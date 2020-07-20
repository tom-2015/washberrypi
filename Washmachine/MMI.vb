Imports WiringPiNet.Wrapper.SPI
Imports System.Threading
Imports WiringPiNet


''' <summary>
''' the Man Machine Interface class
''' used for driving the buttons and displays using 2 MCP23S17 ICs
''' </summary>
Public Class MMI

    Public PrintDebugData As Boolean = True

    'output led matrix of 9 row x 10 columns bits, see Eagle schematic MMI.sh file
    Dim m_Bits(0 To 9) As Integer
    Dim m_MCP1 As MCP23S17
    Dim m_MCP2 As MCP23S17
    Dim m_Rendering As Boolean 'true if rendering the m_Bits to output is on, this will drive leds in each column sequentially
    Dim m_RenderingThread As Thread
    Dim m_UpdateDisplay As Boolean = True

    '7 Segment lookup
    Dim m_SegmentLookup(0 To 17) As Byte

    Dim m_GPIO As PiGpio
    Dim m_BuzzerPin As GpioPin

    Public Enum Buttons As Integer
        RotationButtonUp = 1
        RotationButtonDown = 2
        PowerButton = 3
        PlayPauseButton = 4
        TempButton = 5
        CentrifugeButton = 6
        OptionsButton = 7
        DelayedEndButton = 8
    End Enum

    ''' <summary>
    ''' Current selected values on the MMI display
    ''' </summary>
    Dim m_SelectedProgramType As ProgramManager.ProgramTypes = ProgramManager.ProgramTypes.DailyWash
    Dim m_SelectedOptions As ProgramManager.ProgramOptions = ProgramManager.ProgramOptions.None
    Dim m_SelectedRPM As Integer = 1400
    Dim m_SelectedTemp As Integer = 30
    Dim m_Machine As WashingMachine


    Public Event DebugEvent(ByVal MMI As MMI, ByVal Message As String)
    Public Event ButtonPressed(ByVal Button As Buttons, ByVal Val As Boolean)

    Public Sub New(ByVal Machine As WashingMachine)

        m_BuzzerPin = New GpioPin(Machine.GPIO, 26)

        DisableBuzzer()

        m_MCP1 = New MCP23S17(0, 10000000, 0)
        m_MCP2 = New MCP23S17(1, 10000000, 0)

        'write default registers, configure Inputs and outputs of the port expander ICs
        If Not m_MCP1.WriteReg(MCP23S17.REG_IODIRA, 0) Then Console.WriteLine("SPI Error MCP1")
        If Not m_MCP1.WriteReg(MCP23S17.REG_IODIRB, 0) Then Console.WriteLine("SPI Error MCP1")
        If Not m_MCP2.WriteReg(MCP23S17.REG_IODIRA, &HF8) Then Console.WriteLine("SPI Error MCP2")
        If Not m_MCP2.WriteReg(MCP23S17.REG_IODIRB, &HFF) Then Console.WriteLine("SPI Error MCP2")
        If Not m_MCP2.WriteReg(MCP23S17.REG_GPPUA, &HF8) Then Console.WriteLine("SPI Error MCP2")
        If Not m_MCP2.WriteReg(MCP23S17.REG_GPPUB, &HFF) Then Console.WriteLine("SPI Error MCP2")
        If Not m_MCP1.WriteReg(MCP23S17.REG_GPIOA, 0) Then Console.WriteLine("SPI Error MCP1")
        If Not m_MCP1.WriteReg(MCP23S17.REG_GPIOB, 0) Then Console.WriteLine("SPI Error MCP1")
        If Not m_MCP2.WriteReg(MCP23S17.REG_GPIOA, 0) Then Console.WriteLine("SPI Error MCP2")
        If Not m_MCP2.WriteReg(MCP23S17.REG_GPIOB, 0) Then Console.WriteLine("SPI Error MCP2")

        m_MCP1.WriteReg(MCP23S17.REG_DEFVALA, &HAA) 'write a value and read it in, if it doesn't match there is a problem with the port expander
        If m_MCP1.ReadReg(MCP23S17.REG_DEFVALA) <> &HAA Then Console.WriteLine("SPI Error MCP1, reg def a= " & m_MCP1.ReadReg(MCP23S17.REG_DEFVALA))

        m_MCP2.WriteReg(MCP23S17.REG_DEFVALA, &HAA)
        If m_MCP2.ReadReg(MCP23S17.REG_DEFVALA) <> &HAA Then Console.WriteLine("SPI Error MCP2, reg def a= " & m_MCP2.ReadReg(MCP23S17.REG_DEFVALA))


        m_SegmentLookup(0) = &H3F '0111111
        m_SegmentLookup(1) = &H6  '0000110
        m_SegmentLookup(2) = &H5B '1011011
        m_SegmentLookup(3) = &H4F '1001111
        m_SegmentLookup(4) = &H66 '1100110
        m_SegmentLookup(5) = &H6D '1101101
        m_SegmentLookup(6) = &H7D '1111101
        m_SegmentLookup(7) = &H7  '0000111
        m_SegmentLookup(8) = &H7F '1111111
        m_SegmentLookup(9) = &H6F '1101111
        m_SegmentLookup(14) = &H79 '1111001 -> display a small o, used to indicate CPU is busy with accelerometer, it would be better to use DMA to drive the LED matrix but didn't have time do to
        m_SegmentLookup(17) = 0

        m_Rendering = True
        m_RenderingThread = New Thread(AddressOf RenderDisplay)
        m_RenderingThread.IsBackground = True
        m_RenderingThread.Name = "Display render"
        m_RenderingThread.Start()

        'update the display bits
        SetProgramOptions(m_SelectedOptions)
        SetProgram(m_SelectedProgramType)
        SetCentrifuge(m_SelectedRPM)
        SetTemp(m_SelectedTemp)

    End Sub

    ''' <summary>
    ''' Enables the buzzer, output 1kHz
    ''' </summary>
    Public Sub EnableBuzzer()
        m_BuzzerPin.SetMode(PinMode.PwmToneOutput)
        m_BuzzerPin.SetClock(1000)
        m_BuzzerPin.WritePwm(512)
    End Sub

    ''' <summary>
    ''' Stops buzzing
    ''' </summary>
    Public Sub DisableBuzzer()
        m_BuzzerPin.SetMode(PinMode.Input)
    End Sub

    ''' <summary>
    ''' Removes everything from 7 segment display
    ''' </summary>
    Public Sub Clear7Segments()
        Set7Segment(0, 17)
        Set7Segment(1, 17)
        Set7Segment(2, 17)
        Set7Segment(3, 17)
        Set7Segment(4, 17)
    End Sub

    ''' <summary>
    ''' Sets the program options leds, not everything implemented yet
    ''' </summary>
    ''' <param name="Options"></param>
    Public Sub SetProgramOptions(ByVal Options As ProgramManager.ProgramOptions)
        m_SelectedOptions = Options
        If Options And ProgramManager.ProgramOptions.WaitForFinalCentrifuge Then
            SetDelayLed(True)
        Else
            SetDelayLed(False)
        End If
    End Sub

    ''' <summary>
    ''' Sets a 7 segment value
    ''' 0 -> most left 7 segment (only can display a 1 symbol)
    ''' </summary>
    ''' <param name="SegmentIndex">0-3</param>
    ''' <param name="Value"></param>
    Public Sub Set7Segment(ByVal SegmentIndex As Integer, ByVal Value As Byte)
        If SegmentIndex = 0 Then
            If Value <> 1 Then Value = 17 '17 = off
        End If
        m_Bits(SegmentIndex) = m_SegmentLookup(Value) Or (m_Bits(SegmentIndex) And &HFF80)
    End Sub

    ''' <summary>
    ''' Sets the colon of the display
    ''' </summary>
    ''' <param name="Value"></param>
    Public Sub SetColon(ByVal Value As Boolean)
        If Value Then
            m_Bits(0) = m_Bits(0) Or &H60 '1100000
        Else
            m_Bits(0) = m_Bits(0) And Not &H60
        End If
    End Sub

    ''' <summary>
    ''' Sets the delayed end options led
    ''' </summary>
    ''' <param name="Value"></param>
    Public Sub SetDelayLed(ByVal Value As Boolean)
        If Value Then
            m_Bits(4) = m_Bits(4) Or 32
        Else
            m_Bits(4) = m_Bits(4) And Not 32
        End If
    End Sub

    ''' <summary>
    ''' Sets centrifuge speed led
    ''' </summary>
    ''' <param name="Speed">0-1400</param>
    Public Sub SetCentrifuge(ByVal Speed As Integer)
        m_SelectedRPM = Speed
        Dim Bit As Integer
        Dim Mask As Integer = &H7C '1111100
        Select Case Speed
            Case 0
                Bit = 4
            Case <= 400
                Bit = 64
            Case <= 800
                Bit = 32
            Case <= 1200
                Bit = 16
            Case Else
                Bit = 8
        End Select
        m_Bits(6) = (m_Bits(6) And Not Mask) Or Bit
    End Sub

    ''' <summary>
    ''' Sets temperature selection led
    ''' </summary>
    ''' <param name="Temp">0-90</param>
    Public Sub SetTemp(ByVal Temp As Integer)
        Dim Bit As Integer
        Dim Mask As Integer = &H7C '1111100
        m_SelectedTemp = Temp
        Select Case Temp
            Case <= 20
                Bit = 4
            Case <= 30
                Bit = 64
            Case <= 40
                Bit = 32
            Case <= 60
                Bit = 16
            Case Else
                Bit = 8
        End Select
        m_Bits(7) = (m_Bits(7) And Not Mask) Or Bit
    End Sub

    ''' <summary>
    ''' Sets the selected program (rotating knob)
    ''' Not all programs are implemented
    ''' </summary>
    ''' <param name="ProgramType"></param>
    Public Sub SetProgram(ByVal ProgramType As ProgramManager.ProgramTypes)
        m_SelectedProgramType = ProgramType
        Dim ColIndex As Integer = 8
        Dim ProgramIndex As Integer
        Select Case ProgramType
            Case ProgramManager.ProgramTypes.Centrifuge
                ProgramIndex = 1
            Case ProgramManager.ProgramTypes.DailyWash
                ProgramIndex = 10
            Case ProgramManager.ProgramTypes.Delicate
                ProgramIndex = 2
            Case ProgramManager.ProgramTypes.ExtraDirty
                ProgramIndex = 11
            Case ProgramManager.ProgramTypes.Fast
                ProgramIndex = 8
            Case ProgramManager.ProgramTypes.Rinse
                ProgramIndex = 0
        End Select
        Dim Bit As Integer
        Dim Mask As Integer = &H7E '1111110
        Select Case ProgramIndex
            Case 0
                Bit = 8
            Case 1
                Bit = 16
            Case 2
                Bit = 32
            Case 3
                Bit = 4
            Case 4
                Bit = 64
            Case 5
                Bit = 2
            Case 6
                Bit = 2
                ColIndex = 9
            Case 7
                Bit = 64
                ColIndex = 9
            Case 8
                Bit = 4
                ColIndex = 9
            Case 9
                Bit = 32
                ColIndex = 9
            Case 10
                Bit = 16
                ColIndex = 9
            Case 11
                Bit = 8
                ColIndex = 9
        End Select
        m_Bits(8) = (m_Bits(8) And Not Mask)
        m_Bits(9) = (m_Bits(9) And Not Mask)
        m_Bits(ColIndex) = m_Bits(ColIndex) Or Bit
    End Sub

    ''' <summary>
    ''' starts program with selected settings
    ''' </summary>
    Private Sub StartProgram()
        Machine.StartProgram(Machine.ProgramManager.GenerateProgram(m_SelectedProgramType, m_SelectedTemp, m_SelectedRPM, m_SelectedOptions))
    End Sub

    ''' <summary>
    ''' Stops the program
    ''' </summary>
    Private Sub StopProgram()
        If Machine.Program IsNot Nothing Then
            Machine.Program.StopExecute()
            Machine.Program = Nothing
        End If
    End Sub

    ''' <summary>
    ''' This sub processed the event of a button press
    ''' </summary>
    ''' <param name="Button"></param>
    ''' <param name="Val"></param>
    Private Sub SendButtonEvent(ByVal Button As Buttons, ByVal Val As Boolean)
        Dim t As New Thread(Sub()
                                RaiseEvent ButtonPressed(Button, Val) 'forward event to listeners
                                If Val Then
                                    If Machine.Program Is Nothing OrElse Machine.Program.State = Program.ProgramStates.Finished Then 'check if a program is running
                                        Select Case Button
                                            Case MMI.Buttons.CentrifugeButton 'speed button
                                                Select Case m_SelectedRPM
                                                    Case 0
                                                        SetCentrifuge(400)
                                                    Case <= 400
                                                        SetCentrifuge(800)
                                                    Case <= 800
                                                        SetCentrifuge(1200)
                                                    Case <= 1200
                                                        SetCentrifuge(1400)
                                                    Case <= 1400
                                                        SetCentrifuge(0)
                                                End Select
                                            Case MMI.Buttons.DelayedEndButton 'option button for waiting final centrifuge
                                                If m_SelectedOptions And ProgramManager.ProgramOptions.WaitForFinalCentrifuge Then
                                                    SetProgramOptions(m_SelectedOptions And Not ProgramManager.ProgramOptions.WaitForFinalCentrifuge)
                                                Else
                                                    SetProgramOptions(m_SelectedOptions Or ProgramManager.ProgramOptions.WaitForFinalCentrifuge)
                                                End If
                                            Case MMI.Buttons.OptionsButton

                                            Case Buttons.PlayPauseButton 'start
                                                If Machine.Program Is Nothing Then
                                                    RaiseEvent DebugEvent(Me, "Starting new program")
                                                    StartProgram()
                                                Else
                                                    RaiseEvent DebugEvent(Me, "Ending program")
                                                    StopProgram()
                                                End If
                                            Case MMI.Buttons.PowerButton 'the power button
                                                Clear7Segments()
                                                Console.WriteLine("Shutting down.")
                                            'Shell("shutdown -h now")
                                            Case MMI.Buttons.RotationButtonDown 'rotation button, select program
                                                Select Case m_SelectedProgramType
                                                    Case ProgramManager.ProgramTypes.Centrifuge
                                                        SetProgram(ProgramManager.ProgramTypes.Rinse)
                                                    Case ProgramManager.ProgramTypes.DailyWash
                                                        SetProgram(ProgramManager.ProgramTypes.Fast)
                                                    Case ProgramManager.ProgramTypes.Delicate
                                                        SetProgram(ProgramManager.ProgramTypes.Centrifuge)
                                                    Case ProgramManager.ProgramTypes.ExtraDirty
                                                        SetProgram(ProgramManager.ProgramTypes.DailyWash)
                                                    Case ProgramManager.ProgramTypes.Fast
                                                        SetProgram(ProgramManager.ProgramTypes.Delicate)
                                                    Case ProgramManager.ProgramTypes.Rinse

                                                End Select
                                            Case MMI.Buttons.RotationButtonUp 'roation button, select program
                                                Select Case m_SelectedProgramType
                                                    Case ProgramManager.ProgramTypes.Centrifuge
                                                        SetProgram(ProgramManager.ProgramTypes.Delicate)
                                                    Case ProgramManager.ProgramTypes.DailyWash
                                                        SetProgram(ProgramManager.ProgramTypes.ExtraDirty)
                                                    Case ProgramManager.ProgramTypes.Delicate
                                                        SetProgram(ProgramManager.ProgramTypes.Fast)
                                                    Case ProgramManager.ProgramTypes.ExtraDirty

                                                    Case ProgramManager.ProgramTypes.Fast
                                                        SetProgram(ProgramManager.ProgramTypes.DailyWash)
                                                    Case ProgramManager.ProgramTypes.Rinse
                                                        SetProgram(ProgramManager.ProgramTypes.Centrifuge)
                                                End Select
                                            Case MMI.Buttons.TempButton 'temp button
                                                Select Case m_SelectedTemp
                                                    Case <= 20
                                                        SetTemp(30)
                                                    Case <= 30
                                                        SetTemp(40)
                                                    Case <= 40
                                                        SetTemp(60)
                                                    Case <= 60
                                                        SetTemp(90)
                                                    Case Else
                                                        SetTemp(20)
                                                End Select
                                        End Select
                                    Else
                                        If Machine.Program.State = Program.ProgramStates.Waiting Then 'if the wait for final centrifuge is set
                                            If Button = Buttons.PlayPauseButton Then
                                                If Val Then
                                                    Machine.Program.State = Program.ProgramStates.Running
                                                End If
                                            End If
                                        ElseIf Machine.Program.State = Program.ProgramStates.Error OrElse Machine.Program.State = Program.ProgramStates.Aborted Then 'press power button to start again
                                            If Button = Buttons.PowerButton Then
                                                If Val Then
                                                    RaiseEvent DebugEvent(Me, "Power button pressed, resetting.")
                                                    Machine.StopProgram()
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End Sub) With {
            .IsBackground = True
                            }
        t.Start()
    End Sub

    ''' <summary>
    ''' Temporarly disables the display to get more CPU resources for capturing accelerometer packets
    ''' displays a small o on display
    ''' using the DMA controller this would not be needed to do
    ''' </summary>
    Public Sub SuspendDisplay()
        Dim Data(0 To 3) As Byte
        Dim Channel1 As Integer
        Dim Channel2 As Integer

        m_UpdateDisplay = False

        Thread.Sleep(100)

        Channel1 = m_MCP1.SpiChannel
        Channel2 = m_MCP2.SpiChannel

        'Reset col if i > 6
        Data(0) = 64
        Data(1) = MCP23S17.REG_GPIOA
        Data(2) = 0
        WiringPiSPIDataRW(Channel2, Data, 3)

        'first write MCP1
        Data(0) = 64
        Data(1) = MCP23S17.REG_GPIOA
        Data(2) = &H23 '0100011
        Data(3) = &H10 '10000
        WiringPiSPIDataRW(Channel1, Data, 4) 'note: overwrites data! 

    End Sub

    ''' <summary>
    ''' Resumes display
    ''' </summary>
    Public Sub ResumeDisplay()
        m_UpdateDisplay = True
    End Sub

    ''' <summary>
    ''' This will render the display in a seperate thread
    ''' </summary>
    Private Sub RenderDisplay()
        Dim Data(0 To 3) As Byte
        Dim Channel1 As Integer
        Dim Channel2 As Integer
        Dim ColBit As Integer
        Dim Bits As Integer
        Dim ButtonBits As Integer
        Dim PButtonBits As Integer
        Dim PRotationButton As Integer
        Dim PPowerButton As Integer
        Dim PPlayPauseButton As Integer
        Dim PTempButton As Integer
        Dim PCentrifugeButton As Integer
        Dim POptionsButton As Integer
        Dim PDelayedEndButton As Integer
        Dim StartingUp As Boolean = True

        Channel1 = m_MCP1.SpiChannel
        Channel2 = m_MCP2.SpiChannel

        While Volatile.Read(m_Rendering)
            If m_UpdateDisplay Then
                For i As Integer = 0 To m_Bits.Count - 1

                    'Reset col if i > 6
                    If i = 0 OrElse i > 6 Then
                        Data(0) = 64
                        Data(1) = MCP23S17.REG_GPIOA
                        Data(2) = 0
                        WiringPiSPIDataRW(Channel2, Data, 3)
                    End If

                    'first write MCP1
                    Data(0) = 64
                    Data(1) = MCP23S17.REG_GPIOA
                    ColBit = (1 << (i + 1))
                    Bits = Not (m_Bits(i))
                    Data(2) = Bits And &HFF '(Notm_Bits(i))
                    Data(3) = ((Bits And &H100) >> 8) Or (ColBit And &HFF)
                    WiringPiSPIDataRW(Channel1, Data, 4) 'note: overwrites data! 

                    'write MCP2
                    If i > 6 Then
                        Data(0) = 64
                        Data(1) = MCP23S17.REG_GPIOA
                        Data(2) = (ColBit And &H700) >> 8
                        WiringPiSPIDataRW(Channel2, Data, 3)
                    End If

                    'read buttons
                    Data(0) = 65
                    Data(1) = MCP23S17.REG_GPIOA
                    WiringPiSPIDataRW(Channel2, Data, 4)

                    ButtonBits = (Data(2) And &HF8) Or ((Data(3) And 7) << 8)

                    If ButtonBits <> PButtonBits Then

                        If (ButtonBits And 8) <> PPlayPauseButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.PlayPauseButton, (ButtonBits And 8) = 0)
                            PPlayPauseButton = ButtonBits And 8
                        End If

                        If (ButtonBits And 16) <> PPowerButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.PowerButton, (ButtonBits And 16) = 0)
                            PPowerButton = ButtonBits And 16
                        End If

                        If (ButtonBits And 64) <> PRotationButton Then
                            If (ButtonBits And 64) = 0 Then
                                If (ButtonBits And &H60) = 32 Then
                                    If Not StartingUp Then SendButtonEvent(Buttons.RotationButtonUp, True)
                                ElseIf (ButtonBits And &H60) = 0 Then
                                    If Not StartingUp Then SendButtonEvent(Buttons.RotationButtonDown, True)
                                End If
                            End If
                            PRotationButton = ButtonBits And 64
                        End If

                        If (ButtonBits And 128) <> PDelayedEndButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.DelayedEndButton, (ButtonBits And 128) = 0)
                            PDelayedEndButton = ButtonBits And 128
                        End If

                        If (ButtonBits And 256) <> POptionsButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.OptionsButton, (ButtonBits And 256) = 0)
                            POptionsButton = ButtonBits And 256
                        End If

                        If (ButtonBits And 512) <> PCentrifugeButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.CentrifugeButton, (ButtonBits And 512) = 0)
                            PCentrifugeButton = ButtonBits And 512
                        End If

                        If (ButtonBits And 1024) <> PTempButton Then
                            If Not StartingUp Then SendButtonEvent(Buttons.TempButton, (ButtonBits And 1024) = 0)
                            PTempButton = ButtonBits And 1024
                        End If
                    End If
                    StartingUp = False

                    PButtonBits = ButtonBits

                    Thread.Sleep(1)
                Next
            Else
                Thread.Sleep(1000)
            End If
        End While

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        Volatile.Write(m_Rendering, False)
    End Sub
End Class
