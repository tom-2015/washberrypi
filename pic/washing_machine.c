#include <system.h>
#include <system.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <memory.h>
#include "sp.h"

#pragma CLOCK_FREQ 48000000

//#pragma DATA	_CONFIG1L, _PLLDIV_5_1L & _CPUDIV_OSC1_PLL2_1L & _USBDIV_2_1L  //20Mhz
#pragma DATA	_CONFIG1L, _PLLDIV_3_1L & _CPUDIV_OSC1_PLL2_1L & _USBDIV_2_1L  //12Mhz
#pragma DATA    _CONFIG1H, _FOSC_HSPLL_HS_1H & _FCMEN_OFF_1H & _IESO_OFF_1H

//#pragma DATA    _CONFIG2L, _PWRT_OFF_2L & _BOR_OFF_2L  & _VREGEN_ON_2L
#pragma DATA    _CONFIG2L, _PWRT_OFF_2L & _BOR_ON_2L & _BORV_3_2L  & _VREGEN_ON_2L
#pragma DATA    _CONFIG2H, _WDT_ON_2H & _WDTPS_64_2H // 1/4 sec, _WDTPS_1024_2H //WDT ON, 4s
#pragma DATA    _CONFIG3H, _CCP2MX_ON_3H & _LPT1OSC_OFF_3H & _PBADEN_OFF_3H & _MCLRE_OFF_3H
#pragma DATA    _CONFIG4L, _STVREN_ON_4L & _LVP_OFF_4L & _DEBUG_OFF_4L & _XINST_OFF_4L
#pragma DATA    _CONFIG5L, _CP0_OFF_5L & _CP1_OFF_5L & _CP2_OFF_5L & _CP3_OFF_5L
#pragma DATA    _CONFIG5H, _CPB_OFF_5H & _CPD_OFF_5H
#pragma DATA    _CONFIG6L, _WRT0_OFF_6L & _WRT1_OFF_6L & _WRT2_OFF_6L & _WRT3_OFF_6L
#pragma DATA    _CONFIG6H, _WRTC_OFF_6H & _WRTB_OFF_6H & _WRTD_OFF_6H
#pragma DATA    _CONFIG7L, _EBTR0_OFF_7L & _EBTR1_OFF_7L & _EBTR2_OFF_7L & _EBTR3_OFF_7L
#pragma DATA    _CONFIG7H, _EBTRB_OFF_7H


#define TRIAC_ON 0
#define TRIAC_OFF 1
#define TRIAC_OUTPUT porta.4
#define DIRECTION_OUTPUT porta.3
#define BOOST_OUTPUT porta.2
#define LED_OUTPUT porta.5

//error bits in error byte
#define ERROR_ZERO_CROSSING 1
#define ERROR_TACHO 2
#define ERROR_WATER 3
#define ERROR_SUNBALANCE 4
#define ERROR_DUNBALANCE 5

//#define TRIAC_OUTPUT porta.3

int wanted_speed=0;
unsigned int current_speed=0;
bool direction=0;          //0 -> speed +, 1 -> speed -
bool changing_direction=0; //becomes 1 if we need to change direction (wait for 0 rpm)
bool speed_control_enabled=true;

unsigned int temperature=0; //holds result of ADC
bool adc_started; //1 if an ADC was started previously

unsigned int tacho_time=0; //time measured between 16 pulses, 1 timer tick is 8µs, drum speed is: +/- 10 ms / (timer / 16 * 0.008)
unsigned int tacho_min=0xFFFF;
unsigned int tacho_max=0;
unsigned char captures=0;
unsigned int p_tacho_min=0;
unsigned int p_tacho_max=0;
bool tacho_captured=false;
unsigned int water_level=0; //time measured of water level frequency
unsigned int water_level_min=0xFFFF;
unsigned int water_level_max=0;
unsigned int p_water_level_min=0;
unsigned int p_water_level_max=0;
unsigned char water_level_captures=0;
bool water_level_captured=false;

