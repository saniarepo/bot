
using System;
using System.Collections.Generic;
namespace bot
{
	public static class Command
	{
		public static void execute(String command)
		{
			String[] items = command.Split (new char[]{' '});
			List<String> list = new List<String> (); 
			for (int i = 0; i < items.Length; i++) 
			{
				if (( items[i].Trim()).Length != 0 )
				{
					list.Add(items[i]);
				}	
			}

			switch (list [0]) 
			{
				case "move": move(list); break;
				case "quit": nop(); break;
				default: nodefine(); break;
			}
		}

		private static void move(List<String> list)
		{
			System.Console.WriteLine ("move");
			if (list.Count >= 4) {
				float x = (float)Convert.ToDouble (list[1].Trim());
				float y = (float)Convert.ToDouble  (list [2].Trim());
				float z = (float)Convert.ToDouble  (list [3].Trim ());
				//System.Console.WriteLine(x.ToString()+"  " +y.ToString()+"  "+ z.ToString());
				WorldClick.ClickTo (x, y, z, 0, ClickType.Move, 0.5f);
			} else {
				badFormat("move x y z");
			}

		}

		private static void nodefine()
		{
			System.Console.WriteLine ("Command not recognised!");
		}

		private static void badFormat(String text)
		{
			System.Console.WriteLine ("This command format: ", text);
		}

		private static void nop(){}
	}
}

