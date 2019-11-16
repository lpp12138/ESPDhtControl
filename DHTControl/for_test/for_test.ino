/*
 Name:		for_test.ino
 Created:	2019/11/15 19:28:32
 Author:	lpp12138
  Mail:     lpp12138@outlook.com
*/

// the setup function runs once when you press reset or power the board
#include <WiFiServer.h>
#include <WiFiUdp.h>
#include <WiFiClient.h>
#include <ESP8266WiFi.h>
#include <ArduinoJson.h>
#include <EEPROM.h>

byte rstConfigpin = D5;//GPIO 14
const void connectWifi();

typedef struct configData
{
	//char deviceName[32];
	//char deviceType[16];
	//char sensorType[16];
	char ssid[64];
	char password[32];

}configData;
configData configdata;

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

void setup() {
	Serial.begin(115200);
	WiFi.persistent(false);
	pinMode(rstConfigpin,INPUT_PULLUP);
	loadConfig();
	Serial.println("config loaded ssid:"+String(configdata.ssid)+" psk:"+String(configdata.password));
}

// the loop function runs over and over again until power down or reset
void loop() {
	delay(100);
	if (digitalRead(rstConfigpin) == LOW) Serial.println("LOW");
	if (configdata.ssid == NULL || digitalRead(rstConfigpin)==LOW)
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
					Serial.println("Serial Received: "+serialData);
					JsonObject& root = receiveJsonBuffer.parseObject(serialData);
					strcpy(configdata.ssid,root["ssid"]);
					strcpy(configdata.password, root["password"]);
					Serial.println("config loaded ssid:" + String(configdata.ssid) + " psk:" + String(configdata.password));
					saveConfig();
					break;
				}
			}
		}
	}
	connectWifi();
	delay(5000);
}

const void connectWifi()
{
	delay(10);
	Serial.print("Connecting to ");
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