unsigned char output_power=0; //holds value of wanted power 0-100% will be converted to triac_on period value
unsigned char rx_buff[32];    //holds received serial data
unsigned char rx_idx=0;       //position of last character
bool serial_cmd_received=0;

unsigned char triac_on=255; //when must triac be turned on in 0.2 ms, 0 = immediately, value range 0-49, triac will be max of 0.2ms on
unsigned char triac_off=255;
unsigned char period_timer=0; //timer that increases every 0.2 ms

unsigned char next_triac_on=255; //buffer of values to be copied to triac_on and triac_off on next sine wave start
unsigned char next_triac_off=255;

unsigned int clock=0; //increments every +/- 10 ms

unsigned char error=0;

void interrupt(){
	if (intcon.INT0F){
		//zero crossing detected
		//fill timer 2 register
		tmr2=0;			
		pir1.TMR2IF=0;
		period_timer=0; //50Hz starts now, sync the timer
		triac_on=next_triac_on;
		triac_off=next_triac_off;
		TRIAC_OUTPUT=TRIAC_OFF;
		intcon.INT0F=0;
	}
	
	if (pir1.TMR2IF){ //interrupt every 0.2ms / 5KHz
		if (period_timer!=255){ //invalid value, no zero crossing detected
			if (period_timer == triac_on){ //see if we must turn on the triac
				TRIAC_OUTPUT = TRIAC_ON;
			}else if (period_timer >= triac_off){ //must turn off triac?
				TRIAC_OUTPUT = TRIAC_OFF;
			}
			error.ERROR_ZERO_CROSSING=0;
			period_timer++; //hold period timer (this is time within half a sine)			
		}else{
			error.ERROR_ZERO_CROSSING=1;
			TRIAC_OUTPUT = TRIAC_OFF;
		}

		pir1.TMR2IF=0;
	}

	//if timer 1 overflows motor is not turning
	if (pir1.TMR1IF){
		tacho_time=0;
		pir1.TMR1IF=0;
		tacho_captured=true;
		if (triac_on!=255) error.ERROR_TACHO=1; //set error if motor should be running
	}
	
	//this captures/measures the frequency of the tacho
	if (pir1.CCP1IF){
		t1con.TMR1ON=0;
		tacho_captured=true;
		MAKESHORT(tacho_time, ccpr1l, ccpr1h);
		tmr1h=0;
		tmr1l=0;
		t1con.TMR1ON=1;
		pir1.CCP1IF=0;
		error.ERROR_TACHO=0;
	}
	
	if (pir2.CCP2IF){
		t3con.TMR3ON=0;
		water_level_captured=true;
		MAKESHORT(water_level, ccpr2l, ccpr2h);
		tmr3h=0;
		tmr3l=0;
		error.ERROR_WATER=0;
		t3con.TMR3ON=1;
		pir2.CCP2IF=0;	
	}

}

//low priority interrupts
void interrupt_low(){

	if (intcon.TMR0IF){
		clock++;
		intcon.TMR0IF=0;
	}

	//serial receive interrupt
	if (pir1.RCIF){ 
		unsigned char rx = rcreg;
		LED_OUTPUT = ~ LED_OUTPUT;
		if (rx!='\r'){
			if (rx=='\n'){
				serial_cmd_received=1;
				rx=0;
			}
			rx_buff[rx_idx] = rx;
			rx_idx++;
			if (rx_idx==sizeof(rx_buff)) rx_idx=0;
		}
	}
	

	
	//error TMR3 is not supposed to flow over
	if (pir2.TMR3IF){
		water_level_captured=true;
		water_level=0;
		pir2.TMR3IF=0;
		error.ERROR_WATER=1;
	}

}

