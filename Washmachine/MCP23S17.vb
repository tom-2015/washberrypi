Imports WiringPiNet.Wrapper.SPI

''' <summary>
''' This class is used to communicate with the MCP23S17 used for driving the display and buttons
''' Requires WiringPiNet library
''' </summary>
Public Class MCP23S17

    Protected m_Fd As Integer
    Protected m_Channel As Integer
    Protected m_Address As Integer

    Public Const REG_IODIRA As Integer = 0
    Public Const REG_IODIRB As Integer = 1
    Public Const REG_IOPOLA As Integer = 2
    Public Const REG_IOPOLB As Integer = 3
    Public Const REG_GPINTENA As Integer = 4
    Public Const REG_GPINTENB As Integer = 5
    Public Const REG_DEFVALA As Integer = 6
    Public Const REG_DEFVALB As Integer = 7
    Public Const REG_INTCONA As Integer = 8
    Public Const REG_INTCONB As Integer = 9
    Public Const REG_IOCON As Integer = &HA
    Public Const REG_GPPUA As Integer = &HC
    Public Const REG_GPPUB As Integer = &HD
    Public Const REG_INTFA As Integer = &HE
    Public Const REG_INTFB As Integer = &HF
    Public Const REG_INTCAPA As Integer = &H10
    Public Const REG_INTCAPB As Integer = &H11
    Public Const REG_GPIOA As Integer = &H12
    Public Const REG_GPIOB As Integer = &H13
    Public Const REG_OLATA As Integer = &H14
    Public Const REG_OLATB As Integer = &H15

    Public Sub New(ByVal SpiChannel As Integer, ByVal SpiSpeed As Integer, ByVal Address As Integer)
        m_Fd = WiringPiSPISetup(SpiChannel, SpiSpeed)
        m_Channel = SpiChannel
        m_Address = Address
    End Sub

    ''' <summary>
    ''' Simple write reg
    ''' </summary>
    ''' <param name="Reg"></param>
    ''' <param name="Value"></param>
    ''' <returns></returns>
    Public Function WriteReg(ByVal Reg As Integer, ByVal Value As Byte) As Boolean
        Dim Data(0 To 2) As Byte
        Data(0) = 64 + m_Address
        Data(1) = Reg
        Data(2) = Value
        Return WiringPiSPIDataRW(m_Channel, Data, Data.Length) <> -1
    End Function

    ''' <summary>
    ''' Writes 2 regs (for writing to port A and B in one instruction)
    ''' </summary>
    ''' <param name="Reg"></param>
    ''' <param name="Value"></param>
    ''' <returns></returns>
    Public Function WriteRegs(ByVal Reg As Integer, ByVal Value As UInt16) As Boolean
        Dim Data(0 To 3) As Byte
        Data(0) = 64 + m_Address
        Data(1) = Reg
        Data(2) = Value And &HFF
        Data(3) = Value >> 8
        Return WiringPiSPIDataRW(m_Channel, Data, Data.Length) <> -1
    End Function

    ''' <summary>
    ''' Simple read reg
    ''' </summary>
    ''' <param name="Reg"></param>
    ''' <returns></returns>
    Public Function ReadReg(ByVal Reg As Integer) As Integer
        Dim Data(0 To 2) As Byte
        Data(0) = 64 + m_Address + 1
        Data(1) = Reg

        If WiringPiSPIDataRW(m_Channel, Data, Data.Length) <> -1 Then
            Return Data(2)
        End If
        Return -1
    End Function

    Public ReadOnly Property SpiChannel() As Integer
        Get
            Return m_Channel
        End Get
    End Property

End Class
