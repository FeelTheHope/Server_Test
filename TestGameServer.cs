using System;
using NUnit.Framework;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private FakeTransport transport;
        private FakeClock clock;
        private GameServer server;

        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCount, Is.EqualTo(5));

            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);


            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();


            Packet move = new Packet(3, AvatarId, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();

            GameObject tester_Pos = server.Get_GameObject(AvatarId);

            Assert.That(tester_Pos.X, Is.Not.EqualTo(1.0f));

            Assert.That(tester_Pos.Y, Is.Not.EqualTo(1.0f));

            Assert.That(tester_Pos.Z, Is.Not.EqualTo(2.0f));
        }

        //[Test]
        //public void Ack_Test()
        //{
        //    Packet packet_00 = new Packet(0);
        //    //packet.NeedAck = true;
        //    transport.ClientEnqueue(packet_00, "tester", 0);

        //    server.SingleStep();

        //    Packet packet_01 = new Packet(0);
        //    packet_01.NeedAck = true;
        //    transport.ClientEnqueue(packet_01, "Tester_01", 1);

        //    server.SingleStep();

        //    FakeData DataPack_00 = transport.ClientDequeue();
        //    FakeData DataPack_01 = transport.ClientDequeue();
        //    FakeData DataPack_02 = transport.ClientDequeue(); // should be ack Packet that informs Tester that Tester_01 
        //                                                      // has joined the server
        //    FakeData DataPack_03 = transport.ClientDequeue();
        //    FakeData DataPack_04 = transport.ClientDequeue();// last packet found

        //    Console.WriteLine(BitConverter.ToInt32(DataPack_00.data, 0));
        //    Console.WriteLine(BitConverter.ToInt32(DataPack_01.data, 0));
        //    Console.WriteLine(BitConverter.ToInt32(DataPack_02.data, 0));
        //    Console.WriteLine(BitConverter.ToInt32(DataPack_03.data, 0));
        //    Console.WriteLine(BitConverter.ToInt32(DataPack_04.data, 0));

        //}

        [Test]
        public void Verify_right_avatarID()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Assert.That(AvatarId, Is.EqualTo(1)); //First client to connect , should have avataID = 1
        }

        [Test]
        public void Verify_right_avatarID_positive()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Assert.That(AvatarId, Is.Not.EqualTo(2)); //First client to connect , should have avataID = 1
        }

        [Test]
        public void Verify_right_avatarID_negative()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);
            Console.WriteLine(AvatarId);

            Assert.That(AvatarId, Is.Not.EqualTo(0)); //First client to connect , should have avataID = 1
        }

        [Test]
        public void Verify_right_ObjectID()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();

            uint AvatarID = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);
            GameObject GObj = server.Get_GameObject(AvatarID);

            Assert.That(GObj.Id, Is.EqualTo(1));
        }


        [Test]
        public void Verify_right_ObjectID_positive()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();

            uint AvatarID = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);
            GameObject GObj = server.Get_GameObject(AvatarID);

            Assert.That(GObj.Id, Is.Not.EqualTo(2));
        }


        [Test]
        public void Verify_right_ObjectID_negative()
        {
            Packet packet_00 = new Packet(0);
            transport.ClientEnqueue(packet_00, "tester", 0);
            server.SingleStep();

            uint AvatarID = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);
            GameObject GObj = server.Get_GameObject(AvatarID);

            Assert.That(GObj.Id, Is.Not.EqualTo(0));
        }


        [Test]
        public void Verify_Second_GameObject_Assignment()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);


            transport.ClientEnqueue(packet, "Tester_1", 1);
            server.SingleStep();
            transport.ClientDequeue();
            uint AvatarID_Tester_01 = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            GameObject GObj = server.Get_GameObject(AvatarID_Tester_01);

            Assert.That(GObj.Id, Is.EqualTo(2));
        }

        [Test]
        public void Verify_Second_GameObject_Assignment_positive()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);


            transport.ClientEnqueue(packet, "Tester_1", 1);
            server.SingleStep();
            transport.ClientDequeue();
            uint AvatarID_Tester_01 = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            GameObject GObj = server.Get_GameObject(AvatarID_Tester_01);

            Assert.That(GObj.Id, Is.Not.EqualTo(3));
        }

        [Test]
        public void Verify_Second_GameObject_Assignment_Negative()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint AvatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);


            transport.ClientEnqueue(packet, "Tester_1", 1);
            server.SingleStep();
            transport.ClientDequeue();
            uint AvatarID_Tester_01 = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            GameObject GObj = server.Get_GameObject(AvatarID_Tester_01);

            Assert.That(GObj.Id, Is.Not.EqualTo(1));
        }

        [Test]
        public void Verify_Move_obj()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            uint AvatarID_Tester = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet move = new Packet(3, AvatarID_Tester, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            GameObject GObj = server.Get_GameObject(AvatarID_Tester);

            Assert.That(GObj.X , Is.EqualTo(1.0f));
            Assert.That(GObj.Y, Is.EqualTo(1.0f));
            Assert.That(GObj.Z, Is.EqualTo(2.0f));
        }

        [Test]
        public void Verify_Move_obj_positive()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            uint AvatarID_Tester = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet move = new Packet(3, AvatarID_Tester, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            GameObject GObj = server.Get_GameObject(AvatarID_Tester);

            Assert.That(GObj.X, Is.Not.EqualTo(2.0f));
            Assert.That(GObj.Y, Is.Not.EqualTo(2.0f));
            Assert.That(GObj.Z, Is.Not.EqualTo(3.0f));
        }

        [Test]
        public void Verify_Move_obj_negative()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            uint AvatarID_Tester = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet move = new Packet(3, AvatarID_Tester, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            GameObject GObj = server.Get_GameObject(AvatarID_Tester);

            Assert.That(GObj.X, Is.Not.EqualTo(0.0f));
            Assert.That(GObj.Y, Is.Not.EqualTo(0.0f));
            Assert.That(GObj.Z, Is.Not.EqualTo(1.0f));
        }
    }
}
