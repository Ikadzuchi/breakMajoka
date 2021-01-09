using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace decodeWav
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
			FileStream fsout = new FileStream(dir+Path.DirectorySeparatorChar+filename+"_out.bin", FileMode.Create);

			byte[] pcmhead = { 0x00,0x00 }; //TODO

			fs.Seek(0x82,SeekOrigin.Begin);
			//fs.Seek(0x12,SeekOrigin.Begin);
			int b;
			while(true)
			{
				int sample0 = fs.ReadByte()+(fs.ReadByte()<<8);
				int index0 = fs.ReadByte();
				fs.ReadByte();
				ADPCM adpcm = new ADPCM(sample0,index0);
				for(int i=0; i<0x3FC; i++)
				//while(true)
				{
					b = fs.ReadByte();
					if(b==-1)
					{
						goto exit;
					}
					int sample;
					sample = adpcm.next(b&0x0F);
					fsout.WriteByte((byte)(sample&0xFF));
					fsout.WriteByte((byte)(sample>>8));
					sample = adpcm.next(b>>4);
					fsout.WriteByte((byte)(sample&0xFF));
					fsout.WriteByte((byte)(sample>>8));
				}
			}
			exit:
			fs.Close();
			fsout.Close();

			
		}
		public class ADPCM
		{
			int current;
			int index;
			int[] StepTab = {
				 7, 8, 9,10,11,12,13,14,
				16,17,19,21,23,25,28,31,
				34,37,41,45,50,55,60,66,
				 73, 80, 88, 97,107,118,130,143,
				157,173,190,209,230,253,279,307,
				337,371,408,449,494,544,598,658,
				 724, 796, 876, 963,1060,1166,1282,1411,
				1552,1707,1878,2066,2272,2499,2749,3024,
				3327,3660,4026,4428,4871,5358,5894,6484,
				 7132, 7845, 8630, 9493,10442,11487,12635,13899,
				15289,16818,18500,20350,22385,24623,27086,29794,
				32767 };
			int[] IndexTab = { -1,-1,-1,-1,2,4,6,8,-1,-1,-1,-1,2,4,6,8 };

			public ADPCM(int initialSample,int initialIndex)
			{
				current = (int)(Int16)initialSample;
				index = initialIndex;
			}
			public void reset(int initialSample,int initialIndex)
			{
				current = (int)(Int16)initialSample;
				index = initialIndex;
				return;
			}

			public int next(int sample)
			{
				int diff = 0;
				if((sample&0x04)!=0) diff += StepTab[index];
				if((sample&0x02)!=0) diff += StepTab[index]>>1;
				if((sample&0x01)!=0) diff += StepTab[index]>>2;
				diff+=StepTab[index]>>3;
				if((sample&0x08)!=0) diff = -diff;
				current += diff;
				if(current > 0x7FFF) current = 0x7FFF;
				if(current < -0x8000) current = -0x8000; //※intは32bit
				index += IndexTab[sample];
				if(index < 0) index = 0;
				if(index > 88) index = 88;
				return current;
			}

		}
	}
}
