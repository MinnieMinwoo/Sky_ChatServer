using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                    Thread delClient = new Thread(DeleteClient);
                    delClient.Start(acceptClient);
                    return;
                }
            }
        }

        //클라이언트 연결 확인
        private void CheckConnect()
        {
            //1초에 한번씩 모든 클라이언트에 더미 메시지를 전송합니다. 이 때 오류가 발생하면 클라이언트와 연결이 해제합니다.
            string returnmessage = "$$#$$Dummy$$#$$" + "$$#$$Dummy$$#$$";
            byte[] header = new byte[returnmessage.Length];
            header = Encoding.UTF8.GetBytes(returnmessage);
            foreach (ClientList clientListData in clientListData)
            {
                foreach (ClientData targetClient in clientListData.clientList)
                {
                    try
                    {
                        targetClient.client.GetStream().Write(header);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Thread delClient = new Thread(DeleteClient);
                        delClient.Start(targetClient.client);
                        continue;
                    }
                }
            }
            Thread.Sleep(1000);
        }

        //클라이언트 데이터 추가
        private void AddClient(TcpClient client, string clientName, string testType, string RoomName)
        {

            //채팅방이 있는경우 해당 채팅방에 대응되는 클라이언트 리스트에 등록
            if (clientListData.Count != 0)
            {
                foreach (ClientList clientList in clientListData)
                {
                    if (clientList.chattingRoomName == RoomName)
                    {
                        ClientData _addClient = new ClientData(client, clientName, testType);
                        clientList.clientList.Add(_addClient);
                        return;
                    }
                }
            }

            //없으면 여기서 등록
            ClientList AddClientList = new ClientList(RoomName);
            clientListData.Add(AddClientList);
            ClientData addClient = new ClientData(client, clientName, testType);
            clientListData[clientListData.Count - 1].clientList.Add(addClient);
            return;
        }

        //클라이언트 리스트에서 TcpClient를 찾아 제거하는 메서드입니다.
        private void DeleteClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            foreach (ClientList clientList in clientListData)
            {

                for (int i = 0; i < clientList.clientList.Count; i++)
                {
                    if (clientList.clientList[i].client == client)
                    {
                        clientList.clientList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        //메세지를 받아서 헤더에 맞는 작업을 진행합니다.
        private void ReceiveMesasage(IAsyncResult ar)
        {

            //데이터를 string으로 변환하는 과정
            int bytesRead;
            ClientData callbackClient = ar.AsyncState as ClientData;
            try
            {
                bytesRead = callbackClient.client.GetStream().EndRead(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread delClient = new Thread(DeleteClient);
                delClient.Start(callbackClient.client);
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
                DeleteClient(callbackClient.client);
            }

            //메시지 전송 메서드
            else if (messageData[1] == "Message")
            {
                //패킷에서 채팅방 가져오기
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

                //모두에게 메시지 보내기
                if (messageData[3] == "All")
                {
                    foreach (ClientData clientdata in clientList)
                    {
                        MessageTarget.Add(clientdata);
                    }
                }
                //감독관한테 보내기 (물체 감지시 활용)
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

                //특정인에게만 메시지 전송 (그 외의 경우)
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
                    try
                    {
                        targetClient.client.GetStream().Write(header);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Thread delClient = new Thread(DeleteClient);
                        delClient.Start(callbackClient.client);
                        return;
                    }
                }
            }

            //다시 메시지 전송을 기다림
            callbackClient.client.GetStream().BeginRead(callbackClient.messageData, 0, callbackClient.messageData.Length, new AsyncCallback(ReceiveMesasage), callbackClient);

        }


    }
}