void set_power(unsigned int val){
	if (val > output_power){
		if (output_power ==0){
			if (val > 30) val = 30;
		}else{
			if ((val - output_power) > 10){ //for safety reason, you don't want motor to go from 0 to 100% power in 1 step because of a bug
				val = output_power + 10;
			}
		}
	}
	
	//if no power to motor do not increase power
	if (error.ERROR_ZERO_CROSSING){
		output_power=0;
		return;
	}
	
	output_power = val;

	unsigned char center = 100-output_power;
	if (center < 95){
		if (center<3){
			next_triac_on=0;
			next_triac_off=255;
		}else{
			next_triac_off=center+3;
			next_triac_on=center-3;
		}
	}else{
		next_triac_off=255;
		next_triac_on=255;
	}

}


unsigned int get_water_level_min(){
	water_level_captured=false;
	unsigned int water_level_min_cpy = p_water_level_min;
	if (water_level_captured) water_level_min_cpy = p_water_level_min;
	return water_level_min_cpy;
}

unsigned int get_water_level_max(){
	water_level_captured=false;
	unsigned int water_level_max_cpy = p_water_level_max;
	if (water_level_captured) water_level_max_cpy = p_water_level_max;
	return water_level_max_cpy;
}

unsigned int get_tacho_min(){
	tacho_captured=false;
	unsigned int tacho_min_cpy = p_tacho_min;
	if (tacho_captured) tacho_min_cpy = p_tacho_min;
	return tacho_min_cpy;
}

unsigned int get_tacho_max(){
	tacho_captured=false;
	unsigned int tacho_max_cpy = p_tacho_max;
	if (tacho_captured) tacho_max_cpy = p_tacho_max;
	return tacho_max_cpy;
}

//returns tacho time
unsigned int get_tacho_time(){
	tacho_captured=false;
	unsigned int tacho_time_cpy = tacho_time;
	if (tacho_captured) tacho_time_cpy = tacho_time;
	return tacho_time_cpy;
}

unsigned int get_water_level(){
	water_level_captured=false;
	unsigned int water_level_cpy = water_level;
	if (water_level_captured) water_level_cpy=water_level;
	return water_level_cpy;
}

unsigned int get_rpm(){
	//  (10000/ tacho_time/16*8)*60 -> 60000 / (tacho_time / 2)
	//return ((unsigned int)((unsigned long)639559 / (unsigned long)(tacho_time))) << 1;
	
	//tacho_time * 2 / 3 --> time in µS --> 10 000 / (tacho_time * 2 / 3) *60 --> 15 000 / tacho_time *60
	unsigned long tacho_time_cpy=get_tacho_time();
	if (tacho_time_cpy==0) return 0;
	return (unsigned int) ((unsigned long)900000 / tacho_time_cpy);
	
	
}


unsigned char power_adjust_delay=2;
unsigned char power_adjust_idx=0;

//adjusts power to match wanted speed
void adjust_power(){
	unsigned int wanted;
	
	if (!speed_control_enabled) return; //exit if auto speed control is disabled
	
	if (direction){
		wanted = -wanted_speed;
	}else{
		wanted = wanted_speed;
	}
	
	unsigned int upper = wanted + wanted  / 15;
	unsigned int lower = wanted - wanted  / 15;
	
	current_speed = get_rpm();
	
	if (wanted>=60){
		power_adjust_delay=2;
	}else{
		if (current_speed<20){
			power_adjust_delay=2;
		}else if (current_speed < 20){
			power_adjust_delay=10;
		}else if (current_speed < 30){
			power_adjust_delay=18;
		}else if (current_speed < 50){
			power_adjust_delay=30;
		}else{
			power_adjust_delay=60;
		}
	}
	

	if (changing_direction){
		if (current_speed<10){
			changing_direction=0;
			DIRECTION_OUTPUT=direction;
			set_power(0);
		}	
	}else{
		if (wanted == 0){
			set_power(0);
		}else if (current_speed < lower){
			if (output_power<20){
				set_power(25);
			}
			if (output_power<100){
				if (output_power > 45 && current_speed < 5) output_power = 45; //saftey prevent power going up if no voltage goes to motor
				if (power_adjust_idx>=power_adjust_delay || output_power < 25){
					set_power(output_power+1);
					power_adjust_idx=0;
				}
				power_adjust_idx++;
			}
		}else if (current_speed > upper){
			if (output_power<10) output_power=0;
			if (output_power>0){
				set_power(output_power-1);
			}
		}
	}

}

