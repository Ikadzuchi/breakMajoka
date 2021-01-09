using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace composeMajoka
{
	class Program
	{
		static void Main(string[] args)
		{
			if(args.Length <1)
			{
				Console.WriteLine("Usage:\r\ncomposeMajoka filelist\r\n1行1ファイル、JPEG複数、WAVが0個または1個");
				return;
			}
			
			StreamReader sr = new StreamReader(args[0],Encoding.UTF8,true,0x100);
			List<string> filenamelist = new List<string>();

			string wavfilename = null;
			while(!sr.EndOfStream)
			{
				filenamelist.Add(sr.ReadLine());
			}
			if(Path.GetExtension(filenamelist[filenamelist.Count-1]).ToUpperInvariant() == ".WAV")
			{
				wavfilename = filenamelist[filenamelist.Count-1];
				filenamelist.RemoveAt(filenamelist.Count-1);
			}
			foreach(string filename in filenamelist)
			{
				Console.WriteLine(filename);
			}
			Console.WriteLine("------");
			Console.WriteLine(wavfilename??"(no wav)");

			//ヘッダの用意できる所は用意(まだ書き込まない)
			int headerlen_by_4 = 12 + filenamelist.Count;
			headerlen_by_4 = ((headerlen_by_4+3)&0x7FFFFFFC);
			int headerlen = headerlen_by_4*4;
			int[] head = new int[headerlen_by_4]; //byte列は扱いにくいのでまずintで作る
			head[0] = 0x49564153; //"SAVI"
			int img0addr ;
			int size_jpegregion;
			int jpegpad; //
			head[4] = filenamelist.Count;
			int wavaddr;
			int wavsize;
			head[7] = 0;
			head[8] = 0x30;
			head[9] = ((filenamelist.Count+3)&0x7FFFFFFC)*4;
			head[10]= headerlen;
			int jpegheadsize;

			int currentAddr = 0;

			FileStream fsout = new FileStream("out.savi", FileMode.Create);
			fsout.Seek(headerlen,SeekOrigin.Begin);
			currentAddr = headerlen;

			//最初のファイルからJPEGヘッダを取る。
			FileStream fsin = new FileStream(filenamelist[0],FileMode.Open);
			List<byte> jpeghead = new List<byte>();
			while(true)
			{
				int b = fsin.ReadByte();
				if(b!=0xFF)
				{
					jpeghead.Add((byte)b);
				}
				else
				{
					int b2 = fsin.ReadByte();
					if(b2!=0xDA)
					{
						jpeghead.Add((byte)b);
						jpeghead.Add((byte)b2);
					}
					else
					{
						break;
					}
				}
			}
			jpegpad = 0x10-(jpeghead.Count&0x0F);
			fsout.Write(jpeghead.ToArray(),0,jpeghead.Count);
			fsout.Seek(jpegpad,SeekOrigin.Current);
			jpegheadsize = jpeghead.Count+jpegpad;

			currentAddr += jpegheadsize;
			img0addr = currentAddr;
			head[0+12] = currentAddr;

			byte[] buf = new byte[0x100];
			//読み飛ばしたFFDAのぶんseek
			fsin.Seek(-2,SeekOrigin.Current);
			//ファイルのヘッダ以外の部分を読んで書き写す
			int readcount;
			while((readcount = fsin.Read(buf,0,0x100))==0x100)
			{
				fsout.Write(buf,0,0x100);
				currentAddr+=readcount;
			}
			fsout.Write(buf,0,readcount);
			fsout.Seek(0x10-readcount&0x0F,SeekOrigin.Current);
			currentAddr+=readcount+(0x10-readcount&0x0F);

			fsin.Close();

			for(int i=1; i<filenamelist.Count; i++)
			{
				fsin = new FileStream(filenamelist[i],FileMode.Open);

				head[i+12] = currentAddr;
				//ヘッダを飛ばす
				while(true)
				{
					int b = fsin.ReadByte();
					if(b==0xFF)
					{
						int b2 = fsin.ReadByte();
						if(b2==0xDA)
						{
							break;
						}
					}
				}
				//ファイルのヘッダ以外を書く
				fsin.Seek(-2,SeekOrigin.Current);
				while((readcount = fsin.Read(buf,0,0x100))==0x100)
				{
					fsout.Write(buf,0,0x100);
					currentAddr+=readcount;
				}
				fsout.Write(buf,0,readcount);
				fsout.Seek(0x10-readcount&0x0F,SeekOrigin.Current);
				currentAddr+=readcount+(0x10-readcount&0x0F);

				fsin.Close();
			}
			wavaddr = currentAddr;
			size_jpegregion = wavaddr-img0addr;

			if(wavfilename == null)
			{
				wavsize = 0;
			}
			else
			{
				//TODO WAVも入れる
				wavsize = 0;
			}

			//ヘッダの残りを埋め、書く
			head[1] = img0addr;
			head[2] = size_jpegregion;
			head[3] = jpegpad;
			head[5] = wavaddr;
			head[6] = wavsize;
			head[11]= jpegheadsize;

			fsout.Seek(0,SeekOrigin.Begin);
			for(int i = 0; i<headerlen_by_4; i++)
			{
				fsout.WriteByte((byte)(head[i]    &0xFF));
				fsout.WriteByte((byte)(head[i]>>8 &0xFF));
				fsout.WriteByte((byte)(head[i]>>16&0xFF));
				fsout.WriteByte((byte)(head[i]>>24&0xFF));
			}


			fsout.Close();


		}
	}
}
