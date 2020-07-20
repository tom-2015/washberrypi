
/*after loading page*/
$(function () {
		update_status();
		$('#dlg_start_prgram').dialog({ autoOpen: false,
									  height: 400,
									  width: 350,
									  modal: true,
									  title: "Start program",
									  buttons: {
										"Start": function () { start_program(); dialog.dialog( "close" ); },
										"Annuleren": function() {
										  dialog.dialog( "close" );
										}
									  },
									  close: function() {
										$('#frm_start')[0].reset();
									  }
									});
									
		$('#dlg_message,#dlg_ask_question').dialog({ autoOpen: false});
		
		$('.jq-button').button();
		$('#cmd_start_program').on("click", function (){
											$('#dlg_start_prgram').dialog('open');
										});
		$('#cmd_stop_program').on("click", function (){
									confirm_yes_no("Stoppen?", function () {
																	stop_program();
																	});
								});
								
		$('#cmd_continue_program').on("click", function (){
	
						});
	}
);

/*
updates status text
*/
function update_status (){
	$.getJSON( "status", function( machine ) {
		var state = machine.state;
		var current_block = null;
		
		switch (machine.state){
			case 0: //stand by
				$('#status').text("Standby");
				$('#cmd_start_program').button('enable');
				$('#cmd_stop_program').button('disable');
				$('#cmd_continue_program').button('disable');
				break;
			case 1: //washing
				current_block = machine.program.blocks[machine.program.current_block];
				var current_block_time = Math.floor(current_block.time_left / 3600) + ':' + Math.floor((current_block.time_left / 60) % 60) + ':' + Math.floor(current_block.time_left % 60)
				
				$('#status').text("Bezig");
				$('#current_state').text(current_block.name + ' (' + current_block_time + ')');
				$('#cmd_start_program').button('disable');
				$('#cmd_stop_program').button('enable');
				$('#cmd_continue_program').button('disable');
				break;
			case 2: //wait for input
				$('#status').text("Wachten");
				$('#current_state').text(current_block.name);
				$('#cmd_start_program').button('disable');
				$('#cmd_stop_program').button('enable');
				$('#cmd_continue_program').button('enable');
				break;
			case 3: //ready
				$('#status').text("Klaar");
				$('#current_state').text("");
				$('#cmd_start_program').button('enable');
				$('#cmd_stop_program').button('disable');
				$('#cmd_continue_program').button('disable');
				break;
			case 4: //aborted
				$('#status').text("Gestopt");
				$('#current_state').text("");
				$('#cmd_start_program').button('enable');
				$('#cmd_stop_program').button('disable');
				$('#cmd_continue_program').button('disable');
				break;
			case 5: //error
				$('#status').text("Fout: " + machine.program.err);
				$('#cmd_start_program').button('enable');
				$('#cmd_stop_program').button('disable');
				$('#cmd_continue_program').button('disable');
				break;
		}
		
		$('#current_temp').text(machine.temp);
		$('#current_water').text(machine.water);
		$('#current_rpm').text(machine.rpm);
		
		
		if (machine.state){
			//var t = new Date();
			//t.setSeconds( machine.program.time_left);
			$('#time_left').text(Math.floor(machine.program.time_left / 3600) + ':' + Math.floor((machine.program.time_left / 60) % 60) + ':' + Math.floor(machine.program.time_left % 60)); //t.getHours()+':'+t.getMinutes()+':'+t.getSeconds());
		}else{
			$('#time_left').text('');
		}
		

	});
}

/*
starts a program
*/
function start_program(){
	var form = $('#frm_start');
	$.ajax({
      type: "POST",
	  dataType: "json",
      url: form.attr( 'action' ),
      data: form.serialize(),
      success: function( response ) {
		  if (response.error==0){
			  show_message('ok');
		  }
          console.log( response );
      }
    });
}

function stop_program(){
	$.ajax({
      type: "POST",
	  dataType: "json",
      url: "/stop",
      success: function( response ) {
		  if (response.error==0){
			  show_message('ok');
		  }
          console.log( response );
      }
    });	
}

function continue_program(){
	$.ajax({
      type: "POST",
	  dataType: "json",
      url: "/continue",
      success: function( response ) {
		  if (response.error==0){
			  show_message('ok');
		  }
          console.log( response );
      }
    });	
}

function confirm_yes_no (message, on_yes, on_no){
    $('#dlg_ask_question').html(message);
    $( "#dlg_ask_question" ).dialog({
                                title: 'Opgelet',
                                resizable: false,
                                modal: true,
                                buttons: {
                                    "Ja": function() {
                                        if (on_yes!=null) on_yes();
                                        dialog.dialog( "close" );
                                    },
                                    "Nee": function() {
                                        if (on_no!=null) on_no();
                                        dialog.dialog( "close" );
                                    }
                                  }
                                });   

}

function show_message(message, on_message_close){
    $('#dlg_message').html(message);
    $("#dlg_message" ).dialog({
                                title: "message",
                                resizable: false,
                                modal: true,
                                buttons: {
                                    "OK": function() {
                                        if (on_message_close!=null) on_message_close();
                                        $( this ).dialog( "close" );
                                    }
                                  }
                                });     
}

setInterval(update_status, 5000);
	