/**
  Flight Sim Toggle Box 
  @description Communicates with PC software over serial to indicate what events happen. Uses serial to allow all arduino type boards to work.
  @author Shane McIntosh
  @email shmcinto@gmail.com

*/

//Toggle Pins:
byte togglePins[20] = { 2,3,4,5,6,7,8,9,10,11,12,13,A0,A1,A2,A3,A4,A5,A6,A7 }; //Arduino Nano
//byte togglePins[18] = { 2,3,4,5,6,7,8,9,10,11,12,13,A0,A1,A2,A3,A4,A5}; //Arduino Uno
char xferData[22] = {0}; 


void setup() {
  
  Serial.begin(115200);
  Serial.println("Device Started!");

  for(byte pin : togglePins) {
    pinMode(pin, INPUT_PULLUP);
  }

}

void loop() {
  
  //This section is a simple CHK/ACK Challenge/Response check that allows the PC Software to check to see if the device is still attached
  //PC will send CHK\r\n and in response the device should send a ACK\r\n. If no ACK is recieved with in 2 seconds it can be safely assumed 
  //the device is disconnected. PC sending a POL is a request for the current state of the inputs and will send as as such as a unsigned int (4 bytes)
  //Buttons are in reverse order,so when read the first bit is button 20, the next is button 19 and so on and so forth
  String remoteBuffer;
  if (Serial.available()) {
    remoteBuffer = Serial.readStringUntil('\n');
    if (remoteBuffer == "CHK\r") {
      String ackStr = "ACK";
      Serial.println(ackStr);
    }
    else if (remoteBuffer == "POL\r") {
      Serial.println(xferData);
    }
    else {
      Serial.write(remoteBuffer.c_str());
    }
  }

  //Loop through all the button pins and update the states
  for(int curBtn = 0; curBtn < 18; curBtn++) { //18 for Arduino Uno //20 for Arudino Nano Note A6 & A7 are analog only inputs and won't work with digital functions or pull up resistor
    //Serial.print("Current Button Read is ");
    //Serial.println(curBtn);
    if (digitalRead(togglePins[curBtn])) {
      xferData[curBtn] = '1';
    }
    else {
      xferData[curBtn] = '0';
    }
    //Serial.print("xferData currently is ");
    //Serial.println(xferData);
  }

}