void set_wanted_speed(int rpm){
	if (rpm==0){
		set_power(0);
	}else if (rpm < 0){
		if (direction==0){
			direction=1;
			changing_direction=1;
			set_power(0);
		}
	}else{
		if (direction==1){
			direction=0;
			changing_direction=1;
			set_power(0);
		}
	}
	wanted_speed=rpm;
}

void read_temperature(){
	if(adcon0.GO_DONE==0){
		if(adc_started){
			MAKESHORT(temperature,adresl,adresh);
			adc_started=0;
		}
		adcon0.GO_DONE=1;
		adc_started=1;
	}
}

void init_ADC(){
	adcon0 = 0;
	adcon1 = 0b1110; // AN0 is analog input, all others are digital, VSS and VDD as voltage references	
	
	adcon2.ADFM =1;   //right side adjustment
	adcon2.ADCS2=1; //conversion clock= 48/64
	adcon2.ADCS1=1;
	adcon2.ADCS0=0;
	
	adcon2.ACQT2=1;
	adcon2.ACQT1=0;
	adcon2.ACQT0=0; //wait sampling time 8tad +/-10µs?
	adcon0 = 1;     //enable A/D
}

bool is_int(unsigned char * str){
	if (*str=='-') *str++;
	if (*str==0) return false;
	while (*str>='0' && *str<='9') str++;
	return *str==0; //end of string no other characters
}

