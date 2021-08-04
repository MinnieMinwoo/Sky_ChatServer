using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Sky_ChatServer
{
    class ClientData
    {
        public TcpClient client; //tcpclient
        public string clientName; //이름
        public string testType; //학생 or 감독관
        public string chattingRoom; //채팅방 이름
        public byte[] messageData; //메세지 데이터를 담을 버퍼


        //정보를 입력받습니다. 채팅방 이름은 우선은 1로 설정했지만, 나중에 구분가능하게 변경해야 합니다.
        public ClientData(TcpClient _client)
        {
            client = _client;
            messageData = new byte[1024];
        }
    }
}
