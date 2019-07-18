using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WindowsFormsApp12
{
    public partial class Form1 : Form
    {
        bool alive = false; // будет ли работать поток для приема
        UdpClient client;
        const int LOCALPORT = 8001; // порт для приема сообщений
        const int REMOTEPORT = 8001; // порт для отправки сообщений
        const int TTL = 20;
        const string HOST = "235.5.5.1"; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки

        string userName; // имя пользователя в чате
        string userPass;
        private System.ComponentModel.IContainer components = null;
        public Form1()
        {
            InitializeComponent();
            button1.Enabled = true; // кнопка входа
            button2.Enabled = false; // кнопка выхода
            button3.Enabled = false; // кнопка отправки
            button4.Enabled = false;
            richTextBox1.ReadOnly = true; // поле для сообщений
            textBox3.ReadOnly = true;
            groupAddress = IPAddress.Parse(HOST);


        }

        private void button1_Click(object sender, EventArgs e)
        {
            userName = textBox1.Text;
            textBox1.ReadOnly = true;
            userPass = textBox2.Text;
            textBox2.ReadOnly = true;

            try
            {
                client = new UdpClient(LOCALPORT);
                // присоединяемся к групповой рассылке
                client.JoinMulticastGroup(groupAddress, TTL);

                // запускаем задачу на прием сообщений
                // Task receiveTask = new Task(ReceiveMessages);
                // receiveTask.Start();

                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.Start();
              

                // отправляем первое сообщение о входе нового пользователя
                string message = userName + " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);

                button1.Enabled = false;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    // добавляем полученное сообщение в текстовое поле
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        richTextBox1.Text = time + " " + message + "\r\n" + richTextBox1.Text;
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExitChat()
        {
            string message = userName + " покидает чат";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);

            alive = false;
            client.Close();

            button1.Enabled = true;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            textBox1.ReadOnly = false;
            textBox2.ReadOnly = false;
        }
     
        private void button2_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                textBox3.ReadOnly = false;
                string message = String.Format("{0}: {1}", userName, textBox3.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                textBox3.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
                ExitChat();
        }
    }
}
