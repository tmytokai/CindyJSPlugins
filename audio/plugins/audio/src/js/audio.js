/*
Copyright (C) 2018 tmytokai

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

    let SetupAudioContext = function () {

	if( audioCtx == null ){
	    audioCtx = new (window.AudioContext || window.webkitAudioContext)();
	}
    }

    let SetupBufferSrc = function ( buffer ) {

	if( audioBufferSrc == null ){
	    audioBufferSrc = audioCtx.createBufferSource();
	    audioBufferSrc.buffer = buffer;
	    audioBufferSrc.loop = true;

	    console.log( "rate = " + audioBufferSrc.buffer.sampleRate );
	    console.log( "duration = " + audioBufferSrc.buffer.duration );
	    console.log( "channels = " + audioBufferSrc.buffer.numberOfChannels );
	}
    }

    let SetupSplitter = function () {

	if( audioSplitter == null ){
	    audioSplitter = audioCtx.createChannelSplitter( audioBufferSrc.buffer.numberOfChannels );
	}
    }

    let SetupAnalyser = function () {

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

    let SetupProcessor = function () {

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

    let SetupCanvas = function () {

	CreateColormap();
	for( let ch = 0; ch < audioConfigs.maxChannels; ++ch ){
	    CreateCanvas(ch);
	    ClearCanvas(ch);
	}
	audioSpecGram.pos = 0;
    }

    let CreateColormap = function(){

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

    let CreateCanvas = function( ch ){

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

    let ClearCanvas = function( ch ){

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

    let RenderCanvas = function ( ch ){

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

    let PlayAudio = function ( buffer ) {

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

    let PlayLocalAudio = function ( id ) {

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

    let PlayOnlineAudio = function ( url ) {

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

    let GetDstPoint = function( ch, dst ){

        let clw = api.instance["canvas"]["clientWidth"];
        let clh = api.instance["canvas"]["clientHeight"];
        let iw = api.instance["canvas"]["width"];
        let ih = api.instance["canvas"]["height"];
        let m = api.getInitialMatrix();
	let transf = function (m, px, py) { //copied from evaluator.screen$0 in src/js/libcs/Operators.js 
	    let xx = px - m.tx;
	    let yy = py + m.ty;
	    let x = (xx * m.d - yy * m.b) / m.det;
	    let y = -(-xx * m.c + yy * m.a) / m.det;
	    return {
		x: x,
		y: y
	    };
	};
        let cul = transf(m,0,0);
        let cll = transf(m,0,clh);
        let clr = transf(m,clw,clh);
	let dstx = Math.abs( (dst.x - cll.x) / (clr.x - cll.x) ) * clw;
	let dsty = Math.abs( (cul.y - dst.y) / (cul.y - cll.y) ) * clh;
/*
	console.log( "dst " + dst.x + " / " + dst.y );
	console.log( "cul " + cul.x + " / " + cul.y );
	console.log( "cll " + cll.x + " / " + cll.y );
	console.log( "clr " + clr.x + " / " + clr.y );
	console.log( "dstx " + dstx );
	console.log( "dsty " + dsty );
*/
	return [dstx, dsty];
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

	let dst = api.extractPoint( api.evaluateAndVal( args[0] ) );
	let dstpoint = GetDstPoint( ch, dst );

	if( csctx == null ){
            csctx = api.instance["canvas"].getContext("2d");
	    if( csctx == null ) return api.nada;
	}

	if( audioSpec.canvas[ch] == null ){
	    csctx.fillStyle = "black";
	    csctx.fillRect( dstpoint[0], dstpoint[1], audioSpec.Width, audioSpec.Height );
	    return api.nada;
        }

	csctx.putImageData( audioSpec.image[ch], dstpoint[0], dstpoint[1] );

        return api.nada;
    });

    api.defineFunction( "audiodrawspectrogram", 2, function(args, modifs) {

	let ch = api.evaluate( args[1] );
	if( ch.ctype != "number" ) return api.nada;
	ch = ch.value.real;

	let dst = api.extractPoint( api.evaluateAndVal( args[0] ) );
	let dstpoint = GetDstPoint( ch, dst );

	if( csctx == null ){
            csctx = api.instance["canvas"].getContext("2d");
	    if( csctx == null ) return api.nada;
	}

	if( audioSpecGram.canvas[ch] == null ){
	    csctx.fillStyle = "hsl(240, 100%, 50%)";
	    csctx.fillRect( dstpoint[0], dstpoint[1], audioSpecGram.Width, audioSpecGram.Height );
	    return api.nada;
        }

	csctx.putImageData( audioSpecGram.image[ch], 
			    dstpoint[0]-audioSpecGram.pos, 
			    dstpoint[1], 
			    audioSpecGram.pos, 0, audioSpecGram.Width-audioSpecGram.pos, audioSpecGram.Height );
	if( audioSpecGram.pos > 0 ){
	    csctx.putImageData( audioSpecGram.image[ch], 
				dstpoint[0] + audioSpecGram.Width - audioSpecGram.pos, 
				dstpoint[1], 0, 0, audioSpecGram.pos, audioSpecGram.Height );
	}

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
