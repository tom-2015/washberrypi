
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Text
Imports System.Collections.Specialized


''' <summary>
''' Wrapper around the HttpListener class
''' </summary>
Public Class WebServer

    Dim m_Listener As HttpListener
    Dim m_Port As Integer
    Dim m_Directories As New List(Of String)
    Dim m_MimeTypes As Dictionary(Of String, String)


    Public Class HttpRequestParameters
        Public IsProcessed As Boolean 'turn true to prevent hosting files in the configured directory
    End Class

    ''' <summary>
    ''' Fired when new incoming request
    ''' </summary>
    ''' <param name="Server"></param>
    ''' <param name="Context"></param>
    ''' <param name="Params"></param>
    Public Event HttpRequest(ByVal Server As WebServer, ByVal Context As HttpListenerContext, ByVal Params As HttpRequestParameters)


    Public Sub New()
    End Sub

    Public Sub Listen(ByVal Port As Integer)
        m_Port = Port

        If m_Listener IsNot Nothing Then
            StopListen()
        End If

        m_MimeTypes = New Dictionary(Of String, String)(System.StringComparison.OrdinalIgnoreCase)
        m_MimeTypes.Add("avi", "video/avi")
        m_MimeTypes.Add("css", "text/css")
        m_MimeTypes.Add("exe", "application/octet-stream")
        m_MimeTypes.Add("gif", "image/gif")
        m_MimeTypes.Add("htm", "text/html")
        m_MimeTypes.Add("html", "text/html")
        m_MimeTypes.Add("jpg", "image/jpeg")
        m_MimeTypes.Add("jpeg", "image/jpeg")
        m_MimeTypes.Add("js", "application/javascript")
        m_MimeTypes.Add("json", "application/json")
        m_MimeTypes.Add("log", "text/plain")
        m_MimeTypes.Add("mid", "audio/midi")
        m_MimeTypes.Add("mov", "video/quicktime")
        m_MimeTypes.Add("mp3", "audio/mpeg3")
        m_MimeTypes.Add("mpeg", "video/mpeg")
        m_MimeTypes.Add("png", "image/png")
        m_MimeTypes.Add("txt", "text/plain")
        m_MimeTypes.Add("zip", "application/x-compressed")
        m_MimeTypes.Add("ico", "image/vnd.microsoft.icon")

        m_Listener = New HttpListener()

        m_Listener.Prefixes.Add("http://*:" & m_Port & "/")
        m_Listener.Start()
        m_Listener.BeginGetContext(New AsyncCallback(AddressOf ListenHelper), m_Listener)

    End Sub

    Public Sub AddDirectory(ByVal Path As String)
        m_Directories.Add(Path)
    End Sub

    ''' <summary>
    ''' Async listener
    ''' </summary>
    ''' <param name="Result"></param>
    Protected Sub ListenHelper(ByVal Result As IAsyncResult)
        Dim Listener As HttpListener = CType(Result.AsyncState, HttpListener)
        Dim Context As HttpListenerContext = m_Listener.EndGetContext(Result)
        Listener.BeginGetContext(New AsyncCallback(AddressOf ListenHelper), Listener)

        Dim Params As New HttpRequestParameters

        RaiseEvent HttpRequest(Me, Context, Params)

        If Not Params.IsProcessed Then

            For Each Dir As String In m_Directories
                Dim FileName As String = Dir & Context.Request.Url.AbsolutePath

                If File.Exists(FileName) Then
                    Dim Ext As String = ""
                    Dim Idx As Integer = FileName.LastIndexOf(".")

                    If Idx > 0 AndAlso FileName.Length > Idx + 1 Then
                        Ext = FileName.Substring(Idx + 1)
                    End If

                    If Ext.Length > 1 AndAlso m_MimeTypes.ContainsKey(Ext.ToLower()) Then
                        Context.Response.ContentType = m_MimeTypes(Ext.ToLower())
                    Else
                        Context.Response.ContentType = "application/octet-stream"
                    End If

                    Context.Response.StatusCode = 200

                    Context.Response.ContentLength64 = FileLen(FileName)
                    SendFile(Context, FileName)
                    Params.IsProcessed = True
                    Exit For
                Else

                End If
            Next


        End If

        If Not Params.IsProcessed Then
            Context.Response.StatusCode = 404
            Context.Response.ContentLength64 = Len("Not Found")
            SendText(Context, "Not Found")
        End If

        Context.Response.OutputStream.Close()


    End Sub

    ''' <summary>
    ''' Sends Text as entire response body
    ''' </summary>
    ''' <param name="Context"></param>
    ''' <param name="Text"></param>
    ''' <param name="ContentType"></param>
    ''' <param name="ResponseCode"></param>
    ''' <returns></returns>
    Public Function SendResponseText(ByVal Context As HttpListenerContext, ByVal Text As String, Optional ByVal ContentType As String = "", Optional ByVal ResponseCode As Integer = 200) As Integer
        Dim Buffer() As Byte = Context.Response.ContentEncoding.GetBytes(Text)
        If ContentType <> "" Then
            Context.Response.ContentType = ContentType
        End If
        Context.Response.StatusCode = ResponseCode
        Context.Response.ContentLength64 = Buffer.Length
        Context.Response.OutputStream.Write(Buffer, 0, Buffer.Length)
        Return Buffer.Length
    End Function

    ''' <summary>
    ''' Sends text to the outputstream
    ''' </summary>
    ''' <param name="Context"></param>
    ''' <param name="Text"></param>
    ''' <returns>number of bytes transferred</returns>
    Public Function SendText(ByVal Context As HttpListenerContext, ByVal Text As String) As Integer
        Dim Buffer() As Byte = Context.Response.ContentEncoding.GetBytes(Text)
        Context.Response.OutputStream.Write(Buffer, 0, Buffer.Length)
        Return Buffer.Length
    End Function

    ''' <summary>
    ''' Sends a File to the output stream and returns number of bytes transferred
    ''' </summary>
    ''' <param name="Context"></param>
    ''' <param name="FileName"></param>
    ''' <returns></returns>
    Public Function SendFile(ByVal Context As HttpListenerContext, ByVal FileName As String) As Integer
        Dim FileStream As New FileStream(FileName, FileMode.Open)
        Dim FileLen As Integer

        Dim Buffer(0 To 1023) As Byte
        Dim Read As Integer = FileStream.Read(Buffer, 0, Buffer.Length)
        While Read > 0
            FileLen += Read
            Context.Response.OutputStream.Write(Buffer, 0, Read)
            Read = FileStream.Read(Buffer, 0, Buffer.Length)
        End While

        FileStream.Close()
        Return FileLen
    End Function

    Public Shared Function DecodePostBody(ByVal Request As HttpListenerRequest) As NameValueCollection
        Dim Variables As New NameValueCollection()
        If Request.HasEntityBody Then
            Select Case Request.ContentType.Split(";")(0).ToLower().Trim()
                Case "application/x-www-form-urlencoded"
                    Dim Reader As New StreamReader(Request.InputStream, Request.ContentEncoding)
                    Dim RawParams() As String = Reader.ReadToEnd.Split("&")
                    For Each RawParam As String In RawParams
                        If RawParam <> "" Then
                            Dim KeyValue() As String = RawParam.Split("=")
                            Dim Key As String = HttpUtility.UrlDecode(KeyValue(0))
                            Dim Value As String = ""
                            If KeyValue.Length > 1 Then
                                Value = HttpUtility.UrlDecode(KeyValue(1))
                            End If
                            Variables.Add(Key, Value)
                        End If
                    Next
                Case "multipart/form-data"
                    'not implemented yet
                    'Content-Disposition' header tells boundary
                Case "text/plain"
            End Select
        End If
        Return Variables
    End Function


    Public Sub StopListen()
        If m_Listener IsNot Nothing Then
            m_Listener.Close()
            m_Listener = Nothing
        End If
    End Sub


End Class