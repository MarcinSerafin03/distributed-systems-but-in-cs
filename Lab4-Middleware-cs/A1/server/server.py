import time
import random
import grpc
import smart_home_pb2
import smart_home_pb2_grpc
from concurrent import futures
import logging
import sys

logging.basicConfig(level=logging.INFO)

DEVICES_SERVER_1 = {
    "cam1": {
        "id": "cam1",
        "name": "Living Room Camera",
        "type": smart_home_pb2.SECURITY_CAMERA,
        "subType": "PTZ",
        "online": True,
        "info": {
            "location": "Living Room",
            "recording": False,
            "position": {"pan": 0.0, "tilt": 0.0, "zoom": 1.0},
            "batteryLevel": 85
        }
    },
    "cam2": {
        "id": "cam2",
        "name": "Front Door Camera",
        "type": smart_home_pb2.SECURITY_CAMERA,
        "subType": "Fixed",
        "online": True,
        "info": {
            "location": "Front Door",
            "recording": True,
            "position": {"pan": 0.0, "tilt": 0.0, "zoom": 1.0},
            "batteryLevel": 92
        }
    },
    "therm1": {
        "id": "therm1",
        "name": "Living Room Thermostat",
        "type": smart_home_pb2.THERMOSTAT,
        "subType": "Smart",
        "online": True,
        "info": {
            "location": "Living Room",
            "temperatureUnit": "Celsius",
            "currentTemperature": 22.5,
            "targetTemperature": 21.0,
            "batteryLevel": 78
        }
    },
    "therm2": {
        "id": "therm2",
        "name": "Bedroom Thermostat",
        "type": smart_home_pb2.THERMOSTAT,
        "subType": "Basic",
        "online": True,
        "info": {
            "location": "Bedroom",
            "temperatureUnit": "Celsius",
            "currentTemperature": 20.0,
            "targetTemperature": 19.0,
            "batteryLevel": 65
        }
    },
    "fridge1": {
        "id": "fridge1",
        "name": "Kitchen Refrigerator",
        "type": smart_home_pb2.REFRIGERATOR,
        "subType": "Smart",
        "online": True,
        "info": {
            "mode": smart_home_pb2.RefrigeratorInfo.NORMAL,
            "currentTemperature": 4.0,
            "doorOpen": False,
            "compartments": [
                {"name": "Main", "currentTemperature": 4.0, "targetTemperature": 4.0},
                {"name": "Freezer", "currentTemperature": -18.0, "targetTemperature": -18.0}
            ]
        }
    }
}

DEVICES_SERVER_2 = {
    "cam3": {
        "id": "cam3",
        "name": "Backyard Camera",
        "type": smart_home_pb2.SECURITY_CAMERA,
        "subType": "Outdoor",
        "online": True,
        "info": {
            "location": "Backyard",
            "recording": True,
            "position": {"pan": 45.0, "tilt": 10.0, "zoom": 2.0},
            "batteryLevel": 70
        }
    },
    "therm3": {
        "id": "therm3",
        "name": "Kitchen Thermostat",
        "type": smart_home_pb2.THERMOSTAT,
        "subType": "Premium",
        "online": True,
        "info": {
            "location": "Kitchen",
            "temperatureUnit": "Celsius",
            "currentTemperature": 23.5,
            "targetTemperature": 22.0,
            "batteryLevel": 90
        }
    },
    "fridge2": {
        "id": "fridge2",
        "name": "Garage Refrigerator",
        "type": smart_home_pb2.REFRIGERATOR,
        "subType": "Basic",
        "online": True,
        "info": {
            "mode": smart_home_pb2.RefrigeratorInfo.ECO,
            "currentTemperature": 5.0,
            "doorOpen": False,
            "compartments": [
                {"name": "Main", "currentTemperature": 5.0, "targetTemperature": 5.0},
                {"name": "Freezer", "currentTemperature": -16.0, "targetTemperature": -16.0}
            ]
        }
    }
}

