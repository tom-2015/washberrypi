<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmTest
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.TmrRefresh = New System.Windows.Forms.Timer(Me.components)
        Me.LblMotorSpeed = New System.Windows.Forms.Label()
        Me.LblTemp = New System.Windows.Forms.Label()
        Me.LblWater = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.CmdStartWash = New System.Windows.Forms.Button()
        Me.CmdStartCentrifuge = New System.Windows.Forms.Button()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.TxtWantedTemp = New System.Windows.Forms.TextBox()
        Me.CmdReset = New System.Windows.Forms.Button()
        Me.TxtInfo = New System.Windows.Forms.TextBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.LblLoad = New System.Windows.Forms.Label()
        Me.TmrTime = New System.Windows.Forms.Timer(Me.components)
        Me.LstProgress = New System.Windows.Forms.ListBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.TxtRPM = New System.Windows.Forms.TextBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.TxtPower = New System.Windows.Forms.TextBox()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.LblAccelX = New System.Windows.Forms.Label()
        Me.LblAccelY = New System.Windows.Forms.Label()
        Me.LblAccelZ = New System.Windows.Forms.Label()
        Me.LblTimeLeft = New System.Windows.Forms.Label()
        Me.LblTotalTime = New System.Windows.Forms.Label()
        Me.LblTimeRunning = New System.Windows.Forms.Label()
        Me.LblXMax = New System.Windows.Forms.Label()
        Me.LblYMax = New System.Windows.Forms.Label()
        Me.LblZMax = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.LblZAvg = New System.Windows.Forms.Label()
        Me.LblYAvg = New System.Windows.Forms.Label()
        Me.LblXAvg = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button5 = New System.Windows.Forms.Button()
        Me.Button6 = New System.Windows.Forms.Button()
        Me.Button7 = New System.Windows.Forms.Button()
        Me.CmbProgramType = New System.Windows.Forms.ComboBox()
        Me.ChWaitFinalCentrifuge = New System.Windows.Forms.CheckBox()
        Me.SuspendLayout()
        '
        'TmrRefresh
        '
        Me.TmrRefresh.Interval = 1000
        '
        'LblMotorSpeed
        '
        Me.LblMotorSpeed.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblMotorSpeed.Location = New System.Drawing.Point(280, 9)
        Me.LblMotorSpeed.Name = "LblMotorSpeed"
        Me.LblMotorSpeed.Size = New System.Drawing.Size(259, 48)
        Me.LblMotorSpeed.TabIndex = 0
        Me.LblMotorSpeed.Text = "0"
        '
        'LblTemp
        '
        Me.LblTemp.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblTemp.Location = New System.Drawing.Point(280, 57)
        Me.LblTemp.Name = "LblTemp"
        Me.LblTemp.Size = New System.Drawing.Size(259, 48)
        Me.LblTemp.TabIndex = 1
        Me.LblTemp.Text = "0"
        '
        'LblWater
        '
        Me.LblWater.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblWater.Location = New System.Drawing.Point(280, 105)
        Me.LblWater.Name = "LblWater"
        Me.LblWater.Size = New System.Drawing.Size(259, 48)
        Me.LblWater.TabIndex = 2
        Me.LblWater.Text = "0"
        '
        'Label1
        '
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(259, 48)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Toerental:"
        '
        'Label2
        '
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(15, 57)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(259, 48)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Temperatuur:"
        '
        'Label3
        '
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(15, 105)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(259, 48)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "Water:"
        '
        'CmdStartWash
        '
        Me.CmdStartWash.Location = New System.Drawing.Point(32, 345)
        Me.CmdStartWash.Name = "CmdStartWash"
        Me.CmdStartWash.Size = New System.Drawing.Size(167, 68)
        Me.CmdStartWash.TabIndex = 6
        Me.CmdStartWash.Text = "Start wassen"
        Me.CmdStartWash.UseVisualStyleBackColor = True
        '
        'CmdStartCentrifuge
        '
        Me.CmdStartCentrifuge.Location = New System.Drawing.Point(32, 434)
        Me.CmdStartCentrifuge.Name = "CmdStartCentrifuge"
        Me.CmdStartCentrifuge.Size = New System.Drawing.Size(167, 68)
        Me.CmdStartCentrifuge.TabIndex = 7
        Me.CmdStartCentrifuge.Text = "Start centrifugeren"
        Me.CmdStartCentrifuge.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(29, 253)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(37, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Temp:"
        '
        'TxtWantedTemp
        '
        Me.TxtWantedTemp.Location = New System.Drawing.Point(86, 250)
        Me.TxtWantedTemp.Name = "TxtWantedTemp"
        Me.TxtWantedTemp.Size = New System.Drawing.Size(113, 20)
        Me.TxtWantedTemp.TabIndex = 9
        Me.TxtWantedTemp.Text = "60"
        '
        'CmdReset
        '
        Me.CmdReset.Location = New System.Drawing.Point(32, 533)
        Me.CmdReset.Name = "CmdReset"
        Me.CmdReset.Size = New System.Drawing.Size(167, 68)
        Me.CmdReset.TabIndex = 10
        Me.CmdReset.Text = "Reset"
        Me.CmdReset.UseVisualStyleBackColor = True
        '
        'TxtInfo
        '
        Me.TxtInfo.Location = New System.Drawing.Point(246, 253)
        Me.TxtInfo.Multiline = True
        Me.TxtInfo.Name = "TxtInfo"
        Me.TxtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TxtInfo.Size = New System.Drawing.Size(405, 195)
        Me.TxtInfo.TabIndex = 11
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(32, 632)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(167, 68)
        Me.Button1.TabIndex = 13
        Me.Button1.Text = "Test"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.Location = New System.Drawing.Point(15, 153)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(259, 48)
        Me.Label5.TabIndex = 14
        Me.Label5.Text = "Belasting:"
        '
        'LblLoad
        '
        Me.LblLoad.Font = New System.Drawing.Font("Microsoft Sans Serif", 27.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblLoad.Location = New System.Drawing.Point(280, 153)
        Me.LblLoad.Name = "LblLoad"
        Me.LblLoad.Size = New System.Drawing.Size(259, 48)
        Me.LblLoad.TabIndex = 15
        Me.LblLoad.Text = "0"
        '
        'TmrTime
        '
        Me.TmrTime.Enabled = True
        '
        'LstProgress
        '
        Me.LstProgress.FormattingEnabled = True
        Me.LstProgress.Location = New System.Drawing.Point(246, 460)
        Me.LstProgress.Name = "LstProgress"
        Me.LstProgress.Size = New System.Drawing.Size(404, 186)
        Me.LstProgress.TabIndex = 18
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(29, 281)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(44, 13)
        Me.Label6.TabIndex = 19
        Me.Label6.Text = "Toeren:"
        '
        'TxtRPM
        '
        Me.TxtRPM.Location = New System.Drawing.Point(86, 276)
        Me.TxtRPM.Name = "TxtRPM"
        Me.TxtRPM.Size = New System.Drawing.Size(113, 20)
        Me.TxtRPM.TabIndex = 20
        Me.TxtRPM.Text = "1400"
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(246, 671)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(99, 23)
        Me.Button2.TabIndex = 21
        Me.Button2.Text = "Run At Power:"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'TxtPower
        '
        Me.TxtPower.Location = New System.Drawing.Point(351, 674)
        Me.TxtPower.Name = "TxtPower"
        Me.TxtPower.Size = New System.Drawing.Size(100, 20)
        Me.TxtPower.TabIndex = 22
        Me.TxtPower.Text = "25"
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(673, 307)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(134, 43)
        Me.Button3.TabIndex = 23
        Me.Button3.Text = "Gordijnen"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'LblAccelX
        '
        Me.LblAccelX.AutoSize = True
        Me.LblAccelX.Location = New System.Drawing.Point(649, 105)
        Me.LblAccelX.Name = "LblAccelX"
        Me.LblAccelX.Size = New System.Drawing.Size(14, 13)
        Me.LblAccelX.TabIndex = 30
        Me.LblAccelX.Text = "X"
        '
        'LblAccelY
        '
        Me.LblAccelY.AutoSize = True
        Me.LblAccelY.Location = New System.Drawing.Point(649, 128)
        Me.LblAccelY.Name = "LblAccelY"
        Me.LblAccelY.Size = New System.Drawing.Size(14, 13)
        Me.LblAccelY.TabIndex = 31
        Me.LblAccelY.Text = "Y"
        '
        'LblAccelZ
        '
        Me.LblAccelZ.AutoSize = True
        Me.LblAccelZ.Location = New System.Drawing.Point(649, 153)
        Me.LblAccelZ.Name = "LblAccelZ"
        Me.LblAccelZ.Size = New System.Drawing.Size(14, 13)
        Me.LblAccelZ.TabIndex = 32
        Me.LblAccelZ.Text = "Z"
        '
        'LblTimeLeft
        '
        Me.LblTimeLeft.AutoSize = True
        Me.LblTimeLeft.Location = New System.Drawing.Point(649, 9)
        Me.LblTimeLeft.Name = "LblTimeLeft"
        Me.LblTimeLeft.Size = New System.Drawing.Size(34, 13)
        Me.LblTimeLeft.TabIndex = 33
        Me.LblTimeLeft.Text = "00:00"
        '
        'LblTotalTime
        '
        Me.LblTotalTime.AutoSize = True
        Me.LblTotalTime.Location = New System.Drawing.Point(649, 33)
        Me.LblTotalTime.Name = "LblTotalTime"
        Me.LblTotalTime.Size = New System.Drawing.Size(34, 13)
        Me.LblTotalTime.TabIndex = 34
        Me.LblTotalTime.Text = "00:00"
        '
        'LblTimeRunning
        '
        Me.LblTimeRunning.AutoSize = True
        Me.LblTimeRunning.Location = New System.Drawing.Point(649, 57)
        Me.LblTimeRunning.Name = "LblTimeRunning"
        Me.LblTimeRunning.Size = New System.Drawing.Size(34, 13)
        Me.LblTimeRunning.TabIndex = 35
        Me.LblTimeRunning.Text = "00:00"
        '
        'LblXMax
        '
        Me.LblXMax.AutoSize = True
        Me.LblXMax.Location = New System.Drawing.Point(706, 105)
        Me.LblXMax.Name = "LblXMax"
        Me.LblXMax.Size = New System.Drawing.Size(13, 13)
        Me.LblXMax.TabIndex = 39
        Me.LblXMax.Text = "0"
        '
        'LblYMax
        '
        Me.LblYMax.AutoSize = True
        Me.LblYMax.Location = New System.Drawing.Point(706, 129)
        Me.LblYMax.Name = "LblYMax"
        Me.LblYMax.Size = New System.Drawing.Size(13, 13)
        Me.LblYMax.TabIndex = 40
        Me.LblYMax.Text = "0"
        '
        'LblZMax
        '
        Me.LblZMax.AutoSize = True
        Me.LblZMax.Location = New System.Drawing.Point(706, 153)
        Me.LblZMax.Name = "LblZMax"
        Me.LblZMax.Size = New System.Drawing.Size(13, 13)
        Me.LblZMax.TabIndex = 41
        Me.LblZMax.Text = "0"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(609, 105)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(14, 13)
        Me.Label13.TabIndex = 42
        Me.Label13.Text = "X"
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(609, 129)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(14, 13)
        Me.Label14.TabIndex = 43
        Me.Label14.Text = "Y"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(609, 153)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(14, 13)
        Me.Label15.TabIndex = 44
        Me.Label15.Text = "Z"
        '
        'LblZAvg
        '
        Me.LblZAvg.AutoSize = True
        Me.LblZAvg.Location = New System.Drawing.Point(770, 153)
        Me.LblZAvg.Name = "LblZAvg"
        Me.LblZAvg.Size = New System.Drawing.Size(13, 13)
        Me.LblZAvg.TabIndex = 47
        Me.LblZAvg.Text = "0"
        '
        'LblYAvg
        '
        Me.LblYAvg.AutoSize = True
        Me.LblYAvg.Location = New System.Drawing.Point(770, 129)
        Me.LblYAvg.Name = "LblYAvg"
        Me.LblYAvg.Size = New System.Drawing.Size(13, 13)
        Me.LblYAvg.TabIndex = 46
        Me.LblYAvg.Text = "0"
        '
        'LblXAvg
        '
        Me.LblXAvg.AutoSize = True
        Me.LblXAvg.Location = New System.Drawing.Point(770, 105)
        Me.LblXAvg.Name = "LblXAvg"
        Me.LblXAvg.Size = New System.Drawing.Size(13, 13)
        Me.LblXAvg.TabIndex = 45
        Me.LblXAvg.Text = "0"
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(648, 81)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(15, 13)
        Me.Label10.TabIndex = 48
        Me.Label10.Text = "G"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(704, 81)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(35, 13)
        Me.Label11.TabIndex = 49
        Me.Label11.Text = "GMax"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(768, 81)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(34, 13)
        Me.Label12.TabIndex = 50
        Me.Label12.Text = "GAvg"
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(732, 227)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(75, 23)
        Me.Button4.TabIndex = 51
        Me.Button4.Text = "Show Chart"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button5
        '
        Me.Button5.Location = New System.Drawing.Point(695, 172)
        Me.Button5.Name = "Button5"
        Me.Button5.Size = New System.Drawing.Size(44, 23)
        Me.Button5.TabIndex = 52
        Me.Button5.Text = "Reset"
        Me.Button5.UseVisualStyleBackColor = True
        '
        'Button6
        '
        Me.Button6.Location = New System.Drawing.Point(673, 370)
        Me.Button6.Name = "Button6"
        Me.Button6.Size = New System.Drawing.Size(134, 43)
        Me.Button6.TabIndex = 53
        Me.Button6.Text = "Dagelijkse was"
        Me.Button6.UseVisualStyleBackColor = True
        '
        'Button7
        '
        Me.Button7.Location = New System.Drawing.Point(673, 430)
        Me.Button7.Name = "Button7"
        Me.Button7.Size = New System.Drawing.Size(167, 68)
        Me.Button7.TabIndex = 54
        Me.Button7.Text = "Kookwas"
        Me.Button7.UseVisualStyleBackColor = True
        '
        'CmbProgramType
        '
        Me.CmbProgramType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.CmbProgramType.FormattingEnabled = True
        Me.CmbProgramType.Items.AddRange(New Object() {"Dagelijkse Was", "Vlekken", "Extra vuil", "Centrifugeren", "Spoelen"})
        Me.CmbProgramType.Location = New System.Drawing.Point(32, 223)
        Me.CmbProgramType.Name = "CmbProgramType"
        Me.CmbProgramType.Size = New System.Drawing.Size(167, 21)
        Me.CmbProgramType.TabIndex = 55
        '
        'ChWaitFinalCentrifuge
        '
        Me.ChWaitFinalCentrifuge.AutoSize = True
        Me.ChWaitFinalCentrifuge.Location = New System.Drawing.Point(32, 302)
        Me.ChWaitFinalCentrifuge.Name = "ChWaitFinalCentrifuge"
        Me.ChWaitFinalCentrifuge.Size = New System.Drawing.Size(75, 17)
        Me.ChWaitFinalCentrifuge.TabIndex = 56
        Me.ChWaitFinalCentrifuge.Text = "SpoelStop"
        Me.ChWaitFinalCentrifuge.UseVisualStyleBackColor = True
        '
        'FrmTest
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(873, 748)
        Me.Controls.Add(Me.ChWaitFinalCentrifuge)
        Me.Controls.Add(Me.CmbProgramType)
        Me.Controls.Add(Me.Button7)
        Me.Controls.Add(Me.Button6)
        Me.Controls.Add(Me.Button5)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.LblZAvg)
        Me.Controls.Add(Me.LblYAvg)
        Me.Controls.Add(Me.LblXAvg)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.LblZMax)
        Me.Controls.Add(Me.LblYMax)
        Me.Controls.Add(Me.LblXMax)
        Me.Controls.Add(Me.LblTimeRunning)
        Me.Controls.Add(Me.LblTotalTime)
        Me.Controls.Add(Me.LblTimeLeft)
        Me.Controls.Add(Me.LblAccelZ)
        Me.Controls.Add(Me.LblAccelY)
        Me.Controls.Add(Me.LblAccelX)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.TxtPower)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.TxtRPM)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.LstProgress)
        Me.Controls.Add(Me.LblLoad)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.TxtInfo)
        Me.Controls.Add(Me.CmdReset)
        Me.Controls.Add(Me.TxtWantedTemp)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.CmdStartCentrifuge)
        Me.Controls.Add(Me.CmdStartWash)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.LblWater)
        Me.Controls.Add(Me.LblTemp)
        Me.Controls.Add(Me.LblMotorSpeed)
        Me.Name = "FrmTest"
        Me.Text = "FrmTest"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TmrRefresh As Windows.Forms.Timer
    Friend WithEvents LblMotorSpeed As Windows.Forms.Label
    Friend WithEvents LblTemp As Windows.Forms.Label
    Friend WithEvents LblWater As Windows.Forms.Label
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents CmdStartWash As Windows.Forms.Button
    Friend WithEvents CmdStartCentrifuge As Windows.Forms.Button
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents TxtWantedTemp As Windows.Forms.TextBox
    Friend WithEvents CmdReset As Windows.Forms.Button
    Friend WithEvents TxtInfo As Windows.Forms.TextBox
    Friend WithEvents Button1 As Windows.Forms.Button
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents LblLoad As Windows.Forms.Label
    Friend WithEvents TmrTime As Windows.Forms.Timer
    Friend WithEvents LstProgress As Windows.Forms.ListBox
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents TxtRPM As Windows.Forms.TextBox
    Friend WithEvents Button2 As Windows.Forms.Button
    Friend WithEvents TxtPower As Windows.Forms.TextBox
    Friend WithEvents Button3 As Windows.Forms.Button
    Friend WithEvents LblAccelX As Windows.Forms.Label
    Friend WithEvents LblAccelY As Windows.Forms.Label
    Friend WithEvents LblAccelZ As Windows.Forms.Label
    Friend WithEvents LblTimeLeft As Windows.Forms.Label
    Friend WithEvents LblTotalTime As Windows.Forms.Label
    Friend WithEvents LblTimeRunning As Windows.Forms.Label
    Friend WithEvents LblXMax As Windows.Forms.Label
    Friend WithEvents LblYMax As Windows.Forms.Label
    Friend WithEvents LblZMax As Windows.Forms.Label
    Friend WithEvents Label13 As Windows.Forms.Label
    Friend WithEvents Label14 As Windows.Forms.Label
    Friend WithEvents Label15 As Windows.Forms.Label
    Friend WithEvents LblZAvg As Windows.Forms.Label
    Friend WithEvents LblYAvg As Windows.Forms.Label
    Friend WithEvents LblXAvg As Windows.Forms.Label
    Friend WithEvents Label10 As Windows.Forms.Label
    Friend WithEvents Label11 As Windows.Forms.Label
    Friend WithEvents Label12 As Windows.Forms.Label
    Friend WithEvents Button4 As Windows.Forms.Button
    Friend WithEvents Button5 As Windows.Forms.Button
    Friend WithEvents Button6 As Windows.Forms.Button
    Friend WithEvents Button7 As Windows.Forms.Button
    Friend WithEvents CmbProgramType As Windows.Forms.ComboBox
    Friend WithEvents ChWaitFinalCentrifuge As Windows.Forms.CheckBox
End Class
