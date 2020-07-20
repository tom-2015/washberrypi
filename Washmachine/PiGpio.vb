''' <summary>
''' this is a very basic class for using some PiGpio APIs without having to upgrade to .NET core 3 because that is what the library requires
''' </summary>
Public Class PiGpio


    ''' <summary>
    ''' Defines the different operation result codes from calling pigpio API.
    ''' 0 is OK. Anything negative is an error.
    ''' </summary>
    Public Enum ResultCode As Integer
        Ok = 0
        InitFailed = -1
        BadUserGpio = -2
        BadGpio = -3
        BadMode = -4
        BadLevel = -5
        BadPud = -6
        BadPulsewidth = -7
        BadDutycycle = -8
        BadTimer = -9
        BadMs = -10
        BadTimetype = -11
        BadSeconds = -12
        BadMicros = -13
        TimerFailed = -14
        BadWdogTimeout = -15
        NoAlertFunc = -16
        BadClkPeriph = -17
        BadClkSource = -18
        BadClkMicros = -19
        BadBufMillis = -20
        BadDutyrange = -21
        BadSignum = -22
        BadPathname = -23
        NoHandle = -24
        BadHandle = -25
        BadIfFlags = -26
        BadChannel = -27
        BadSocketPort = -28
        BadFifoCommand = -29
        BadSecoChannel = -30
        NotInitialised = -31
        Initialised = -32
        BadWaveMode = -33
        BadCfgInternal = -34
        BadWaveBaud = -35
        TooManyPulses = -36
        TooManyChars = -37
        NotSerialGpio = -38
        BadSerialStruc = -39
        BadSerialBuf = -40
        NotPermitted = -41
        SomePermitted = -42
        BadWvscCommnd = -43
        BadWvsmCommnd = -44
        BadWvspCommnd = -45
        BadPulselen = -46
        BadScript = -47
        BadScriptId = -48
        BadSerOffset = -49
        GpioInUse = -50
        BadSerialCount = -51
        BadParamNum = -52
        DupTag = -53
        TooManyTags = -54
        BadScriptCmd = -55
        BadVarNum = -56
        NoScriptRoom = -57
        NoMemory = -58
        SockReadFailed = -59
        SockWritFailed = -60
        TooManyParam = -61
        ScriptNotReady = -62
        BadTag = -63
        BadMicsDelay = -64
        BadMilsDelay = -65
        BadWaveId = -66
        TooManyCbs = -67
        TooManyOol = -68
        EmptyWaveform = -69
        NoWaveformId = -70
        I2cOpenFailed = -71
        SerOpenFailed = -72
        SpiOpenFailed = -73
        BadI2cBus = -74
        BadI2cAddr = -75
        BadSpiChannel = -76
        BadFlags = -77
        BadSpiSpeed = -78
        BadSerDevice = -79
        BadSerSpeed = -80
        BadParam = -81
        I2cWriteFailed = -82
        I2cReadFailed = -83
        BadSpiCount = -84
        SerWriteFailed = -85
        SerReadFailed = -86
        SerReadNoData = -87
        UnknownCommand = -88
        SpiXferFailed = -89
        BadPointer = -90
        NoAuxSpi = -91
        NotPwmGpio = -92
        NotServoGpio = -93
        NotHclkGpio = -94
        NotHpwmGpio = -95
        BadHpwmFreq = -96
        BadHpwmDuty = -97
        BadHclkFreq = -98
        BadHclkPass = -99
        HpwmIllegal = -100
        BadDatabits = -101
        BadStopbits = -102
        MsgToobig = -103
        BadMallocMode = -104
        TooManySegs = -105
        BadI2cSeg = -106
        BadSmbusCmd = -107
        NotI2cGpio = -108
        BadI2cWlen = -109
        BadI2cRlen = -110
        BadI2cCmd = -111
        BadI2cBaud = -112
        ChainLoopCnt = -113
        BadChainLoop = -114
        ChainCounter = -115
        BadChainCmd = -116
        BadChainDelay = -117
        ChainNesting = -118
        ChainTooBig = -119
        Deprecated = -120
        BadSerInvert = -121
        BadEdge = -122
        BadIsrInit = -123
        BadForever = -124
        BadFilter = -125
        BadPad = -126
        BadStrength = -127
        FilOpenFailed = -128
        BadFileMode = -129
        BadFileFlag = -130
        BadFileRead = -131
        BadFileWrite = -132
        FileNotRopen = -133
        FileNotWopen = -134
        BadFileSeek = -135
        NoFileMatch = -136
        NoFileAccess = -137
        FileIsADir = -138
        BadShellStatus = -139
        BadScriptName = -140
        BadSpiBaud = -141
        NotSpiGpio = -142
        BadEventId = -143
        CmdInterrupted = -144
        PigifErr0 = -2000
        PigifErr99 = -2099
        CustomErr0 = -3000
        CustomErr999 = -3999
    End Enum


    ''' <returns>Returns the pigpio version number if OK, otherwise PI_INIT_FAILED.</returns>
    Public Declare Function GpioInitialise Lib "libpigpio.so" Alias "gpioInitialise" () As ResultCode

    ''' <summary>
    ''' Clean up resources on program exit
    ''' </summary>
    Public Declare Sub GpioTerminate Lib "libpigpio.so" Alias "gpioTerminate" ()

    ''' <summary>
    ''' This returns a handle for the device at the address on the I2C bus.
    '''
    ''' No flags are currently defined.  This parameter should be set to zero.
    '''
    ''' Physically buses 0 and 1 are available on the Pi.  Higher numbered buses
    ''' will be available if a kernel supported bus multiplexor is being used.
    '''
    ''' For the SMBus commands the low level transactions are shown at the end
    ''' of the function description.  The following abbreviations are used.
    '''
    ''' </summary>
    ''' <remarks>
    ''' S      (1 bit) : Start bit
    ''' P      (1 bit) : Stop bit
    ''' Rd/Wr  (1 bit) : Read/Write bit. Rd equals 1, Wr equals 0.
    ''' A, NA  (1 bit) : Accept and not accept bit.
    ''' Addr   (7 bits): I2C 7 bit address.
    ''' i2cReg (8 bits): Command byte, a byte which often selects a register.
    ''' Data   (8 bits): A data byte.
    ''' Count  (8 bits): A byte defining the length of a block operation.
    '''
    ''' [..]: Data sent by the device.
    ''' </remarks>
    ''' <param name="i2cBus">>=0.</param>
    ''' <param name="i2cAddr">0-0x7F.</param>
    ''' <param name="i2cFlags">0.</param>
    ''' <returns>Returns a handle (>=0) if OK, otherwise PI_BAD_I2C_BUS, PI_BAD_I2C_ADDR, PI_BAD_FLAGS, PI_NO_HANDLE, or PI_I2C_OPEN_FAILED.</returns>
    Private Declare Function I2cOpenUnmanaged Lib "libpigpio.so" Alias "i2cOpen" (ByVal i2cBus As UInteger, ByVal i2cAddr As UInteger, ByVal i2cFlags As UInteger) As Integer



    ''' <summary>
    ''' This sends a single byte to the device associated with handle.
    '''
    ''' Send byte. SMBus 2.0 5.5.2.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] bVal [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="bVal">0-0xFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Private Declare Function I2cWriteByteUnmanaged Lib "libpigpio.so" Alias "i2cWriteByte" (ByVal handle As UIntPtr, ByVal bVal As UInteger) As ResultCode

    ''' <summary>
    ''' This reads a single byte from the device associated with handle.
    '''
    ''' Receive byte. SMBus 2.0 5.5.3.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Rd [A] [Data] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <returns>Returns the byte read (>=0) if OK, otherwise PI_BAD_HANDLE, or PI_I2C_READ_FAILED.</returns>
    Private Declare Function I2cReadByteUnmanaged Lib "libpigpio.so" Alias "i2cReadByte" (ByVal handle As UIntPtr) As Integer

    ''' <summary>
    ''' This writes a single byte to the specified register of the device
    ''' associated with handle.
    '''
    ''' Write byte. SMBus 2.0 5.5.4.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] bVal [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="i2cReg">0-255, the register to write.</param>
    ''' <param name="bVal">0-0xFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Private Declare Function I2cWriteByteDataUnmanaged Lib "libpigpio.so" Alias "i2cWriteByteData" (ByVal handle As UIntPtr, ByVal i2cReg As UInteger, ByVal bVal As UInteger) As ResultCode

    ''' <summary>
    ''' This writes a single 16 bit word to the specified register of the device
    ''' associated with handle.
    '''
    ''' Write word. SMBus 2.0 5.5.4.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] wValLow [A] wValHigh [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to write.</param>
    ''' <param name="word">0-0xFFFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Private Declare Function I2cWriteWordDataUnmanaged Lib "libpigpio.so" Alias "i2cWriteWordData" (ByVal handle As UIntPtr, ByVal register As UInteger, ByVal word As UInteger) As ResultCode

    ''' <summary>
    ''' This reads a single byte from the specified register of the device
    ''' associated with handle.
    '''
    ''' Read byte. SMBus 2.0 5.5.5.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] S Addr Rd [A] [Data] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <returns>Returns the byte read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Private Declare Function I2cReadByteDataUnmanaged Lib "libpigpio.so" Alias "i2cReadByteData" (ByVal handle As UIntPtr, ByVal register As UInteger) As Integer

    ''' <summary>
    ''' This reads a single 16 bit word from the specified register of the device
    ''' associated with handle.
    '''
    ''' Read word. SMBus 2.0 5.5.5.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] S Addr Rd [A] [DataLow] A [DataHigh] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <returns>Returns the word read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Private Declare Function I2cReadWordDataUnmanaged Lib "libpigpio.so" Alias "i2cReadWordData" (ByVal handle As UIntPtr, ByVal register As UInteger) As Integer


    ''' <summary>
    ''' This reads a block of up to 32 bytes from the specified register of
    ''' the device associated with handle.
    '''
    ''' The amount of returned data is set by the device.
    '''
    ''' Block read. SMBus 2.0 5.5.7.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A]
    '''    S Addr Rd [A] [Count] A [buf0] A [buf1] A ... A [bufn] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to .</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <param name="buffer">an array to receive the read data.</param>
    ''' <returns>Returns the number of bytes read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Private Declare Function I2cReadBlockDataUnmanaged Lib "libpigpio.so" Alias "i2cReadBlockData" (ByVal handle As UIntPtr, ByVal register As UInteger, ByVal buffer() As Byte) As Integer

    ''' <summary>
    ''' This reads count bytes from the specified register of the device
    ''' associated with handle .  The count may be 1-32.
    '''
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A]
    '''    S Addr Rd [A] [buf0] A [buf1] A ... A [bufn] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <param name="buffer">an array to receive the read data.</param>
    ''' <param name="count">1-32, the number of bytes to read.</param>
    ''' <returns>Returns the number of bytes read (>0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Private Declare Function I2cReadI2cBlockDataUnmanaged Lib "libpigpio.so" Alias "i2cReadI2CBlockData" (ByVal handle As UIntPtr, ByVal register As UInteger, ByVal buffer() As Byte, ByVal count As UInteger) As Integer

    ''' <summary>
    ''' This function sets the current library internal configuration
    ''' settings.
    '''
    ''' </summary>
    ''' <param name="configFlags">see source code.</param>
    ''' <returns>The result code. 0 for success. See the <see cref="ResultCode"/> enumeration.</returns>
    Public Declare Function GpioCfgSetInternals Lib "libpigpio.so" Alias "gpioCfgSetInternals" (ByVal configFlags As ConfigFlags) As ResultCode

    ''' <summary>
    ''' This function returns the current library internal configuration
    ''' settings.
    ''' </summary>
    ''' <returns>The result code. 0 for success. See the <see cref="ResultCode"/> enumeration.</returns>
    Public Declare Function GpioCfgGetInternals Lib "libpigpio.so" Alias "gpioCfgGetInternals" () As ConfigFlags

    ''' <summary>
    ''' Enumerates the different configuration flags.
    ''' </summary>
    <Flags()>
    Public Enum ConfigFlags As UInteger
        DebugLevel0 = 1
        DebugLevel1 = (1 << 1)
        DebugLevel2 = (1 << 2)
        DebugLevel3 = (1 << 3)
        AlertFrequency0 = (1 << 4)
        AlertFrequency1 = (1 << 5)
        AlertFrequency2 = (1 << 6)
        AlertFrequency3 = (1 << 7)
        RealTimePriority = (1 << 8)
        Stats = (1 << 9)
        NoSignalHandler = (1 << 10)
    End Enum

    Public Shared Function InitializeGPIO() As Boolean

        Dim config As ConfigFlags = GpioCfgGetInternals()

        ' config = config.ApplyBits(false, 3, 2, 1, 0); // Clear debug flags

        config = config Or ConfigFlags.NoSignalHandler

        If GpioCfgSetInternals(config) <> ResultCode.Ok Then
            Return False
        End If

        Dim Res As ResultCode = PiGpio.GpioInitialise()
        If Res <= 0 Then
            Throw New Exception("PiGPIO init failed! Resut: " & Res) 'sometimes failes, raspberry pi power off is required (reboot doesn't fix problem)
        End If
        Return True
    End Function


    ''' <summary>
    ''' This returns a handle for the device at the address on the I2C bus.
    '''
    ''' No flags are currently defined.  This parameter should be set to zero.
    '''
    ''' Physically buses 0 and 1 are available on the Pi.  Higher numbered buses
    ''' will be available if a kernel supported bus multiplexor is being used.
    '''
    ''' For the SMBus commands the low level transactions are shown at the end
    ''' of the function description.  The following abbreviations are used.
    '''
    ''' </summary>
    ''' <remarks>
    ''' S      (1 bit) : Start bit
    ''' P      (1 bit) : Stop bit
    ''' Rd/Wr  (1 bit) : Read/Write bit. Rd equals 1, Wr equals 0.
    ''' A, NA  (1 bit) : Accept and not accept bit.
    ''' Addr   (7 bits): I2C 7 bit address.
    ''' i2cReg (8 bits): Command byte, a byte which often selects a register.
    ''' Data   (8 bits): A data byte.
    ''' Count  (8 bits): A byte defining the length of a block operation.
    '''
    ''' [..]: Data sent by the device.
    ''' </remarks>
    ''' <param name="i2cBus">>=0.</param>
    ''' <param name="i2cAddress">0-0x7F.</param>
    ''' <returns>Returns a handle (>=0) if OK, otherwise PI_BAD_I2C_BUS, PI_BAD_I2C_ADDR, PI_BAD_FLAGS, PI_NO_HANDLE, or PI_I2C_OPEN_FAILED.</returns>
    Public Shared Function I2cOpen(ByVal i2cBus As UInteger, ByVal i2cAddress As UInteger) As UIntPtr
        Dim result = BoardException.ValidateResult(I2cOpenUnmanaged(i2cBus, i2cAddress, 0))
        Return New UIntPtr(CType(result, UInteger))
    End Function

    ''' <summary>
    ''' This sends a single byte to the device associated with handle.
    '''
    ''' Send byte. SMBus 2.0 5.5.2.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] bVal [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="value">0-0xFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Public Shared Function I2cWriteByte(ByVal handle As UIntPtr, ByVal value As Byte) As ResultCode
        Return I2cWriteByteUnmanaged(handle, value)
    End Function

    ''' <summary>
    ''' This reads a single byte from the device associated with handle.
    '''
    ''' Receive byte. SMBus 2.0 5.5.3.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Rd [A] [Data] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <returns>Returns the byte read (>=0) if OK, otherwise PI_BAD_HANDLE, or PI_I2C_READ_FAILED.</returns>
    Public Shared Function I2cReadByte(ByVal handle As UIntPtr) As Byte
        Dim result = BoardException.ValidateResult(I2cReadByteUnmanaged(handle))
        Return CType(result, Byte)
    End Function

    ''' <summary>
    ''' This writes a single byte to the specified register of the device
    ''' associated with handle.
    '''
    ''' Write byte. SMBus 2.0 5.5.4.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] bVal [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to write.</param>
    ''' <param name="value">0-0xFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Public Shared Function I2cWriteByteData(ByVal handle As UIntPtr, ByVal register As Byte, ByVal value As Byte) As ResultCode
        Return I2cWriteByteDataUnmanaged(handle, register, value)
    End Function

    ''' <summary>
    ''' This writes a single 16 bit word to the specified register of the device
    ''' associated with handle.
    '''
    ''' Write word. SMBus 2.0 5.5.4.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] wValLow [A] wValHigh [A] P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to write.</param>
    ''' <param name="word">0-0xFFFF, the value to write.</param>
    ''' <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_WRITE_FAILED.</returns>
    Public Shared Function I2cWriteWordData(ByVal handle As UIntPtr, ByVal register As Byte, ByVal word As System.UInt16) As ResultCode
        Return I2cWriteWordDataUnmanaged(handle, register, word)
    End Function

    ''' <summary>
    ''' This reads a single byte from the specified register of the device
    ''' associated with handle.
    '''
    ''' Read byte. SMBus 2.0 5.5.5.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] S Addr Rd [A] [Data] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <returns>Returns the byte read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Public Shared Function I2cReadByteData(ByVal handle As UIntPtr, ByVal register As Byte) As Byte
        Dim result = BoardException.ValidateResult(I2cReadByteDataUnmanaged(handle, register))
        If result > 255 OrElse result < 0 Then Return 0
        Return Convert.ToByte(result)
    End Function

    ''' <summary>
    ''' This reads a single 16 bit word from the specified register of the device
    ''' associated with handle.
    '''
    ''' Read word. SMBus 2.0 5.5.5.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A] S Addr Rd [A] [DataLow] A [DataHigh] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to <see cref="I2cOpen"/>.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <returns>Returns the word read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Public Shared Function I2cReadWordData(ByVal handle As UIntPtr, ByVal register As Byte) As System.UInt16
        Dim result = BoardException.ValidateResult(I2cReadWordDataUnmanaged(handle, register))
        Return Convert.ToUInt16(result)
    End Function


    ''' <summary>
    ''' This reads a block of up to 32 bytes from the specified register of
    ''' the device associated with handle.
    '''
    ''' The amount of returned data is set by the device.
    '''
    ''' Block read. SMBus 2.0 5.5.7.
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A]
    '''    S Addr Rd [A] [Count] A [buf0] A [buf1] A ... A [bufn] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <returns>Returns the number of bytes read (>=0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Public Shared Function I2cReadBlockData(ByVal handle As UIntPtr, ByVal register As Byte) As Byte()
        Dim buffer = New Byte((32) - 1) {}
        Dim count = BoardException.ValidateResult(I2cReadBlockDataUnmanaged(handle, register, buffer))
        If (count = buffer.Length) Then
            Return buffer
        End If

        Dim output = New Byte((count) - 1) {}
        Array.Copy(buffer, 0, output, 0, output.Length)
        Return output
    End Function

    ''' <summary>
    ''' This reads count bytes from the specified register of the device
    ''' associated with handle .  The count may be 1-32.
    '''
    ''' </summary>
    ''' <remarks>
    ''' S Addr Wr [A] i2cReg [A]
    '''    S Addr Rd [A] [buf0] A [buf1] A ... A [bufn] NA P.
    ''' </remarks>
    ''' <param name="handle">>=0, as returned by a call to.</param>
    ''' <param name="register">0-255, the register to read.</param>
    ''' <param name="count">The amount of bytes to read from 1 to 32.</param>
    ''' <returns>Returns the number of bytes read (>0) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_I2C_READ_FAILED.</returns>
    Public Shared Function I2cReadI2cBlockData(ByVal handle As UIntPtr, ByVal register As Byte, ByVal count As Integer) As Byte()
        Dim buffer = New Byte((count) - 1) {}
        Dim result = BoardException.ValidateResult(I2cReadI2cBlockDataUnmanaged(handle, register, buffer, Convert.ToUInt32(buffer.Length)))
        If (result = buffer.Length) Then
            Return buffer
        End If

        Dim output = New Byte((result) - 1) {}
        Array.Copy(buffer, 0, output, 0, output.Length)
        Return output
    End Function




    ''' <summary>
    ''' Represents a PiGpio Library call exception.
    ''' </summary>
    ''' <seealso cref="Exception" />
    Public Class BoardException
        Inherits Exception

        ''' <summary>
        ''' Initializes a new instance of the <see cref="BoardException"/> class.
        ''' </summary>
        ''' <param name="resultCode">The result code.</param>
        Private Sub New(ByVal resultCode As Integer)
            MyBase.New(BoardException.GetStarndardMessage(resultCode))
            resultCode = CType(resultCode, ResultCode)
        End Sub

        ''' <summary>
        ''' Gets the result code.
        ''' </summary>
        Public ResultCode As ResultCode

        ''' <summary>
        ''' Validates the result. This call is typically used for Setter methods.
        ''' </summary>
        ''' <param name="resultCode">The result code.</param>
        ''' <returns>The Result Code.</returns>
        Friend Overloads Shared Function ValidateResult(ByVal resultCode As ResultCode) As ResultCode
            Return CType(BoardException.ValidateResult(CType(resultCode, Integer)), ResultCode)
        End Function

        ''' <summary>
        ''' Validates the result. This call is typically used for Getter methods.
        ''' </summary>
        ''' <param name="resultCode">The result code.</param>
        ''' <returns>The integer result.</returns>
        Friend Overloads Shared Function ValidateResult(ByVal resultCode As Integer) As Integer
            If (resultCode < 0) Then
                Throw New BoardException(resultCode)
            End If

            Return resultCode
        End Function

        ''' <summary>
        ''' Validates the result. This call is typically used for execute methods.
        ''' </summary>
        ''' <param name="handle">The handle.</param>
        ''' <returns>The pointer or handle.</returns>
        Friend Overloads Shared Function ValidateResult(ByVal handle As UIntPtr) As UIntPtr
            If (handle = UIntPtr.Zero) Then
                Throw New BoardException(CType(ResultCode.BadHandle, Integer))
            End If

            Return handle
        End Function

        ''' <summary>
        ''' Gets the starndard message.
        ''' </summary>
        ''' <param name="resultCode">The result code.</param>
        ''' <returns>The standard corresponding error message based on the result code.</returns>
        Private Shared Function GetStarndardMessage(ByVal resultCode As Integer) As String
            Return ("Hardware Exception Encountered. Error Code {resultCode}: {(ResultCode)resultCode}: " + "{Constants.GetResultCodeMessage(resultCode)}")
        End Function
    End Class


End Class
