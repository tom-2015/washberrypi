#include "sp.h"
#include <system.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>

void serial_init(){
	trisc.6=1;   //enable the outputs/inputs
	trisc.7=1;
	txsta.BRGH=1;
	baudcon.BRG16=1;
	spbrgh=1249/256; //16bit brg @ 9600 baud
	spbrg=1249%256;	
	txsta.SYNC=0;
	rcsta.SPEN=1;
	txsta.TXEN=1; //transmit enable
	rcsta.CREN=1; //receive enable
	ipr1.RCIP=0; //low prio
	pie1.RCIE=1; //enable interrupts
}

void serial_send_byte(unsigned char byte){
	while (txsta.TRMT==0);
	txreg = byte;
}

//sends an array of chars to the serial port
void serial_send_data(unsigned char * data, unsigned char len){
	for (int i=0;i<len;i++){
		while (txsta.TRMT==0);
		txreg = data[i];
	}
}

//sends a string to the serial port
void serial_send_string(unsigned char * data){
	while(*data!=0){
		serial_send_byte(*data);
		data++;
	}
}

void serial_send_string(rom char * text){
	unsigned char data = text[0];
	unsigned char i=0;
	while (data!=0){
		while (txsta.TRMT==0);
		txreg = data;
		i++;
		data = text[i];
	}
}

void serial_send_string_line(unsigned char * text){
	while(*text!=0){
		serial_send_byte(*text);
		text++;
	}
	serial_send_byte('\r');
	serial_send_byte('\n');
}

void serial_send_string_line(rom char * text){
	serial_send_string(text);
	serial_send_byte('\r');
	serial_send_byte('\n');
}

//prints an interger
void serial_send_int(unsigned int val){
	char str[7];
	itoa(val, str,10);
	serial_send_string(str);
}

void serial_send_uint(unsigned int val){
	char str[7]={0};
	sprintf(str, "%u", val);
	serial_send_string(str);
}

void serial_send_long(unsigned long val){
	char str[14];
	ltoa(val, str, 10);
	serial_send_string(str);
}

void serial_send_hex(unsigned char byte){
	unsigned char h = byte >> 4;
	unsigned char l = byte & 0b1111;
	if (h < 10) 
		h = '0' + h;
	else
		h = 'A' + h - 10;
	serial_send_data(&h, 1);
	if (l < 10)
		l = '0' + l;
	else
		l = 'A' + l - 10;
	serial_send_data(&l,1);
}

void serial_send_ip(unsigned char * ip){
	for (unsigned char i=0;i<3;i++){
		serial_send_int(ip[i]);
		serial_send_string("."); 
	}
	serial_send_int(ip[3]);
}

void serial_send_mac(unsigned char * mac){
	for (unsigned char i=0;i<5;i++){
		serial_send_hex(mac[i]);
		serial_send_string("-");
	}
	serial_send_hex(mac[5]);
}

