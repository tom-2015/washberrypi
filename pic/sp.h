#ifndef SERIAL_PORT_H
	#define SERIAL_PORT_H
	
	//hardware serial functions
	void serial_init();
	void serial_send_byte(unsigned char byte);
	void serial_send_data(unsigned char * data, unsigned char len);
	void serial_send_string(unsigned char * data);
	void serial_send_string(rom char * text);
	void serial_send_string_line(unsigned char * text);
	void serial_send_int(unsigned int val);
	void serial_send_long(unsigned long val);
	void serial_send_hex(unsigned char byte);
	void serial_send_ip(unsigned char * ip);
	void serial_send_mac(unsigned char* mac);
	void serial_send_uint(unsigned int val);
	//software serial functions
	//void soft_serial_init();
	//void soft_serial_send(unsigned char b);
	//void soft_serial_send_data (unsigned char * data, unsigned char len);
	//void soft_serial_send_string(unsigned char * text);
// soft_serial_send_int(int val);
#endif