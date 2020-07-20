Imports WiringPiNet
Imports WiringPiNet.Wrapper

'https://github.com/Seeed-Studio/Accelerometer_MMA7660/blob/master/MMA7660.cpp

Public Class MMA7660

    Public Const MMA7660_ADDR As Byte = &H4C

    Public Const MMA7660_X As Byte = &H0
    Public Const MMA7660_Y As Byte = &H1
    Public Const MMA7660_Z As Byte = &H2
    Public Const MMA7660_TILT As Byte = &H3
    Public Const MMA7660_SRST As Byte = &H4
    Public Const MMA7660_SPCNT As Byte = &H5
    Public Const MMA7660_INTSU As Byte = &H6
    Public Const MMA7660_SHINTX As Byte = &H80
    Public Const MMA7660_SHINTY As Byte = &H40
    Public Const MMA7660_SHINTZ As Byte = &H20
    Public Const MMA7660_GINT As Byte = &H10
    Public Const MMA7660_ASINT As Byte = &H8
    Public Const MMA7660_PDINT As Byte = &H4
    Public Const MMA7660_PLINT As Byte = &H2
    Public Const MMA7660_FBINT As Byte = &H1
    Public Const MMA7660_MODE As Byte = &H7
    Public Const MMA7660_STAND_BY As Byte = &H0
    Public Const MMA7660_ACTIVE As Byte = &H1
    Public Const MMA7660_SR As Byte = &H8 ' sample rate register
    Public Const AUTO_SLEEP_120 As Byte = &H0 ' 120 sample per second
    Public Const AUTO_SLEEP_64 As Byte = &H1
    Public Const AUTO_SLEEP_32 As Byte = &H2
    Public Const AUTO_SLEEP_16 As Byte = &H3
    Public Const AUTO_SLEEP_8 As Byte = &H4
    Public Const AUTO_SLEEP_4 As Byte = &H5
    Public Const AUTO_SLEEP_2 As Byte = &H6
    Public Const AUTO_SLEEP_1 As Byte = &H7
    Public Const MMA7660_PDET As Byte = &H9
    Public Const MMA7660_PD As Byte = &HA

    Dim i2c_handle As Integer

    Public Sub New()
        InitAccelTable()
        i2c_handle = I2C.WiringPiI2CSetup(MMA7660_ADDR)
        SetMode(MMA7660_STAND_BY)
        setSampleRate(AUTO_SLEEP_120)
        SetMode(MMA7660_ACTIVE)
    End Sub

    Public Function SetMode(ByVal Mode As Byte) As Boolean
        Return WriteReg(MMA7660_MODE, Mode)
    End Function

    Public Function setSampleRate(ByVal Rate As Byte) As Boolean
        Return WriteReg(MMA7660_SR, Rate)
    End Function

    Public Function WriteReg(ByVal Reg As Byte, ByVal Data As Byte) As Boolean
        Return I2C.WiringPiI2CWriteReg8(i2c_handle, Reg, Data) > 0
    End Function

    Public Function ReadReg(ByVal Reg As Byte) As Byte
        Dim Res As Byte
        I2C.WiringPiI2CReadReg8(i2c_handle, Res)
        Return Res
    End Function

    Protected Sub InitAccelTable()

    End Sub

    Public Function getXYZ(ByRef x As Byte, ByRef y As Byte, ByRef z As Byte)

        x = I2C.WiringPiI2CReadReg8(i2c_handle, MMA7660_X) And &H7F
        y = I2C.WiringPiI2CReadReg8(i2c_handle, MMA7660_Y) And &H7F
        z = I2C.WiringPiI2CReadReg8(i2c_handle, MMA7660_Z) And &H7F

        Return True

    End Function

End Class
