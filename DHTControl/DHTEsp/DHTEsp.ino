、/*
 Name:		DHTEsp.ino
 Created:	2019/6/19 17:02:51
 Author:	lpp
 Mail:      lpp12138@outlook.com
*/

#include <IRremoteESP8266.h> 
#include <IRsend.h> 
#include <ir_Haier.h> 
#include <DHTesp.h>  
#include <EEPROM.h>
#include <ArduinoJson.h>
#include <WiFiUdp.h>
#include <WiFiServer.h>
#include <WiFiClient.h>
#include <ESP8266WiFi.h>
#include "jsonDataDefs.h"

//定义设备和传感器类型 保留兼容性之后扩展
char* deviceType = "AC";//本设备类型为空调
char* sensorType = "DHT";//传感器类型为DHT温湿度传感器

const unsigned short localUdpPort = 2333; //定义的udp广播端口
String serverIP = ""; //用于储存服务器IP的变量
unsigned short serverTcpPort = 2334; //定义的用于连接服务器的端口
DHTesp dht; //DHT传感器对象
WiFiUDP myUdp;  //udp接口
WiFiClient myTcp; //tcp接口
bool switchFlag = false; 
const  uint16_t sendPin = D1;//GPIO 5 
IRHaierACYRW02 myAC(sendPin); //定义红外发射二极管连接的引脚
byte rstConfigpin = D2;//GPIO 4 定义用于检测是否需要重置内部储存的WIFI数据
String jsonData = ""; //用于储存接收到的json数据

//利用EEPROM储存WIFI数据
typedef struct configData
{
	char deviceName[32];
	//char deviceType[16];
	//char sensorType[16];
	char ssid[64];
	char password[32];

}configData;
configData configdata;

//EEPROM 设置函数声明
void flushconfig(); //清空
void saveConfig(); //储存
void loadConfig(); //加载


String recUdpPackage(); //处理UDP数据包 函数声明
const void connectWifi(); //连接WIFI 函数声明

//后文中Serial开头的语句基本都为调试用

void setup() 
{
	Serial.begin(115200);
	loadConfig();
	pinMode(rstConfigpin, INPUT_PULLUP); //将重置检测接口声明为输入并接内置上拉电阻
	delay(1000);
	if (digitalRead(rstConfigpin) == LOW) Serial.println("LOW");
	else Serial.println("HIGH");
	if (configdata.ssid == NULL || digitalRead(rstConfigpin) == LOW) //接收json wifi数据，并解析
	{
		Serial.println("waiting setup data");
		while (true)
		{
			if (Serial.available())
			{
				StaticJsonBuffer<500> receiveJsonBuffer;
				String serialData;
				serialData = Serial.readString();
				if (serialData != "")
				{
					Serial.println("Serial Received: " + serialData);
					JsonObject& root = receiveJsonBuffer.parseObject(serialData);
					strcpy(configdata.ssid, root["ssid"]);
					strcpy(configdata.deviceName, root["deviceName"]);
					strcpy(configdata.password, root["password"]);
					Serial.println("config loaded ssid:" + String(configdata.ssid) + " psk:" + String(configdata.password));
					saveConfig();
					break;
				}
			}
		}
	}
	WiFi.setSleepMode(WIFI_NONE_SLEEP); //不进入睡眠
	myUdp.begin(localUdpPort); //启动udp传输
	dht.setup(D7, DHTesp::DHT22);//GPIO13 启动DHT传感器
	myAC.begin(); //初始化空调对象
	WiFi.persistent(false); //阻止内置自动重连以防连接到无关WIFI，因为使用了自己的EEPROM内的WIFI数据
}

