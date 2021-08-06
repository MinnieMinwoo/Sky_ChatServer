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
        List<ClientList> clientListData;
        public ServerManager()
        {
            clientListData = new List<ClientList>();
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
                    DeleteClient(clientData);
                }
            }
        }

        //클라이언트 연결 확인
        private void CheckConnect()
        {

        }

        //클라이언트 데이터 추가
        private void AddClient(TcpClient client, string clientName, string testType, string RoomName)
        {
            if (clientListData.Count != 0)
            {
                foreach (ClientList clientList in clientListData)
                {
                    if (clientList.chattingRoomName == RoomName)
                    {
                        foreach (ClientData clientdata in clientList.clientList)
                        {
                            if (clientdata.client == client)
                            {
                                if (clientdata.clientName == null)
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

                        ClientData _addClient = new ClientData(client);
                        _addClient.clientName = clientName;
                        _addClient.testType = testType;
                        clientList.clientList.Add(_addClient);
                        return;
                    }
                }
            }
            ClientList AddClientList = new ClientList(RoomName);
            clientListData.Add(AddClientList);
            ClientData addClient = new ClientData(client, clientName, testType);
            clientListData[clientListData.Count - 1].clientList.Add(addClient);
            return;
        }

        private void DeleteClient(ClientData clientData)
        {
            foreach (ClientList clientList in clientListData)
            {
                if (clientList.chattingRoomName == clientData.chattingRoom)
                {
                    for (int i = 0; i < clientList.clientList.Count; i++)
                    {
                        if (clientList.clientList[i].client == clientData.client)
                        {
                            clientList.clientList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
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
                DeleteClient(callbackClient);
                return;
            }
            string readString = Encoding.UTF8.GetString(callbackClient.messageData, 0, bytesRead);
            string[] messageData = readString.Split("$$#$$");
            if (messageData[1] == "UserInfo")
            {
                AddClient(callbackClient.client, messageData[2], messageData[3], messageData[4]);

            }

            else if (messageData[1] == "Disconnect")
            {
                DeleteClient(callbackClient);
            }
            
            else if (messageData[1] == "Message")
            {
                List<ClientData> clientList = new List<ClientData>();
                foreach (ClientList clientlist in clientListData)
                {
                    if (clientlist.chattingRoomName == messageData[2])
                    {
                        clientList = clientlist.clientList;
                        break;
                    }
                }
                Console.WriteLine("{0} : {1}", messageData[4], messageData[5]);
                List<ClientData> MessageTarget = new List<ClientData>();
                // 메시지를 보낼 대상을 결정하는 부분입니다.

                if (messageData[3] == "All")
                {
                    foreach (ClientData clientdata in clientList)
                    {
                        MessageTarget.Add(clientdata);
                    }
                }

                else if (messageData[3] == "SuperVisor")
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
                string returnmessage = "$$#$$Message$$#$$" + messageData[4] + "$$#$$" + messageData[5] + "$$#$$Message$$#$$";
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
