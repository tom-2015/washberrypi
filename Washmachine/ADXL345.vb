Imports System.Threading
Imports WiringPiNet
Imports WiringPiNet.Wrapper

''' <summary>
''' This class helps driving the ADXL345 accelerometer which has a 32 sample FIFO 
''' </summary>
Public Class ADXL345

    'https://www.analog.com/media/en/technical-documentation/data-sheets/ADXL345.pdf
    ' ADXL345 device address
    Const ADXL345_DEVICE As Byte = &H53

    ' ADXL345 constants
    Const EARTH_GRAVITY_MS2 As Single = 9.80665
    Const SCALE_MULTIPLIER As Single = 0.004

    Const DATA_FORMAT As Byte = &H31
    Const BW_RATE As Byte = &H2C
    Const POWER_CTL As Byte = &H2D
    Const MEASURE As Byte = &H8
    Const AXES_DATA As Byte = &H32
    Const FIFO_CTL As Byte = &H38
    Const FIFO_STATUS As Byte = &H39

    Public Enum SampleRate As Byte
        BW_RATE_1600HZ = &HF
        BW_RATE_800HZ = &HE
        BW_RATE_400HZ = &HD
        BW_RATE_200HZ = &HC
        BW_RATE_100HZ = &HB
        BW_RATE_50HZ = &HA
        BW_RATE_25HZ = &H9
    End Enum

    Public Enum Ranges As Byte
        RANGE_2G = &H0
        RANGE_4G = &H1
        RANGE_8G = &H2
        RANGE_16G = &H3
    End Enum

    Public Enum FIFOModes As Byte
        FIFO_NONE = 0
        FIFO_FIFO = 1
        FIFO_STREAM = 2
        FIFO_TRIGGER = 3
    End Enum

    Public Event NewSample(ByVal Sample As GSample)

    Public Structure GSample
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public Sub New(ByVal X As Single, ByVal Y As Single, ByVal Z As Single)
            Me.X = X : Me.Y = Y : Me.Z = Z
        End Sub
    End Structure

    Dim i2c_handle As Integer
    Dim m_SampleTimer As Timer
    Dim m_SampleProcessThread As Thread
    Dim m_Samples As Queue(Of GSample)
    Dim m_EventsEnabled As Boolean

    Public Sub New()
        If Not PiGpio.InitializeGPIO() Then
            Console.WriteLine("PiGPIO init failed!")
        End If

        i2c_handle = PiGpio.I2cOpen(1, ADXL345_DEVICE)

        SetBandwidthRate(SampleRate.BW_RATE_400HZ)
        SetRange(Ranges.RANGE_8G)
        EnableMeasurement()
    End Sub

    ''' <summary>
    ''' Enables measurement
    ''' </summary>
    Public Function EnableMeasurement() As Boolean
        Return WriteReg(POWER_CTL, MEASURE)
    End Function

    ''' <summary>
    ''' Sets number 
    ''' </summary>
    ''' <param name="rate_flag"></param>
    Public Function SetBandwidthRate(ByVal rate_flag As Byte) As Boolean
        Return WriteReg(BW_RATE, rate_flag)
    End Function

    ''' <summary>
    ''' Sets the FIFO mode
    ''' </summary>
    ''' <param name="Mode"></param>
    ''' <returns></returns>
    Public Function SetFifoMode(ByVal Mode As FIFOModes) As Boolean
        Dim value As Byte = ReadReg(FIFO_CTL)
        value = value And &H1F
        value = value Or (Mode << 5)
        Return WriteReg(FIFO_CTL, value)
    End Function

    ''' <summary>
    ''' Returns number of FIFO samples available
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFifoSamplesAvailable() As Integer
        Dim Value As Byte = ReadReg(FIFO_STATUS)
        Return Value And &H7F
    End Function

    ''' <summary>
    ''' Set the measurement range
    ''' for 10 bit this is 0.004g per bit
    ''' </summary>
    ''' <param name="range_flag"></param>
    ''' <returns></returns>
    Public Function SetRange(ByVal range_flag As Byte) As Boolean
        Dim value As Byte = ReadReg(DATA_FORMAT)

        value = value And Not &HF
        value = value Or range_flag
        value = value Or &H8 'full res bit

        Return WriteReg(DATA_FORMAT, value)
    End Function

    ''' <summary>
    ''' Reads X,Y and Z sample from FIFO or DATA reg (depending on selected mode)
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <param name="z"></param>
    ''' <returns></returns>
    Public Function GetXYZ(ByRef x As Single, ByRef y As Single, ByRef z As Single) As Boolean
        Try
            Dim Bytes() As Byte = PiGpio.I2cReadI2cBlockData(i2c_handle, AXES_DATA, 6)

            x = BitConverter.ToInt16(Bytes, 0) * SCALE_MULTIPLIER
            y = BitConverter.ToInt16(Bytes, 2) * SCALE_MULTIPLIER
            z = BitConverter.ToInt16(Bytes, 4) * SCALE_MULTIPLIER

            Return True
        Catch e As PiGpio.BoardException

        End Try
        Return False
    End Function

    ''' <summary>
    ''' Returns X,Y,Z sample from FIFO or data reg
    ''' </summary>
    ''' <param name="Sample"></param>
    ''' <returns></returns>
    Public Function GetSample(ByRef Sample As GSample) As Boolean
        Return GetXYZ(Sample.X, Sample.Y, Sample.Z)
    End Function

    ''' <summary>
    ''' This sub will read samples from the queue and sends raisevent
    ''' </summary>
    Protected Sub ProcessSamplesThreadHelper()
        Try
            While m_EventsEnabled
                While m_Samples.Count > 0
                    Dim Sample As GSample
                    SyncLock m_Samples
                        Sample = m_Samples.Dequeue()
                    End SyncLock
                    RaiseEvent NewSample(Sample)
                End While
                Thread.Sleep(10)
            End While
        Catch e As ThreadAbortException

        End Try
    End Sub

    ''' <summary>
    ''' Enables events, sends an event for every measured sample
    ''' requires a lot of CPU!
    ''' </summary>
    Public Sub EnableEvents()
        If Not m_EventsEnabled Then
            m_EventsEnabled = True
            m_Samples = New Queue(Of GSample)
            m_SampleTimer = New Timer(Sub()
                                          Dim SamplesAvailable As Integer = GetFifoSamplesAvailable()
                                          Dim x As Single, y As Single, z As Single, idx As Integer
                                          While SamplesAvailable > 2 AndAlso SamplesAvailable < 33 AndAlso idx < 50
                                              'If SamplesAvailable >= 32 Then Console.WriteLine("OV")
                                              For i As Integer = 0 To SamplesAvailable - 1
                                                  If GetXYZ(x, y, z) Then
                                                      SyncLock m_Samples
                                                          m_Samples.Enqueue(New GSample(x, y, z))
                                                      End SyncLock
                                                  End If
                                              Next
                                              SamplesAvailable = GetFifoSamplesAvailable()
                                              idx += 1
                                          End While
                                      End Sub, Me, 0, 1)

            m_SampleProcessThread = New Thread(AddressOf ProcessSamplesThreadHelper)
            m_SampleProcessThread.Name = "ADXL345 sample thread"
            m_SampleProcessThread.Start()
        End If
    End Sub

    ''' <summary>
    ''' Stops gathering events
    ''' </summary>
    Public Sub DisableEvents()
        If m_SampleTimer IsNot Nothing Then
            m_EventsEnabled = False
            m_SampleTimer.Change(Timeout.Infinite, Timeout.Infinite)
            m_SampleTimer = Nothing
            m_SampleProcessThread.Abort()
            m_SampleProcessThread = Nothing
        End If
    End Sub

    ''' <summary>
    ''' Writes a reg of the accelerometer
    ''' </summary>
    ''' <param name="Reg"></param>
    ''' <param name="Data"></param>
    ''' <returns></returns>
    Public Function WriteReg(ByVal Reg As Byte, ByVal Data As Byte) As Boolean
        Try
            Return PiGpio.I2cWriteByteData(i2c_handle, Reg, Data) = PiGpio.ResultCode.Ok ' I2C.WiringPiI2CWriteReg8(i2c_handle, Reg, Data) > 0
        Catch e As PiGpio.BoardException
        End Try
        Return False
    End Function

    ''' <summary>
    ''' reads a reg from accelerometer
    ''' </summary>
    ''' <param name="Reg"></param>
    ''' <returns></returns>
    Public Function ReadReg(ByVal Reg As Byte) As Byte
        Dim Res As Byte
        Try
            Res = PiGpio.I2cReadByteData(i2c_handle, Reg)
        Catch e As PiGpio.BoardException
        End Try
        'I2C.WiringPiI2CReadReg8(i2c_handle, Res)
        Return Res
    End Function

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        DisableEvents()
    End Sub
End Class