void loop() 
{
	if (WiFi.status() != WL_CONNECTED) //检测wifi连接状态
	{
		connectWifi();
		return;
	}
	jsonData = recUdpPackage(); //接收UDP数据 
	myUdp.flush(); //清空UDP缓冲区
	delay(10);
	if (jsonData != "") //处理json数据
	{
		StaticJsonBuffer<300> receiveJsonBuffer;
		JsonObject& root = receiveJsonBuffer.parseObject(jsonData);
		if (root.success())
		{
			Serial.println("json parse success");
			if (serverIP == "" || root["protocol"] != "myEspNet" || !myTcp.connect(serverIP, serverTcpPort)) //其他情况处理
			{
				Serial.print("connect Failed or no server ip address");
				jsonData = "";
				return;
			}
			else
			{
				bool needSendData = false; //是否需要向服务器回传数据的旗标，默认不需要
				String command = root.get<String>("command");//从json数据中找出指令
				Serial.println(command);
				StaticJsonBuffer<200> sendJsonBuffer;
				JsonObject& returnData = sendJsonBuffer.createObject(); //初始化向服务器返回的json对象
				returnData["protocol"] = "myEspNet";
				returnData["deviceName"] = configdata.deviceName;
				returnData["deviceType"] = deviceType;
				JsonObject& returndata = returnData.createNestedObject("data");
				JsonObject& data = root["data"];
				if (command == "ping") //服务器发出ping指令，返回湿度数据
				{
					needSendData = true; 
					int hum = dht.getHumidity();
					returndata["dht"] = String(hum);
					Serial.println("hum: " + String(hum));
				}
				if (command == "set") //服务器要求控制空调
				{
					String sdata;
					data.printTo(sdata);
					Serial.println(sdata);
					myAC.setTemp(data["temp"]);
					myAC.setPower(data["power"]);
					if (data["fanSpeed"] == "auto")myAC.setFan(HAIER_AC_YRW02_FAN_AUTO);
					else if (data["fanSpeed"] == "high")myAC.setFan(HAIER_AC_YRW02_FAN_HIGH);
					else if (data["fanSpeed"] == "med") myAC.setFan(HAIER_AC_YRW02_FAN_MED);
					else if (data["fanSpeed"] == "low")	myAC.setFan(HAIER_AC_YRW02_FAN_LOW);
					if (data["mode"] == "cool")myAC.setMode(HAIER_AC_YRW02_COOL);
					else if (data["mode"] == "dry")myAC.setMode(HAIER_AC_YRW02_DRY);
					else if (data["mode"] == "heat") myAC.setMode(HAIER_AC_YRW02_HEAT);
					else if (data["mode"] == "auto")	myAC.setMode(HAIER_AC_YRW02_AUTO);
					needSendData = true;
					myAC.send();
					returnData["message"] = "setting complete";
				}
				if (needSendData)//如果需要，向服务器发送数据
				{
					String sendData;
					returnData.printTo(sendData);
					myTcp.println(sendData);
					Serial.println(sendData);
					Serial.println();
				}
			}
		}
		jsonData = "";
	}
}

String recUdpPackage()
{
	int packageSize = myUdp.parsePacket();
	if (packageSize)
	{
		char incomeingPackage[packageSize];
		int packageLength = myUdp.read(incomeingPackage, packageSize);
		if (packageLength > 0)
		{
			//如果是正常包，则在其末尾补\0使其成为完整string
			incomeingPackage[packageLength] = 0;
			//获取服务器IP
			serverIP = myUdp.remoteIP().toString();
			Serial.println("UDP Pack received");
			Serial.println("UDP_DATA:" + String(incomeingPackage));
			Serial.println("Remote IP:" + myUdp.remoteIP().toString());
			Serial.println("Remote Port:" + String(myUdp.remotePort()));
		}
		return String(incomeingPackage);
	}
	return "";
}

const void connectWifi()
{
	delay(10);
	Serial.print("Connecting to ");
	Serial.println(configdata.ssid);
	WiFi.mode(WIFI_STA);
	WiFi.begin(configdata.ssid, configdata.password);

	while (WiFi.status() != WL_CONNECTED) {
		delay(500);
		Serial.print(".");
	}

	Serial.println("");
	Serial.println("WiFi connected");
	Serial.println("IP address: ");
	Serial.println(WiFi.localIP());
}

void flushconfig()
{
	EEPROM.begin(1024);
	uint8_t* p = (uint8_t*)(&configdata);
	for (int i = 0; i < sizeof(configdata); i++)
	{
		EEPROM.write(i, 0);
	}
	EEPROM.commit();
}

void saveConfig()
{
	EEPROM.begin(1024);
	uint8_t* p = (uint8_t*)(&configdata);
	for (int i = 0; i < sizeof(configdata); i++)
	{
		EEPROM.write(i, *(p + i));
	}
	EEPROM.commit();
}

void loadConfig()
{
	EEPROM.begin(1024);
	uint8_t* p = (uint8_t*)(&configdata);
	for (int i = 0; i < sizeof(configdata); i++)
	{
		*(p + i) = EEPROM.read(i);
	}
	EEPROM.commit();
}
