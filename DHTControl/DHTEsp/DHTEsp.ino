/*
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

char* deviceType = "AC";
char* sensorType = "DHT";
const unsigned short localUdpPort = 2333;
String serverIP = "";
unsigned short serverTcpPort = 2334;
DHTesp dht;
WiFiUDP myUdp;
WiFiClient myTcp;
bool switchFlag = false;
const  uint16_t sendPin = D1;//GPIO 5 
IRHaierACYRW02 myAC(sendPin);
byte rstConfigpin = D2;//GPIO 4
String jsonData = "";

//store config data in EEPROM using structure
typedef struct configData
{
	char deviceName[32];
	//char deviceType[16];
	//char sensorType[16];
	char ssid[64];
	char password[32];

}configData;
configData configdata;

//EEPROM config
void flushconfig();
void saveConfig();
void loadConfig();


String recUdpPackage();
const void connectWifi();

void setup() 
{
	Serial.begin(115200);
	loadConfig();
	pinMode(rstConfigpin, INPUT_PULLUP);
	delay(1000);
	if (digitalRead(rstConfigpin) == LOW) Serial.println("LOW");
	else Serial.println("HIGH");
	if (configdata.ssid == NULL || digitalRead(rstConfigpin) == LOW)
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
	WiFi.setSleepMode(WIFI_NONE_SLEEP);
	myUdp.begin(localUdpPort);
	dht.setup(D7, DHTesp::DHT22);//GPIO13
	myAC.begin();
	WiFi.persistent(false);
	myUdp.begin(localUdpPort);
}

void loop() 
{
	if (WiFi.status() != WL_CONNECTED)
	{
		connectWifi();
		return;
	}
	jsonData = recUdpPackage();
	myUdp.flush();
	delay(10);
	if (jsonData != "")
	{
		StaticJsonBuffer<300> receiveJsonBuffer;
		JsonObject& root = receiveJsonBuffer.parseObject(jsonData);
		if (root.success())
		{
			Serial.println("json parse success");
			if (serverIP == "" || root["protocol"] != "myEspNet" || !myTcp.connect(serverIP, serverTcpPort))
			{
				Serial.print("connect Failed or no server ip address");
				jsonData = "";
				return;
			}
			else
			{
				bool needSendData = false;
				String command = root.get<String>("command");
				Serial.println(command);
				StaticJsonBuffer<200> sendJsonBuffer;
				JsonObject& returnData = sendJsonBuffer.createObject();
				returnData["protocol"] = "myEspNet";
				returnData["deviceName"] = configdata.deviceName;
				returnData["deviceType"] = deviceType;
				JsonObject& returndata = returnData.createNestedObject("data");
				JsonObject& data = root["data"];
				if (command == "ping")
				{
					needSendData = true;
					int hum = dht.getHumidity();
					returndata["dht"] = String(hum);
					Serial.println("hum: " + String(hum));
				}
				if (command == "set")
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
				if (needSendData)
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
			//获取服务器IP，服务器TCP端口(服务器UDP端口+1)
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
