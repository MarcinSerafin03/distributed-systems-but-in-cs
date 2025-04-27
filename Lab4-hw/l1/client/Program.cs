using System;
using Ice;

namespace DynamicInvocationClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (Communicator communicator = Util.initialize(ref args))
                {
                    ObjectPrx basePrx = communicator.stringToProxy("MoreTrivialService:default -p 10000");
                    
                    Console.WriteLine("Dynamic Invocation Client");
                    Console.WriteLine("------------------------");
                    Console.WriteLine("Available methods:");
                    Console.WriteLine("1. add(int a, int b)");
                    Console.WriteLine("2. concat(string a, string b)");
                    Console.WriteLine("3. processList(List<Item> items)");
                    Console.WriteLine("\nUsage: <method> <args...>");
                    Console.WriteLine("Example: add 5 7");
                    Console.WriteLine("Example: concat \"Hello \" \"World!\"");
                    Console.WriteLine("Example: processList 3 (followed by id/name pairs)");
                    Console.WriteLine("Type 'exit' to quit\n");

                    while (true)
                    {
                        Console.Write("> ");
                        string input = Console.ReadLine().Trim();
                        
                        if (string.IsNullOrEmpty(input))
                            continue;
                            
                        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                            break;
                            
                        string[] parts = input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        string method = parts[0].ToLower();
                        
                        try
                        {
                            switch (method)
                            {
                                case "add":
                                    if (parts.Length != 3)
                                    {
                                        Console.WriteLine("Usage: add <int> <int>");
                                        continue;
                                    }
                                    int a = int.Parse(parts[1]);
                                    int b = int.Parse(parts[2]);
                                    DynamicAdd(basePrx, a, b);
                                    break;
                                    
                                case "concat":
                                    if (parts.Length != 3)
                                    {
                                        Console.WriteLine("Usage: concat <string> <string>");
                                        continue;
                                    }
                                    DynamicConcat(basePrx, parts[1], parts[2]);
                                    break;
                                    
                                case "processlist":
                                    if (parts.Length < 2 || !int.TryParse(parts[1], out int count))
                                    {
                                        Console.WriteLine("Usage: processList <count> [<id> <name> ...]");
                                        continue;
                                    }
                                    
                                    if (parts.Length != 2 + count * 2)
                                    {
                                        Console.WriteLine($"Expected {count} items, each requiring an id and name");
                                        continue;
                                    }
                                    
                                    DynamicProcessList(basePrx, parts);
                                    break;
                                    
                                case "help":
                                    PrintHelp();
                                    break;
                                    
                                default:
                                    Console.WriteLine($"Unknown method: {method}");
                                    PrintHelp();
                                    break;
                            }
                        }
                        catch (Ice.Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Ice.Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }
        
        static void PrintHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  add <int> <int> - Add two numbers");
            Console.WriteLine("  concat <string> <string> - Concatenate two strings");
            Console.WriteLine("  processList <count> [<id> <name> ...] - Process a list of items");
            Console.WriteLine("  help - Show this help");
            Console.WriteLine("  exit - Exit the program");
        }
        
        static void DynamicAdd(ObjectPrx proxy, int a, int b)
        {
            Console.WriteLine($"\nCalling add({a}, {b}) dynamically:");
            
            OutputStream outStream = new OutputStream(proxy.ice_getCommunicator());
            outStream.startEncapsulation();
            outStream.writeInt(a);
            outStream.writeInt(b);
            outStream.endEncapsulation();
            
            byte[] inParams = outStream.finished();
            
            
            byte[] outParams;
            bool ok = proxy.ice_invoke("add", OperationMode.Normal, inParams, out outParams);
            
            if (ok)
            {
                InputStream inStream = new InputStream(proxy.ice_getCommunicator(), outParams);
                inStream.startEncapsulation();
                int result = inStream.readInt();
                inStream.endEncapsulation();
                
                Console.WriteLine($"Result: {result}");
            }
            else
            {
                Console.WriteLine("Operation failed");
            }
        }
        
        static void DynamicConcat(ObjectPrx proxy, string a, string b)
        {
            Console.WriteLine($"\nCalling concat(\"{a}\", \"{b}\") dynamically:");
            
            OutputStream outStream = new OutputStream(proxy.ice_getCommunicator());
            outStream.startEncapsulation();
            outStream.writeString(a);
            outStream.writeString(b);
            outStream.endEncapsulation();
            
            byte[] inParams = outStream.finished();
            
            byte[] outParams;
            bool ok = proxy.ice_invoke("concat", OperationMode.Normal, inParams, out outParams);
            
            if (ok)
            {
                InputStream inStream = new InputStream(proxy.ice_getCommunicator(), outParams);
                inStream.startEncapsulation();
                string result = inStream.readString();
                inStream.endEncapsulation();
                
                Console.WriteLine($"Result: \"{result}\"");
            }
            else
            {
                Console.WriteLine("Operation failed");
            }
        }
        
        static void DynamicProcessList(ObjectPrx proxy, string[] args)
        {
            int count = int.Parse(args[1]);
            Console.WriteLine($"\nCalling processList() with {count} items dynamically:");
            
            OutputStream outStream = new OutputStream(proxy.ice_getCommunicator());
            outStream.startEncapsulation();
            
            outStream.writeSize(count);
            
            for (int i = 0; i < count; i++)
            {
                int id = int.Parse(args[2 + i * 2]);
                string name = args[3 + i * 2];
                
                outStream.writeInt(id);
                outStream.writeString(name);
                Console.WriteLine($"  Sending: id={id}, name={name}");
            }
            
            outStream.endEncapsulation();
            
            byte[] inParams = outStream.finished();
            
            byte[] outParams;
            bool ok = proxy.ice_invoke("processList", OperationMode.Normal, inParams, out outParams);
            
            if (ok)
            {
                InputStream inStream = new InputStream(proxy.ice_getCommunicator(), outParams);
                inStream.startEncapsulation();
                int result = inStream.readInt();
                inStream.endEncapsulation();
                
                Console.WriteLine($"Result: {result}");
            }
            else
            {
                Console.WriteLine("Operation failed");
            }
        }
    }
}