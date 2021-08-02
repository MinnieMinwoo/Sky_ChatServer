using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Sky_ChatServer
{
    class ServerManager
    {
        List<ClientData> clientList;
        public ServerManager()
        {
            clientList = new List<ClientData>();
            ServerStart();
        }

        //서버 가동
        private void ServerStart()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));
            listener.Start();
            while (true)
            {
                TcpClient acceptClient = listener.AcceptTcpClient();
                ClientData clientData = new ClientData(acceptClient);
                try
                {
                    clientData.client.GetStream().BeginRead(clientData.messageData, 0, clientData.messageData.Length, new AsyncCallback(ReceiveMesasage), clientData);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    DeleteClient();
                }
            }
        }

        //클라이언트 연결 확인
        private void CheckConnect()
        {

        }

        //클라이언트 데이터 추가
        private void AddClient(string clientName, string testType, string ipAdress)
        {
            foreach (ClientData clientdata in clientList)
            {
                if (clientdata.ipAdress == ipAdress)
                {
                    if(clientdata.clientName == null)
                    {
                        clientdata.clientName = clientName;
                    }
                    if (clientdata.testType == null)
                    {
                        clientdata.testType = testType;
                    }
                    Console.WriteLine("성공");
                    return;
                }
            }



        }

        private void DeleteClient()
        {

        }

        //메세지를 받아서 헤더에 맞는 작업을 진행합니다.
        private void ReceiveMesasage(IAsyncResult ar)
        {
           Console.WriteLine("데이터 들어옴");
            int bytesRead;
            ClientData callbackClient = ar.AsyncState as ClientData;
            try
            {
            bytesRead = callbackClient.client.GetStream().EndRead(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
            string readString = Encoding.UTF8.GetString(callbackClient.messageData, 0, bytesRead);
            string[] messageData = readString.Split("$$#$$");
            if (messageData[1] == "UserInfo")
            {
                AddClient(messageData[2], messageData[3], callbackClient.ipAdress);

            }

            else if (messageData[1] == "Disconnect")
            {
                DeleteClient();
            }
            
            else if (messageData[1] == "Message")
            {
                Console.WriteLine("{0} : {1}", messageData[3], messageData[4]);
                string returnmessage = "$$#$$Message$$#$$" + messageData[2] + "$$#$$" + messageData[3] + "$$#$$" + messageData[4] + "$$#$$Message$$#$$";
                byte[] header = new byte[returnmessage.Length];
                header = Encoding.UTF8.GetBytes(returnmessage);
                callbackClient.client.GetStream().Write(header);
            }

            else
            {
            }

            callbackClient.client.GetStream().BeginRead(callbackClient.messageData, 0, callbackClient.messageData.Length, new AsyncCallback(ReceiveMesasage), callbackClient);

        }


    }
}