class SmartHomeServicer(smart_home_pb2_grpc.SmartHomeServiceServicer):
    """Implementation of SmartHome service"""
    
    def __init__(self, devices, server_id):
        self.devices = devices
        self.server_id = server_id

    def ListDevices(self, request, context):
        """Returns a list of all devices for a user"""
        logging.info(f"Server {self.server_id}: ListDevices called for user: {request.userId}")
        
        devices = []
        for device_id, device_data in self.devices.items():
            device = smart_home_pb2.Device(
                id=device_data["id"],
                name=device_data["name"],
                type=device_data["type"],
                subType=device_data["subType"],
                online=device_data["online"]
            )
            devices.append(device)
        
        return smart_home_pb2.ListDevicesResponse(devices=devices)

    def GetDeviceInfo(self, request, context):
        """Returns detailed information about a specific device"""
        print(request)
        print(context)
        device_id = request.deviceId
        logging.info(f"Server {self.server_id}: GetDeviceInfo called for device: {device_id}")
        
        if device_id not in self.devices:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f"Device with ID {device_id} not found")
            return smart_home_pb2.DeviceInfoResponse()
        
        device_data = self.devices[device_id]
        device = smart_home_pb2.Device(
            id=device_data["id"],
            name=device_data["name"],
            type=device_data["type"],
            subType=device_data["subType"],
            online=device_data["online"]
        )
        
        response = smart_home_pb2.DeviceInfoResponse(device=device)
        
        if device_data["type"] == smart_home_pb2.SECURITY_CAMERA:
            info = device_data["info"]
            position = smart_home_pb2.Position(
                pan=info["position"]["pan"],
                tilt=info["position"]["tilt"],
                zoom=info["position"]["zoom"]
            )
            camera_info = smart_home_pb2.SecurityCameraInfo(
                location=info["location"],
                recording=info["recording"],
                position=position,
                batteryLevel=info["batteryLevel"]
            )
            response.securityCameraInfo.CopyFrom(camera_info)
            
        elif device_data["type"] == smart_home_pb2.THERMOSTAT:
            info = device_data["info"]
            thermostat_info = smart_home_pb2.ThermostatInfo(
                location=info["location"],
                temperatureUnit=info["temperatureUnit"],
                currentTemperature=info["currentTemperature"],
                targetTemperature=info["targetTemperature"],
                batteryLevel=info["batteryLevel"]
            )
            response.thermostatInfo.CopyFrom(thermostat_info)
            
        elif device_data["type"] == smart_home_pb2.REFRIGERATOR:
            info = device_data["info"]
            compartments = []
            for comp in info["compartments"]:
                compartment = smart_home_pb2.Compartment(
                    name=comp["name"],
                    currentTemperature=comp["currentTemperature"],
                    targetTemperature=comp["targetTemperature"]
                )
                compartments.append(compartment)
                
            refrigerator_info = smart_home_pb2.RefrigeratorInfo(
                mode=info["mode"],
                currentTemperature=info["currentTemperature"],
                doorOpen=info["doorOpen"],
                compartments=compartments
            )
            response.refrigeratorInfo.CopyFrom(refrigerator_info)
        
        return response

    def ControlDevice(self, request, context):
        """Controls a specific device"""
        device_id = request.deviceId
        logging.info(f"Server {self.server_id}: ControlDevice called for device: {device_id}")
        
        if device_id not in self.devices:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f"Device with ID {device_id} not found")
            return smart_home_pb2.ControlResponse(
                deviceId=device_id,
                success=False,
                message=f"Device with ID {device_id} not found"
            )
        
        device_data = self.devices[device_id]
        success = False
        message = "Unknown command"
        
        if request.HasField("setPosition"):
            if device_data["type"] == smart_home_pb2.SECURITY_CAMERA:
                position = request.setPosition.position
                device_data["info"]["position"]["pan"] = position.pan
                device_data["info"]["position"]["tilt"] = position.tilt
                device_data["info"]["position"]["zoom"] = position.zoom
                success = True
                message = "Position updated successfully"
            else:
                message = "Device does not support position control"
                
        elif request.HasField("setRecording"):
            if device_data["type"] == smart_home_pb2.SECURITY_CAMERA:
                device_data["info"]["recording"] = request.setRecording.recording
                success = True
                message = f"Recording {'started' if request.setRecording.recording else 'stopped'}"
            else:
                message = "Device does not support recording control"
                
        elif request.HasField("setTemperature"):
            if device_data["type"] == smart_home_pb2.THERMOSTAT:
                device_data["info"]["targetTemperature"] = request.setTemperature.temperature
                success = True
                message = f"Temperature set to {request.setTemperature.temperature}"
            else:
                message = "Device does not support temperature control"
                
        elif request.HasField("setMode"):
            if device_data["type"] == smart_home_pb2.REFRIGERATOR:
                device_data["info"]["mode"] = request.setMode.mode
                success = True
                message = f"Mode set to {request.setMode.mode}"
            else:
                message = "Device does not support mode control"
        
        return smart_home_pb2.ControlResponse(
            deviceId=device_id,
            success=success,
            message=message
        )

    def MonitorDevice(self, request, context):
        """Monitors a device and streams status updates"""
        device_id = request.deviceId
        interval = request.interval
        logging.info(f"Server {self.server_id}: MonitorDevice called for device: {device_id} with interval: {interval}s")
        
        if device_id not in self.devices:
            context.set_code(grpc.StatusCode.NOT_FOUND)
            context.set_details(f"Device with ID {device_id} not found")
            return
        
        device_data = self.devices[device_id]
        
        try:
            while True:
                self._update_device_status(device_id)
                
                status = smart_home_pb2.DeviceStatus(
                    deviceId=device_id,
                    deviceType=device_data["type"],
                    isOnline=device_data["online"]
                )
                
                if device_data["type"] == smart_home_pb2.SECURITY_CAMERA:
                    info = device_data["info"]
                    position = smart_home_pb2.Position(
                        pan=info["position"]["pan"],
                        tilt=info["position"]["tilt"],
                        zoom=info["position"]["zoom"]
                    )
                    camera_info = smart_home_pb2.SecurityCameraInfo(
                        location=info["location"],
                        recording=info["recording"],
                        position=position,
                        batteryLevel=info["batteryLevel"]
                    )
                    status.securityCameraInfo.CopyFrom(camera_info)
                    
                elif device_data["type"] == smart_home_pb2.THERMOSTAT:
                    info = device_data["info"]
                    thermostat_info = smart_home_pb2.ThermostatInfo(
                        location=info["location"],
                        temperatureUnit=info["temperatureUnit"],
                        currentTemperature=info["currentTemperature"],
                        targetTemperature=info["targetTemperature"],
                        batteryLevel=info["batteryLevel"]
                    )
                    status.thermostatInfo.CopyFrom(thermostat_info)
                    
                elif device_data["type"] == smart_home_pb2.REFRIGERATOR:
                    info = device_data["info"]
                    compartments = []
                    for comp in info["compartments"]:
                        compartment = smart_home_pb2.Compartment(
                            name=comp["name"],
                            currentTemperature=comp["currentTemperature"],
                            targetTemperature=comp["targetTemperature"]
                        )
                        compartments.append(compartment)
                        
                    refrigerator_info = smart_home_pb2.RefrigeratorInfo(
                        mode=info["mode"],
                        currentTemperature=info["currentTemperature"],
                        doorOpen=info["doorOpen"],
                        compartments=compartments
                    )
                    status.refrigeratorInfo.CopyFrom(refrigerator_info)
                
                yield status
                time.sleep(interval)
                
        except Exception as e:
            logging.error(f"Error in MonitorDevice: {e}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"Internal error: {e}")
    
    def _update_device_status(self, device_id):
        """Simulates random changes to device status for monitoring"""
        device_data = self.devices[device_id]
        
        if device_data["type"] in [smart_home_pb2.SECURITY_CAMERA, smart_home_pb2.THERMOSTAT]:
            device_data["info"]["batteryLevel"] = max(0, device_data["info"]["batteryLevel"] - random.uniform(0, 1))
        
        if device_data["type"] == smart_home_pb2.SECURITY_CAMERA:
            device_data["info"]["position"]["pan"] += random.uniform(-0.5, 0.5)
            device_data["info"]["position"]["tilt"] += random.uniform(-0.5, 0.5)
            
        elif device_data["type"] == smart_home_pb2.THERMOSTAT:
            current_temp = device_data["info"]["currentTemperature"]
            target_temp = device_data["info"]["targetTemperature"]
            if current_temp < target_temp:
                device_data["info"]["currentTemperature"] += random.uniform(0, 0.2)
            elif current_temp > target_temp:
                device_data["info"]["currentTemperature"] -= random.uniform(0, 0.2)
            else:
                device_data["info"]["currentTemperature"] += random.uniform(-0.1, 0.1)
                
        elif device_data["type"] == smart_home_pb2.REFRIGERATOR:
            device_data["info"]["currentTemperature"] += random.uniform(-0.2, 0.2)
            if random.random() < 0.01:
                device_data["info"]["doorOpen"] = not device_data["info"]["doorOpen"]
            
            for comp in device_data["info"]["compartments"]:
                comp["currentTemperature"] += random.uniform(-0.1, 0.1)


def serve(server_id):
    """Starts the gRPC server with the specified ID"""
    if server_id == 1:
        devices = DEVICES_SERVER_1
        port = 50051
    else:
        devices = DEVICES_SERVER_2
        port = 50052
    
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    print(f"Starting server {server_id} on port {port}")
    smart_home_pb2_grpc.add_SmartHomeServiceServicer_to_server(
        SmartHomeServicer(devices, server_id), server)
    server.add_insecure_port(f'[::]:{port}')
    server.start()
    logging.info(f"Server {server_id} started on port {port}")
    try:
        while True:
            time.sleep(86400)
    except KeyboardInterrupt:
        server.stop(0)
        logging.info(f"Server {server_id} stopped")


if __name__ == '__main__':
    if len(sys.argv) != 2 or sys.argv[1] not in ['1', '2']:
        print("Usage: python server.py [1|2]")
        print("  1 - Start server instance 1 on port 50051")
        print("  2 - Start server instance 2 on port 50052")
        sys.exit(1)
    
    server_id = int(sys.argv[1])
    serve(server_id)