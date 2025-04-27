import sys
import Ice
import DynamicInvocation_ice

class MoreTrivialServiceI(DynamicInvocation_ice._M_DynamicInvocation.MoreTrivialService):
    def add(self, a, b, current=None):
        print(f"Adding {a} + {b}")
        return a + b
    
    def concat(self, a, b, current=None):
        print(f"Concatenating '{a}' and '{b}'")
        return a + b
    
    def processList(self, list, current=None):
        print(f"Processing list with {len(list)} items")
        for item in list:
            print(f"  Item: id={item.id}, name={item.name}")
        return len(list)
def run_server():
    with Ice.initialize(sys.argv) as communicator:
        adapter = communicator.createObjectAdapterWithEndpoints(
            "MoreTrivialServiceAdapter", "default -p 10000")
        
        servant = MoreTrivialServiceI()
        proxy = adapter.add(servant, communicator.stringToIdentity("MoreTrivialService"))
        
        print(f"Server running at: {proxy}")
        adapter.activate()
        communicator.waitForShutdown()

if __name__ == "__main__":
    run_server()