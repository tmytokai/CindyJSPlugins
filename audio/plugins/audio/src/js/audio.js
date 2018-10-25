/*
Copyright (C) 2018 https://github.com/tmytokai/CindyJSPlugins

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

CindyJS.registerPlugin( 1, "audio", function(api) {

    "use strict";

    // Canvas Context
    let csctx = null;

    // AudioContext
    let audioCtx = null; 

    // AudioNodes
    let audioBufferSrc = null;
    let audioSplitter = null;
    let audioAnalyser = [null,null]; // left = 0, right = 1
    let audioProcessor = null;

    let audioStarttime = 0;

    // global configs
    let audioConfigs = {
	maxChannels : 2,
    }

    // data structure for FFT
    let audioFft = {
	data : [null,null], // left = 0, right = 1

	// default
	Smoothing : 0,
	FftSize : 2048,
	MinDecibels : -100,
	MaxDecibels : -30,
    }

    // data structure for spectrum
    let audioSpec = {
	canvas : [null,null], // left = 0, right = 1
	canvasCtx : [null,null], // left = 0, right = 1
	image : [null,null], // left = 0, right = 1

	// default
	Width : 256,
	Height : 256,
    }

    // data structure for spectrogram
    let audioSpecGram = {
	canvas : [null,null], // left = 0, right = 1
	canvasCtx : [null,null], // left = 0, right = 1
	image : [null,null], // left = 0, right = 1

	pos : 0,

	// default
	Width : 256,
	Height : 256,

        minfreq : 0,
        maxfreq : 0,
    }

    // data structure for colormap
    let audioColorMap = {
	canvas : null,
	canvasCtx : null,
	image : null,

	// default
	Width : 10,
	Height : 256,
    }

    const SetupAudioContext = function () {

	if( audioCtx == null ){
	    audioCtx = new (window.AudioContext || window.webkitAudioContext)();
	}
    }

    const SetupBufferSrc = function ( buffer ) {

	if( audioBufferSrc == null ){
	    audioBufferSrc = audioCtx.createBufferSource();
	    audioBufferSrc.buffer = buffer;
	    audioBufferSrc.loop = true;

            audioSpecGram.maxfreq = audioBufferSrc.buffer.sampleRate/2;

	    console.log( "rate = " + audioBufferSrc.buffer.sampleRate );
	    console.log( "duration = " + audioBufferSrc.buffer.duration );
	    console.log( "channels = " + audioBufferSrc.buffer.numberOfChannels );
	}
    }

    const SetupSplitter = function () {

	if( audioSplitter == null ){
	    audioSplitter = audioCtx.createChannelSplitter( audioBufferSrc.buffer.numberOfChannels );
	}
    }

    const SetupAnalyser = function () {

	for( let ch = 0; ch < audioBufferSrc.buffer.numberOfChannels; ++ch ){

	    if( audioAnalyser[ch] == null ){
		audioAnalyser[ch] = audioCtx.createAnalyser();
		audioAnalyser[ch].smoothingTimeConstant = audioFft.Smoothing;
		audioAnalyser[ch].fftSize = audioFft.FftSize;
		audioAnalyser[ch].minDecibels = audioFft.MinDecibels;
		audioAnalyser[ch].maxDecibels = audioFft.MaxDecibels;
	    }
	}

	for( let ch = 0; ch < audioConfigs.maxChannels; ++ch ){
	    if( audioFft.data[ch] == null ){
		audioFft.data[ch] = new Float32Array( audioFft.FftSize/2 );
	    }
	}
    }

    const SetupProcessor = function () {

	if( audioProcessor == null ){

	    // NOTE: ScriptProcessorNode will be deprecated and replaced by Audio Worker
	    audioProcessor = audioCtx.createScriptProcessor( audioFft.FftSize, 
							     audioBufferSrc.buffer.numberOfChannels, 
							     audioBufferSrc.buffer.numberOfChannels );
	    audioProcessor.onaudioprocess = function( event ) {
		for( let ch = 0; ch < audioBufferSrc.buffer.numberOfChannels; ++ch ){
		    audioAnalyser[ch].getFloatFrequencyData( audioFft.data[ch] );
		    RenderCanvas( ch );
		}
		audioSpecGram.pos = ( audioSpecGram.pos+1 ) % audioSpecGram.Width;
	    };
	}
    }

    const SetupCanvas = function () {

	CreateColormap();
	for( let ch = 0; ch < audioConfigs.maxChannels; ++ch ){
	    CreateCanvas(ch);
	    ClearCanvas(ch);
	}
	audioSpecGram.pos = 0;
    }

    const CreateColormap = function(){

	if( audioColorMap.canvas == null ){
	    audioColorMap.canvas = document.createElement("canvas");
	    audioColorMap.canvas.id = "audiocolormap";
	    audioColorMap.canvas.style.display = "none";
	    audioColorMap.canvas.width = audioColorMap.Width;
	    audioColorMap.canvas.height = audioColorMap.Height;
	    audioColorMap.canvasCtx = audioColorMap.canvas.getContext("2d");
	    // hsl colormap
	    for( var i = 0; i < audioColorMap.canvas.height; ++i ){
		let hue = 240*i/audioColorMap.canvas.height;
		audioColorMap.canvasCtx.fillStyle = "hsl(" + hue + ", 100%, 50%)";
		audioColorMap.canvasCtx.fillRect(0, i, audioColorMap.canvas.width, 1);
	    }
	    audioColorMap.image = audioColorMap.canvasCtx.getImageData( 0, 0, audioColorMap.canvas.width, audioColorMap.canvas.height );
	}
    }

    const CreateCanvas = function( ch ){

	if( audioSpec.canvas[ch] == null ){
	    audioSpec.canvas[ch] = document.createElement("canvas");
	    audioSpec.canvas[ch].id = "audiocanvassp" + ch;
	    audioSpec.canvas[ch].style.display = "none";
	    audioSpec.canvas[ch].width = audioSpec.Width;
	    audioSpec.canvas[ch].height = audioSpec.Height;
	    audioSpec.canvasCtx[ch] = audioSpec.canvas[ch].getContext("2d");
	    audioSpec.image[ch] = audioSpec.canvasCtx[ch].getImageData( 0, 0, audioSpec.Width, audioSpec.Height );
	}

	if( audioSpecGram.canvas[ch] == null ){
	    audioSpecGram.canvas[ch] = document.createElement("canvas");
	    audioSpecGram.canvas[ch].id = "audiocanvasspgr" + ch;
	    audioSpecGram.canvas[ch].style.display = "none";
	    audioSpecGram.canvas[ch].width = audioSpecGram.Width;
	    audioSpecGram.canvas[ch].height = audioSpecGram.Height;
	    audioSpecGram.canvasCtx[ch] = audioSpecGram.canvas[ch].getContext("2d");
	    audioSpecGram.image[ch] = audioSpecGram.canvasCtx[ch].getImageData( 0, 0, audioSpecGram.Width, audioSpecGram.Height );
	}
    }

    const ClearCanvas = function( ch ){

	if( audioSpec.canvas[ch] != null ){
	    for( let idx = 0; idx < audioSpec.Height*audioSpec.Width*4; idx += 4 ){
		audioSpec.image[ch].data[ idx + 0 ] = 0;
		audioSpec.image[ch].data[ idx + 1 ] = 0;
		audioSpec.image[ch].data[ idx + 2 ] = 0;
		audioSpec.image[ch].data[ idx + 3 ] = 255;
	    }
	}

	if( audioSpecGram.canvas[ch] != null ){
	    let idx_cl = (audioColorMap.canvas.height-1) * audioColorMap.canvas.width * 4;
	    for( let idx = 0; idx < audioSpecGram.Height*audioSpecGram.Width*4; idx += 4 ){
		audioSpecGram.image[ch].data[ idx + 0 ] = audioColorMap.image.data[ idx_cl + 0 ];
		audioSpecGram.image[ch].data[ idx + 1 ] = audioColorMap.image.data[ idx_cl + 1 ];
		audioSpecGram.image[ch].data[ idx + 2 ] = audioColorMap.image.data[ idx_cl + 2 ];
		audioSpecGram.image[ch].data[ idx + 3 ] = 255;
	    }
	}
    }

    const RenderCanvas = function ( ch ){

	if( audioSpec.canvas[ch] != null ){

	    for( let x = 0; x < audioSpec.Width; ++x ){

		let db = audioFft.data[ch][ (audioFft.FftSize/2) * x / audioSpec.Width ];
		db = Math.min( audioFft.MaxDecibels, Math.max( audioFft.MinDecibels,  db ) );
		let height = Math.floor( (db - audioFft.MinDecibels) / (audioFft.MaxDecibels - audioFft.MinDecibels ) * audioSpec.Height );
		let r = 255;
		let g = 255;
		let b = 0;
		for( let y = 0; y < audioSpec.Height; ++y ){
		    let idx = ( (audioSpec.Height-1-y) * audioSpec.Width + x ) * 4;
		    if( y == height ){ r = g = b = 0; }
		    audioSpec.image[ch].data[ idx + 0 ] = r;
		    audioSpec.image[ch].data[ idx + 1 ] = g;
		    audioSpec.image[ch].data[ idx + 2 ] = 0;
		    audioSpec.image[ch].data[ idx + 3 ] = 255;
		}
	    }
	}

	if( audioSpecGram.canvas[ch] != null ){

	    for( let y = 0; y < audioSpecGram.Height; ++y ){

		let idx = ( (audioSpecGram.Height-1-y) * audioSpecGram.Width + audioSpecGram.pos ) * 4;

		let db = audioFft.data[ch][ (audioFft.FftSize/2) * y / audioSpecGram.Height ];
		db = Math.min( audioFft.MaxDecibels, Math.max( audioFft.MinDecibels,  db ) );
		let c = (db - audioFft.MinDecibels) / (audioFft.MaxDecibels - audioFft.MinDecibels )  * ( audioColorMap.canvas.height-1 );
		let idx_colormap = (audioColorMap.canvas.height-1 - Math.floor(c) ) * audioColorMap.canvas.width * 4;

		audioSpecGram.image[ch].data[ idx + 0 ] = audioColorMap.image.data[ idx_colormap + 0 ];
		audioSpecGram.image[ch].data[ idx + 1 ] = audioColorMap.image.data[ idx_colormap + 1 ];
		audioSpecGram.image[ch].data[ idx + 2 ] = audioColorMap.image.data[ idx_colormap + 2 ];
		audioSpecGram.image[ch].data[ idx + 3 ] = 255;
	    }
	}
    }

    const PlayAudio = function ( buffer ) {

	SetupBufferSrc( buffer );
	SetupSplitter();
	SetupAnalyser();
	SetupProcessor();
	SetupCanvas();

	audioBufferSrc.connect( audioSplitter );
	for( let ch = 0; ch < audioBufferSrc.buffer.numberOfChannels; ++ch ) audioSplitter.connect( audioAnalyser[ch], ch );

	audioBufferSrc.connect( audioProcessor );
	audioProcessor.connect( audioCtx.destination ); // dummy

	audioBufferSrc.connect( audioCtx.destination );

	audioStarttime = audioCtx.currentTime;
	audioBufferSrc.start( 0 );
    }

    const PlayLocalAudio = function ( id ) {

	if( audioBufferSrc != null ) return;

	console.log( "id " + id );

	let file = document.getElementById( id ).files[0];
	console.log( "loading " + file.name );

	var fr = new FileReader;
	fr.onload = function(){

	    SetupAudioContext();
	    audioCtx.decodeAudioData( 
		fr.result, 
		function ( buffer ){
		    PlayAudio( buffer );
		}, 
		function ( err ) {
		    console.log( "decodeAudioData failed");
		}
	    );
	}

	fr.readAsArrayBuffer( file );
    }

    const PlayOnlineAudio = function ( url ) {

	if( audioBufferSrc != null ) return;

	console.log( "loading " + url );

	let request = new XMLHttpRequest();
	request.open( 'GET', url, true );
	request.responseType = 'arraybuffer';
	request.onerror = function () {
	    console.log( "XMLHttpRequest failed" );
	};
	request.onload = function () {

	    SetupAudioContext();
	    audioCtx.decodeAudioData( 
		this.response, 
		function ( buffer ){
		    PlayAudio( buffer );
		}, 
		function ( err ) {
		    console.log( "decodeAudioData failed");
		}
	    );
	};

	request.send();
    }

    const PointToCanvasPoint = function( px, py ){

        let m = api.getInitialMatrix();
	return {
	   x: m.a*px - m.b*py + m.tx,
	   y: m.c*px - m.d*py - m.ty
	};
    }

    const CanvasPointToPoint = function( px, py ){

        let m = api.getInitialMatrix();
        var xx = px - m.tx;
        var yy = py + m.ty;
	return {
	   x:  ( m.d*xx - m.b*yy) / m.det,
	   y: -(-m.c*xx + m.a*yy) / m.det
	};
    }

    api.defineFunction( "playaudio", 1, function(args, modifs) {

	let src = api.evaluate( args[0] );
        if ( src.ctype !== "string") return api.nada;
	if( src.value.startsWith( "file://") ){
	    PlayLocalAudio( src.value.substr( 7 ) );
	}
	else{
	    PlayOnlineAudio( src.value );
	}

        return api.nada;
    });

    api.defineFunction( "stopaudio", 0, function(args, modifs) {

	if( audioBufferSrc == null ) return;

	audioBufferSrc.stop();
	audioCtx.close();

	audioCtx = null;
	audioBufferSrc = null;
	audioSplitter = null;
	audioAnalyser = [null,null];
	audioProcessor = null;

	return api.nada;
    });

    api.defineFunction( "audiospectrum", 2, function(args, modifs) {

	let spec = audioFft.MinDecibels;
	let ch = api.evaluate( args[0] );
	if( ch.ctype == "number" && audioFft.data[ ch.value.real ] != null ){
	    let idx = api.evaluate( args[1] );
	    if( idx.ctype == "number" ) spec = audioFft.data[ ch.value.real ][ idx.value.real ];
        }

        return {
            ctype: "number",
            value: { real: spec, imag: 0 }
        };
    });

    api.defineFunction( "audiodrawspectrum", 2, function(args, modifs) {

	let ch = api.evaluate( args[1] );
	if( ch.ctype != "number" ) return api.nada;
	ch = ch.value.real;

	let pt = api.extractPoint( api.evaluateAndVal( args[0] ) );
	let pt_canvas = PointToCanvasPoint( pt.x, pt.y );

	if( csctx == null ){
            csctx = api.instance["canvas"].getContext("2d");
	    if( csctx == null ) return api.nada;
	}

	if( audioSpec.canvas[ch] == null ){
	    csctx.fillStyle = "black";
	    csctx.fillRect( pt_canvas.x, pt_canvas.y, audioSpec.Width, audioSpec.Height );
	    return api.nada;
        }

	csctx.putImageData( audioSpec.image[ch], pt_canvas.x, pt_canvas.y );

        return api.nada;
    });

    api.defineFunction( "audiodrawspectrogram", 2, function(args, modifs) {

	let ch = api.evaluate( args[1] );
	if( ch.ctype != "number" ) return api.nada;
	ch = ch.value.real;

	let pt = api.extractPoint( api.evaluateAndVal( args[0] ) );
	let pt_canvas = PointToCanvasPoint( pt.x, pt.y );

	if( csctx == null ){
            csctx = api.instance["canvas"].getContext("2d");
	    if( csctx == null ) return api.nada;
	}

	if( audioSpecGram.canvas[ch] == null ){
	    csctx.fillStyle = "hsl(240, 100%, 50%)";
	    csctx.fillRect( pt_canvas.x, pt_canvas.y, audioSpecGram.Width, audioSpecGram.Height );
	    return api.nada;
        }

	csctx.putImageData( audioSpecGram.image[ch], 
			    pt_canvas.x-audioSpecGram.pos, 
			    pt_canvas.y, 
			    audioSpecGram.pos, 0, audioSpecGram.Width-audioSpecGram.pos, audioSpecGram.Height );
	if( audioSpecGram.pos > 0 ){
	    csctx.putImageData( audioSpecGram.image[ch], 
				pt_canvas.x + audioSpecGram.Width - audioSpecGram.pos, 
				pt_canvas.y, 0, 0, audioSpecGram.pos, audioSpecGram.Height );
	}

        // draw frequency
        let cscode = '';
        let dfreq = 2000.0;
        let f = audioSpecGram.minfreq;
        let n = ( audioSpecGram.maxfreq - audioSpecGram.minfreq ) / dfreq;
        let h = CanvasPointToPoint( 0, 0 ).y - CanvasPointToPoint( 0, audioSpecGram.Height ).y;
        let x = (pt.x-0.05);
        let dy = h / n;
        for( let i = 0; i <= Math.floor(n); ++i ){
          cscode += 'drawtext(['+x+','+(pt.y-h+dy*i-0.05)+'],'+f+',align->"right",size->10);';
          f += dfreq;
        }
        cscode += 'drawtext(['+x+','+(pt.y+0.05)+'],"Hz",align->"right",size->10);';
        api.instance.evalcs( cscode );

        return api.nada;
    });

    api.defineFunction( "audioposition", 0, function(args, modifs) {

	let pos = 0;
	if( audioBufferSrc != null ) pos = audioCtx.currentTime - audioStarttime;

        return {
            ctype: "number",
            value: { real: pos, imag: 0 }
        };
    });

});