void main()
{
	//tacho_time_ptr=(unsigned char *) & tacho_time;
	//general configuration
	ucon.3=0; //turn off USB
	ucfg.3=1; //turn usb transceiver off	
	
	rcon.IPEN=1; //enable interrupt priority
	adcon1 = 0x0F; //initialize the AD con 1 to enable digital inputs
	ipr2.USBIP=0; //low priority for USB interrupts
	
	//analog
	init_ADC();
	
	//IO:
	TRIAC_OUTPUT=TRIAC_OFF;
	cmcon=7; //disable comparator	
	trisa.0=1; //analog input
	trisa.2=0; //output boost
	trisa.3=0; //output direction
	trisa.4=0; //triac
	trisa.5=0; //LED
	trisa.1=0; //test
	trisb.0=1; //INT for zero crossing
	TRIAC_OUTPUT=TRIAC_OFF;
	
	trisc.1=1; //CCP2 input
	trisc.2=1; //CCP1 input
	trisc.7=1; //RX
	trisc.6=0; //TX
	
	LED_OUTPUT=1;
	BOOST_OUTPUT=0;
	DIRECTION_OUTPUT=0;
	
	//init serial port
	serial_init();
	
	//configure edge detection for zero crossing, synchronizing the TMR2
	intcon2.INTEDG0=0; //falling edge
	intcon.INT0F=0;
	intcon.INT0E=1;
	
	//configure TMR2 used for determing when to turn on TRIAC
	t2con.T2CKPS0=1;
	t2con.T2CKPS1=1;
	t2con.TOUTPS0=0; // 1
	t2con.TOUTPS1=0;
	t2con.TOUTPS2=0;
	t2con.TOUTPS3=0;
	pr2=74; // 750kHz / 75 = 10KHz
	pie1.TMR2IE=1;
	t2con.TMR2ON=1;
	
	//configure TMR0 for "RTC"
	t0con.T0CS=0;
	t0con.PSA=0;
	t0con.T0PS0=0;
	t0con.T0PS1=0;
	t0con.T0PS2=0;
	t0con.T08BIT=0;
	intcon.TMR0IE=1;
	intcon2.TMR0IP=0;
	t0con.TMR0ON=1;
	
	//configure TMR1 1.5 MHz and capture module for tacho coil
	t1con.RD16=1;
	t1con.TMR1CS=0;
	t1con.T1CKPS0=1;
	t1con.T1CKPS1=1;
	t1con.TMR1ON=1;
	ipr1.TMR1IP=1;
	pie1.TMR1IE=1;
	
	ccp1con.CCP1M0=1; //capture every rising edge
	ccp1con.CCP1M1=0;
	ccp1con.CCP1M2=1;
	ccp1con.CCP1M3=0;
	pir1.CCP1IF=0;
	pie1.CCP1IE=1;
	
	//set T3 for capture the water level sensor frequency (22-24KHz)
	//T3 runs at 1.5MHz capturing 16 rising edges = value * 1/1500000/16
	t3con.TMR3CS=0;
	t3con.RD16=1;
	t3con.T3CCP1=1;
	t3con.T3CCP2=0;	
	t3con.T3CKPS0=1;
	t3con.T3CKPS1=1;
	t3con.TMR3ON=1;
	ipr2.TMR3IP=0;
	pie2.TMR3IE=1;
	
	ccp2con.CCP2M0=1; //capture every 16th rising edge
	ccp2con.CCP2M1=1;
	ccp2con.CCP2M2=1;
	ccp2con.CCP2M3=0;
	pir2.CCP2IF=0;
	pie2.CCP2IE=1;	
	
	intcon.GIE=1;
	intcon.PEIE=1;

	if (rcon.TO==0){
		serial_send_string_line("wad!");
	}else{
		serial_send_string_line("start");
	}
	
	unsigned int i=0;
	unsigned int p_clock=0;
	
	while (1){
		if (output_power==0){
			triac_on=255;
			next_triac_on=255;
			TRIAC_OUTPUT=TRIAC_OFF; //make sure output is off
		}
		
		if (p_clock > clock) {
			p_clock=0;
		}
		
		
		if ((clock-p_clock)>9){ //+/- each 100m
			adjust_power();
			read_temperature();
			
			if (i==10){
				LED_OUTPUT = ~ LED_OUTPUT;
				serial_send_string("{pwr:");
				serial_send_int(output_power);
				serial_send_string(",tacho:");
				serial_send_long(get_tacho_time());
				serial_send_string(",rpm:");
				serial_send_uint(get_rpm());
				serial_send_string(",temp:");
				serial_send_uint(temperature);
				serial_send_string(",spd:");
				serial_send_int(wanted_speed);
				serial_send_string(",water:");
				serial_send_uint(get_water_level());
				serial_send_string(",err:");
				serial_send_uint(error);
				serial_send_string("}\n");
				i=0;
				error=0;	
			}			

			p_clock=clock;	
			i++;
		}
		
		if (serial_cmd_received){
			if (strcmp(rx_buff, "B=1")==0){
				BOOST_OUTPUT=1;
				serial_send_string_line("OK");
			}else if (strcmp(rx_buff, "B=0")==0){
				BOOST_OUTPUT=0;
				serial_send_string_line("OK");
			}else if (strncmp(rx_buff, "P=",2)==0){
				if (is_int(&rx_buff[2])){
					speed_control_enabled=false;
					set_power(atoi(&rx_buff[2]));
					serial_send_string_line("OK");
				}
			}else if (strncmp(rx_buff, "S=",2)==0){
				if (is_int(&rx_buff[2])){
					speed_control_enabled=true;
					set_wanted_speed(atoi(&rx_buff[2]));
					serial_send_string_line("OK");
				}
			}
			rx_idx=0;
			serial_cmd_received=0;
		}
		
		clear_wdt();
	}

}
