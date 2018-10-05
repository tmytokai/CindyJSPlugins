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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Server{

	private Decoder decoder;

	private bool listening;

	private TcpListener server;
	private TcpClient client;
	private NetworkStream stream;
	private int Port = 8000;

	private int BufferSize = 1024 * 1024;
	private byte[] buffer;

	public Server ( Decoder _decoder )
	{
		decoder = _decoder;

		server = null;
		client = null;
		stream = null;
		buffer = new Byte[ BufferSize ];

		listening = true;
		Connect();
	}

	public void Quit ()
	{
		listening = false;
		Disconnect ();
	}

	private void Connect()
	{
		// to use Task.Run(), set Scripting Runtime Version to .NET4.6 or later
		Task.Run (() => {
			if ( Listen () ) {
				if( listening ){
					Connect();  // reboot server
				}
			}
		});
	}

	private void Disconnect()
	{
		if (stream != null) {
			stream.Close ();
			stream = null;
		}

		if (client != null) {
			client.Close ();
			client = null;
		}				

		if (server != null) {
			server.Stop ();
			server = null;
		}
	}

	private bool Listen ()
	{  
		try {	
			Manager.DebugLog ("Server::Listen: listening...");

			server = new TcpListener (IPAddress.Loopback, Port);
			server.Start ();
			client = server.AcceptTcpClient ();

			Manager.DebugLog ("Server::Listen: connected");

			stream = client.GetStream ();

			while (true) {

				var size = 0;

				while (size < 1 + 4 ) {
					var tmpsize = stream.Read (buffer, size, (1 + 4) - size);
					if (tmpsize == 0) {
						break;
					}
					size += tmpsize;
				}
				if (size < 1 + 4) {
					break;
				}

				var chunksize = BitConverter.ToInt32 (buffer, 1);
				while (size < (1 + 4) + chunksize) {
					var tmpsize = stream.Read (buffer, size, (1 + 4) + chunksize - size);
					if (tmpsize == 0) {
						break;
					}
					size += tmpsize;
				}
				if (size < (1 + 4) + chunksize) {
					break;
				}

				decoder.Register(buffer, size);
			}
		} catch (SocketException e) {
			Manager.DebugLog ("Server::Listen: SocketException " + e.Message);
		} catch (Exception e) {			
			Manager.DebugLog ("Server::Listen: Exception " + e.Message);
		} finally {		
			Disconnect ();
			Manager.DebugLog ("Server::Listen: disconnected");
		}

		Manager.DebugLog ("Server::Listen: finished");
		return true;
	}
}
