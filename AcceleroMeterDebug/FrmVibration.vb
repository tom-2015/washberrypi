Imports System.Drawing
Imports System.Net
Imports System.Net.Sockets

''' <summary>
''' This form can be used for debugging / determine the accelerometer threshold values
''' </summary>

Public Class FrmVibration


    Public Socket As UdpClient

    Public Structure AccSample
        Public Counter As UInt32
        Public X As Integer
        Public Y As Integer
        Public Z As Integer
        Public HighPassX As Single
        Public HighPassY As Single
        Public HighPassZ As Single
        Public CounterX As Integer
        Public CounterY As Integer
        Public CounterZ As Integer
    End Structure

    Public SampleLock As New Object
    Public Samples() As AccSample
    Public ChartSizeX As Integer
    Public ChartSizeY As Integer
    Public UnbalanceAlarm As Integer

    ''' <summary>
    ''' Draws a line on the chart for visualising threshold values
    ''' </summary>
    Public ThresholdX As Single = 1.3
    Public ThresholdY As Single = 2.4
    Public ThresholdZ As Single = 2.3

    Dim Zoom As Single = 50

    Private Sub FrmVibration_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ChartSizeX = Me.Width
        ChartSizeY = 200
        ReDim Samples(0 To ChartSizeX - 1)

        Socket = New UdpClient(2005)
        Socket.BeginReceive(New AsyncCallback(AddressOf ReceiveUDPData), Nothing)
    End Sub


    Private Sub FrmVibration_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        SyncLock SampleLock
            ChartSizeX = Me.Width
            ChartSizeY = 200

            ReDim Preserve Samples(0 To ChartSizeX - 1)
        End SyncLock
    End Sub

    Private Sub NewSample(Sample As AccSample)
        SyncLock SampleLock
            For i As Integer = 0 To ChartSizeX - 2
                Samples(i) = Samples(i + 1)
            Next
            Samples(ChartSizeX - 1) = Sample
        End SyncLock
    End Sub

    Public Sub UpdateChart(ByVal Gfx As Graphics)
        'SyncLock SampleLock
        Gfx.Clear(Color.DarkBlue)
        For idx = 0 To 2

            Dim DotPen As New Pen(Color.Yellow)
            Dim ChartPen As New Pen(Color.Yellow)
            Dim FilterPen As New Pen(Color.Red)
            DotPen.DashStyle = Drawing2D.DashStyle.Dash
            Gfx.DrawLine(DotPen, New Point(0, idx * ChartSizeY + ChartSizeY / 2), New Point(ChartSizeX, idx * ChartSizeY + ChartSizeY / 2))
            Select Case idx
                Case 0
                    Gfx.DrawLine(New Pen(Color.Green), New Point(0, GetY(idx, ThresholdX)), New Point(ChartSizeX, GetY(idx, ThresholdX)))
                Case 1
                    Gfx.DrawLine(New Pen(Color.Green), New Point(0, GetY(idx, ThresholdY)), New Point(ChartSizeX, GetY(idx, ThresholdY)))
                Case 2
                    Gfx.DrawLine(New Pen(Color.Green), New Point(0, GetY(idx, ThresholdZ)), New Point(ChartSizeX, GetY(idx, ThresholdZ)))
            End Select



            Dim HighPassValueY As Single
            Dim PHighPassValueY As Single
            Dim y1 As Single
            Dim y2 As Single

            For i As Integer = 0 To ChartSizeX - 2

                Select Case idx
                    Case 0
                        y1 = Samples(i).X
                        y2 = Samples(i + 1).X
                        HighPassValueY = Samples(i).HighPassX
                        PHighPassValueY = Samples(i + 1).HighPassX
                    Case 1
                        y1 = Samples(i).Y
                        y2 = Samples(i + 1).Y
                        HighPassValueY = Samples(i).HighPassY
                        PHighPassValueY = Samples(i + 1).HighPassY
                    Case 2
                        y1 = Samples(i).Z
                        y2 = Samples(i + 1).Z
                        HighPassValueY = Samples(i).HighPassZ
                        PHighPassValueY = Samples(i + 1).HighPassZ
                End Select

                Gfx.DrawLine(ChartPen, New Point(i, GetY(idx, y1)), New Point(i + 1, GetY(idx, y2)))
                Gfx.DrawLine(FilterPen, New Point(i, GetY(idx, HighPassValueY)), New Point(i + 1, GetY(idx, PHighPassValueY)))

            Next
        Next
        ' End SyncLock
    End Sub

    Private Function GetY(ByVal ChartIndex As Integer, ByVal SampleVal As Single) As Integer
        Return ChartIndex * ChartSizeY + ChartSizeY - ChartSizeY * (SampleVal * Zoom / 0.004 + 32768) / UInt16.MaxValue
    End Function

    Private Sub TmrRefresh_Tick(sender As Object, e As EventArgs) Handles TmrRefresh.Tick
        Me.Refresh()
    End Sub

    'Private Sub Button1_Click(sender As Object, e As EventArgs)
    '    Zoom = TxtPort.Text
    'End Sub




    Private Sub ReceiveUDPData(ByVal SocketReceiveResult As IAsyncResult)
        Dim RemoteEndPoint As New IPEndPoint(New IPAddress(0), 0)
        Dim Sample As New AccSample

        Dim Datagram() As Byte = Socket.EndReceive(SocketReceiveResult, RemoteEndPoint)

        Sample.Counter = BitConverter.ToUInt32(Datagram, 0)
        Sample.X = BitConverter.ToSingle(Datagram, 4)
        Sample.Y = BitConverter.ToSingle(Datagram, 8)
        Sample.Z = BitConverter.ToSingle(Datagram, 12)
        Sample.HighPassX = BitConverter.ToSingle(Datagram, 16)
        Sample.HighPassY = BitConverter.ToSingle(Datagram, 20)
        Sample.HighPassZ = BitConverter.ToSingle(Datagram, 24)

        Sample.CounterX = BitConverter.ToInt32(Datagram, 28)
        Sample.CounterY = BitConverter.ToInt32(Datagram, 32)
        Sample.CounterZ = BitConverter.ToInt32(Datagram, 36)

        NewSample(Sample)
        Socket.BeginReceive(New AsyncCallback(AddressOf ReceiveUDPData), Nothing)
    End Sub


    Private Sub FrmVibration_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        UpdateChart(e.Graphics)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Zoom = Zoom - 1
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Zoom = Zoom + 1
    End Sub
End Class