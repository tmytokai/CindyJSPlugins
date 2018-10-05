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

CindyJS.registerPlugin(1, "UnityCindy3D", function(api) {

    const MODIFIERS = {
	Radius : 0,
	Color  : 1,
	Colors : 2,
	Topology : 3
    };

    const TOPOLOGY = {
	Open : 0,
	Close : 1
    };

    //////////////////////////////////////////////////////////////////////
    // API bindings

    let nada = api.nada;
    let evaluate = api.evaluate;
    let defOp = api.defineFunction;

    //////////////////////////////////////////////////////////////////////
    // Global variables

    let gameobjs = [];
    let thisid = -1;
    let idx_uc3dBuffer = 0;

    //////////////////////////////////////////////////////////////////////
    // Helper functions

    const initOperation = function () {
	if( typeof(uc3dBuffer) == "undefined" ) return false;
	idx_uc3dBuffer = 0;
	return true;
    }

    const setId = function ( id ){
	uc3dBuffer[idx_uc3dBuffer++] = id;
    }

    const setBoolean = function ( bl ){
	uc3dBuffer[idx_uc3dBuffer++] = bl;
    }

    const setPointsFromArgs = function ( args, offset, pointssize ) {
	uc3dBuffer[idx_uc3dBuffer++] = pointssize;
	for(let p = offset; p < offset + pointssize ; ++p ){
	    let pos = coerce.toHomog(evaluate(args[p]));
	    for (let i = 0; i < 3; i++) {
		uc3dBuffer[idx_uc3dBuffer++] = pos[i];
	    }
	}
    }

    const setPointsFromList = function ( lst ){
	let pointssize = lst.length;
	uc3dBuffer[idx_uc3dBuffer++] = pointssize;
	for(let p = 0; p < pointssize ; ++p ){
	    let pos = coerce.toHomog(lst[p]);
	    for (let i = 0; i < 3; i++) {
		uc3dBuffer[idx_uc3dBuffer++] = pos[i];
	    }
	}
    }

    const setModifs = function ( modifs, _size, colors ) {

	let color = null;
	let alpha = null;
	let shininess = null;
	let size = _size;
	let topology = null;

	let handlers = {
	    "color": ( a => color = coerce.toColor(a) ),
	    "alpha": ( a => alpha = coerce.toInterval(0, 1, a) ),
	    "shininess": ( a => shininess = coerce.toInterval(0, 128, a) ),
	    "size": ( a => size = coerce.toReal(a) ),
	    "topology": ( a => topology = coerce.toString(a, topology).toLowerCase() ),
	};

	for( let key in modifs ){
	    let handler = handlers[ key ];
	    if ( handler != null ) handler( evaluate( modifs[key] ) );
	}

	if( color != null ){
	    uc3dBuffer[idx_uc3dBuffer++] = MODIFIERS.Color;
	    uc3dBuffer[idx_uc3dBuffer++] = color[0];
	    uc3dBuffer[idx_uc3dBuffer++] = color[1];
	    uc3dBuffer[idx_uc3dBuffer++] = color[2];
	}

	if( colors != null ){
	    uc3dBuffer[idx_uc3dBuffer++] = MODIFIERS.Colors;
	    uc3dBuffer[idx_uc3dBuffer++] = colors.length;
            for (let i = 0; i < colors.length; ++i) {
		let cl = coerce.toColor( colors[i] );
 		uc3dBuffer[idx_uc3dBuffer++] = cl[0];
		uc3dBuffer[idx_uc3dBuffer++] = cl[1];
		uc3dBuffer[idx_uc3dBuffer++] = cl[2];
	    }
	}

	if( size != null ){
	    uc3dBuffer[idx_uc3dBuffer++] = MODIFIERS.Radius;
	    uc3dBuffer[idx_uc3dBuffer++] = size;
	}

	if( topology != null ){
	    uc3dBuffer[idx_uc3dBuffer++] = MODIFIERS.Topology;
	    if( topology == "close" ) uc3dBuffer[idx_uc3dBuffer++] = TOPOLOGY.Close;
	    else uc3dBuffer[idx_uc3dBuffer++] = TOPOLOGY.Open;
	}

	uc3dBuffer[idx_uc3dBuffer] = -1;
    }

    //////////////////////////////////////////////////////////////////////
    // Object handling functions

    defOp("clear3d", 0, function(args, modifs) {

	if( initOperation() == false ) return nada;

	gameInstance.SendMessage ( 'Manager', 'Clear3D', "" ); 

	gameobjs = [];
	thisid = -1;
	idx_uc3dBuffer = 0;

	return nada;
    });

    defOp("begin3d", 0, function(args, modifs) {
	return begin3dImpl( "Default", modifs, true );
    });

    defOp("begin3d", 1, function(args, modifs) {
	return begin3dImpl( coerce.toString(evaluate(args[0])), modifs, true );
    });

    defOp("createprototype3d", 1, function(args, modifs) {
	return begin3dImpl( coerce.toString(evaluate(args[0])), modifs, false );
    });

    const begin3dImpl = function ( classname, modifs, active ){

	if( thisid != -1 ) return nada;
	if( initOperation() == false ) return nada;

	setBoolean( active );

	gameInstance.SendMessage ( 'Manager', 'Begin3D', classname );
	thisid = Math.floor(uc3dBuffer[0]);

	let gameobj = 
	    {
		id: thisid,
		classname: classname,
		active: active,
		init: false,
		start: null,
		update: null
	    };
	gameobjs.push( gameobj );

	return {
	    "ctype": "number",
	    "value": {
		'real': thisid,
		'imag': 0
	    }
	};
    }

    defOp("instantiate3d", 2, function(args, modifs) {
	let ret = instantiate3dImpl( args, modifs, true );
	return {
	    "ctype": "number",
	    "value": {
		'real': ret,
		'imag': 0
	    }
	};
    });

    defOp("extends3d", 1, function(args, modifs) {

	let ret = -1;

	if( thisid == -1 ){
	    ret = instantiate3dImpl( args, modifs, false );
	    thisid = ret;
	}

	return {
	    "ctype": "number",
	    "value": {
		'real': ret,
		'imag': 0
	    }
	};
    });

    const instantiate3dImpl = function ( args, modifs, active ){

	let ret = -1;

	if( initOperation() == false ) return ret;

	let id = coerce.toInt( evaluate(args[0] ) );

	gameobjs.forEach( obj => {

	    if( obj.id == id ){

		setId( id );

		setBoolean( active );

		if( active ) setPointsFromArgs( args, 1, 1 );

		gameInstance.SendMessage ( 'Manager', 'Instantiate3D', "" );
		ret = Math.floor(uc3dBuffer[0]);

		let gameobj = 
		    {
			id: ret,
			classname: obj.classname,
			active: active,
			init: false,
			start: obj.start,
			update: obj.update
		    };
		gameobjs.push( gameobj );
	    }
	} );

	return ret;
    }

    defOp("end3d", 0, function(args, modifs) {

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	gameInstance.SendMessage ( 'Manager', 'End3D', "" );

	thisid = -1;

	return nada;
    });

    //////////////////////////////////////////////////////////////////////
    // Appearance handling functions

    defOp("draw3d", 1, function(args, modifs) {

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setPointsFromArgs( args, 0, 1 );

	setModifs( modifs, null, null );

	gameInstance.SendMessage ( 'Manager', 'AddPoint3D', "" );

	return nada;
    });

    defOp("draw3d", 2, function(args, modifs) {

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setPointsFromArgs( args, 0, 2 );

	setModifs( modifs, null, null );

	gameInstance.SendMessage ( 'Manager', 'AddLine3D', "" );

	return nada;
    });

    defOp("connect3d", 1, function(args, modifs) {
	return connect3dImpl(args, modifs);
    });

    defOp("connect3d", 2, function(args, modifs) {
	return connect3dImpl(args, modifs);
    });

    const connect3dImpl = function (args, modifs){

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setPointsFromList( coerce.toList( evaluate(args[0]) ) );

	let colors = null;
	if( args.length == 2 ) colors = coerce.toList(evaluate(args[1]));
	setModifs( modifs, null, colors);

	gameInstance.SendMessage ( 'Manager', 'AddLine3D', "" );

	return nada;
    }

    defOp("fillpoly3d", 1, function(args, modifs) {

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setPointsFromList( coerce.toList( evaluate(args[0]) ) );

	setModifs( modifs, null, null );

	gameInstance.SendMessage ( 'Manager', 'AddPolygon3D', "" );

	return nada;
    });

    defOp("drawsphere3d", 2, function(args, modifs) {

	if( thisid == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setPointsFromArgs( args, 0, 1 );

	let radius = coerce.toReal(evaluate(args[1]));
	setModifs( modifs, radius, null );

	gameInstance.SendMessage ( 'Manager', 'AddSphere3D', "" );

	return nada;
    });

    //////////////////////////////////////////////////////////////////////
    // Property handling functions

    defOp("setactive3d", 1, function(args, modifs) {
	return setactive3dImpl( thisid, args, 0, modifs );
    });

    defOp("setactive3d", 2, function(args, modifs) {
	return setactive3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const setactive3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	let gameobj = null;
	gameobjs.forEach( obj => { if( obj.id == id ) gameobj = obj; } );
	if( gameobj == null ) return nada;

	let act = coerce.toBool( evaluate(args[offset+1]) );
	gameobj.active = act;

	setId( id );

	setBoolean( act, null );

	gameInstance.SendMessage ( 'Manager', 'SetActive3D', "" );

	return nada;
    }

    defOp("usegravity3d", 1, function(args, modifs) {
	return usegravity3dImpl( thisid, args, 0, modifs );
    });

    defOp("usegravity3d", 2, function(args, modifs) {
	return usegravity3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const usegravity3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	setBoolean( coerce.toBool( evaluate(args[offset]) ), null );

	gameInstance.SendMessage ( 'Manager', 'UseGravity3D', "" );

	return nada;
    }

    defOp("addcollider3d", 0, function(args, modifs) {
	return addcollider3dImpl( thisid, args, modifs );
    });

    defOp("addcollider3d", 1, function(args, modifs) {
	return addcollider3dImpl( coerce.toInt( evaluate(args[0]) ), args, modifs );
    });

    const addcollider3dImpl = function ( id, args, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	gameInstance.SendMessage ( 'Manager', 'AddCollider3D', "" );

	return nada;
    }

    //////////////////////////////////////////////////////////////////////
    // Method handling functions

    defOp("setstart3d", 1, function(args, modifs) {
	return setMethod( thisid, "start", args, 0, modifs );
    });

    defOp("setstart3d", 2, function(args, modifs) {
	return setMethod( coerce.toInt( evaluate(args[0]) ), "start", args, 1, modifs );
    });

    defOp("setupdate3d", 1, function(args, modifs) {
	return setMethod( thisid, "update", args, 0, modifs );
    });

    defOp("setupdate3d", 2, function(args, modifs) {
	return setMethod( coerce.toInt( evaluate(args[0]) ), "update", args, 1, modifs );
    });

    const setMethod = function ( id, method, args, offset, modifs ){

	if( id == -1 ) return nada;

	let gameobj = null;
	gameobjs.forEach( obj => { if( obj.id == id ) gameobj = obj; } );
	if( gameobj == null ) return nada;

	if (args[offset]["ctype"] !== "function") {
	    console.log("argument is not a function");
	    return nada;
	};

        gameobj[ method ] = args[offset]["oper"].replace( "$0", "();" );

	return nada;
    }

    defOp("update3d", 0, function(args, modifs) {

	if( cdy == null ) return nada;

	gameobjs.forEach( obj => {
	    
	    if( obj.active ){
		if( ! obj.init && obj.start != null ){
		    thisid = obj.id;
		    cdy.evokeCS( obj.start );
		    obj.init = true;
		}
		if( obj.update != null ){
		    thisid = obj.id;
		    cdy.evokeCS( obj.update );
		}
	    }
	});

	return nada;
    });

    //////////////////////////////////////////////////////////////////////
    // Move functions

    defOp("setposition3d", 1, function(args, modifs) {
	return setposition3dImpl( thisid, args, 0, modifs );
    });

    defOp("setposition3d", 2, function(args, modifs) {
	return setposition3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const setposition3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	setPointsFromArgs( args, offset, 1 );

	gameInstance.SendMessage ( 'Manager', 'SetPosition3D', "" );

	return nada;
    }

    defOp("getposition3d", 0, function(args, modifs) {
	return getposition3dImpl( thisid, args, 0, modifs );
    });

    defOp("getposition3d", 1, function(args, modifs) {
	return getposition3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const getposition3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	gameInstance.SendMessage ( 'Manager', 'GetPosition3D', "" );
	let x = {
	    "ctype": "number",
	    "value": {
		'real': uc3dBuffer[0],
		'imag': 0
	    }
	};
	let y = {
	    "ctype": "number",
	    "value": {
		'real': uc3dBuffer[1],
		'imag': 0
	    }
	};
	let z = {
	    "ctype": "number",
	    "value": {
		'real': uc3dBuffer[2],
		'imag': 0
	    }
	};

	return {
	    "ctype": "list",
	    "value": [x,y,z]
	};
    }

    defOp("setrotation3d", 1, function(args, modifs) {
	return setrotation3dImpl( thisid, args, 0, modifs );
    });

    defOp("setrotation3d", 2, function(args, modifs) {
	return setrotation3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const setrotation3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	setPointsFromArgs( args, offset, 1 );

	gameInstance.SendMessage ( 'Manager', 'SetRotation3D', "" );

	return nada;
    };

    defOp("setvelocity3d", 1, function(args, modifs) {
	return setvelocity3dImpl( thisid, args, 0, modifs );
    });

    defOp("setvelocity3d", 2, function(args, modifs) {
	return setvelocity3dImpl( coerce.toInt( evaluate(args[0]) ), args, 1, modifs );
    });

    const setvelocity3dImpl = function ( id, args, offset, modifs ){

	if( id == -1 ) return nada;
	if( initOperation() == false ) return nada;

	setId( id );

	setPointsFromArgs( args, offset, 1 );

	gameInstance.SendMessage ( 'Manager', 'SetVelocity3D', "" );

	return nada;
    }

    //////////////////////////////////////////////////////////////////////
    // Input functions

    defOp("getkey3d", 1, function(args, modifs) {

	let ret = false;
	if( initOperation() == false ) return nada;

	gameInstance.SendMessage ( 'Manager', 'GetKey3D', coerce.toString( evaluate(args[0]) ) );
	if( uc3dBuffer[0] ) ret = true;

	return { ctype: 'boolean', value: ret };
    });

});
