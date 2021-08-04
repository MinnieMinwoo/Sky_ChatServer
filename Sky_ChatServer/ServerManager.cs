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
        private void AddClient(TcpClient client, string clientName, string testType)
        {
            foreach (ClientData clientdata in clientList)
            {
                if (clientdata.client == client)
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

            ClientData addClient = new ClientData(client);
            addClient.clientName = clientName;
            addClient.testType = testType;
            clientList.Add(addClient);
            Console.WriteLine("성공");
        }

        private void DeleteClient()
        {

        }

        //메세지를 받아서 헤더에 맞는 작업을 진행합니다.
        private void ReceiveMesasage(IAsyncResult ar)
        {
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
                AddClient(callbackClient.client, messageData[2], messageData[3]);

            }

            else if (messageData[1] == "Disconnect")
            {
                DeleteClient();
            }
            
            else if (messageData[1] == "Message")
            {

                Console.WriteLine("{0} : {1}", messageData[3], messageData[4]);
                List<ClientData> MessageTarget = new List<ClientData>();
                // 메시지를 보낼 대상을 결정하는 부분입니다.
                if (messageData[2] == "All")
                {
                    MessageTarget = clientList;
                }

                else if (messageData[2] == "SuperVisor")
                {
                    foreach(ClientData clientdata in clientList)
                    {
                        if (clientdata.testType == "2")
                        {
                            MessageTarget.Add(clientdata);
                        }
                    }
                }
                else
                {
                    foreach (ClientData clientdata in clientList)
                    {
                        if (clientdata.clientName == messageData[2])
                        {
                            MessageTarget.Add(clientdata);
                        }
                    }
                }

                //실제 메시지 전송
                string returnmessage = "$$#$$Message$$#$$" + messageData[3] + "$$#$$" + messageData[4] + "$$#$$Message$$#$$";
                byte[] header = new byte[returnmessage.Length];
                header = Encoding.UTF8.GetBytes(returnmessage);
                foreach (ClientData targetClient in MessageTarget)
                {
                    targetClient.client.GetStream().Write(header);
                }
                //callbackClient.client.GetStream().Write(header);

            }

            callbackClient.client.GetStream().BeginRead(callbackClient.messageData, 0, callbackClient.messageData.Length, new AsyncCallback(ReceiveMesasage), callbackClient);

        }


    }
}
