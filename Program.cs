using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace breakMajoka
{
	class Program
	{
		static void Main(string[] args)
		{
			string fullfilename = args[0];
			string dir = Path.GetDirectoryName(fullfilename);
			string filename = Path.GetFileNameWithoutExtension(fullfilename);
			Console.WriteLine($"{fullfilename} = {dir} + {Path.DirectorySeparatorChar} + {filename}");
			
			FileStream fs = new FileStream(fullfilename, FileMode.Open);

			//まずヘッダの固定部の分だけ読む
			byte[] buf = new byte[12*4];
			int[] buf2 = new int[12];
			fs.Read(buf, 0, 4*12);

			for (int i = 0; i < 12; i++)
			{
				Console.WriteLine(read4byte(buf,i*4).ToString("X08"));
				buf2[i] = read4byte(buf, i * 4);
			}

			for (int i = 0; i < 12; i++)
			{
				Console.WriteLine(buf2[i].ToString("X08"));
			}
			int head = buf2[0];
			if (head != 0x49564153)
			{
				Console.WriteLine(string.Format("ヘッダ不正{0:X08}",head));
				return;
			}
			List<int> movieFrames = new List<int>();
			movieFrames.Add(buf2[1]);
			int maybeJPEGPad = buf2[3];
			int movieFrameCount = buf2[4];
			//int[] movieFrames = new int[movieFrameCount+1];
			int wavStart = buf2[5];
			int wavSize = buf2[6];
			int JPEGHead = buf2[10];
			int JPEGHeadSize = buf2[11] - maybeJPEGPad;

			Console.WriteLine(
				$"head{head:X08} movieFrameCount{movieFrameCount:X08} wavStart{wavStart:X08} wavSize{wavSize:X08}");

			//ファイル全部読む
			buf = new byte[wavStart+wavSize];
			fs.Seek(0, SeekOrigin.Begin);
			fs.Read(buf, 0, wavStart + wavSize);
			//ヘッダの残りを解釈
			buf2 = new int[movieFrameCount+12];
			for(int i=0; i < 12+movieFrameCount; i++)
			{
				buf2[i] = read4byte(buf, i*4);
			}
			for(int i = 0; i < movieFrameCount; i++)
			{
				movieFrames.Add(buf2[i+12]);
			}
			//Console.WriteLine($"frame012:{movieFrames[0]:X08} {movieFrames[1]:X08} {movieFrames[2]:X08}");
			Console.WriteLine("movie frame indeces:");
			for(int i = 0; i<movieFrames.Count; i++)
			{
				Console.Write($"{i}: {movieFrames[i]:X08}, ");
			}

			FileStream fsout = new FileStream(dir+Path.DirectorySeparatorChar+filename+"_wav.wav", FileMode.Create);
			fsout.Write(buf, wavStart, wavSize);
			fsout.Close();

			for(int i = 0; i<movieFrameCount+1; i++)
			{
				fsout = new FileStream(dir+Path.DirectorySeparatorChar+filename+"_jpeg_"+i+".jpg",FileMode.Create);
				fsout.Write(buf,JPEGHead,JPEGHeadSize);
				int j = 0;
				while(true)
				{
					//FFD9が来るまで1バイトづつ書き写す
					if(buf[movieFrames[i]+j] != 0xFF ||
						buf[movieFrames[i]+j+1] != 0xD9)
					{
						fsout.Write(buf,movieFrames[i]+j,1);
						j+=1;
					}
					else
					{
						//FFD9ならFFD9を書いて終わり
						fsout.Write(buf,movieFrames[i]+j,2);
						break;
					}
				}

				fsout.Close();
			}

		}

		static int read4byte(byte[] buf, int i)
		{
			return buf[i] + (buf[i + 1] << 8) + (buf[i + 2] << 16) + (buf[i + 3] << 24);
		}
	}
}
